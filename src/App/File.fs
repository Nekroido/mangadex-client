module File

open System.IO
open System.IO.Compression
open System.Text.Json

open Data
open Utils

let serialize metadata =
    {| ``ComicBookInfo/1.0`` = metadata |}
    |> JsonSerializer.Serialize

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

    use zip = new Ionic.Zip.ZipFile(filename)

    zip.Comment <-
        serialize
            {| title =
                   $"{manga |> Manga.getTitle} - {chapter |> Chapter.getFormattedTitle}"
               series = manga |> Manga.getTitle
               credits = manga |> Manga.getCredits
               publicationYear = manga |> Manga.getYear
               tags = manga |> Manga.getTags
               volume = chapter |> Chapter.getVolume
               issue = chapter |> Chapter.getChapterNumber |}

    zip.Save()

    Directory.Delete(temporaryFolder, recursive = true)
