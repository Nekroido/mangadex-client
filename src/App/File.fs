module File

open System.IO
open System.IO.Compression
open System.Text.Json

open Data
open Utils

type private Credits =
    { person: string
      role: string
      primary: bool }

type private Metadata =
    { title: string
      series: string
      credits: Credits seq
      publicationYear: int option
      tags: string seq
      volume: string
      issue: string }

let private mapCredits (entity: Relationship) =
    let typeToRole t =
        match t with
        | "author" -> "Writer"
        | "artist" -> "Artist"
        | _ -> t

    { person = entity.Attributes.Value.Name
      role = entity.Type |> typeToRole
      primary = true }

let private mapChapterMetadata (chapter: Chapter) (manga: Manga) =
    { title = $"{manga |> Manga.getTitle} - {chapter |> Chapter.getFormattedTitle}"
      series = manga |> Manga.getTitle
      credits = manga |> Manga.getCredits |> Seq.map mapCredits
      publicationYear = manga |> Manga.getYear
      tags = manga |> Manga.getTags
      volume = chapter |> Chapter.getVolume
      issue = chapter |> Chapter.getChapterNumber }

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

    let metadata =
        mapChapterMetadata chapter manga |> serialize

    use zip = new Ionic.Zip.ZipFile(filename)

    zip.Comment <- metadata

    zip.Save()

    Directory.Delete(temporaryFolder, recursive = true)

type CBZSettings =
    { SavePath: string
      Manga: Manga
      Chapter: Chapter
      Pages: Async<Stream> seq }

type CBZBuilder() =
    member _.Yield _ =
        { SavePath = Unchecked.defaultof<string>
          Manga = Unchecked.defaultof<Manga>
          Chapter = Unchecked.defaultof<Chapter>
          Pages = [] }

    [<CustomOperation("save_path")>]
    member _.SetSavePath(settings, path) = { settings with SavePath = path }

    [<CustomOperation("manga")>]
    member _.SetManga(settings, manga) = { settings with Manga = manga }

    [<CustomOperation("chapter")>]
    member _.SetChapter(settings, chapter) = { settings with Chapter = chapter }

    [<CustomOperation("pages")>]
    member _.SetPages(settings, pages) = { settings with Pages = pages }

    member _.Run settings =
        let metadata =
            mapChapterMetadata settings.Chapter settings.Manga
            |> serialize

        use zip = new Ionic.Zip.ZipFile(settings.SavePath)

        zip.Comment <- metadata

        settings.Pages
        |> Seq.iteri
            (fun index page ->
                let entry = page |> Async.RunSynchronously
                zip.AddEntry($"%02d{index}", entry) |> ignore)

        zip.Save()
