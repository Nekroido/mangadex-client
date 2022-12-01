module Chapter

open Data
open Preferences

let listChapters limit offset language (manga: Manga) =
    async {
        let preferredLanguage =
            match language with
            | Language.English -> "en"
            | Language.Japanese -> "ja"

        let args =
            [ ("manga", $"%A{manga.Id}")
              ("translatedLanguage[]", preferredLanguage)
              ("order[chapter]", "asc")
              ("includes[]", "scanlation_group")
              ("limit", $"%d{limit}")
              ("offset", $"%d{offset}") ]

        return!
            makeRequestUrl $"chapter" args
            |> ChapterList.AsyncLoad
    }
