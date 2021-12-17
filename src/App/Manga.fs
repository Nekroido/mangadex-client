module Manga

open Data
open Preferences

let listManga args =
    async { return! makeRequestUrl "manga" args |> MangaList.AsyncLoad }

let searchManga limit offset language (query: string) =
    let preferredLanguage =
        language
        |> function
            | Language.English -> "en"
            | Language.Japanese -> "ja"

    [ ("title", query)
      ("order[relevance]", "desc")
      ("includes[]", "author")
      ("includes[]", "artist")
      ("availableTranslatedLanguage[]", preferredLanguage)
      ("limit", $"%d{limit}")
      ("offset", $"%d{offset}") ]
    |> listManga
