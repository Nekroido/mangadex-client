module Data

open System.Web

open Utils

[<Literal>]
let BaseUrl = "https://api.mangadex.org/"

[<Literal>]
let MangaListSampleUrl = BaseUrl + "manga?title=darling"

[<Literal>]
let ChapterListSampleUrl =
    BaseUrl
    + "chapter?manga=42caa178-b6dc-4ed1-bcbf-18f457bbd121"

let makeRequestUrl endpoint args : string =
    let query =
        args
        |> Seq.map
            (fun (key: string, value: string) ->
                let k = HttpUtility.UrlEncode(key)
                let v = HttpUtility.UrlEncode(value)
                $"{k}={v}")
        |> String.concat "&"

    BaseUrl
    |> UriBuilder.fromString
    |> UriBuilder.setPath endpoint
    |> UriBuilder.setQuery query
    |> UriBuilder.toString

open FSharp.Data

type ChapterList = JsonProvider<ChapterListSampleUrl>
type MangaList = JsonProvider<MangaListSampleUrl>

type Chapter = ChapterList.Datum
type Manga = MangaList.Datum

let listChapters args (manga: Manga) =
    let id = manga.Id.ToString()

    makeRequestUrl "chapter" (("manga", manga.Id.ToString()) :: args)
    |> ChapterList.AsyncLoad

let listManga args =
    makeRequestUrl "manga" args |> MangaList.AsyncLoad

module Manga =
    type T = Manga

    let title (manga: T) : string =
        manga.Attributes.Title.En
        |> Option.defaultValue "---"

module Chapter =
    type T = Chapter

    let title (chapter: T) =
        chapter.Attributes.Title
        |> Option.defaultValue "---"

    let toString (chapter: T) =
        sprintf "C%02d - %s" chapter.Attributes.Chapter (chapter |> title)
