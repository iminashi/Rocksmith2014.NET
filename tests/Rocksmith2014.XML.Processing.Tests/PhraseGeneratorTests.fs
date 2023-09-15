module PhraseGeneratorTests

open Expecto
open Rocksmith2014.XML
open Rocksmith2014.XML.Processing

// In the tests the content starts at the second beat, so the second phrase will be created at 10s.

let beats = ResizeArray.init 15 (fun i -> Ebeat((i + 1) * 1000, int16 i))

let createBaseArrangement () =
    let levels = ResizeArray(seq { Level() })
    InstrumentalArrangement(Ebeats = beats, Levels = levels)

[<Tests>]
let tests =
    testList "Phrase/Section Generator Tests" [
        testCase "Creates phrases and sections when one note" <| fun _ ->
            let arr = createBaseArrangement()
            arr.Levels.[0].Notes.Add(Note(Time = 2_000))
            arr.Levels.[0].Anchors.Add(Anchor(1y, 2_000))

            PhraseGenerator.generate arr

            Expect.isNonEmpty arr.Phrases "Phrases were created"
            Expect.isNonEmpty arr.PhraseIterations "Phrase iterations were created"
            Expect.isNonEmpty arr.Sections "Sections were created"

            Expect.equal arr.PhraseIterations.[0].Time 1000 "First phrase is at first beat"
            Expect.equal arr.PhraseIterations.[1].Time 2000 "Second phrase is at first note"
            Expect.equal arr.Sections.[0].Time 2000 "First section is at first note"

        testCase "Does not create a phrase in the middle of a handshape" <| fun _ ->
            let arr = createBaseArrangement()
            arr.Levels.[0].Notes.Add(Note(Time = 2_000))
            arr.Levels.[0].Anchors.Add(Anchor(1y, 2_000))
            arr.Levels.[0].Chords.Add(Chord(Time = 8_500))
            arr.Levels.[0].HandShapes.Add(HandShape(StartTime = 8_500, EndTime = 10_800))

            PhraseGenerator.generate arr

            // The end time is closer to the time where the phrase would be created (10s)
            Expect.exists arr.PhraseIterations (fun x -> x.Time = 10_800) "Phrase was created at the handshape end time"
            Expect.exists arr.Sections (fun x -> x.Time = 10_800) "Section was created at the handshape end time"

        testCase "Does not create a phrase in the middle of note sustain" <| fun _ ->
            let arr = createBaseArrangement()
            arr.Levels.[0].Notes.Add(Note(Time = 2_000))
            arr.Levels.[0].Anchors.Add(Anchor(1y, 2_000))
            arr.Levels.[0].Notes.Add(Note(Time = 9_500, Sustain = 3_000))

            PhraseGenerator.generate arr

            Expect.exists arr.PhraseIterations (fun x -> x.Time = 9500) "Phrase was created at the note time"
            Expect.exists arr.Sections (fun x -> x.Time = 9500) "Section was created at the note time"

        testCase "Does not create a phrase that breaks note link-next" <| fun _ ->
            let arr = createBaseArrangement()
            arr.Levels.[0].Notes.Add(Note(Time = 2_000))
            arr.Levels.[0].Anchors.Add(Anchor(1y, 2_000))
            arr.Levels.[0].Notes.Add(Note(Time = 9_500, Sustain = 500, IsLinkNext = true))
            arr.Levels.[0].Notes.Add(Note(Time = 10_000, Sustain = 1000))

            PhraseGenerator.generate arr

            // Possible good phrase times are 9.5s and 11s
            // 9.5s is closer to 10s
            Expect.equal arr.PhraseIterations[2].Time 9500 "Phrase was created at the note time"
            Expect.equal arr.Sections[1].Time 9500 "Section was created at the note time"

        testCase "Does not create a phrase that breaks note link-next (no sustain on linknext target note)" <| fun _ ->
            let arr = createBaseArrangement()
            arr.Levels.[0].Notes.Add(Note(Time = 2_000))
            arr.Levels.[0].Anchors.Add(Anchor(1y, 2_000))
            arr.Levels.[0].Notes.Add(Note(Time = 9_500, Sustain = 500, IsLinkNext = true))
            arr.Levels.[0].Notes.Add(Note(Time = 10_000, Sustain = 0))

            PhraseGenerator.generate arr

            // Possible good phrase times are 9.5s and "some time after 10s"
            // When the linknext target note has no sustain, the candidate time after the note is note time + 100ms
            Expect.equal arr.PhraseIterations[2].Time 10_100 "Phrase was created at the note time"
            Expect.equal arr.Sections[1].Time 10_100 "Section was created at the note time"

        testCase "Does not create a phrase that breaks note link-next (multiple link-next notes)" <| fun _ ->
            let arr = createBaseArrangement()
            arr.Levels.[0].Notes.Add(Note(Time = 2_000))
            arr.Levels.[0].Anchors.Add(Anchor(1y, 2_000))
            arr.Levels.[0].Notes.Add(Note(Time = 9_200, Sustain = 300, IsLinkNext = true))
            arr.Levels.[0].Notes.Add(Note(Time = 9_500, Sustain = 500, IsLinkNext = true))
            arr.Levels.[0].Notes.Add(Note(Time = 10_000, Sustain = 500))

            PhraseGenerator.generate arr

            // Possible good phrase times are 9.2s and 10.5s
            // 10.5s is closer to 10s
            Expect.equal arr.PhraseIterations[2].Time 10_500 "Phrase was created at proper place"
            Expect.equal arr.Sections[1].Time 10_500 "Section was created at proper place"

        testCase "Does not create a phrase that breaks chord link-next" <| fun _ ->
            let arr = createBaseArrangement()
            arr.ChordTemplates.Add(ChordTemplate("", "", [| 1y; -1y; -1y; -1y; -1y; -1y |], [| 1y; -1y; -1y; -1y; -1y; -1y |]))
            arr.Levels.[0].Notes.Add(Note(Time = 2_000))
            arr.Levels.[0].Anchors.Add(Anchor(1y, 2_000))
            let cn = ResizeArray(seq { Note(Time = 9_500, Sustain = 500, IsLinkNext = true) })
            arr.Levels.[0].Chords.Add(Chord(Time = 9_500, ChordNotes = cn, IsLinkNext = true))
            arr.Levels.[0].Notes.Add(Note(Time = 10_000, Sustain = 700))
            // Note: handshape is shorter than chord note sustain
            arr.Levels.[0].HandShapes.Add(HandShape(StartTime = 9_500, EndTime = 9_800))

            PhraseGenerator.generate arr

            // Possible good phrase times are 9.5s and 10.7s
            // 9.5s is closer to 10s
            Expect.equal arr.PhraseIterations[2].Time 9500 "Phrase was created at the chord time"
            Expect.equal arr.Sections[1].Time 9500 "Section was created at the chord time"

        testCase "Does not create END phrase on the last note" <| fun _ ->
            let arr = createBaseArrangement()
            arr.Levels.[0].Notes.AddRange([ Note(Time = 2_000); Note(Time = 6_000) ])

            PhraseGenerator.generate arr

            let endPharse = arr.PhraseIterations |> Seq.last
            Expect.notEqual endPharse.Time 6_000 "Phrase was not created on the last note"
   ]
