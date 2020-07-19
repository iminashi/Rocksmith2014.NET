module Rocksmith2014.SNG.Tests.Main

open Expecto

[<EntryPoint>]
let main argv =
    Tests.runTestsInAssembly defaultConfig argv
