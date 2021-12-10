module Manga

open System.Web
open Data

let listManga args =
    async { return! makeRequestUrl "manga" args |> MangaList.AsyncLoad }

let searchManga limit offset (query: string) =
    [ ("title", query)
      ("order[relevance]", "desc")
      ("limit", $"%d{limit}")
      ("offset", $"%d{offset}") ]
    |> listManga
