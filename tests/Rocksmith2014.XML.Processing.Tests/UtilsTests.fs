module UtilsTests

open Expecto
open Rocksmith2014.XML.Processing
open Rocksmith2014.XML

[<Tests>]
let tests =
    testList "Utility tests" [
        testCase "getFirstNoteTime does not fail when there are no phrase iterations" <| fun _ ->
            let arr = InstrumentalArrangement()
            let notes = ![ Note(Time = 4000) ]
            arr.Levels <- ![ Level(0y); Level(1y, Notes = notes) ]

            let time = Utils.getFirstNoteTime arr

            Expect.equal time (Some 4000) "First note time is correct"

        testCase "getFirstNoteTime finds first note when there are DD levels" <| fun _ ->
            let phrases = ![ Phrase("default", 0uy, PhraseMask.None); Phrase("riff", 1uy, PhraseMask.None) ]
            let phraseIterations = ![ PhraseIteration(0, 0); PhraseIteration(5000, 1) ]
            let arr = InstrumentalArrangement(Phrases = phrases, PhraseIterations = phraseIterations)
            let notes = ![ Note(Time = 5400) ]
            let chords = ![ Chord(Time = 5000) ]
            arr.Levels <- ![ Level(0y); Level(1y, Notes = notes, Chords = chords)  ]

            let time = Utils.getFirstNoteTime arr

            Expect.equal time (Some 5000) "First note time is correct"

        testCase "findPreviousChordUsingSameString finds a chord using the seventh string" <| fun _ ->
            let templates = ![
                ChordTemplate("", "", Array.replicate 7 -1y, [| 2y; -1y; -1y; -1y; -1y; -1y; 4y |])
            ]

            let chords = ![ Chord(Time = 1000, ChordId = 0s) ]
            let result = Utils.findPreviousChordUsingSameString templates chords 6y 2000

            match result with
            | Some (chord, fret) ->
                Expect.equal chord.Time 1000 "Chord is found"
                Expect.equal fret 4y "Template fret of the seventh string is used"
            | None ->
                failwith "Chord was not found"

        testCase "findPreviousChordUsingSameString does not fail on six-string templates" <| fun _ ->
            let templates = ![
                ChordTemplate("", "", Array.replicate 6 -1y, [| 2y; 2y; -1y; -1y; -1y; -1y |])
            ]

            let chords = ![ Chord(Time = 1000, ChordId = 0s) ]
            let result = Utils.findPreviousChordUsingSameString templates chords 6y 2000

            Expect.isNone result "A six-string template does not use string 6"
    ]
