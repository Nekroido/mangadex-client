namespace Mangadexter.Application

open FSharp.Configuration

open Mangadexter.Core
open System.IO

[<RequireQualifiedAccess>]
module Preferences =
    [<Literal>]
    let preferencesFilePath = "preferences.yaml"

    type PreferencesFile = YamlConfig<preferencesFilePath>

    module PreferencesFile =
        let toDomain (x: PreferencesFile) : Preferences =
            { PreferredLanguage = x.PreferredLanguage |> Language.fromStringLiteral
              SaveLocation = x.SaveLocation |> SaveLocation.make }

    let updatePreferences (preferences: Preferences) : Async<Result<unit, exn>> =
        async {
            try
                // create directory if necessary
                Directory.createForPath preferencesFilePath

                let preferencesFile = PreferencesFile()

                preferencesFile.PreferredLanguage <-
                    Language.toStringLiteral preferences.PreferredLanguage

                preferencesFile.SaveLocation <-
                    SaveLocation.getValue preferences.SaveLocation

                preferencesFile.Save(preferencesFilePath)

                return Ok()
            with
            | ex -> return Error ex
        }

    let loadPreferences () : Async<Result<Preferences, exn>> =
        async {
            try
                let file = PreferencesFile()

                match File.Exists preferencesFilePath with
                | false ->
                    do!
                        file
                        |> PreferencesFile.toDomain
                        |> updatePreferences
                        |> Async.Ignore
                | true -> ()

                file.Load(preferencesFilePath)

                return file |> PreferencesFile.toDomain |> Ok
            with
            | ex -> return Error ex
        }
