module Pages.Manga

open System

open Console
open Data
open FSharp.Control
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
                      |> Option.bind (sprintf "%O" >> Some)
                      |> Option.defaultValue "-"
                      manga |> Manga.getStatus ]

let showActions (manga: Manga) =
    Console.clear ()

    manga |> renderMangaDetails |> Console.render

    MenuPrompt.create<Action> "Select action:" (DiscriminatedUnion.listCases<Action> ()) Action.toString
    |> Console.prompt

let fetchChapters (manga: Manga) =
    let preferredLanguage =
        Preferences.getLanguage
        <| Preferences.loadPreferences ()

    let fetchLimit = 100

    let batchChapters offset =
        async {
            let! chapterListResult =
                manga
                |> Chapter.listChapters fetchLimit offset preferredLanguage

            if offset < chapterListResult.Total then
                return
                    Some
                    <| (chapterListResult.Data |> List.ofSeq, (chapterListResult.Offset + fetchLimit))
            else
                return None
        }

    AsyncSeq.unfoldAsync batchChapters 0
    |> AsyncSeq.concatSeq
    |> AsyncSeq.toArrayAsync
    |> Console.status $"Fetching chapters for %s{manga |> Manga.getTitle}"

let filterDuplicatedChapters (chapters: Chapter seq) =
    chapters
    // prioritizing the latest chapters
    |> Seq.sortByDescending Chapter.getPublishDate
    |> Seq.distinctBy Chapter.getFormattedChapterNumber
    |> Seq.sortBy Chapter.getFormattedChapterNumber

let pickChaptersByName (chapters: Chapter seq) (selectedChapters: string seq) =
    chapters
    |> Seq.filter (fun chapter ->
        selectedChapters
        |> Seq.contains (chapter |> formatChapter))

let selectChapters (chapters: Chapter seq) =
    let prompt = MultiSelectionPrompt.create<string> "Select chapters:"

    chapters
    |> Seq.sortBy Chapter.getFormattedChapterNumber
    |> Seq.groupBy Chapter.getFormattedVolumeNumber
    |> Seq.sortBy fst
    |> Seq.iter (fun (volume, volumeChapters) ->
        prompt
        |> MultiSelectionPrompt.addChoiceGroup (volume |> formatVolume) (volumeChapters |> Seq.map formatChapter)
        |> ignore)

    prompt
    |> Console.prompt
    |> pickChaptersByName chapters

let downloadChapters (manga: Manga) (chapters: Chapter seq) =
    let preferences = Preferences.loadPreferences ()
    let preferredQuality = preferences |> Preferences.getQuality
    let savePath = preferences |> Preferences.getSavePath

    let downloadPage totalPages pageNum downloadUrl =
        async {
            $"Downloading page {pageNum} of {totalPages}"
            |> Console.echo

            let stream = File.createStream ()

            return! stream |> Http.fetchFile downloadUrl
        }

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
    |> Seq.map (fun chapter ->
        async {
            "Obtaining download information" |> Console.echo

            let! downloadInfo =
                chapter
                |> ChapterDownload.getChapterDownloadInformation

            let pages =
                downloadInfo
                |> ChapterDownloadInfo.getPages preferredQuality

            let download = downloadPage (pages |> Seq.length)

            let! downloadedPages =
                pages
                |> Seq.map (ChapterDownload.getPageDownloadUrl downloadInfo preferredQuality)
                |> Seq.mapi (fun index downloadUrl -> download (index + 1) downloadUrl)
                |> Async.Sequential

            let isOk = downloadedPages |> Seq.forall Result.isOk

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
    |> Seq.map (fun (chapter, expr) ->
        async {
            do Console.clear ()

            do! Console.live $"Obtaining chapter {chapter |> Chapter.getFormattedTitle}" expr
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
