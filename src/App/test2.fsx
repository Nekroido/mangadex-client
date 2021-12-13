open System.Collections.Generic

type TestChapter = { Volume: string; Name: string }

let chapters =
    [ { Volume = "01"; Name = "001" }
      { Volume = "01"; Name = "002" }
      { Volume = "01"; Name = "003" }
      { Volume = "02"; Name = "004" }
      { Volume = "02"; Name = "005" }
      { Volume = "03"; Name = "006" } ]

let dict = Dictionary<string, TestChapter list>()

chapters
|> Seq.groupBy (fun c -> c.Volume)
|> Seq.sortBy fst
|> Seq.iter (fun (v, c) -> dict.Add(v, c |> List.ofSeq))
