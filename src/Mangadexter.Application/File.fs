namespace Mangadexter.Application

open System.IO
open FSharp.Data
open System.Text.Json
open System.Text.Json.Serialization

open Mangadexter.Core

[<RequireQualifiedAccess>]
module CbzMetadata =
    type Credits =
        { person: string
          role: string
          primary: bool }

    type Metadata =
        { title: string
          series: string
          credits: Credits seq
          publicationYear: int option
          tags: string seq
          volume: int option
          issue: decimal option }

    type Container = { ``ComicBookInfo/1.0``: Metadata }

    let private mapCredits (author: Author) =
        let makeRecord name role isPrimary =
            { person = name |> AuthorName.getValue
              role = role
              primary = isPrimary }

        author
        |> function
            | Writer name -> name, "writer", true
            | Artist name -> name, "artist", false
        |> Tuple.uncurry3 makeRecord

    let serialize (manga: Manga) (chapter: Chapter) =
        let options = JsonSerializerOptions()
        options.Converters.Add(JsonFSharpConverter())

        let metadata =
            { title =
                sprintf
                    "%s - %s"
                    (manga |> Manga.getReadableTitle)
                    (chapter |> Chapter.formatChapter)
              series = manga |> Manga.getReadableTitle
              credits = manga.Authors |> Seq.map mapCredits
              publicationYear = manga.Year |> Option.map int
              tags = manga.Tags |> Seq.map Tag.getValue
              volume =
                chapter.Volume
                |> Option.map (VolumeNumber.getValue >> int)
              issue =
                chapter.Number
                |> Option.map (ChapterNumber.getValue) }

        JsonSerializer.Serialize({ ``ComicBookInfo/1.0`` = metadata }, options)

[<RequireQualifiedAccess>]
module File =
    open System.IO.Compression

    open Mangadexter.Core.File

    let createStream () = new MemoryStream()

    let streamToBytes (stream: MemoryStream) : byte array = stream.ToArray()

    let fetchFile url output =
        async {
            let! request = url |> Http.AsyncRequestStream

            match request.StatusCode with
            | x when x = 200 ->
                do!
                    request.ResponseStream.CopyToAsync(output)
                    |> Async.AwaitTask

                return Result.Ok output
            | _ ->
                return
                    Result.Error $"File download resulted in {request.StatusCode} status"
        }

    let createCbz (args: CreateCBZArgs) : Async<Result<MemoryStream, exn>> =
        async {
            try
                use stream = new MemoryStream()
                use file = new ZipArchive(stream, ZipArchiveMode.Create)
                file.Comment <- CbzMetadata.serialize args.Manga args.Chapter

                args.DownloadedPages
                |> Seq.iteri (fun index data ->
                    use dataStream = new MemoryStream(data)
                    let entry = file.CreateEntry(index |> String.format "000")
                    use entryStream = entry.Open()

                    dataStream.Seek(0, SeekOrigin.Begin) |> ignore

                    dataStream.CopyTo(entryStream)
                    entryStream.Flush()
                    dataStream.Flush())

                return stream |> Ok
            with
            | ex -> return Error ex
        }

    let storeFile (args: StoreFileArgs) : Async<Result<unit, exn>> =
        async {
            try
                args.Filename |> Directory.createForPath

                use dataStream = new MemoryStream(args.Data)

                use fileStream =
                    new FileStream(path = args.Filename, mode = FileMode.OpenOrCreate)

                dataStream.Seek(0, SeekOrigin.Begin) |> ignore

                do!
                    dataStream.CopyToAsync(fileStream)
                    |> Async.AwaitTask

                fileStream.Flush()
                dataStream.Flush()

                return Ok()
            with
            | ex -> return Error ex
        }
