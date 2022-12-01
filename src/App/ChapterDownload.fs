module ChapterDownload

open Data
open Preferences

let getChapterDownloadInformation (chapter: Chapter) =
    makeRequestUrl $"at-home/server/%A{chapter.Id}" []
    |> ChapterDownload.AsyncLoad

let getPageDownloadUrl downloadInfo quality page =
    let quality =
        match quality with
        | Quality.High -> "data"
        | Quality.Low -> "data-saver"

    $"{downloadInfo |> ChapterDownloadInfo.getBaseUrl}/{quality}/{downloadInfo |> ChapterDownloadInfo.getChapterHash}/{page}"
