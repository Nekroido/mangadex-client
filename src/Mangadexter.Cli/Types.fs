namespace Mangadexter.Cli

open Mangadexter.Core

type ApplicationAction =
    | Download of DownloadAction
    | Preferences of PreferencesAction

and DownloadAction =
    | Add of Chapter
    | ClearFinished

and PreferencesAction =
    | UpdateSaveLocation of SaveLocation
    | UpdatePreferredLanguage of Language
