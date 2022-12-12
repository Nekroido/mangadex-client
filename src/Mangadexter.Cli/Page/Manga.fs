namespace Mangadexter.Cli.Page

open Elmish

open Mangadexter.Core
open Mangadexter.Core.Request.ChapterRequest
open Mangadexter.Application

open Mangadexter.Cli

[<RequireQualifiedAccess>]
module Manga =
    type ExternalMsg =
        | DownloadChapters of (Manga * Chapter) list
        | Return

    type Action =
        | DownloadAll
        | PickChapters
        | PickFilteredChapters
        | DisplayPrompt
        | Return
        | Wait

    type Msg =
        | SetManga of Manga * preferredLanguage: Language
        | PerformAction of Action
        | DownloadChapters of Chapter list
        | ListChapterPicker of AsyncOperationStatus<Result<ChapterList, exn>>

    type State =
        { manga: Manga option
          preferredLanguage: Language option
          action: Action
          chapters: Deferred<ChapterList> }

    let init =
        { manga = None
          preferredLanguage = None
          action = Action.DisplayPrompt
          chapters = HasNotStartedYet }

    let fetchChapters (manga: Manga) (lang: Language option) =
        async {
            let! result =
                { MangaId = manga.Id
                  PreferredLanguage = lang
                  Take = 50u
                  Skip = 0u }
                |> Mangadex.listChapters

            return result |> Finished |> ListChapterPicker
        }

    let update msg (state: State) =
        match msg with
        | SetManga (manga, lang) ->
            { state with
                manga = Some manga
                preferredLanguage = Some lang },
            Cmd.none,
            None
        | DownloadChapters chapters ->
            { state with action = Action.Wait },
            Cmd.none,
            (chapters
             |> List.map (fun c -> state.manga |> Option.get, c)
             |> ExternalMsg.DownloadChapters
             |> Some)
        | ListChapterPicker Started ->
            { state with chapters = InProgress }, Cmd.none, None
        | ListChapterPicker (Finished (Ok chapters)) ->
            { state with chapters = Resolved chapters }, Cmd.none, None
        | ListChapterPicker (Finished (Error ex)) ->
            { state with chapters = HasNotStartedYet }, Cmd.none, None
        | PerformAction Action.PickChapters ->
            { state with action = Action.PickChapters },
            Cmd.fromAsync (
                fetchChapters (state.manga |> Option.get) state.preferredLanguage
            ),
            None
        | PerformAction Action.Return -> state, Cmd.none, Some ExternalMsg.Return
        | _ -> state, Cmd.none, None

    let mangaDetails (manga: Manga) =
        let details =
            [ (manga |> Manga.getReadableTitle)
              (manga |> Manga.getFormattedAuthors)
              (manga |> Manga.getLastChapterNumber)
              (manga |> Manga.getReadableStatus) ]
            |> List.singleton

        Console.table
            [ "Title"
              "Authors"
              "Last chapter"
              "Status" ]
            details

    let selectAction dispatch =
        Console.singleChoice
            "Pick action"
            [ ("Download all chapters", Action.DownloadAll)
              ("Pick chapters for download", Action.PickFilteredChapters)
              ("Pick chapters for download (including all translation variants)",
               Action.PickChapters)
              ("← Return", Action.Return) ]
            (PerformAction >> dispatch)

    let selectChapters (chapters: Chapter list) dispatch =
        let options =
            let formatVolume =
                Option.map VolumeNumber.getFormatted
                >> Option.defaultValue "- no volume -"

            chapters
            |> List.map (fun c ->
                c.Volume |> formatVolume, (c |> Chapter.formatChapter, c))

        Console.multiChoice
            "Select chapters to download:"
            options
            (fun selectedChapters -> selectedChapters |> DownloadChapters |> dispatch)

    let chapterPicker chapters dispatch =
        match chapters with
        | HasNotStartedYet
        | InProgress -> Console.echo "Loading..."
        | Resolved chapters -> selectChapters chapters.Items dispatch

    let view (state: State) dispatch =
        match state.manga with
        | Some manga ->
            Console.clear ()
            mangaDetails manga

            match state.action with
            | Action.Wait ->
                Console.clear ()
                Console.echo "Loading..."
            | Action.DisplayPrompt -> selectAction dispatch
            | Action.DownloadAll -> ()
            | Action.PickChapters -> chapterPicker state.chapters dispatch
            | Action.PickFilteredChapters -> ()
            | Action.Return -> ()
        | None -> Console.echo "No manga selected"
