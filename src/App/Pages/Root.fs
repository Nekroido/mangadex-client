module Pages.Root

open Console
open Utils

[<Literal>]
let SearchMangaLabel = "Search manga"

[<Literal>]
let ExitLabel = "Exit"

[<RequireQualifiedAccess>]
type Action =
    | SearchManga
    | Exit

    static member toString x =
        match x with
        | Action.SearchManga -> SearchMangaLabel
        | Action.Exit -> ExitLabel

    static member fromString x =
        match x with
        | SearchMangaLabel -> Action.SearchManga
        | ExitLabel -> Action.Exit
        | _ -> failwith $"Unknown action {x}"

let showActions () =
    Console.clear ()

    MenuPrompt.create<Action>
        "Select action"
        (DiscriminatedUnion.listCases<Action> ())
        Action.toString
    |> Console.prompt

let rec handleAction action =
    action
    |> function
        | Action.SearchManga -> Search.initialize initialize
        | Action.Exit -> "Exiting..." |> Console.echo

and initialize = showActions >> handleAction
