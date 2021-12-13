module App

open Console
open Pages

[<RequireQualifiedAccess>]
type Quality =
    | High
    | Low

[<RequireQualifiedAccess>]
type Language =
    | English
    | Japanese

type AppSettings =
    { Quality: Quality
      Language: Language }

type App(settings: AppSettings) =
    member _.Exit() = "Exiting..." |> Console.echo

    member _.Search() = ()

    member _.Run() =
        Root.initialize ()

        (*let actions =
            SelectionPrompt<string>()
            |> SelectionPrompt.setTitle "Select action"
            |> SelectionPrompt.addChoices [| "test"
                                             "search"
                                             "exit" |]

        actions
        |> Console.prompt
        |> function
            | "exit" -> Console.echo "Exiting..."
            | "test" ->
                let selectedManga =
                    askForMangaTitle ()
                    |> searchMangaByTitle
                    |> Result.proceedIfOk
                    |> listMangaOptions
                    |> Console.prompt

                let mangaChapters =
                    selectedManga
                    |> getMangaChapters
                    |> Result.proceedIfOk

                let selectedChapterNames =
                    mangaChapters.Data
                    |> listChapterOptions
                    |> Console.prompt

                handleChapterSelections selectedChapterNames mangaChapters.Data
                |> Seq.map (fun chapter -> chapter |> Chapter.getFormattedTitle)
                |> Seq.iter Console.echo

                "Done!" |> Console.echo
            | "search" ->
                let query = "Manga title:" |> Console.ask

                $"Searching for %s{query}" |> Console.echo

                let mangaListResult =
                    query
                    |> Manga.searchManga 30 0
                    |> Async.RunSynchronously

                let items =
                    SelectionPrompt<Manga>()
                    |> SelectionPrompt.setTitle "Found works:"
                    |> SelectionPrompt.addChoices mangaListResult.Data
                    |> SelectionPrompt.withConverter (Func<Manga, string>(Manga.getTitle))

                let selectedManga = items |> Console.prompt

                $"Listing chapters for %s{selectedManga.Attributes.Title.En.Value}"
                |> Console.echo

                let chapterListResult =
                    selectedManga
                    |> Chapter.listChapters 100 0
                    |> Async.RunSynchronously

                let chapters =
                    SelectionPrompt<Chapter>()
                    |> SelectionPrompt.setTitle "Select chapter:"
                    |> SelectionPrompt.addChoices chapterListResult.Data
                    |> SelectionPrompt.withConverter (
                        Func<Chapter, string>(Chapter.toString)
                    )

                let selectedChapter = chapters |> Console.prompt

                $"Downloading chapter %s{selectedChapter |> Chapter.toString}..."
                |> Console.echo

                let baseUrl =
                    selectedChapter
                    |> Chapter.getChapterBaseUrl
                    |> Async.RunSynchronously

                selectedChapter
                |> Chapter.getPages
                |> Seq.mapi
                    (fun index pageFile ->
                        Download.downloadPage
                            baseUrl
                            selectedManga
                            selectedChapter
                            pageFile
                            index)
                |> Async.Sequential
                |> Async.Ignore
                |> Async.RunSynchronously

                "Creating manga file..." |> Console.echo

                File.createCBZ selectedManga selectedChapter

                "Done!" |> Console.echo

                ()
            | _ -> failwith "todo"*)

        0

type AppBuilder() =
    member _.Yield _ =
        { Quality = Quality.High
          Language = Language.English }

    [<CustomOperation("quality")>]
    member _.SetQuality(settings, quality) = { settings with Quality = quality }

    [<CustomOperation("language")>]
    member _.SetLanguage(settings, language) = { settings with Language = language }

    member _.Run(settings) = App(settings)

let application = AppBuilder()

let run (app: App) = app.Run()
