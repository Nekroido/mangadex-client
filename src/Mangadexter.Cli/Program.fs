open System.Threading
open Elmish

open Mangadexter.Cli.Page

let release = new ManualResetEvent(false)

let update = Root.update
let init _ = Root.init, Cmd.none
let view = Root.view

System.Console.OutputEncoding <- System.Text.Encoding.UTF8

Program.mkProgram init update view
//|> Program.withConsoleTrace
|> Program.run

release.WaitOne() |> ignore
