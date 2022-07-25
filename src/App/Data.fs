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
    + "manga?title=made&includes[]=author&includes[]=artist&hasAvailableChapters=true"

[<Literal>]
let ChapterListSample = "chapters-sample.json"

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

type ChapterList = JsonProvider<"chapters-sample.json">

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

    let getFormattedCredits (manga: T) =
        manga
        |> getCredits
        |> Seq.map (fun credit -> credit.Attributes.Value.Name)
        |> String.join ", "

    let getLastChapterNumber (manga: T) = manga.Attributes.LastChapter

    let getStatus (manga: T) = manga.Attributes.Status

    let toString (manga: T) = manga |> getTitle

module Chapter =
    type T = Chapter

    open Preferences

    let getPages quality (chapter: T) =
        match quality with
        | Quality.High -> chapter.Attributes.Data
        | Quality.Low -> chapter.Attributes.DataSaver

    let getChapter (chapter: T) = chapter.Attributes.Chapter

    let getFormattedChapterNumber (chapter: T) =
        (chapter |> getChapter).ToString("000.###")

    let getFormattedChapter (chapter: T) =
        $"Chapter {chapter |> getFormattedChapterNumber}"

    let getTitle (chapter: T) =
        chapter.Attributes.Title |> Option.defaultValue ""

    let getVolume (chapter: T) = chapter.Attributes.Volume

    let getFormattedVolumeNumber (chapter: T) =
        chapter
        |> getVolume
        |> Option.bind (fun volume -> volume.ToString("00") |> Some)

    let getFormattedVolume (chapter: T) =
        chapter
        |> getVolume
        |> Option.bind (fun volume -> $"Volume {volume}" |> Some)
        |> Option.defaultValue ""

    let getTranslatedLanguage (chapter: T) = chapter.Attributes.TranslatedLanguage

    let getTranslatorGroups (chapter: T) =
        chapter.Relationships
        |> Seq.filter (fun r -> r.Attributes.IsSome && r.Type = "scanlation_group")

    let getFormattedTranslatorGroup (chapter: T) =
        chapter
        |> getTranslatorGroups
        |> Seq.map (fun r -> r.Attributes.Value.Name)
        |> String.join ", "

    let getPublishDate (chapter: T) = chapter.Attributes.PublishAt

    let getHash (chapter: T) = chapter.Attributes.Hash.ToString("N")

    let getFormattedTitle (chapter: T) =
        [| chapter |> getFormattedVolume
           chapter |> getFormattedChapter
           chapter |> getTitle |]
        |> String.join " - "

    let toString (chapter: T) =
        $"%s{chapter |> getFormattedTitle}[%s{chapter |> getTranslatedLanguage}]"
