namespace Utils

open System
open System.IO
open Microsoft.FSharp.Reflection

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
module Path =
    let toSafePath (path: string) =
        String.Join("_", Path.GetInvalidFileNameChars() |> path.Split)

[<RequireQualifiedAccess>]
module Result =
    let proceedIfOk result =
        match result with
        | Result.Ok r -> r
        | Result.Error ex -> failwith ex

[<RequireQualifiedAccess>]
module DiscriminatedUnion =
    let createCase (caseInfo: UnionCaseInfo) =
        FSharpValue.MakeUnion(caseInfo, Array.zeroCreate (caseInfo.GetFields().Length))

    let listCases<'a> () =
        typeof<'a>
        |> FSharpType.GetUnionCases
        |> Seq.map (fun caseInfo -> caseInfo |> createCase :?> 'a)

[<RequireQualifiedAccess>]
module String =
    let join separator parts =
        parts
        |> Seq.filter (fun x -> String.IsNullOrWhiteSpace(x) = false)
        |> String.concat separator
