module Rocksmith2014.XML.Processing.Tests.ArrangementCheckerTests

open Expecto
open Rocksmith2014.XML
open Rocksmith2014.XML.Processing

let toneChanges = ResizeArray(seq { ToneChange("test", 5555, 1uy) })
let sections = ResizeArray(seq { Section("noguitar", 6000, 1s); Section("riff", 6500, 1s) })
let testArr = InstrumentalArrangement(Sections = sections)
testArr.Tones.Changes <- toneChanges

[<Tests>]
let eventTests =
    testList "Arrangement Checker (Events)" [

        testCase "Detects missing applause end event" <| fun _ ->
            let xml = InstrumentalArrangement()
            xml.Events.Add(Event("E3", 1000))

            let results = ArrangementChecker.checkCrowdEventPlacement xml

            Expect.hasLength results 1 "One message created"
            Expect.stringContains results.[0] "without an end event" "Contains correct message"

        testCase "Can detect unexpected crowd speed event" <| fun _ ->
            let xml = InstrumentalArrangement()
            xml.Events.Add(Event("E3", 1000))
            xml.Events.Add(Event("e2", 2000))
            xml.Events.Add(Event("E13", 5000))

            let results = ArrangementChecker.checkCrowdEventPlacement xml

            Expect.hasLength results 1 "One message created"
            Expect.stringContains results.[0] "Unexpected" "Contains correct message"

        testCase "Can detect unexpected intro applause event" <| fun _ ->
            let xml = InstrumentalArrangement()
            xml.Events.Add(Event("E3", 1000))
            xml.Events.Add(Event("E3", 2000))
            xml.Events.Add(Event("E13", 5000))

            let results = ArrangementChecker.checkCrowdEventPlacement xml

            Expect.hasLength results 1 "One message created"
            Expect.stringContains results.[0] "Unexpected" "Contains correct message"

        testCase "Can detect unexpected outro applause event" <| fun _ ->
            let xml = InstrumentalArrangement()
            xml.Events.Add(Event("E3", 1000))
            xml.Events.Add(Event("D3", 2000))
            xml.Events.Add(Event("E13", 5000))

            let results = ArrangementChecker.checkCrowdEventPlacement xml

            Expect.hasLength results 1 "One message created"
            Expect.stringContains results.[0] "Unexpected" "Contains correct message"

        testCase "Can detect multiple unexpected events" <| fun _ ->
            let xml = InstrumentalArrangement()
            xml.Events.Add(Event("E3", 1000))
            xml.Events.Add(Event("D3", 2000))
            xml.Events.Add(Event("e0", 3000))
            xml.Events.Add(Event("e1", 3000))
            xml.Events.Add(Event("E13", 5000))

            let results = ArrangementChecker.checkCrowdEventPlacement xml

            Expect.hasLength results 3 "Three messages created"
    ]

[<Tests>]
let noteTests =
    testList "Arrangement Checker (Notes)" [
        testCase "Detects unpitched slide note with linknext" <| fun _ ->
            let notes = ResizeArray(seq { Note(IsLinkNext = true, SlideUnpitchTo = 12y) })
            let level = Level(Notes = notes)

            let results = ArrangementChecker.checkNotes testArr level

            Expect.hasLength results 1 "One message created"

        testCase "Detects note with both harmonic and pinch harmonic" <| fun _ ->
            let notes = ResizeArray(seq { Note(IsPinchHarmonic = true, IsHarmonic = true) })
            let level = Level(Notes = notes)

            let results = ArrangementChecker.checkNotes testArr level

            Expect.hasLength results 1 "One message created"

        testCase "Detects notes beyond 23rd fret without ignore status" <| fun _ ->
            let notes = ResizeArray(seq { Note(Fret = 23y); Note(Fret = 24y); Note(Fret = 23y, IsIgnore = true) })
            let level = Level(Notes = notes)

            let results = ArrangementChecker.checkNotes testArr level

            Expect.hasLength results 2 "Two messages created"

        testCase "Detects harmonic note on 7th fret with sustain" <| fun _ ->
            let notes = ResizeArray(seq { Note(Fret = 7y, IsHarmonic = true, Sustain = 200); Note(Fret = 7y, IsHarmonic = true) })
            let level = Level(Notes = notes)

            let results = ArrangementChecker.checkNotes testArr level

            Expect.hasLength results 1 "One message created"

        testCase "Detects note with missing bend values" <| fun _ ->
            let bendValues = ResizeArray(seq { BendValue() })
            let notes = ResizeArray(seq { Note(Fret = 7y, BendValues = bendValues) })
            let level = Level(Notes = notes)

            let results = ArrangementChecker.checkNotes testArr level

            Expect.hasLength results 1 "One message created"

        testCase "Detects tone change that occurs on a note" <| fun _ ->
            let notes = ResizeArray(seq { Note(Time = 5555) })
            let level = Level(Notes = notes)

            let results = ArrangementChecker.checkNotes testArr level

            Expect.hasLength results 1 "One message created"

        testCase "Detects note inside noguitar section" <| fun _ ->
            let notes = ResizeArray(seq { Note(Time = 6000) })
            let level = Level(Notes = notes)

            let results = ArrangementChecker.checkNotes testArr level

            Expect.hasLength results 1 "One message created"
    ]

[<Tests>]
let chordTests =
    testList "Arrangement Checker (Chords)" [
        testCase "Detects chord with inconsistent chord note sustains" <| fun _ ->
            let cn = ResizeArray(seq { Note(Sustain = 200); Note(Sustain = 400) })
            let chords = ResizeArray(seq { Chord(ChordNotes = cn) })
            let level = Level(Chords = chords)

            let results = ArrangementChecker.checkChords testArr level

            Expect.hasLength results 1 "One message created"

        testCase "Detects chord note with linknext and unpitched slide" <| fun _ ->
            let cn = ResizeArray(seq { Note(IsLinkNext = true, SlideUnpitchTo = 10y) })
            let chords = ResizeArray(seq { Chord(ChordNotes = cn) })
            let level = Level(Chords = chords)

            let results = ArrangementChecker.checkChords testArr level

            Expect.hasLength results 1 "One message created"

        testCase "Detects chord note with both harmonic and pinch harmonic" <| fun _ ->
            let cn = ResizeArray(seq { Note(IsHarmonic = true, IsPinchHarmonic = true) })
            let chords = ResizeArray(seq { Chord(ChordNotes = cn) })
            let level = Level(Chords = chords)

            let results = ArrangementChecker.checkChords testArr level

            Expect.hasLength results 1 "One message created"

        testCase "Detects chord beyond 23rd fret without ignore" <| fun _ ->
            let cn = ResizeArray(seq { Note(Fret = 23y); Note(Fret = 24y) })
            let chords = ResizeArray(seq { Chord(ChordNotes = cn) })
            let level = Level(Chords = chords)

            let results = ArrangementChecker.checkChords testArr level

            Expect.hasLength results 1 "One message created"

        testCase "Detects harmonic chord note on 7th fret with sustain" <| fun _ ->
            let cn = ResizeArray(seq { Note(Fret = 7y, Sustain = 200, IsHarmonic = true) })
            let chords = ResizeArray(seq { Chord(ChordNotes = cn) })
            let level = Level(Chords = chords)

            let results = ArrangementChecker.checkChords testArr level

            Expect.hasLength results 1 "One message created"

        testCase "Detects tone change that occurs on a chord" <| fun _ ->
            let cn = ResizeArray(seq { Note() })
            let chords = ResizeArray(seq { Chord(ChordNotes = cn, Time = 5555) })
            let level = Level(Chords = chords)

            let results = ArrangementChecker.checkChords testArr level

            Expect.hasLength results 1 "One message created"

        testCase "Detects chord at the end of handshape" <| fun _ ->
            let hs = ResizeArray(seq { HandShape(1s, 6500, 7000) })
            let chords = ResizeArray(seq { Chord(ChordId = 1s, Time = 7000) })
            let level = Level(Chords = chords, HandShapes = hs)

            let results = ArrangementChecker.checkChords testArr level

            Expect.hasLength results 1 "One message created"

        testCase "Detects chord inside noguitar section" <| fun _ ->
            let chords = ResizeArray(seq { Chord(Time = 6100) })
            let level = Level(Chords = chords)

            let results = ArrangementChecker.checkChords testArr level

            Expect.hasLength results 1 "One message created"
    ]