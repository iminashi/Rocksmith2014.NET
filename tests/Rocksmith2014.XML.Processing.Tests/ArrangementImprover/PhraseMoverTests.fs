module Rocksmith2014.XML.Processing.Tests.PhraseMoverTests

open Expecto
open Rocksmith2014.XML
open Rocksmith2014.XML.Processing

[<Tests>]
let phraseMoverTests =
    testList "Arrangement Improver (Phrase Mover)" [
        testCase "Can move phrase to next note" <| fun _ ->
            let iter = PhraseIteration(1000, 0)
            let notes = ![ Note(Time = 1200) ]
            let arr =
                InstrumentalArrangement(
                    Phrases = ![ Phrase("mover1", 0uy, PhraseMask.None) ],
                    PhraseIterations = ![ iter ],
                    Levels = ![ Level(Notes = notes) ]
                )

            PhraseMover.improve arr

            Expect.equal iter.Time 1200 "Phrase iteration was moved to correct time"

        testCase "Can move phrase to chord" <| fun _ ->
            let iter = PhraseIteration(1000, 0)
            let notes = ![ Note(Time = 1200) ]
            let chords = ![ Chord(Time = 1600) ]
            let arr =
                InstrumentalArrangement(
                    Phrases = ![ Phrase("mover2", 0uy, PhraseMask.None) ],
                    PhraseIterations = ![ iter ],
                    Levels = ![ Level(Notes = notes, Chords = chords) ]
                )

            PhraseMover.improve arr

            Expect.equal iter.Time 1600 "Phrase iteration was moved to correct time"

        testCase "Can move phrase beyond multiple notes at the same time code" <| fun _ ->
            let iter = PhraseIteration(1000, 0)
            let notes = ![ Note(Time = 1200); Note(String = 1y, Time = 1200); Note(String = 2y, Time = 1200); Note(Time = 2500) ]
            let arr =
                InstrumentalArrangement(
                    Phrases = ![ Phrase("mover2", 0uy, PhraseMask.None) ],
                    PhraseIterations = ![ iter ],
                    Levels = ![ Level(Notes = notes) ]
                )

            PhraseMover.improve arr

            Expect.equal iter.Time 2500 "Phrase iteration was moved to correct time"

        testCase "Can move a phrase on the same time code as a note" <| fun _ ->
            let iter = PhraseIteration(1000, 0)
            let notes = ![ Note(Time = 1000); Note(Time = 7500) ]
            let arr =
                InstrumentalArrangement(
                    Phrases = ![ Phrase("mover2", 0uy, PhraseMask.None) ],
                    PhraseIterations = ![ iter ],
                    Levels = ![ Level(Notes = notes) ]
                )

            PhraseMover.improve arr

            Expect.equal iter.Time 7500 "Phrase iteration was moved to correct time"

        testCase "Section is also moved" <| fun _ ->
            let iter = PhraseIteration(1000, 0)
            let section = Section("", 1000, 1s)
            let notes = ![ Note(Time = 7500) ]
            let arr =
                InstrumentalArrangement(
                    Phrases = ![ Phrase("mover1", 0uy, PhraseMask.None) ],
                    PhraseIterations = ![ iter ],
                    Sections = ![ section ],
                    Levels = ![ Level(Notes = notes) ]
                )

            PhraseMover.improve arr

            Expect.equal section.Time 7500 "Section was moved to correct time"

        testCase "Anchor is also moved" <| fun _ ->
            let notes = ![ Note(Time = 7500) ]
            let anchors = ![ Anchor(Time = 1000) ]
            let arr =
                InstrumentalArrangement(
                    Phrases = ![ Phrase("mover1", 0uy, PhraseMask.None) ],
                    PhraseIterations = ![ PhraseIteration(1000, 0) ],
                    Levels = ![ Level(Notes = notes, Anchors = anchors) ]
                )

            PhraseMover.improve arr

            Expect.hasLength anchors 1 "One anchor exists"
            Expect.exists anchors (fun a -> a.Time = 7500) "Anchor was moved to correct time"

        testCase "Throws an exception when no integer given" <| fun _ ->
            let arr =
                InstrumentalArrangement(
                    Phrases = ![ Phrase("mover", 0uy, PhraseMask.None) ],
                    PhraseIterations = ![ PhraseIteration(1000, 0) ]
                )

            Expect.throwsC
                (fun _ -> PhraseMover.improve arr)
                (fun ex -> Expect.stringContains ex.Message "Unable to parse" "Correct exception was thrown")
    ]
