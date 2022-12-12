namespace Mangadexter.Cli.Page

open Elmish

open Mangadexter.Core
open Mangadexter.Core.File
open Mangadexter.Cli
open Mangadexter.Application

open FSharp.Control
open System

[<RequireQualifiedAccess>]
module Downloads =
    type ExternalMsg = | Return

    type Msg =
        | Download of (Manga * Chapter) list
        | ClearFinished

    type DownloadStatus =
        | New
        | InProgress of progress: float
        | Finished

    type State =
        { downloads: (Manga * Chapter * DownloadStatus) list }

    let init = { downloads = List.empty }

    let update msg (state: State) =
        match msg with
        | Download chapters ->
            { state with downloads = chapters |> List.map (fun (m, c) -> m, c, New) },
            Cmd.none,
            None
        | ClearFinished -> { state with downloads = List.empty }, Cmd.none, Some Return

    let downloadPage url =
        async {
            let stream = File.createStream ()

            return! stream |> File.fetchFile url
        }

    let downloadMangaChapter
        (manga: Manga)
        (chapter: Chapter)
        (ctx: Spectre.Console.ProgressTask)
        =
        async {
            let! result = Mangadex.getChapterDownloadInformation { Chapter = chapter }

            match result with
            | Error ex ->
                ctx.Description <- ex.Message
                ctx.StopTask()
            | Ok dl ->
                let percentile = 100.0 / (dl.Pages |> Seq.length |> float)

                let! pages =
                    dl.Pages
                    |> List.map (string >> downloadPage)
                    |> AsyncSeq.ofSeqAsync
                    |> AsyncSeq.map (fun result ->
                        match result with
                        | Ok _ -> ctx.Increment(percentile)
                        | Error ex -> ctx.Description <- ex

                        result)
                    |> AsyncSeq.toArrayAsync

                let! cbzStream =
                    async {
                        ctx.Description <- "Creating CBZ archive"
                        ctx.IsIndeterminate <- true

                        return!
                            { Manga = manga
                              Chapter = chapter
                              DownloadedPages =
                                pages
                                |> List.ofArray
                                |> List.map (Result.toOption >> Option.get)
                                |> List.map (fun s -> upcast s) }
                            |> File.createCbz
                    }

                let filename =
                    sprintf
                        "./manga/%s.cbz"
                        (chapter
                         |> Chapter.formatChapter
                         |> Path.toSafePath)

                let buildStoreArgs data : StoreFileArgs =
                    { Data = data; Filename = filename }

                match! cbzStream
                       |> Result.toOption
                       |> Option.get
                       |> (buildStoreArgs >> File.storeFile)
                    with
                | Ok _ -> ctx.Description <- $"Saved CBZ file to {filename}"
                | Error ex -> ctx.Description <- $"Failed to store CBZ file at {filename}"

                ctx.StopTask()

                return ()

        }

    let view (state: State) dispatch =
        Console.clear ()

        state.downloads
        |> List.length
        |> sprintf "Downloads: (%d)"
        |> Console.echo

        state.downloads
        |> List.map (fun (manga, chapter, _) ->
            (sprintf
                "%s - %s"
                (manga |> Manga.getReadableTitle)
                (chapter |> Chapter.formatChapter)),
            (downloadMangaChapter manga chapter))
        |> Console.progress

        dispatch ClearFinished
