module Chapter

open Data
open Preferences

let listChapters limit offset (manga: Manga) =
    async {
        let args =
            [ ("manga", $"%A{manga.Id}")
              ("translatedLanguage[]", "en")
              ("order[chapter]", "asc")
              ("limit", $"%d{limit}")
              ("offset", $"%d{offset}") ]

        return!
            makeRequestUrl "chapter" args
            |> ChapterList.AsyncLoad
    }

let getChapterBaseUrl (chapter: Chapter) =
    async {
        let! result =
            makeRequestUrl $"at-home/server/%A{chapter.Id}" []
            |> ChapterServer.AsyncLoad

        return result.BaseUrl
    }

let getChapterPageDownloadUrl baseUrl quality chapter page =
    let quality =
        match quality with
        | Quality.High -> "data"
        | Quality.Low -> "data-saver"

    $"{baseUrl}/{quality}/{chapter |> Chapter.getHash}/{page}"
