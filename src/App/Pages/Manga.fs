module Pages.Manga

open System

open Console
open Data
open Utils

let formatVolume volume =
    let volume = volume |> Option.defaultValue "-"

    $"Volume {volume}"

let formatChapter chapter =
    $"Chapter {chapter |> Chapter.getChapterNumber}"

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

let showActions (manga: Manga) =
    Console.clear ()

    MenuPrompt.create<Action>
        $"Manga {manga |> Manga.getTitle}"
        (DiscriminatedUnion.listCases<Action> ())
        Action.toString
    |> Console.prompt

let fetchChapters (manga: Manga) =
    manga
    |> Chapter.listChapters 100 0
    |> Console.status $"Fetching chapters for %s{manga |> Manga.getTitle}"

let selectChapters (chapters: ChapterList.Root) =
    let prompt =
        MultiSelectionPrompt.create<string> "Select chapters:"

    chapters.Data
    |> Seq.groupBy (fun chapter -> chapter |> Chapter.getVolume)
    |> Seq.sortBy fst
    |> Seq.iter
        (fun (volume, volumeChapters) ->
            prompt
            |> MultiSelectionPrompt.addChoiceGroup
                (volume |> formatVolume)
                (volumeChapters |> Seq.map formatChapter)
            |> ignore)

    prompt |> Console.prompt

let filterChaptersByName (chapters: Chapter seq) (selectedChapters: string seq) =
    chapters
    |> Seq.filter
        (fun chapter ->
            selectedChapters
            |> Seq.contains (chapter |> formatChapter))

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

    let downloadChapter chapter =
        let downloadServerUrl =
            chapter
            |> Chapter.getChapterBaseUrl
            |> Async.RunSynchronously

        let pages =
            chapter |> Chapter.getPages preferredQuality

        let pages =
            pages
            |> Seq.map (downloadPage downloadServerUrl chapter)
            |> Async.Sequential
            |> Async.RunSynchronously
            |> Seq.map
                (fun result ->
                    result
                    |> function
                        | Ok page -> downcast page :> System.IO.Stream
                        | Error ex -> failwith ex)
            |> Seq.zip pages

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

    chapters |> Seq.iter downloadChapter

    (*chapters
    |> Seq.map
        (fun chapter ->
            $"Downloading {chapter |> Chapter.getFormattedTitle}",
            chapter |> downloadChapter)
    |> Map.ofSeq
    |> Console.progress
    |> Async.RunSynchronously*)

    ()

let handleAction returnAction manga action =
    action
    |> function
        | Action.ListChapters ->
            let chapters =
                manga |> fetchChapters |> Result.proceedIfOk

            chapters
            |> selectChapters
            |> filterChaptersByName chapters.Data
            |> downloadChapters manga
        | Action.DownloadAllChapters -> returnAction ()
        | Action.Return -> returnAction ()

let initialize returnAction (manga: Manga) =
    manga
    |> (showActions >> handleAction returnAction manga)
