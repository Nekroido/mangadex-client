namespace Mangadexter.Application

[<RequireQualifiedAccess>]
module Mangadex =
    let searchManga = Application.Manga.Request.searchManga
    let listChapters = Application.Chapter.Request.listChapters

    let getChapterDownloadInformation =
        Application.ChapterDownload.Request.getChapterDownloadInformation
