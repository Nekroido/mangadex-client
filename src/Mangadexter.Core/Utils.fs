namespace Mangadexter.Core

open System
open System.IO

[<AutoOpen>]
module Utils =
    let flip f a b = f b a
    let third (_, _, c) = c

    module Tuple =
        let uncurry f (a, b) = f a b
        let uncurry3 f (a, b, c) = f a b c

    [<RequireQualifiedAccess>]
    module UriBuilder =
        let fromString (url: string) = UriBuilder(url)

        let setPath path (uriBuilder: UriBuilder) =
            uriBuilder.Path <- path
            uriBuilder

        let setQuery query (uriBuilder: UriBuilder) =
            uriBuilder.Query <- query
            uriBuilder

        let toString (uriBuilder: UriBuilder) = uriBuilder.ToString()

    [<RequireQualifiedAccess>]
    module String =
        let join separator parts =
            parts
            |> Seq.filter (fun x -> String.IsNullOrWhiteSpace(x) = false)
            |> String.concat separator

        let inline format format (x: ^a) =
            (^a: (member ToString: string -> string) (x, format))

    [<RequireQualifiedAccess>]
    module Result =
        let get =
            function
            | Ok s -> s
            | Error _ -> failwithf "Failed to get item %A"

    [<RequireQualifiedAccess>]
    module Directory =
        let createForPath (path: string) =
            let directory = path |> Path.GetDirectoryName

            directory
            |> String.IsNullOrWhiteSpace
            |> not
            |> function
                | true -> directory |> Directory.CreateDirectory |> ignore
                | false -> ()

    [<RequireQualifiedAccess>]
    module Path =
        let combine (parts: string seq) = parts |> Array.ofSeq |> Path.Combine

        let getFileExtension (path: string) = path |> Path.GetExtension

        let toSafePath (path: string) =
            String.Join("_", Path.GetInvalidFileNameChars() |> path.Split)
