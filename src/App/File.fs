module File

open System.IO
open System.IO.Compression

open Data
open Utils

let createCBZ (manga: Manga) (chapter: Chapter) =
    let temporaryFolder =
        Download.getTemporaryFolder manga chapter

    let targetDirectory =
        [| Directory.GetCurrentDirectory()
           manga |> Manga.toString |> Path.toSafePath |]
        |> Path.Combine

    targetDirectory
    |> Directory.CreateDirectory
    |> ignore

    let filename =
        [| targetDirectory
           $"{chapter |> Chapter.toString}.cbz" |]
        |> Path.Combine

    ZipFile.CreateFromDirectory(temporaryFolder, filename)
