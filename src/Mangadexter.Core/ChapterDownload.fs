namespace Mangadexter.Core.Request

open Mangadexter.Core

module ChapterDownloadRequest =
   type FetchChapterDownloadInfo =
      FetchChapterDownloadInfoArgs -> Async<Result<ChapterDownload, exn>>

   and FetchChapterDownloadInfoArgs = { Chapter: Chapter }
