module Application.Common

open System.Web

open Mangadexter.Core

[<Literal>]
let BaseUrl = "https://api.mangadex.org/"

let makeRequestUrl endpoint args : string =
   let query =
      args
      |> Seq.map (fun (key: string, value: string) ->
         let k = HttpUtility.UrlEncode(key)
         let v = HttpUtility.UrlEncode(value)
         $"{k}={v}")
      |> String.concat "&"

   BaseUrl
   |> UriBuilder.fromString
   |> UriBuilder.setPath endpoint
   |> UriBuilder.setQuery query
   |> UriBuilder.toString
