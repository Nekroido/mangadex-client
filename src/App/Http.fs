module Http

open System.Net
open FSharp.Data

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
            return Result.Error $"File download resulted in {request.StatusCode} status"
    }
