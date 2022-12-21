namespace Mangadexter.Cli.Page

open Elmish

open Mangadexter.Core
open Mangadexter.Core.Request.ChapterRequest
open Mangadexter.Application

open Mangadexter.Cli
open FSharp.Control

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
        | ListChapterPicker of AsyncOperationStatus<Result<Chapter list, exn>>

    type State =
        { manga: Manga option
          preferredLanguage: Language option
          action: Action
          chapters: Deferred<Chapter list> }

    let init =
        { manga = None
          preferredLanguage = None
          action = Action.DisplayPrompt
          chapters = HasNotStartedYet }

    let getAllChaptersRequest (manga: Manga) (lang: Language option) =
        let listChapters take skip =
            { MangaId = manga.Id
              PreferredLanguage = lang
              Take = take
              Skip = skip }
            |> Mangadex.listChapters

        let take = 100u

        let batch skip =
            async {
                let! result = listChapters take skip

                return
                    match result with
                    | Error ex -> raise ex
                    | Ok result ->
                        if result.Pagination.Total > skip then
                            Some(result.Items, (result.Pagination.Skip + take))
                        else
                            None
            }

        AsyncSeq.unfoldAsync batch 0u
        |> AsyncSeq.concatSeq
        |> AsyncSeq.toListAsync

    let getDistinctChaptersRequest (manga: Manga) (lang: Language option) =
        async {
            let! chapters = getAllChaptersRequest manga lang

            // scoring translators by number of translated chapters
            let translators =
                chapters
                |> List.groupBy (fun chapter -> chapter.Translation.Tranlsator)
                |> List.map (fun (translator, translatedChapters) ->
                    translator
                    |> Option.map (fun translator ->
                        translator |> Some, translatedChapters |> List.length)
                    |> Option.defaultValue (None, 0))
                |> Map

            return
                chapters
                |> List.sortByDescending (fun chapter ->
                    translators
                    |> Map.find chapter.Translation.Tranlsator)
                |> List.distinctBy (Chapter.getFormattedChapterNumber)
        }

    let getFilteredChaptersForPicker (manga: Manga) (lang: Language option) =
        async {
            let! result = getDistinctChaptersRequest manga lang

            return result |> Ok |> Finished |> ListChapterPicker
        }

    let getChaptersForPicker (manga: Manga) (lang: Language option) =
        async {
            let! result = getAllChaptersRequest manga lang

            return result |> Ok |> Finished |> ListChapterPicker
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
        | PerformAction Action.DownloadAll ->
            { state with action = Action.DownloadAll },
            Cmd.fromAsync (
                async {
                    let! chapters =
                        getDistinctChaptersRequest
                            (state.manga |> Option.get)
                            state.preferredLanguage

                    return
                        chapters
                        |> List.sortBy (Chapter.getFormattedChapterNumber)
                        |> DownloadChapters
                }
            ),
            None
        | PerformAction Action.PickFilteredChapters ->
            { state with action = Action.PickFilteredChapters },
            Cmd.fromAsync (
                async {
                    let! chapters =
                        getDistinctChaptersRequest
                            (state.manga |> Option.get)
                            state.preferredLanguage

                    return chapters |> Ok |> Finished |> ListChapterPicker
                }
            ),
            None
        | PerformAction Action.PickChapters ->
            { state with action = Action.PickChapters },
            Cmd.fromAsync (
                getChaptersForPicker (state.manga |> Option.get) state.preferredLanguage
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

    let private formatVolume =
        Option.map VolumeNumber.getFormatted
        >> Option.defaultValue "- no volume -"

    let private formatChapter chapter =
        sprintf
            "%s %s"
            (chapter |> Chapter.formatChapter)
            (chapter.Translation |> Translation.getFormatted)

    let chapterPicker chapterFmt chapters dispatch =
        match chapters with
        | HasNotStartedYet
        | InProgress -> Console.echo "Loading..."
        | Resolved chapters ->
            let options =
                chapters
                |> List.sortBy (Chapter.getFormattedChapterNumber)
                |> List.map (fun c -> c.Volume |> formatVolume, (c |> chapterFmt, c))

            Console.multiChoice
                "Select chapters to download:"
                options
                (fun selectedChapters -> selectedChapters |> DownloadChapters |> dispatch)

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
            | Action.DownloadAll ->
                Console.clear ()
                Console.echo "Preparing..."
            | Action.PickChapters -> chapterPicker formatChapter state.chapters dispatch
            | Action.PickFilteredChapters ->
                chapterPicker formatChapter state.chapters dispatch // todo: replace with Chapter.formatChapter
            | Action.Return -> ()
        | None -> Console.echo "No manga selected"
