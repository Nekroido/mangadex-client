namespace Mangadexter.Cli.Page

open Elmish
open Mangadexter.Cli

module Loading =
    type State = { message: string }

    let init = { message = "Loading..." }, Cmd.none

    let view (state: State) _ =
        Console.clear ()
        Console.echo state.message
