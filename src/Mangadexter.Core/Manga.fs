namespace Mangadexter.Core.Request

open Mangadexter.Core

module MangaRequest =
   type MangaList =
      { Items: Manga list
        Pagination: Pagination }

   type SearchManga = SearchMangaArgs -> Async<Result<MangaList, exn>>

   and SearchMangaArgs =
      { Title: Title option
        PreferredLanguage: Language option
        Take: uint
        Skip: uint }

   let defaultSearchMangaArgs =
      { Title = None
        PreferredLanguage = None
        Take = 50u
        Skip = 0u }
