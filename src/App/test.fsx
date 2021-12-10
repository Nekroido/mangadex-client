#r "nuget: FSharp.Data"

open FSharp.Data
open System
open System.Web

module UriBuilder =
    let setPath path (uriBuilder: UriBuilder) =
        uriBuilder.Path <- path
        uriBuilder
    let setQuery query (uriBuilder: UriBuilder) =
        uriBuilder.Query <- query
        uriBuilder

    let toString (uriBuilder: UriBuilder) = uriBuilder.ToString()

[<Literal>]
let baseUrl = "https://api.mangadex.org/"

let GET endpoint args : string =
    let query =
        args
        |> Seq.map
            (fun (key: string, value: string) ->
                 let k = HttpUtility.UrlEncode(key)
                 let v = HttpUtility.UrlEncode(value)
                 $"{k}={v}" )
        |> String.concat "&"

    UriBuilder(baseUrl)
    |> UriBuilder.setPath endpoint
    |> UriBuilder.setQuery query
    |> UriBuilder.toString

[<Literal>]
let mangaListSample = baseUrl + "manga?title=darling"

type MangaList = JsonProvider<mangaListSample>

let list =
    [ ("title", "darling in the franxx") ]
    |> GET "manga"
    |> MangaList.Load

let hit = list.Data |> Seq.head

printfn $"{hit.Attributes.Title.En.Value}"
