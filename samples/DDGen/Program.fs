open System
open Rocksmith2014.DD

[<EntryPoint>]
let main argv =
    match argv with
    | [| fn |] -> Generator.generateForFile { PhraseSearch = WithThreshold 90 } fn $"{fn}.dd.xml"
    | _ -> ()

    0
