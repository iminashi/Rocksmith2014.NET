module PhraseGeneratorTests

open Expecto
open Rocksmith2014.XML
open Rocksmith2014.XML.Processing

let beats = ResizeArray.init 15 (fun i -> Ebeat((i + 1) * 1000, int16 i))

let createBaseArrangement() =
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

        testCase "Does not create a phrase in the middle of handshape" <| fun _ ->
            let arr = createBaseArrangement()
            arr.Levels.[0].Notes.Add(Note(Time = 2_000))
            arr.Levels.[0].Anchors.Add(Anchor(1y, 2_000))
            arr.Levels.[0].Chords.Add(Chord(Time = 8_500))
            arr.Levels.[0].HandShapes.Add(HandShape(StartTime = 8_500, EndTime = 10_800))

            PhraseGenerator.generate arr

            Expect.exists arr.PhraseIterations (fun x -> x.Time = 8500) "Phrase was created at the handshape start time"
            Expect.exists arr.Sections (fun x -> x.Time = 8500) "Section was created at the handshape start time"

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
            arr.Levels.[0].Notes.Add(Note(Time = 10_000, Sustain = 500))

            PhraseGenerator.generate arr

            Expect.exists arr.PhraseIterations (fun x -> x.Time = 9500) "Phrase was created at the note time"
            Expect.exists arr.Sections (fun x -> x.Time = 9500) "Section was created at the note time"

        testCase "Does not create a phrase that breaks chord link-next" <| fun _ ->
            let arr = createBaseArrangement()
            arr.Levels.[0].Notes.Add(Note(Time = 2_000))
            arr.Levels.[0].Anchors.Add(Anchor(1y, 2_000))
            let cn = ResizeArray(seq { Note(Time = 9_500, Sustain = 500, IsLinkNext = true) })
            arr.Levels.[0].Chords.Add(Chord(Time = 9_500, ChordNotes = cn, IsLinkNext = true))
            arr.Levels.[0].Notes.Add(Note(Time = 10_000, Sustain = 500))
            arr.Levels.[0].HandShapes.Add(HandShape(StartTime = 9_500, EndTime = 9_800))

            PhraseGenerator.generate arr

            Expect.exists arr.PhraseIterations (fun x -> x.Time = 9500) "Phrase was created at the chord time"
            Expect.exists arr.Sections (fun x -> x.Time = 9500) "Section was created at the chord time"
   ]
