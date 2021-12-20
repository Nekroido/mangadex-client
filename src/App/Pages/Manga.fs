module Pages.Manga

open System

open Console
open Data
open FSharpx.Control
open Utils

let formatVolume volume =
    let volume = volume |> Option.defaultValue "-"

    $"Volume {volume}"

let formatChapter chapter =
    [ chapter |> Chapter.getFormattedChapter
      chapter |> Chapter.getFormattedTranslatorGroup ]
    |> String.join " - "

[<Literal>]
let ListChaptersLabel = "Select chapters to download"

[<Literal>]
let DownloadAllChaptersLabel = "Download all chapters"

[<Literal>]
let ReturnLabel = "← Return"

[<RequireQualifiedAccess>]
type Action =
    | ListChapters
    | DownloadAllChapters
    | Return

    static member toString x =
        match x with
        | Action.ListChapters -> ListChaptersLabel
        | Action.DownloadAllChapters -> DownloadAllChaptersLabel
        | Action.Return -> ReturnLabel

    static member fromString x =
        match x with
        | ListChaptersLabel -> Action.ListChapters
        | DownloadAllChaptersLabel -> Action.DownloadAllChapters
        | ReturnLabel -> Action.Return
        | _ -> failwith $"Unknown action {x}"

let renderMangaDetails (manga: Manga) =
    Table.create [ "Title"
                   "Authors"
                   "Last chapter"
                   "Status" ]
    |> Table.addRow [ manga |> Manga.getTitle
                      manga |> Manga.getFormattedCredits
                      manga
                      |> Manga.getLastChapterNumber
                      |> Option.bind (sprintf "%i" >> Some)
                      |> Option.defaultValue "-"
                      manga |> Manga.getStatus ]

let showActions (manga: Manga) =
    Console.clear ()

    manga |> renderMangaDetails |> Console.render

    MenuPrompt.create<Action>
        "Select action:"
        (DiscriminatedUnion.listCases<Action> ())
        Action.toString
    |> Console.prompt

let fetchChapters (manga: Manga) =
    let preferredLanguage =
        Preferences.getLanguage
        <| Preferences.loadPreferences ()

    let batchChapters offset =
        async {
            let! chapterListResult =
                manga
                |> Chapter.listChapters 100 offset preferredLanguage

            if offset < chapterListResult.Total then
                return
                    Some
                    <| (chapterListResult.Data |> List.ofSeq,
                        chapterListResult.Data |> Seq.length)
            else
                return None
        }

    AsyncSeq.unfoldAsync batchChapters 0
    |> AsyncSeq.concatSeq
    |> AsyncSeq.toArray
    |> Console.status $"Fetching chapters for %s{manga |> Manga.getTitle}"

let filterDuplicatedChapters (chapters: Chapter seq) =
    chapters
    // prioritizing the latest chapters
    |> Seq.sortByDescending Chapter.getPublishDate
    |> Seq.distinctBy Chapter.getFormattedChapterNumber
    |> Seq.sortBy Chapter.getFormattedChapterNumber

let pickChaptersByName (chapters: Chapter seq) (selectedChapters: string seq) =
    chapters
    |> Seq.filter
        (fun chapter ->
            selectedChapters
            |> Seq.contains (chapter |> formatChapter))

let selectChapters (chapters: Chapter seq) =
    let prompt =
        MultiSelectionPrompt.create<string> "Select chapters:"

    chapters
    |> Seq.sortBy Chapter.getFormattedChapterNumber
    |> Seq.groupBy Chapter.getFormattedVolumeNumber
    |> Seq.sortBy fst
    |> Seq.iter
        (fun (volume, volumeChapters) ->
            prompt
            |> MultiSelectionPrompt.addChoiceGroup
                (volume |> formatVolume)
                (volumeChapters |> Seq.map formatChapter)
            |> ignore)

    prompt
    |> Console.prompt
    |> pickChaptersByName chapters

let downloadChapters (manga: Manga) (chapters: Chapter seq) =
    let preferences = Preferences.loadPreferences ()
    let preferredQuality = preferences |> Preferences.getQuality
    let savePath = preferences |> Preferences.getSavePath

    let downloadPage (downloadServerUrl: string) chapter page =
        let downloadUrl =
            Chapter.getChapterPageDownloadUrl
                downloadServerUrl
                preferredQuality
                chapter
                page

        let stream = File.createStream ()

        stream |> Http.fetchFile downloadUrl

    let downloadPages downloadServerUrl chapter pages =
        pages
        |> Seq.map (downloadPage downloadServerUrl chapter)

    let createCbz pages chapter =
        let filename =
            [| savePath
               manga |> Manga.getTitle |> Path.toSafePath
               $"{chapter |> Chapter.toString |> Path.toSafePath}.cbz" |]
            |> Path.combine

        let file =
            File.cbzBuilder {
                with_manga manga
                with_chapter chapter
                with_pages pages
                save_path filename
            }

        file

    chapters
    |> Seq.map
        (fun chapter ->
            async {
                let pages =
                    chapter |> Chapter.getPages preferredQuality

                let! downloadServerUrl =
                    async {
                        "Obtaining download server" |> Console.echo
                        return! chapter |> Chapter.getChapterBaseUrl
                    }

                let! downloadedPages =
                    pages
                    |> downloadPages downloadServerUrl chapter
                    |> Seq.mapi
                        (fun index downloadExpr ->
                            async {
                                $"Downloading page {index + 1} of {pages |> Seq.length}"
                                |> Console.echo

                                return! downloadExpr
                            })
                    |> Async.Sequential

                let isOk =
                    downloadedPages |> Seq.forall Result.isOk

                match isOk with
                | true ->
                    "Creating CBZ" |> Console.echo

                    createCbz
                        (downloadedPages
                         |> Seq.map Result.proceedIfOk
                         |> Seq.cast<System.IO.Stream>
                         |> Seq.zip pages)
                        chapter

                    "Done!" |> Console.echo
                | false ->
                    "Not all pages were downloaded successfully! Skipping..."
                    |> Console.echo
            })
    |> Seq.zip chapters
    |> Seq.map
        (fun (chapter, expr) ->
            async {
                do Console.clear ()

                do!
                    Console.live
                        $"Obtaining chapter {chapter |> Chapter.getFormattedTitle}"
                        expr
            })
    |> Async.Sequential
    |> Async.Ignore
    |> Async.RunSynchronously

let rec handleAction returnAction manga action =
    let refresh () = manga |> initialize returnAction

    action
    |> function
        | Action.ListChapters ->
            manga
            |> fetchChapters
            |> Result.proceedIfOk
            |> selectChapters
            |> downloadChapters manga
            |> refresh
        | Action.DownloadAllChapters ->
            manga
            |> fetchChapters
            |> Result.proceedIfOk
            |> filterDuplicatedChapters
            |> downloadChapters manga
            |> refresh
        | Action.Return -> returnAction ()

and initialize returnAction (manga: Manga) =
    manga
    |> (showActions >> handleAction returnAction manga)
