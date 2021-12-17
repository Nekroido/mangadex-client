module Pages.Search

open System
open Console
open Data
open Utils

let askForMangaTitle () =
    Console.clear ()
    "Manga title:" |> Console.ask

let searchMangaByTitle title =
    let preferredLanguage =
        Preferences.getLanguage
        <| Preferences.loadPreferences ()

    title
    |> Manga.searchManga 30 0 preferredLanguage
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
