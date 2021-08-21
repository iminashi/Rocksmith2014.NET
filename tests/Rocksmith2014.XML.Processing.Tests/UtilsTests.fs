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

            Expect.equal time (Some 4000) "First note time is correct"

        testCase "getFirstNoteTime finds first note when there are DD levels" <| fun _ ->
            let phrases = ResizeArray(seq { Phrase("default", 0uy, PhraseMask.None); Phrase("riff", 1uy, PhraseMask.None) })
            let phraseIterations = ResizeArray(seq { PhraseIteration(0, 0); PhraseIteration(5000, 1) })
            let arr = InstrumentalArrangement(Phrases = phrases, PhraseIterations = phraseIterations)
            let notes = ResizeArray(seq { Note(Time = 5400) })
            let chords = ResizeArray(seq { Chord(Time = 5000) })
            arr.Levels <- ResizeArray(seq { Level(0y); Level(1y, Notes = notes, Chords = chords)  })

            let time = Utils.getFirstNoteTime arr

            Expect.equal time (Some 5000) "First note time is correct"
    ]
