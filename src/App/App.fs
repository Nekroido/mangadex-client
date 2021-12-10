module App

open System
open Spectre.Console
open Console
open Data

type App() =
    member _.Run() =
        let actions =
            SelectionPrompt<string>()
            |> SelectionPrompt.setTitle "Select action"
            |> SelectionPrompt.addChoices [| "search"
                                             "exit" |]

        actions
        |> Console.prompt
        |> function
            | "exit" -> Console.echo "Exiting..."
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
                    |> SelectionPrompt.withConverter (Func<Manga, string>(Manga.title))

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
                            (index + 1))
                |> Async.Sequential
                |> Async.Ignore
                |> Async.RunSynchronously

                "Creating manga file..." |> Console.echo

                File.createCBZ selectedManga selectedChapter

                "Done!" |> Console.echo

                ()
            | _ -> failwith "todo"

        0

let run (app: App) = app.Run()
