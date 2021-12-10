namespace Utils

open System
open System.IO

module UriBuilder =
    let fromString (url: string) = UriBuilder(url)

    let setPath path (uriBuilder: UriBuilder) =
        uriBuilder.Path <- path
        uriBuilder

    let setQuery query (uriBuilder: UriBuilder) =
        uriBuilder.Query <- query
        uriBuilder

    let toString (uriBuilder: UriBuilder) = uriBuilder.ToString()

module Path =
    let toSafePath (path: string) =
        String.Join("_", Path.GetInvalidFileNameChars() |> path.Split)
