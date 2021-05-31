module UtilsTests

open Expecto
open Rocksmith2014.XML.Processing
open Rocksmith2014.XML

[<Tests>]
let tests =
    testList "Utility tests" [
        testCase "getFirstNoteTime does not fail when there are no phrase iterations" <| fun _ ->
            let arr = InstrumentalArrangement()
            let notes = ResizeArray(seq { Note(Time = 4000) })
            arr.Levels <- ResizeArray(seq { Level(0y); Level(1y, Notes = notes)  })

            let time = Utils.getFirstNoteTime arr

            Expect.equal time 4000 "First note time is correct"
    ]
