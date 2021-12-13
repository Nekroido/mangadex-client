module Data

open System.Web

open Utils

[<Literal>]
let BaseUrl = "https://api.mangadex.org/"

[<Literal>]
let ChapterServerSampleUrl =
    BaseUrl
    + "at-home/server/b9d10b86-c956-4191-b05b-6cce5143cee4"

[<Literal>]
let MangaListSampleUrl =
    BaseUrl
    + "manga?title=darling&includes[]=author&includes[]=artist"

[<Literal>]
let ChapterListSampleUrl =
    BaseUrl
    + "chapter?manga=42caa178-b6dc-4ed1-bcbf-18f457bbd121&translatedLanguage%5b%5d=en&order%5bchapter%5d=asc&limit=100&offset=0"

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

type ChapterServer = JsonProvider<ChapterServerSampleUrl>
type ChapterList = JsonProvider<ChapterListSampleUrl>
type MangaList = JsonProvider<MangaListSampleUrl>

type Server = ChapterServer.Root
type Chapter = ChapterList.Datum
type Manga = MangaList.Datum
type Relationship = MangaList.Relationship

module Manga =
    type T = Manga

    let getTitle (manga: T) : string =
        manga.Attributes.Title.En
        |> Option.defaultValue "---"

    let getYear (manga: T) = manga.Attributes.Year

    let getTags (manga: T) =
        manga.Attributes.Tags
        |> Seq.map (fun tag -> tag.Attributes.Name.En)

    let getCredits (manga: T) : Relationship seq =
        manga.Relationships
        |> Seq.filter
            (fun r ->
                r.Attributes.IsSome
                && [ "author"; "artist" ] |> Seq.contains r.Type)

    let toString (manga: T) = manga |> getTitle

module Chapter =
    type T = Chapter

    let getPages (chapter: T) = chapter.Attributes.Data

    let getChapterNumber (chapter: T) =
        chapter.Attributes.Chapter.ToString("000.###")

    let getTitle (chapter: T) =
        chapter.Attributes.Title |> Option.defaultValue ""

    let getVolume (chapter: T) =
        chapter.Attributes.Volume.ToString("00")

    let getTranslatedLanguage (chapter: T) = chapter.Attributes.TranslatedLanguage

    let getPublishDate (chapter: T) = chapter.Attributes.PublishAt

    let getHash (chapter: T) = chapter.Attributes.Hash.ToString("N")

    let getFormattedTitle (chapter: T) =
        [| $"Volume {chapter |> getVolume} Chapter {chapter |> getChapterNumber}"
           chapter |> getTitle |]
        |> Seq.filter (fun x -> System.String.IsNullOrWhiteSpace(x) = false)
        |> String.concat " - "

    let toString (chapter: T) =
        $"V%s{chapter |> getVolume}-C%s{chapter |> getChapterNumber} - %s{chapter |> getTranslatedLanguage}"
