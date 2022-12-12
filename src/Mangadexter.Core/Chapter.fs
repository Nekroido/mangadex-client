namespace Mangadexter.Core.Request

open Mangadexter.Core

module ChapterRequest =
   type ChapterList =
      { Items: Chapter list
        Pagination: Pagination }

   type ListChapters = ListChaptersArgs -> Async<Result<ChapterList, exn>>

   and ListChaptersArgs =
      { MangaId: Id
        PreferredLanguage: Language option
        Take: uint
        Skip: uint }

   let defaultListChapterArgs =
      { MangaId = Unchecked.defaultof<Id>
        PreferredLanguage = None
        Take = 50u
        Skip = 0u }
