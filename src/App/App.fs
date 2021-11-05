module App

open System
open Spectre.Console
open Console
open Data

type App() =
    member x.Run() =
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

                sprintf "Searching for %s" query |> Console.echo

                let mangaListResult =
                    [ ("title", query) ]
                    |> listManga
                    |> Async.RunSynchronously

                let items =
                    SelectionPrompt<Manga>()
                    |> SelectionPrompt.setTitle "Found works:"
                    |> SelectionPrompt.addChoices mangaListResult.Data
                    |> SelectionPrompt.withConverter (Func<Manga, string>(Manga.title))

                let selectedManga = items |> Console.prompt

                sprintf "Listing chapters for %s" selectedManga.Attributes.Title.En.Value
                |> Console.echo

                let chapterListResult =
                    selectedManga
                    |> listChapters []
                    |> Async.RunSynchronously

                let chapters =
                    SelectionPrompt<Chapter>()
                    |> SelectionPrompt.setTitle "Select chapter:"
                    |> SelectionPrompt.addChoices chapterListResult.Data
                    |> SelectionPrompt.withConverter (
                        Func<Chapter, string>(Chapter.toString)
                    )

                let selectedChapter = chapters |> Console.prompt

                sprintf "Selected chapter %s" (selectedChapter |> Chapter.title)
                |> Console.echo

                ()
            | _ -> failwith "todo"

        0

let run (app: App) = app.Run()
