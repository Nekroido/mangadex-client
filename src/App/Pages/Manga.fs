module Pages.Manga

open System

open Console
open Data
open Utils

let formatVolume volume = $"Volume {volume}"

let formatChapter chapter =
    $"Chapter {chapter |> Chapter.getChapterNumber}"

[<Literal>]
let ListChaptersLabel = "Select chapters to download"

[<Literal>]
let DownloadAllChaptersLabel = "Download all chapters"

[<Literal>]
let ReturnLabel = "Return"

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
    let downloadPage (downloadServerUrl: string) chapter page =
        let downloadUrl =
            Chapter.getChapterPageDownloadUrl downloadServerUrl "data" chapter page

        Async.Sleep 150

    let downloadChapter chapter =
        let downloadServerUrl =
            chapter
            |> Chapter.getChapterBaseUrl
            |> Async.RunSynchronously

        chapter
        |> Chapter.getPages
        |> Seq.map (downloadPage downloadServerUrl chapter)

    chapters
    |> Seq.map
        (fun chapter ->
            $"Downloading {chapter |> Chapter.getFormattedTitle}",
            chapter |> downloadChapter)
    |> Map.ofSeq
    |> Console.progress
    |> Async.RunSynchronously

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
