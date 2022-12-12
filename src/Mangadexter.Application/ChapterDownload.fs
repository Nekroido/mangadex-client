namespace Application.ChapterDownload

module Request =
    open FSharp.Data

    open Mangadexter.Core
    open Mangadexter.Core.Request.ChapterDownloadRequest
    open Application.Common

    [<Literal>]
    let ChapterDownloadSampleUrl =
        BaseUrl
        + "at-home/server/4610197f-9185-4838-8f17-406191547806"

    type ApiChapterDownload = JsonProvider<ChapterDownloadSampleUrl>

    type ApiChapterDownloadInfo = ApiChapterDownload.Root

    module ApiChapterDownloadInfo =
        type t' = ApiChapterDownloadInfo

        let getBaseUrl (info: t') = info.BaseUrl

        let getChapterHash (info: t') = info.Chapter.Hash |> String.format "n"

        let getPages (info: t') = info.Chapter.Data

        let getPageDownloadUrl (info: t') page =
            sprintf "%s/data/%s/%s" (info |> getBaseUrl) (info |> getChapterHash) page

    let getChapterDownloadInformation
        (args: FetchChapterDownloadInfoArgs)
        : Async<Result<ChapterDownload, exn>> =
        async {
            try
                let! result =
                    (sprintf "at-home/server/%s" (args.Chapter.Id |> Id.toString), [])
                    ||> makeRequestUrl
                    |> ApiChapterDownload.AsyncLoad

                return
                    { BaseUrl =
                        result
                        |> ApiChapterDownloadInfo.getBaseUrl
                        |> System.Uri
                      ChapterHash = result |> ApiChapterDownloadInfo.getChapterHash
                      Pages =
                        result
                        |> ApiChapterDownloadInfo.getPages
                        |> List.ofSeq
                        |> List.map (
                            ApiChapterDownloadInfo.getPageDownloadUrl result
                            >> System.Uri
                        ) }
                    |> Ok
            with
            | ex -> return ex |> Error
        }
