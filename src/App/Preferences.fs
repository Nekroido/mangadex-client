module Preferences

open System.IO
open FSharp.Configuration

open Utils

[<Literal>]
let HighQuality = "High"

[<Literal>]
let LowQuality = "Low"

[<RequireQualifiedAccess>]
type Quality =
    | High
    | Low

    static member toString x =
        match x with
        | Quality.High -> HighQuality
        | Quality.Low -> LowQuality

    static member fromString x =
        match x with
        | HighQuality -> Quality.High
        | LowQuality -> Quality.Low
        | _ -> failwith $"Unknown quality option {x}"


[<Literal>]
let private EnglishLanguage = "English"

[<Literal>]
let private JapaneseLanguage = "Japanese"

[<RequireQualifiedAccess>]
type Language =
    | English
    | Japanese

    static member toString x =
        match x with
        | Language.English -> EnglishLanguage
        | Language.Japanese -> JapaneseLanguage

    static member fromString x =
        match x with
        | EnglishLanguage -> Language.English
        | JapaneseLanguage -> Language.Japanese
        | _ -> failwith $"Unknown language option {x}"

[<Literal>]
let preferencesFile = "preferences.yaml"

type Preferences = YamlConfig<preferencesFile>

let preferencesPath =
    [| Directory.GetCurrentDirectory()
       preferencesFile |]
    |> Path.Join

let loadPreferences () =
    let preferences = Preferences()

    match File.Exists preferencesPath with
    | true ->
        preferences.Load(preferencesPath)
        preferences
    | false -> preferences // just return defaults

let getSavePath (preferences: Preferences) = preferences.SavePath

let getQuality (preferences: Preferences) =
    preferences.Quality |> Quality.fromString

let getLanguage (preferences: Preferences) =
    preferences.Language |> Language.fromString

let updateSavePath savePath (preferences: Preferences) =
    preferences.SavePath <- savePath
    preferences

let updateLanguage language (preferences: Preferences) =
    preferences.Language <- Language.toString language
    preferences

let updateQuality quality (preferences: Preferences) =
    preferences.Quality <- Quality.toString quality
    preferences

let storePreferences (preferences: Preferences) =
    let path = preferencesPath

    // create directory if necessary
    path
    |> Path.GetDirectoryName
    |> Directory.CreateDirectory
    |> ignore

    preferences.Save(path)
