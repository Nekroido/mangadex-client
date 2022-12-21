namespace Mangadexter.Cli

open System
open Spectre.Console
open Spectre.Console.Rendering

open Mangadexter.Core

module Table =
    let addColumns (columns: string seq) (table: Table) =
        columns |> Seq.iter (table.AddColumn >> ignore)
        table

    let addRow (values: string seq) (table: Table) =
        values |> Array.ofSeq |> table.AddRow |> ignore
        table

    let create columns = Table() |> addColumns columns

module SelectionPrompt =
    let setTitle title (prompt: SelectionPrompt<_>) =
        prompt.Title <- title
        prompt

    let create<'a> title = SelectionPrompt<'a>() |> setTitle title

    let addChoices (choices: _ seq) (prompt: SelectionPrompt<_>) =
        prompt.AddChoices choices

    let withConverter converter (prompt: SelectionPrompt<_>) =
        prompt.Converter <- converter
        prompt

module MenuPrompt =
    let create<'a> title options formatter =
        SelectionPrompt.create<'a> title
        |> SelectionPrompt.addChoices options
        |> SelectionPrompt.withConverter (Func<'a, string>(formatter))


module MultiSelectionPrompt =
    let setTitle title (prompt: MultiSelectionPrompt<_>) =
        prompt.Title <- title
        prompt

    let create title =
        MultiSelectionPrompt<string>() |> setTitle title

    let addChoiceGroup
        (group: string)
        (choices: _ seq)
        (prompt: MultiSelectionPrompt<_>)
        =
        prompt.AddChoiceGroup(group, choices)

    let withConverter converter (prompt: MultiSelectionPrompt<_>) =
        prompt.Converter <- converter
        prompt

// --
[<RequireQualifiedAccess>]
module Console =
    let clear () = AnsiConsole.Clear()
    let ask question = question |> AnsiConsole.Ask<string>
    let prompt prompt = prompt |> AnsiConsole.Prompt
    let echo (text: string) = text |> AnsiConsole.WriteLine
    let render (something: IRenderable) = something |> AnsiConsole.Write

    let singleChoice<'a>
        (title: string)
        (options: List<string * 'a>)
        (callback: 'a -> unit)
        =
        MenuPrompt.create<string * 'a> title options fst
        |> prompt
        |> snd
        |> callback

    let multiChoice<'a>
        (title: string)
        (options: List<string * (string * 'a)>)
        (callback: 'a list -> unit)
        =
        (MultiSelectionPrompt.create title, options |> List.groupBy fst)
        ||> List.fold (fun table (group, choices) ->
            table
            |> MultiSelectionPrompt.addChoiceGroup
                group
                (choices |> List.map (snd >> fst))) // (_, (label, _))
        |> prompt
        |> List.ofSeq
        |> List.map (fun selection ->
            options
            |> List.find ((snd >> fst) >> (=) selection) // label = selection
            |> (snd >> snd))
        |> callback

    let table (header: string list) (rows: string list list) =
        (Table.create header, rows)
        ||> Seq.fold (flip Table.addRow)
        |> render

    let progress (actions: (string * (ProgressTask -> Async<unit>)) list) =
        AnsiConsole
            .Progress()
            .StartAsync(fun ctx ->
                task {
                    return!
                        actions
                        |> List.map (fun (label, action) ->
                            async { do! action (label |> ctx.AddTask) })
                        |> Async.Sequential
                        |> Async.Ignore
                })
        |> Async.AwaitTask
        |> Async.RunSynchronously
