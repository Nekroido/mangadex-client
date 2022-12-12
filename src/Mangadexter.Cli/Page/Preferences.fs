namespace Mangadexter.Cli

open Elmish

open Mangadexter.Core
open Mangadexter.Application

[<RequireQualifiedAccess>]
module Preferences =
    type UpdateOptions =
        | Language
        | SaveLocation

    type State =
        { preferences: Deferred<Preferences>
          errorMessage: string option }

    type Msg =
        | LoadPreferences
        | UpdatePreferences of Preferences
        | DisplayPreferences of AsyncOperationStatus<Result<Preferences, exn>>

    let init =
        { preferences = HasNotStartedYet
          errorMessage = None }

    let loadPreferences =
        async {
            let! result = Preferences.loadPreferences ()

            return result |> Finished |> DisplayPreferences
        }

    let update (msg: Msg) (state: State) =
        match msg with
        | LoadPreferences -> state, Cmd.fromAsync loadPreferences
        | DisplayPreferences Started -> { state with preferences = InProgress }, Cmd.none
        | DisplayPreferences (Finished (Ok preferences)) ->
            { state with preferences = Resolved preferences }, Cmd.none
        | DisplayPreferences (Finished (Error ex)) ->
            { state with errorMessage = Some ex.Message }, Cmd.none
        | _ -> state, Cmd.none

    let displayPreferences (preferences: Preferences) =
        [ [ (Language.getFormatted preferences.PreferredLanguage)
            (SaveLocation.getValue preferences.SaveLocation) ] ]
        |> Console.table [ "Preferred language"
                           "Save location" ]

    let view (state: State) dispatch =
        match state.preferences with
        | HasNotStartedYet -> LoadPreferences |> dispatch
        | InProgress -> Console.echo "Loading..."
        | Resolved preferences -> displayPreferences preferences
