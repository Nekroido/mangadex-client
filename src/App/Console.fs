module Console

open Spectre.Console

module SelectionPrompt =
    let setTitle title (prompt: SelectionPrompt<_>) =
        prompt.Title <- title
        prompt

    let addChoices choices (prompt: SelectionPrompt<_>) = prompt.AddChoices choices

    let withConverter converter (prompt: SelectionPrompt<_>) =
        prompt.Converter <- converter
        prompt

[<RequireQualifiedAccess>]
module Console =
    let ask question = question |> AnsiConsole.Ask<string>

    let prompt prompt = prompt |> AnsiConsole.Prompt

    let echo (text: string) = text |> AnsiConsole.WriteLine
