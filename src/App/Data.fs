module Data

open System.Web

open Utils

[<Literal>]
let BaseUrl = "https://api.mangadex.org/"

[<Literal>]
let ChapterDownloadSampleUrl =
    BaseUrl
    + "at-home/server/4610197f-9185-4838-8f17-406191547806"

[<Literal>]
let MangaListSampleUrl =
    BaseUrl
    + "manga?title=made&includes[]=author&includes[]=artist&hasAvailableChapters=true&limit=50"

[<Literal>]
let ChapterListSample =
    BaseUrl
    + "chapter?manga=42caa178-b6dc-4ed1-bcbf-18f457bbd121&translatedLanguage[]=en&includes[]scanlation_group&limit=50"

let makeRequestUrl endpoint args : string =
    let query =
        args
        |> Seq.map (fun (key: string, value: string) ->
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

type ChapterDownload = JsonProvider<ChapterDownloadSampleUrl>

type ChapterList = JsonProvider<"chapters-sample.json">

type MangaList = JsonProvider<MangaListSampleUrl>

type ChapterDownloadInfo = ChapterDownload.Root
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
        |> Seq.filter (fun r ->
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

    open System.Globalization

    let getChapter (chapter: T) = chapter.Attributes.Chapter

    let getFormattedChapterNumber (chapter: T) =
        chapter
        |> getChapter
        |> Option.bind (fun chapter ->
            chapter.ToString("000.###", CultureInfo.InvariantCulture)
            |> Some)
        |> Option.defaultValue "-"

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

    //let getHash (chapter: T) = chapter.Attributes.Hash.ToString("N")

    let getFormattedTitle (chapter: T) =
        [| chapter |> getFormattedVolume
           chapter |> getFormattedChapter
           chapter |> getTitle |]
        |> String.join " - "

    let toString (chapter: T) =
        $"%s{chapter |> getFormattedTitle}[%s{chapter |> getTranslatedLanguage}]"

module ChapterDownloadInfo =
    type T = ChapterDownloadInfo

    open Preferences

    let getBaseUrl (info: T) = info.BaseUrl

    let getChapterHash (info: T) = info.Chapter.Hash |> stringf "n"

    let getPages quality (info: T) =
        match quality with
        | Quality.High -> info.Chapter.Data
        | Quality.Low -> info.Chapter.DataSaver
