open System
open Rocksmith2014.DD

[<EntryPoint>]
let main argv =
    match argv with
    | [| fn |] -> Generator.generateForFile fn $"{fn}.dd.xml"
    | _ -> ()

    0 
