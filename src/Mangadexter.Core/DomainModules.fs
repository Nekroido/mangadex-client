namespace Mangadexter.Core

open System

[<RequireQualifiedAccess>]
module Status =
    let fromString (status: string) =
        match status with
        | "completed" -> Status.Completed
        | "ongoing" -> Status.Ongoing
        | "canceled" -> Status.Canceled
        | _ -> failwith $"Unknown status: '{status}'!"

    let toString (status: Status) =
        match status with
        | Status.Completed -> "completed"
        | Status.Ongoing -> "ongoing"
        | Status.Canceled -> "canceled"

[<RequireQualifiedAccess>]
module Id =
    let make (v: Guid) = v |> Id
    let getValue (Id v) = v
    let toString (Id v) = v |> String.format "D"

[<RequireQualifiedAccess>]
module Title =
    let make (v: string) =
        assert (v |> String.length > 0)
        v |> Title

    let getValue (Title v) = v

[<RequireQualifiedAccess>]
module Description =
    let make (v: string) =
        assert (v |> String.length > 0)

        v |> Description

    let getValue (Description v) = v

[<RequireQualifiedAccess>]
module Cover =
    let make (v: Uri) =
        assert (v.IsAbsoluteUri)

        v |> Cover

    let getValue (Cover v) = v

[<RequireQualifiedAccess>]
module Tag =
    type t' = Tag

    let make (v: string) = v |> Tag
    let getValue (Tag v) = v

[<RequireQualifiedAccess>]
module ChapterNumber =
    type t' = ChapterNumber

    let make (v: decimal) =
        assert (v > 0.m)

        v |> ChapterNumber

    let getValue (ChapterNumber v) = v

    let getFormattedValue (v: t') =
        v |> getValue |> String.format "000.###"

    let getFormatted (v: t') =
        v |> getFormattedValue |> sprintf "Ch. %s"

[<RequireQualifiedAccess>]
module VolumeNumber =
    let make (v: uint) =
        assert (v >= 0u)

        v |> VolumeNumber

    let getValue (VolumeNumber v) = v

    let getFormattedValue (v: VolumeNumber) = v |> getValue |> String.format "00"

    let getFormatted (v: VolumeNumber) =
        v |> getFormattedValue |> sprintf "Vol. %s"

[<RequireQualifiedAccess>]
module Author =
    let getName (author: Author) =
        match author with
        | Author.Artist name
        | Author.Writer name -> name

[<RequireQualifiedAccess>]
module AuthorName =
    let make (v: string) =
        assert (v |> String.length > 0)
        v |> AuthorName

    let getValue (AuthorName v) = v

[<RequireQualifiedAccess>]
module Language =
    let getFormatted (lang: Language) =
        match lang with
        | Language.English -> "English"
        | Language.Japanese -> "日本語"

    let toStringLiteral (lang: Language) =
        match lang with
        | Language.English -> "en"
        | Language.Japanese -> "ja"

    let fromStringLiteral (lang: string) =
        match lang with
        | "en" -> Language.English
        | "ja" -> Language.Japanese
        | _ -> failwith $"Unsupported language: '{lang}'!"

[<RequireQualifiedAccess>]
module Manga =
    type t' = Manga

    let getReadableTitle (manga: t') = manga.Title |> Title.getValue

    let getFormattedAuthors (manga: t') =
        manga.Authors
        |> Seq.map (Author.getName >> AuthorName.getValue)
        |> Seq.distinct
        |> String.join ", "

    let getLastChapterNumber (manga: t') =
        manga.LatestChapter
        |> Option.map ChapterNumber.getFormattedValue
        |> Option.defaultValue "-"

    let getReadableStatus (manga: t') = manga.Status |> Status.toString

[<RequireQualifiedAccess>]
module Chapter =
    type t' = Chapter

    let formatChapter (chapter: t') =
        [ chapter.Title
          |> Option.map (Title.getValue >> sprintf "\"%s\"")
          chapter.Volume
          |> Option.map (
              VolumeNumber.getFormattedValue
              >> sprintf "Vol. %s"
          )
          chapter.Number
          |> Option.map (
              ChapterNumber.getFormattedValue
              >> sprintf "Ch. %s"
          ) ]
        |> Seq.map (Option.defaultValue "")
        |> String.join " - "

[<RequireQualifiedAccess>]
module SaveLocation =
    let make (v: string) = v |> SaveLocation
    let getValue (SaveLocation v) = v
