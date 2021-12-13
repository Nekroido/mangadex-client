module Download

open FSharp.Data
open System
open System.IO
open System.Net
open Data
open Utils

[<Literal>]
let appStorageFolder = "mangadex-client"

[<Literal>]
let quality = "data"

let getTemporaryFolder (manga: Manga) (chapter: Chapter) =
    [| Path.GetTempPath()
       appStorageFolder
       manga |> Manga.toString |> Path.toSafePath
       chapter |> Chapter.toString |]
    |> Path.Combine

let downloadPage baseUrl (manga: Manga) (chapter: Chapter) (pageFile: string) pageNumber =
    async {
        let downloadUrl =
            $"{baseUrl}/{quality}/{chapter |> Chapter.getHash}/{pageFile}"

        let temporaryFolder = getTemporaryFolder manga chapter

        temporaryFolder
        |> Directory.CreateDirectory
        |> ignore

        let downloadPath =
            [| temporaryFolder
               $"%03d{pageNumber}{pageFile |> Path.GetExtension}" |]
            |> Path.Combine

        let! request = Http.AsyncRequestStream(downloadUrl)

        use outputFile =
            new FileStream(downloadPath, FileMode.Create)

        do!
            request.ResponseStream.CopyToAsync(outputFile)
            |> Async.AwaitTask
    }

let downloadFile downloadUrl output =
    async {
        let! request = downloadUrl |> Http.AsyncRequestStream

        match request.StatusCode with
        | x when x = 200 ->
            do!
                request.ResponseStream.CopyToAsync(output)
                |> Async.AwaitTask

            return Result.Ok output
        | _ ->
            return Result.Error $"File download resulted in {request.StatusCode} status"
    }
