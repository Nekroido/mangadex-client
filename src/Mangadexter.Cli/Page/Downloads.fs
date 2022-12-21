namespace Mangadexter.Cli.Page

open Elmish

open Mangadexter.Core
open Mangadexter.Core.File
open Mangadexter.Cli
open Mangadexter.Application

open FSharp.Control
open FSharpPlus
open FsToolkit.ErrorHandling

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
            let fetchChapterDlInfo chapter =
                Mangadex.getChapterDownloadInformation { Chapter = chapter }

            let increment percentile = ctx.Increment(percentile)

            let setDescription message =
                ctx.Description <-
                    sprintf
                        "%s [[%s]]: %s"
                        (manga |> Manga.getReadableTitle)
                        (chapter |> Chapter.getShortFormattedChapter)
                        message

            let downloadPages percentile uris =
                let total = uris |> Seq.length

                let bumpStatus index =
                    increment (percentile)

                    sprintf "downloading page %d of %d" (index + 1) total
                    |> setDescription

                uris
                |> List.mapi (fun index uri ->
                    uri
                    |> (string
                        >> downloadPage
                        >> (AsyncResult.map (fun stream ->
                            bumpStatus index
                            stream |> File.streamToBytes))))
                |> AsyncSeq.ofSeqAsync
                |> AsyncSeq.toListAsync
                |> Async.map (fun results ->
                    let pages, errors = results |> Result.partition

                    match errors |> Seq.tryHead with
                    | Some error -> Error(failwith error)
                    | None -> pages |> Ok)

            let buildCBZ pages =
                { Manga = manga
                  Chapter = chapter
                  DownloadedPages = pages }
                |> File.createCbz
                |> AsyncResult.map File.streamToBytes

            let saveCBZ data =
                let filename =
                    sprintf
                        "./manga/%s/%s.cbz"
                        (manga |> Manga.getReadableTitle |> Path.toSafePath)
                        (chapter
                         |> Chapter.formatChapter
                         |> Path.toSafePath)

                { Data = data; Filename = filename }
                |> File.storeFile
                |> AsyncResult.map (fun _ -> filename)

            let! flow =
                asyncResult {
                    setDescription "fetching chapter download information"
                    let! chapterInfo = fetchChapterDlInfo chapter
                    let totalPages = chapterInfo.Pages |> Seq.length

                    let percentile = 100. / ((totalPages + 2) |> float) // pages + build CBZ + save CBZ

                    increment percentile

                    setDescription "downloading pages"
                    let! pages = downloadPages percentile chapterInfo.Pages

                    setDescription "building CBZ"
                    let! cbzData = buildCBZ pages
                    increment percentile

                    setDescription "saving CBZ"
                    let! filename = saveCBZ cbzData
                    increment percentile

                    return filename
                }

            match flow with
            | Ok filename -> setDescription $"saved to {filename}"
            | Error ex -> setDescription $"faulted with \"{ex.Message}\""

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
