module Console

open System
open System.Diagnostics
open Spectre.Console
open Spectre.Console.Rendering

module Table =
    let addColumns (columns: string seq) (table: Table) =
        columns |> Seq.iter (table.AddColumn >> ignore)
        table

    let addRow (values: string seq) (table: Table) =
        values |> Array.ofSeq |> table.AddRow |> ignore
        table

    let create columns = Table() |> addColumns columns

module TextPrompt =
    let setDefault value (prompt: TextPrompt<_>) = value |> prompt.DefaultValue

    let create<'a> title defaultValue =
        title |> TextPrompt<'a> |> setDefault defaultValue

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

    let create<'a> title =
        MultiSelectionPrompt<'a>() |> setTitle title

    let addChoiceGroup
        (group: string)
        (choices: _ seq)
        (prompt: MultiSelectionPrompt<_>)
        =
        prompt.AddChoiceGroup(group, choices)

    let withConverter converter (prompt: MultiSelectionPrompt<_>) =
        prompt.Converter <- converter
        prompt

[<RequireQualifiedAccess>]
module Console =
    let clear () = AnsiConsole.Clear()

    let ask question = question |> AnsiConsole.Ask<string>

    let prompt prompt = prompt |> AnsiConsole.Prompt

    let echo (text: string) = text |> AnsiConsole.WriteLine

    let render (something: IRenderable) = something |> AnsiConsole.Write

    let status title asyncExpression =
        AnsiConsole
            .Status()
            .Start(
                title,
                (fun ctx ->
                    try
                        asyncExpression
                        |> Async.RunSynchronously
                        |> Result.Ok
                    with
                    | ex ->
                        $"Exception: {ex.Message}" |> Debug.Fail
                        "An error has ocurred" |> echo
                        ex.Message |> Result.Error)
            )

    let progress (asyncExpressions: Map<string, Async<unit> seq>) =
        AnsiConsole
            .Progress()
            .Columns(
                TaskDescriptionColumn(),
                ProgressBarColumn(),
                PercentageColumn(),
                SpinnerColumn()
            )
            .StartAsync(fun ctx ->
                task {
                    return!
                        asyncExpressions
                        |> Seq.map
                            (fun (KeyValue (title, expressions)) ->
                                let task = title |> ctx.AddTask
                                // max value is equal to the total number of async expressions
                                task.MaxValue <- asyncExpressions |> Seq.length |> float

                                expressions
                                |> Seq.map
                                    (fun expr ->
                                        async {
                                            let! result = expr
                                            do 1 |> task.Increment
                                            return result
                                        })
                                // grouping expressions
                                |> Async.Sequential
                                |> Async.Ignore)
                        // grouping expressions of expressions
                        |> Async.Sequential
                        |> Async.Ignore
                })
        |> Async.AwaitTask
