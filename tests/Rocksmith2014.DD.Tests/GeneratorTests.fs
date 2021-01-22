module Rocksmith2014.DD.Tests.GeneratorTests

open Expecto
open Rocksmith2014.XML
open Rocksmith2014.DD

let config = { PhraseSearch = WithThreshold 85 }

[<Tests>]
let ddGeneratorTests =
    testList "DD Generator Tests" [
        testCase "Creates difficulty levels" <| fun _ ->
            let notes = ResizeArray(seq { Note() })
            let level = Level(0y, Notes = notes)
            let levels = ResizeArray(seq { level })
            let phrases = ResizeArray(seq { Phrase() })
            let iterations = ResizeArray(seq { PhraseIteration(0, 0) })
            let arr = InstrumentalArrangement(Levels = levels, Phrases = phrases, PhraseIterations = iterations)
            arr.MetaData.SongLength <- 1000
        
            Generator.generateForArrangement config arr |> ignore
        
            Expect.isTrue (arr.Levels.Count > 1) "Levels were generated"
    ]
