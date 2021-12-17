module Strings

type Strings() =
    static let strings =
        System.Resources.ResourceManager("App.Strings", System.Reflection.Assembly.GetCallingAssembly())

    static member GetString key = key |> strings.GetString
