namespace Mangadexter.Core

open System

[<AutoOpen>]
module Domain =
    type Status =
        | Ongoing
        | Completed
        | Canceled

    type Id = Id of Guid
    type Title = Title of string
    type Description = Description of string
    type Cover = Cover of Uri
    type Tag = Tag of string

    type ChapterNumber = ChapterNumber of decimal
    type VolumeNumber = VolumeNumber of uint

    type AuthorName = AuthorName of string

    type Author =
        | Artist of AuthorName
        | Writer of AuthorName

    [<StructuredFormatDisplay("{Title}")>]
    type Manga =
        { Id: Id
          Title: Title
          Description: Description
          Cover: Cover option
          Status: Status
          Year: uint option
          Authors: Author list
          Tags: Tag list
          LatestChapter: ChapterNumber option
          TotalVolumes: uint option }

    type Language =
        | English
        | Japanese

    [<StructuredFormatDisplay("{TranslatedLanguage}")>]
    type Translation =
        { Tranlsator: Translator option
          TranslatedLanguage: Language }

    and Translator = { Id: Id; Name: string }

    [<StructuredFormatDisplay("Vol. {Volume} - Ch. {Number} - {Title} [{Translation}]")>]
    type Chapter =
        { Id: Id
          Number: ChapterNumber option
          Volume: VolumeNumber option
          Title: Title option
          Translation: Translation
          PublishedAt: DateTimeOffset
          TotalPages: uint }

    type ChapterDownload =
        { BaseUrl: Uri
          ChapterHash: string
          Pages: Uri list }

    type Pagination = { Take: uint; Skip: uint; Total: uint }

    type SaveLocation = SaveLocation of string

    type Preferences =
        { PreferredLanguage: Language
          SaveLocation: SaveLocation }
