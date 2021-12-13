open App

[<EntryPoint>]
let main _ =
    let app =
        application {
            quality Quality.High
            language Language.English
        }

    run app
