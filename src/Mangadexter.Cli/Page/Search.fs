namespace Mangadexter.Cli.Page

open System
open Elmish

open Mangadexter.Core
open Mangadexter.Core.Request.MangaRequest
open Mangadexter.Application
open Mangadexter.Cli

[<RequireQualifiedAccess>]
module Search =
    type ExternalMsg = ShowManga of Manga * preferredLanguage: Language

    type Msg =
        | SearchByTitle of string
        | ViewManga of Manga
        | ListManga of AsyncOperationStatus<Result<MangaList, exn>> // <back| .. |forward>

    type State =
        { title: string
          preferredLanguage: Language
          isLoading: bool
          selectedManga: Manga option
          items: Manga list }

    let init =
        { title = ""
          preferredLanguage = Language.English
          selectedManga = None
          isLoading = false
          items = [] }

    let fetchManga title preferredLanguage =
        async {
            let! result =
                { Title = title |> Title.make |> Some
                  PreferredLanguage = preferredLanguage |> Some
                  Take = 50u
                  Skip = 0u }
                |> Mangadex.searchManga

            return
                match result with
                | Ok data -> data |> Ok |> Finished |> ListManga
                | Error ex -> ex |> Error |> Finished |> ListManga
        }

    let update (msg: Msg) (state: State) =
        match msg with
        | SearchByTitle title ->
            if (title.Trim() |> String.length) = 0 then
                state, Cmd.none, None
            else
                { state with title = title },
                Cmd.fromAsync (fetchManga title state.preferredLanguage),
                None
        | ListManga Started -> { state with isLoading = true }, Cmd.none, None
        | ListManga (Finished (Ok result)) ->
            { state with
                isLoading = false
                items = result.Items },
            Cmd.none,
            None
        | ListManga (Finished (Error ex)) ->
            { state with
                isLoading = false
                items = [] },
            Cmd.none,
            None
        | ViewManga manga ->
            { state with isLoading = true },
            Cmd.none,
            ((manga, state.preferredLanguage)
             |> ExternalMsg.ShowManga
             |> Some)

    let listManga (state: State) dispatch =
        let mapToPicker (item: Manga) = (item.Title |> Title.getValue), item

        Console.singleChoice
            "Found works:"
            (state.items |> List.map mapToPicker)
            (ViewManga >> dispatch)

    let searchInput (state: State) dispatch =
        Console.ask "Search manga by title"
        |> (SearchByTitle >> dispatch)

    let view (state: State) dispatch =
        match state with
        | x when x.isLoading ->
            Console.clear ()
            Console.echo "Loading..."
        | x when state.items |> Seq.isEmpty |> not -> listManga state dispatch
        | x when
            not x.isLoading
            && state.title |> String.length = 0
            ->
            searchInput state dispatch
        | _ -> Console.clear ()
