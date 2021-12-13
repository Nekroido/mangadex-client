module Pages.Search

open System
open Console
open Data
open Utils

let askForMangaTitle () =
    Console.clear ()
    "Manga title:" |> Console.ask

let searchMangaByTitle title =
    title
    |> Manga.searchManga 30 0
    |> Console.status $"Searching for %s{title}"

let selectManga (listResult: MangaList.Root) =
    SelectionPrompt.create<Manga> "Found works:"
    |> SelectionPrompt.addChoices listResult.Data
    |> SelectionPrompt.withConverter (Func<Manga, string>(Manga.getTitle))
    |> Console.prompt

let initialize returnAction =
    askForMangaTitle ()
    |> searchMangaByTitle
    |> Result.proceedIfOk
    |> selectManga
    |> Manga.initialize returnAction

(*
let formatVolume volume = $"Volume {volume}"

let formatChapter chapter =
    $"Chapter {chapter |> Chapter.getChapterNumber}"

let askForMangaTitle () = "Manga title:" |> Console.ask

let searchMangaByTitle title =
    title
    |> Manga.searchManga 30 0
    |> Console.status $"Searching for %s{title}"

let listMangaOptions (listResult: MangaList.Root) =
    SelectionPrompt.create<Manga> "Found works:"
    |> SelectionPrompt.addChoices listResult.Data
    |> SelectionPrompt.withConverter (Func<Manga, string>(Manga.getTitle))

let getMangaChapters (manga: Manga) =
    manga
    |> Chapter.listChapters 100 0
    |> Console.status $"Listing chapters for %s{manga |> Manga.getTitle}"

let listChapterOptions (chapters: Chapter seq) =
    let prompt =
        MultiSelectionPrompt.create<string> "Select chapters:"

    chapters
    |> Seq.groupBy (fun chapter -> chapter |> Chapter.getVolume)
    |> Seq.sortBy fst
    |> Seq.iter
        (fun (volume, volumeChapters) ->
            prompt
            |> MultiSelectionPrompt.addChoiceGroup
                (volume |> formatVolume)
                (volumeChapters |> Seq.map formatChapter)
            |> ignore)

    prompt

let handleChapterSelections (selectedChapters: string seq) (chapters: Chapter seq) =
    chapters
    |> Seq.filter
        (fun chapter ->
            // filter chapters by chapter number
            selectedChapters
            |> Seq.contains (chapter |> formatChapter)
            |> not)

let res =
    let selectedManga =
        askForMangaTitle ()
        |> searchMangaByTitle
        |> Result.proceedIfOk
        |> listMangaOptions
        |> Console.prompt

    let mangaChapters =
        selectedManga |> getMangaChapters |> Result.proceedIfOk

    let selectedChapters =
        mangaChapters.Data
        |> listChapterOptions
        |> Console.prompt

    handleChapterSelections selectedChapters mangaChapters.Data
*)
