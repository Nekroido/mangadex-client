namespace Mangadexter.Core

module Persistence =
   type AddMangaToFavorites = Manga -> Async<Result<unit, exn>>
   type RemoveMangaFromFavorites = Id -> Async<Result<unit, exn>>
   type ListFavorites = unit -> Async<Result<Manga list, exn>>
