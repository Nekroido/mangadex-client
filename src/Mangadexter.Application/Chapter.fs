module Application.Chapter

module Request =
   open FSharp.Data

   open Mangadexter.Core
   open Mangadexter.Core.Request.ChapterRequest
   open Application.Common

   [<Literal>]
   let Endpoint = "chapter"

   [<Literal>]
   let ChapterListSampleUrl =
      BaseUrl
      + Endpoint
      + "?manga=80422e14-b9ad-4fda-970f-de370d5fa4e5&translatedLanguage[]=en&includes[]=scanlation_group&limit=50"

   type ApiChapterList =
      JsonProvider<"chapters-sample.json", EmbeddedResource="Mangadexter.Application, Mangadexter.Application.chapters-sample.json">

   type ApiChapter = ApiChapterList.Datum

   [<RequireQualifiedAccess>]
   module ApiChapter =
      let getId (chapter: ApiChapter) = chapter.Id

      let getChapterNumber (chapter: ApiChapter) = chapter.Attributes.Chapter

      let getVolumeNumber (chapter: ApiChapter) = chapter.Attributes.Volume

      let getTitle (chapter: ApiChapter) = chapter.Attributes.Title

      let getTranslator (chapter: ApiChapter) =
         chapter.Relationships
         |> Seq.tryFind (fun x -> x.Type = "scanlation_group")
         |> Option.bind (fun x ->
            x.Attributes
            |> Option.map (fun translator -> x.Id, translator.Name))

      let getTranslatedLanguage (chapter: ApiChapter) =
         chapter.Attributes.TranslatedLanguage

      let getPublishedAt (chapter: ApiChapter) = chapter.Attributes.PublishAt

      let getTotalPages (chapter: ApiChapter) = chapter.Attributes.Pages

      let toDomain (chapter: ApiChapter) : Chapter =
         let translator =
            chapter
            |> getTranslator
            |> Option.map (fun (id, name) -> { Id = id |> Id.make; Name = name })

         { Id = chapter |> getId |> Id.make
           Number =
            chapter
            |> getChapterNumber
            |> Option.map ChapterNumber.make
           Volume =
            chapter
            |> getVolumeNumber
            |> Option.map (uint >> VolumeNumber.make)
           Title = chapter |> getTitle |> Option.map Title.make
           Translation =
            { Tranlsator = translator
              TranslatedLanguage =
               chapter
               |> getTranslatedLanguage
               |> Language.fromStringLiteral }
           PublishedAt = chapter |> getPublishedAt
           TotalPages = chapter |> getTotalPages |> uint }

   let getMangaChapters
      (mangaId: string)
      (preferredLanguage: string option)
      (limit: int option)
      (offset: int option)
      =
      seq {
         yield ("manga", $"%s{mangaId}")
         yield ("order[chapter]", "asc")
         yield ("includes[]", "scanlation_group")
         yield ("limit", limit |> Option.defaultValue 50 |> sprintf "%d")
         yield ("offset", offset |> Option.defaultValue 0 |> sprintf "%d")

         match preferredLanguage with
         | Some lang -> yield ("translatedLanguage[]", lang)
         | None -> ()
      }
      |> makeRequestUrl Endpoint
      |> ApiChapterList.AsyncLoad

   let listChapters (args: ListChaptersArgs) : Async<Result<ChapterList, exn>> =
      async {
         try
            let! result =
               getMangaChapters
                  (args.MangaId |> Id.toString)
                  (args.PreferredLanguage
                   |> Option.map Language.toStringLiteral)
                  (args.Take |> int |> Some)
                  (args.Skip |> int |> Some)

            return
               { Items =
                  result.Data
                  |> Seq.map ApiChapter.toDomain
                  |> List.ofSeq
                 Pagination =
                  { Take = result.Limit |> uint
                    Skip = result.Offset |> uint
                    Total = result.Total |> uint } }
               |> Ok
         with
         | ex -> return ex |> Error
      }
