namespace Mangadexter.Core

open System
open System.IO

module File =
    type CreateCBZ = CreateCBZArgs -> Async<Result<Stream, exn>>

    and CreateCBZArgs =
        { Manga: Manga
          Chapter: Chapter
          DownloadedPages: Stream list }

    type StoreFile = StoreFileArgs -> Async<Result<unit, exn>>

    and StoreFileArgs = { Data: Stream; Filename: string }
