module Rocksmith2014.Conversion.Tests.Main

open Expecto

[<EntryPoint>]
let main argv =
    Tests.runTestsInAssembly defaultConfig argv
