module Pages.Preferences

open Console
open Utils

[<Literal>]
let SavePathLabel = "Save path"

[<Literal>]
let LanguageLabel = "Language"

[<Literal>]
let QualityLabel = "Quality"

[<Literal>]
let ReturnLabel = "← Return"

[<RequireQualifiedAccess>]
type Action =
    | SavePath
    | Language
    | Quality
    | Return

    static member toString x =
        match x with
        | Action.SavePath -> SavePathLabel
        | Action.Language -> LanguageLabel
        | Action.Quality -> QualityLabel
        | Action.Return -> ReturnLabel

    static member fromString x =
        match x with
        | SavePathLabel -> Action.SavePath
        | LanguageLabel -> Action.Language
        | QualityLabel -> Action.Quality
        | ReturnLabel -> Action.Return
        | _ -> failwith $"Unknown action {x}"

let askForSavePath (defaultPath: string) =
    TextPrompt.create "Provide save path" defaultPath
    |> Console.prompt

let askForQuality (defaultQuality: Preferences.Quality) =
    MenuPrompt.create<Preferences.Quality>
        "Select preferred quality"
        (DiscriminatedUnion.listCases<Preferences.Quality> ())
        Preferences.Quality.toString
    |> Console.prompt

let askForLanguage (defaultQuality: Preferences.Language) =
    MenuPrompt.create<Preferences.Language>
        "Select preferred language"
        (DiscriminatedUnion.listCases<Preferences.Language> ())
        Preferences.Language.toString
    |> Console.prompt

let updateSavePath savePath = Preferences.updateSavePath savePath

let updateQuality quality = Preferences.updateQuality quality

let updateLanguage language = Preferences.updateLanguage language

let getCurrentPreferences = Preferences.loadPreferences

let renderPreferencesTable (preferences: Preferences.Preferences) =
    Table.create [ "Save path"
                   "Image quality"
                   "Language" ]
    |> Table.addRow [ preferences.SavePath
                      preferences.Quality
                      preferences.Language ]

let showActions () =
    Console.clear ()

    "Current preferences:" |> Console.echo

    getCurrentPreferences ()
    |> renderPreferencesTable
    |> Console.render

    MenuPrompt.create<Action>
        "Select a setting you wish to update"
        (DiscriminatedUnion.listCases<Action> ())
        Action.toString
    |> Console.prompt

let rec handleAction returnAction action =
    let refresh () = initialize returnAction
    let currentPreferences = getCurrentPreferences ()

    action
    |> function
        | Action.SavePath ->
            currentPreferences.SavePath
            |> askForSavePath
            |> updateSavePath
            <| currentPreferences
            |> Preferences.storePreferences
            |> refresh
        | Action.Quality ->
            currentPreferences
            |> Preferences.getQuality
            |> askForQuality
            |> updateQuality
            <| currentPreferences
            |> Preferences.storePreferences
            |> refresh
        | Action.Language ->
            currentPreferences
            |> Preferences.getLanguage
            |> askForLanguage
            |> updateLanguage
            <| currentPreferences
            |> Preferences.storePreferences
            |> refresh
        | _ -> returnAction ()

and initialize returnAction =
    () |> (showActions >> handleAction returnAction)
