module Application.Manga

module Request =
    open FSharp.Data
    open System

    open Mangadexter.Core
    open Mangadexter.Core.Request.MangaRequest
    open Common

    [<Literal>]
    let Endpoint = "manga"

    [<Literal>]
    let MangaListSampleUrl =
        BaseUrl
        + Endpoint
        + "?title=made&includes[]=author&includes[]=artist&includes[]=cover_art&hasAvailableChapters=true&limit=50"

    let coverUrl = sprintf "https://uploads.mangadex.org/covers/%s/%s"

    type ApiMangaList = JsonProvider<MangaListSampleUrl>
    type ApiManga = ApiMangaList.Datum

    [<RequireQualifiedAccess>]
    module ApiManga =
        type t' = ApiManga

        let getId (manga: t') : Guid = manga.Id

        let getTitle (manga: t') : string option = manga.Attributes.Title.En

        let getDescription (manga: t') : string option = manga.Attributes.Description.En

        let getCoverUrl (manga: t') : Uri option =
            manga.Relationships
            |> Seq.tryFind (fun x -> x.Type = "cover_art")
            |> Option.bind (fun x ->
                x.Attributes
                |> Option.bind (fun attrs -> attrs.FileName))
            |> Option.map (fun filename ->
                coverUrl (manga.Id |> String.format "D") filename
                |> Uri)

        let getStatus (manga: t') : Status =
            match manga.Attributes.Status with
            | "ongoing" -> Status.Ongoing
            | "completed" -> Status.Completed
            | "hiatus"
            | "cancelled" -> Status.Canceled
            | x -> failwith $"Unsupported status {x}!"

        let getYear (manga: t') : int option = manga.Attributes.Year

        let getLatestChapter (manga: t') = manga.Attributes.LastChapter

        let getTotalVolumes (manga: t') = manga.Attributes.LastVolume

        let getAuthors (manga: t') =
            manga.Relationships
            |> Seq.filter (fun x -> [ "author"; "artist" ] |> Seq.contains x.Type)
            |> Seq.map (fun r ->
                r.Type,
                (r.Attributes
                 |> Option.bind (fun x -> x.Name)
                 |> Option.defaultValue "-"))

        let getTags (manga: t') =
            manga.Attributes.Tags
            |> Seq.map (fun tag -> tag.Attributes.Name.En)

        let toDomain (manga: t') : Manga =
            { Id = manga |> getId |> Id.make
              Title =
                manga
                |> getTitle
                |> Option.defaultValue "-no title-"
                |> Title.make
              Description =
                manga
                |> getDescription
                |> Option.defaultValue "-no description-"
                |> Description.make
              Cover = manga |> getCoverUrl |> Option.map Cover.make
              Status = manga |> getStatus
              Year = manga |> getYear |> Option.map uint
              Authors =
                manga
                |> getAuthors
                |> List.ofSeq
                |> List.map (fun (type', name) ->
                    name
                    |> AuthorName.make
                    |> match type' with
                       | "author" -> Author.Writer
                       | "artist" -> Author.Artist
                       | _ -> failwith $"Unsupported author type '{type'}'!")
              Tags =
                manga
                |> getTags
                |> List.ofSeq
                |> List.map Tag.make
              LatestChapter =
                manga
                |> getLatestChapter
                |> Option.map ChapterNumber.make
              TotalVolumes = manga |> getTotalVolumes |> Option.map uint }

    let listManga
        (preferredLanguage: string option)
        (query: string option)
        (limit: int option)
        (offset: int option)
        =
        let url =
            seq {
                yield ("order[relevance]", "desc")
                yield ("includes[]", "author")
                yield ("includes[]", "artist")
                yield ("includes[]", "cover_art")
                yield ("limit", limit |> Option.defaultValue 50 |> sprintf "%d")
                yield ("offset", offset |> Option.defaultValue 0 |> sprintf "%d")

                match preferredLanguage with
                | Some lang -> yield ("availableTranslatedLanguage[]", lang)
                | None -> ()

                match query with
                | Some title -> yield ("title", title)
                | None -> ()
            }
            |> makeRequestUrl "manga"

        System.Diagnostics.Debug.WriteLine url

        url |> ApiMangaList.AsyncLoad

    let searchManga (args: SearchMangaArgs) : Async<Result<MangaList, exn>> =
        async {
            try
                let! result =
                    listManga
                        (args.PreferredLanguage
                         |> Option.map Language.toStringLiteral)
                        (args.Title |> Option.map Title.getValue)
                        (args.Take |> int |> Some)
                        (args.Skip |> int |> Some)

                return
                    { Items =
                        result.Data
                        |> Seq.map ApiManga.toDomain
                        |> List.ofSeq
                      Pagination =
                        { Total = result.Total |> uint
                          Take = result.Limit |> uint
                          Skip = result.Offset |> uint } }
                    |> Ok
            with
            | ex -> return ex |> Error
        }
