namespace Mangadexter.Cli.Page

open Elmish
open Mangadexter.Core
open Mangadexter.Cli

[<RequireQualifiedAccess>]
module Root =
    type CurrentPage =
        | Loading of message: string
        | MainMenu
        | Search
        | Manga
        | Downloads
        | Preferences

    type Msg =
        | NavigateTo of CurrentPage
        | SearchMsg of Search.Msg
        | MangaMsg of Manga.Msg
        | DownloadsMsg of Downloads.Msg
        | PreferencesMsg of Preferences.Msg

    type State =
        { currentPage: CurrentPage
          searchState: Search.State
          mangaState: Manga.State
          downloadsState: Downloads.State
          preferencesState: Preferences.State }

    let handleSearchExtMsg (msg: Search.ExternalMsg option) =
        let handle =
            function
            | Search.ExternalMsg.ShowManga (manga, language) ->
                Cmd.batch [ Loading "Opening manga details"
                            |> NavigateTo
                            |> Cmd.ofMsg
                            ((manga, language)
                             |> Manga.Msg.SetManga
                             |> MangaMsg
                             |> Cmd.ofMsg)
                            Manga |> NavigateTo |> Cmd.ofMsg ]

        msg
        |> Option.map handle
        |> Option.defaultValue Cmd.none

    let handleMangaExtMsg (msg: Manga.ExternalMsg option) =
        let handle =
            function
            | Manga.ExternalMsg.DownloadChapters chapters ->
                ([ chapters
                   |> Downloads.Msg.Download
                   |> DownloadsMsg
                   |> Cmd.ofMsg

                   Loading "Adding chapters to download"
                   |> NavigateTo
                   |> Cmd.ofMsg
                   Downloads |> NavigateTo |> Cmd.ofMsg ])
                |> Cmd.batch
            | Manga.ExternalMsg.Return -> MainMenu |> NavigateTo |> Cmd.ofMsg

        msg
        |> Option.map handle
        |> Option.defaultValue Cmd.none

    let handleDownloadExtMsg (msg: Downloads.ExternalMsg option) =
        let handle =
            function
            | Downloads.ExternalMsg.Return -> MainMenu |> NavigateTo |> Cmd.ofMsg

        msg
        |> Option.map handle
        |> Option.defaultValue Cmd.none

    let init =
        { currentPage = CurrentPage.MainMenu
          searchState = Search.init
          mangaState = Manga.init
          downloadsState = Downloads.init
          preferencesState = Preferences.init }

    let update msg state =
        match msg with
        | NavigateTo page -> { state with currentPage = page }, Cmd.none
        | SearchMsg msg ->
            let st, cmd, extMsg = state.searchState |> Search.update msg

            { state with searchState = st },
            Cmd.batch [ Cmd.map SearchMsg cmd
                        handleSearchExtMsg extMsg ]
        | MangaMsg msg ->
            let st, cmd, extMsg = state.mangaState |> Manga.update msg

            { state with mangaState = st },
            Cmd.batch [ Cmd.map MangaMsg cmd
                        handleMangaExtMsg extMsg ]
        | DownloadsMsg msg ->
            let st, cmd, extMsg = state.downloadsState |> Downloads.update msg

            { state with downloadsState = st },
            Cmd.batch [ Cmd.map DownloadsMsg cmd
                        handleDownloadExtMsg extMsg ]
        | PreferencesMsg msg ->
            let st, cmd = state.preferencesState |> Preferences.update msg
            { state with preferencesState = st }, Cmd.map PreferencesMsg cmd

    let mainMenu (state: State) dispatch =
        Console.singleChoice
            "Pick action"
            [ ("Search manga", CurrentPage.Search)
              ("Preferences", CurrentPage.Preferences) ]
            (NavigateTo >> dispatch)

    let view (state: State) dispatch =
        Console.clear ()

        match state.currentPage with
        | Loading message -> Loading.view { message = message } dispatch
        | MainMenu -> mainMenu state dispatch
        | Search -> Search.view state.searchState (SearchMsg >> dispatch)
        | Manga -> Manga.view state.mangaState (MangaMsg >> dispatch)
        | Downloads -> Downloads.view state.downloadsState (DownloadsMsg >> dispatch)
        | Preferences ->
            Preferences.view state.preferencesState (PreferencesMsg >> dispatch)
        | x -> failwith $"Unfinished page {x}"
