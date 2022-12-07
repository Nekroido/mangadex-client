module Core.Domain

open System

type Status =
    | Ongoing
    | Finished
    | Canceled
    | Hiatus

type Id = Id of Guid
type Title = Title of string
type Description = Description of string
type Cover = Cover of Uri

type ChapterNumber = ChapterNumber of decimal

type AuthorName = AuthorName of string

type Author =
    | Artist of AuthorName
    | Writer of AuthorName

type Manga =
    { Id: Id
      Title: Title
      Description: Description
      Cover: Cover option
      Status: Status
      Year: uint
      Authors: Author list
      LatestChapter: ChapterNumber option
      TotalChapters: uint
      TotalVolumes: uint }

type Language =
    | English
    | Japanese

type Translation =
    { Tranlsator: Translator
      TranslatedLanguage: Language }

and Translator = { Id: Id; Name: string }

type Chapter =
    { Id: Id
      Number: ChapterNumber option
      Title: Title option
      Description: Description option
      Translation: Translation
      PublishedAt: DateTimeOffset
      TotalPages: uint }
