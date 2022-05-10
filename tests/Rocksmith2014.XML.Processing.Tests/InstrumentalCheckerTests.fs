module InstrumentalCheckerTests

open Expecto
open Rocksmith2014.XML
open Rocksmith2014.XML.Processing.Types
open Rocksmith2014.XML.Processing.InstrumentalChecker
open Rocksmith2014.XML.Processing

let toneChanges =
    ResizeArray(seq { ToneChange("test", 5555, 1uy) })

let sections =
    ResizeArray(
        seq {
            Section("noguitar", 6000, 1s)
            Section("riff", 6500, 1s)
            Section("noguitar", 8000, 2s)
        }
    )

let phrases =
    ResizeArray(seq { Phrase("mover6.700", 0uy, PhraseMask.None) })

let phraseIterations =
    ResizeArray(seq { PhraseIteration(6500, 0) })

let chordTemplates =
    ResizeArray(seq {
        ChordTemplate("", "", fingers = [| 2y; 2y; -1y; -1y; -1y; -1y |], frets = [| 2y; 2y; -1y; -1y; -1y; -1y |])
        // 1st finger not on lowest fret
        // | | 3 | | |
        // | 2 | 1 | |
        ChordTemplate("WEIRDO1", "", fingers = [| -1y; 2y; 3y; 1y; -1y; -1y |], frets = [| -1y; 2y; 1y; 2y; -1y; -1y |])
        // 2nd finger not on lowest fret
        // | | 4 | | |
        // | 2 | 3 | |
        ChordTemplate("WEIRDO2", "", fingers = [| -1y; 2y; 4y; 3y; -1y; -1y |], frets = [| -1y; 2y; 1y; 2y; -1y; -1y |])
        // Chord using thumb, fingering perfectly possible
        // | | 1 1 1 |
        // T | | | | |
        ChordTemplate("THUMB", "", fingers = [| 0y; -1y; 1y; 1y; 1y; -1y |], frets = [| 2y; -1y; 1y; 1y; 1y; -1y |])
        // Chord with impossible barre
        // | | o o | |
        // 1 | o o 1 |
        ChordTemplate("BARRE2", "", fingers = [| 1y; -1y; -1y; -1y; 1y; -1y |], frets = [| 3y; -1y; 0y; 0y; 3y; -1y |])
        // Chord with impossible barre
        // | o | | | |
        // 2 o 2 2 1 |
        ChordTemplate("BARRE2", "", fingers = [| 2y; -1y; 2y; 2y; 1y; -1y |], frets = [| 2y; 0y; 2y; 2y; 2y; -1y |])
    })

let testArr =
    InstrumentalArrangement(
        Sections = sections,
        ChordTemplates = chordTemplates,
        Phrases = phrases,
        PhraseIterations = phraseIterations
    )

testArr.Tones.Changes <- toneChanges
testArr.MetaData.SongLength <- 10000

[<Tests>]
let eventTests =
    testList "Arrangement Checker (Events)" [
        testCase "Detects missing applause end event" <| fun _ ->
            let xml = InstrumentalArrangement()
            xml.Events.Add(Event("E3", 1000))

            let results = checkCrowdEventPlacement xml

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.Type ApplauseEventWithoutEnd "Correct issue type"

        testCase "Detects unexpected crowd speed event" <| fun _ ->
            let xml = InstrumentalArrangement()
            xml.Events.Add(Event("E3", 1000))
            xml.Events.Add(Event("e2", 2000))
            xml.Events.Add(Event("E13", 5000))

            let results = checkCrowdEventPlacement xml

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.Type (EventBetweenIntroApplause "e2") "Correct issue type"

        testCase "Detects unexpected intro applause event" <| fun _ ->
            let xml = InstrumentalArrangement()
            xml.Events.Add(Event("E3", 1000))
            xml.Events.Add(Event("E3", 2000))
            xml.Events.Add(Event("E13", 5000))

            let results = checkCrowdEventPlacement xml

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.Type (EventBetweenIntroApplause "E3") "Correct issue type"

        testCase "Detects unexpected outro applause event" <| fun _ ->
            let xml = InstrumentalArrangement()
            xml.Events.Add(Event("E3", 1000))
            xml.Events.Add(Event("D3", 2000))
            xml.Events.Add(Event("E13", 5000))

            let results = checkCrowdEventPlacement xml

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.Type (EventBetweenIntroApplause "D3") "Correct issue type"

        testCase "Detects multiple unexpected events" <| fun _ ->
            let xml = InstrumentalArrangement()
            xml.Events.Add(Event("E3", 1000))
            xml.Events.Add(Event("D3", 2000))
            xml.Events.Add(Event("e0", 3000))
            xml.Events.Add(Event("e1", 3000))
            xml.Events.Add(Event("E13", 5000))

            let results = checkCrowdEventPlacement xml

            Expect.hasLength results 3 "Three issues created"
    ]

[<Tests>]
let noteTests =
    testList "Arrangement Checker (Notes)" [
        testCase "Detects unpitched slide note with linknext" <| fun _ ->
            let notes = ResizeArray(seq { Note(IsLinkNext = true, SlideUnpitchTo = 12y, Sustain = 100)
                                          Note(Fret = 12y, Time = 100)})
            let level = Level(Notes = notes)

            let results = checkNotes testArr level

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.Type UnpitchedSlideWithLinkNext "Correct issue type"

        testCase "Detects note with both harmonic and pinch harmonic" <| fun _ ->
            let notes = ResizeArray(seq { Note(IsPinchHarmonic = true, IsHarmonic = true) })
            let level = Level(Notes = notes)

            let results = checkNotes testArr level

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.Type DoubleHarmonic "Correct issue type"

        testCase "Detects notes beyond 23rd fret without ignore status" <| fun _ ->
            let notes = ResizeArray(seq { Note(Fret = 23y)
                                          Note(Fret = 24y)
                                          Note(Fret = 23y, IsIgnore = true)
                                          Note(Fret = 24y, IsIgnore = true) })
            let level = Level(Notes = notes)

            let results = checkNotes testArr level

            Expect.hasLength results 2 "Two issues created"
            Expect.equal results.Head.Type MissingIgnore "Correct issue type"

        testCase "Detects harmonic note on 7th fret with sustain" <| fun _ ->
            let notes = ResizeArray(seq { Note(Fret = 7y, IsHarmonic = true, Sustain = 200); Note(Fret = 7y, IsHarmonic = true) })
            let level = Level(Notes = notes)

            let results = checkNotes testArr level

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.Type SeventhFretHarmonicWithSustain "Correct issue type"

        testCase "Ignores harmonic note on 7th fret with sustain when ignore set" <| fun _ ->
            let notes = ResizeArray(seq { Note(Fret = 7y, IsHarmonic = true, Sustain = 200, IsIgnore = true) })
            let level = Level(Notes = notes)

            let results = checkNotes testArr level

            Expect.hasLength results 0 "No issues created"

        testCase "Detects note with missing bend values" <| fun _ ->
            let bendValues = ResizeArray(seq { BendValue() })
            let notes = ResizeArray(seq { Note(Fret = 7y, BendValues = bendValues) })
            let level = Level(Notes = notes)

            let results = checkNotes testArr level

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.Type MissingBendValue "Correct issue type"

        testCase "Detects tone change that occurs on a note" <| fun _ ->
            let notes = ResizeArray(seq { Note(Time = 5555) })
            let level = Level(Notes = notes)

            let results = checkNotes testArr level

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.Type ToneChangeOnNote "Correct issue type"

        testCase "Detects note inside noguitar section" <| fun _ ->
            let notes = ResizeArray(seq { Note(Time = 6000) })
            let level = Level(Notes = notes)

            let results = checkNotes testArr level

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.Type NoteInsideNoguitarSection "Correct issue type"

        testCase "Detects note inside last noguitar section" <| fun _ ->
            let notes = ResizeArray(seq { Note(Time = 9000) })
            let level = Level(Notes = notes)

            let results = checkNotes testArr level

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.Type NoteInsideNoguitarSection "Correct issue type"

        testCase "Detects linknext fret mismatch" <| fun _ ->
            let notes = ResizeArray(seq { Note(Fret = 1y, Time = 1000, IsLinkNext = true, Sustain = 100)
                                          Note(Fret = 5y, Time = 1100) })
            let level = Level(Notes = notes)

            let results = checkNotes testArr level

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.Type LinkNextFretMismatch "Correct issue type"

        testCase "Detects note linked to a chord" <| fun _ ->
            let notes = ResizeArray(seq { Note(Fret = 1y, Time = 1000, IsLinkNext = true, Sustain = 100) })
            let cn = ResizeArray(seq { Note(Fret = 1y, Time = 1100) })
            let chords = ResizeArray(seq { Chord(Time = 1100, ChordNotes = cn) })
            let level = Level(Notes = notes, Chords = chords)

            let results = checkNotes testArr level

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.Type NoteLinkedToChord "Correct issue type"

        testCase "Detects linknext slide fret mismatch" <| fun _ ->
            let notes = ResizeArray(seq { Note(Fret = 1y, Time = 1000, IsLinkNext = true, Sustain = 100, SlideTo = 4y)
                                          Note(Fret = 5y, Time = 1100) })
            let level = Level(Notes = notes)

            let results = checkNotes testArr level

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.Type LinkNextSlideMismatch "Correct issue type"

        testCase "Detects linknext bend value mismatch (1/2)" <| fun _ ->
            let bv1 = ResizeArray(seq { BendValue(1050, 1f) })
            let notes = ResizeArray(seq { Note(Fret = 1y, Time = 1000, IsLinkNext = true, Sustain = 100, BendValues = bv1)
                                          Note(Fret = 1y, Time = 1100) })
            let level = Level(Notes = notes)

            let results = checkNotes testArr level

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.Type LinkNextBendMismatch "Correct issue type"

        testCase "Detects linknext bend value mismatch (2/2)" <| fun _ ->
            let bv1 = ResizeArray(seq { BendValue(1050, 1f) })
            let bv2 = ResizeArray(seq { BendValue(1100, 2f) })
            let notes = ResizeArray(seq { Note(Fret = 1y, Time = 1000, IsLinkNext = true, Sustain = 100, BendValues = bv1)
                                          Note(Fret = 1y, Time = 1100, BendValues = bv2) })
            let level = Level(Notes = notes)

            let results = checkNotes testArr level

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.Type LinkNextBendMismatch "Correct issue type"

        testCase "Does not produce false positive when no bend value at note time" <| fun _ ->
            let bv1 = ResizeArray(seq { BendValue(1000, 1f); BendValue(1050, 0f) })
            let bv2 = ResizeArray(seq { BendValue(1150, 1f) })
            let notes = ResizeArray(seq { Note(Fret = 1y, Time = 1000, IsLinkNext = true, Sustain = 100, BendValues = bv1)
                                          Note(Fret = 1y, Time = 1100, Sustain = 100, BendValues = bv2) })
            let level = Level(Notes = notes)

            let results = checkNotes testArr level

            Expect.hasLength results 0 "No issues created"

        testCase "Detects phrase on linknext note's sustain" <| fun _ ->
            let notes = ResizeArray(seq { Note(Fret = 1y, Time = 1300, IsLinkNext = true, Sustain = 500)
                                          Note(Fret = 1y, Time = 1800, Sustain = 100) })
            let level = Level(Notes = notes)
            let phrases =
                ResizeArray(
                    [ Phrase("default", 0uy, PhraseMask.None)
                      Phrase("first", 0uy, PhraseMask.None)
                      Phrase("bad", 0uy, PhraseMask.None) ]
                 )
            let iterations = ResizeArray(seq {  PhraseIteration(0, 0); PhraseIteration(1000, 1); PhraseIteration(1500, 2) })
            let arr = InstrumentalArrangement(Phrases = phrases, PhraseIterations = iterations, Levels = ResizeArray([ level ]))

            let results = checkNotes arr level

            Expect.hasLength results 1 "One issue was created"
            Expect.equal results.Head.Type PhraseChangeOnLinkNextNote "Correct issue type"

        testCase "Mover phrase on linknext note's sustain is ignored" <| fun _ ->
            let notes = ResizeArray(seq { Note(Fret = 1y, Time = 1300, IsLinkNext = true, Sustain = 500)
                                          Note(Fret = 1y, Time = 1800, Sustain = 100) })
            let level = Level(Notes = notes)
            let phrases =
                ResizeArray(
                    [ Phrase("default", 0uy, PhraseMask.None)
                      Phrase("first", 0uy, PhraseMask.None)
                      Phrase("mover1", 0uy, PhraseMask.None) ]
                 )
            let iterations = ResizeArray(seq {  PhraseIteration(0, 0); PhraseIteration(1000, 1); PhraseIteration(1500, 2) })
            let arr = InstrumentalArrangement(Phrases = phrases, PhraseIterations = iterations, Levels = ResizeArray([ level ]))

            let results = checkNotes arr level

            Expect.hasLength results 0 "No issues created"
    ]

[<Tests>]
let chordTests =
    testList "Arrangement Checker (Chords)" [
        // DISABLED
        ptestCase "Detects chord with inconsistent chord note sustains" <| fun _ ->
            let cn = ResizeArray(seq { Note(Sustain = 200); Note(Sustain = 400) })
            let chords = ResizeArray(seq { Chord(ChordNotes = cn) })
            let level = Level(Chords = chords)

            let results = checkChords testArr level

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.Type VaryingChordNoteSustains "Correct issue type"

        testCase "Detects chord note with linknext and unpitched slide" <| fun _ ->
            let cn = ResizeArray(seq { Note(IsLinkNext = true, SlideUnpitchTo = 10y, Sustain = 100) })
            let chords = ResizeArray(seq { Chord(ChordNotes = cn) })
            let level = Level(Chords = chords)

            let results = checkChords testArr level

            Expect.hasLength results 2 "One issue created"
            Expect.exists results (fun x -> x.Type = UnpitchedSlideWithLinkNext) "Correct first issue type"
            Expect.exists results (fun x -> x.Type = LinkNextMissingTargetNote) "Correct second issue type"

        testCase "Detects chord note with both harmonic and pinch harmonic" <| fun _ ->
            let cn = ResizeArray(seq { Note(IsHarmonic = true, IsPinchHarmonic = true) })
            let chords = ResizeArray(seq { Chord(ChordNotes = cn) })
            let level = Level(Chords = chords)

            let results = checkChords testArr level

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.Type DoubleHarmonic "Correct issue type"

        testCase "Detects chord beyond 23rd fret without ignore" <| fun _ ->
            let cn = ResizeArray(seq { Note(Fret = 23y); Note(Fret = 24y) })
            let chords = ResizeArray(seq { Chord(ChordNotes = cn) })
            let level = Level(Chords = chords)

            let results = checkChords testArr level

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.Type MissingIgnore "Correct issue type"

        testCase "Detects harmonic chord note on 7th fret with sustain" <| fun _ ->
            let cn = ResizeArray(seq { Note(Fret = 7y, Sustain = 200, IsHarmonic = true) })
            let chords = ResizeArray(seq { Chord(ChordNotes = cn) })
            let level = Level(Chords = chords)

            let results = checkChords testArr level

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.Type SeventhFretHarmonicWithSustain "Correct issue type"

        testCase "Detects tone change that occurs on a chord" <| fun _ ->
            let cn = ResizeArray(seq { Note() })
            let chords = ResizeArray(seq { Chord(ChordNotes = cn, Time = 5555) })
            let level = Level(Chords = chords)

            let results = checkChords testArr level

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.Type ToneChangeOnNote "Correct issue type"

        testCase "Detects chord at the end of handshape" <| fun _ ->
            let hs = ResizeArray(seq { HandShape(1s, 6500, 7000) })
            let chords = ResizeArray(seq { Chord(ChordId = 1s, Time = 7000) })
            let level = Level(Chords = chords, HandShapes = hs)

            let results = checkChords testArr level

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.Type ChordAtEndOfHandShape "Correct issue type"

        testCase "Detects chord inside noguitar section" <| fun _ ->
            let chords = ResizeArray(seq { Chord(Time = 6100) })
            let level = Level(Chords = chords)

            let results = checkChords testArr level

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.Type NoteInsideNoguitarSection "Correct issue type"

        testCase "Detects chord note linknext slide fret mismatch" <| fun _ ->
            let cn = ResizeArray(seq { Note(Time = 1000, Sustain = 100, IsLinkNext = true, Fret = 1y, SlideTo = 3y) })
            let chords = ResizeArray(seq { Chord(Time = 1000, ChordNotes = cn, IsLinkNext = true) })
            let notes = ResizeArray(seq { Note(Time = 1100, Fret = 12y) })
            let level = Level(Chords = chords, Notes = notes)

            let results = checkChords testArr level

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.Type LinkNextSlideMismatch "Correct issue type"

        testCase "Detects chord note linknext bend value mismatch" <| fun _ ->
            let bv = ResizeArray(seq { BendValue(1050, 1f) })
            let cn = ResizeArray(seq { Note(Time = 1000, Sustain = 100, IsLinkNext = true, Fret = 1y, BendValues = bv) })
            let chords = ResizeArray(seq { Chord(Time = 1000, ChordNotes = cn, IsLinkNext = true) })
            let notes = ResizeArray(seq { Note(Time = 1100, Fret = 1y) })
            let level = Level(Chords = chords, Notes = notes)

            let results = checkChords testArr level

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.Type LinkNextBendMismatch "Correct issue type"

        testCase "Detects incorrect linknext on chord note" <| fun _ ->
            let notes = ResizeArray(seq { Note(String = 1y, Time = 1100)
                                          Note(String = 2y, Time = 1500) })
            let cn = ResizeArray(seq { Note(String = 1y, Time = 1000, IsLinkNext = true, Sustain = 100)
                                       Note(String = 2y, Time = 1000, IsLinkNext = true, Sustain = 100) })
            let chords = ResizeArray(seq { Chord(Time = 1000, IsLinkNext = true, ChordNotes = cn) })
            let level = Level(Notes = notes, Chords = chords)

            let results = checkChords testArr level

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.Type IncorrectLinkNext "Correct issue type"

        testCase "Does not produce false positive for chord note without linknext" <| fun _ ->
            let notes = ResizeArray(seq { Note(String = 1y, Time = 1100)
                                          Note(String = 2y, Time = 1500) })
            let cn = ResizeArray(seq { Note(String = 1y, Time = 1000, IsLinkNext = true, Sustain = 100)
                                       Note(String = 2y, Time = 1000, Sustain = 100) })
            let chords = ResizeArray(seq { Chord(Time = 1000, IsLinkNext = true, ChordNotes = cn) })
            let level = Level(Notes = notes, Chords = chords)

            let results = checkChords testArr level

            Expect.hasLength results 0 "No issues created"

        testCase "Detects missing bend value on chord note" <| fun _ ->
            let bendValues = ResizeArray(seq { BendValue() })
            let cn = ResizeArray(seq { Note(String = 1y, Time = 1000, Sustain = 100, BendValues = bendValues)
                                       Note(String = 2y, Time = 1000, Sustain = 100) })
            let chords = ResizeArray(seq { Chord(Time = 1000, ChordNotes = cn) })
            let level = Level(Chords = chords)

            let results = checkChords testArr level

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.Type MissingBendValue "Correct issue type"

        testCase "Detects linknext chord without any chord notes" <| fun _ ->
            let chords = ResizeArray(seq { Chord(Time = 1000, IsLinkNext = true) })
            let level = Level(Chords = chords)

            let results = checkChords testArr level

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.Type MissingLinkNextChordNotes "Correct issue type"

        testCase "Detects linknext chord without linknext chord notes" <| fun _ ->
            let cn = ResizeArray(seq { Note(String = 1y, Time = 1000, Sustain = 100)
                                       Note(String = 2y, Time = 1000, Sustain = 100) })
            let chords = ResizeArray(seq { Chord(Time = 1000, ChordNotes = cn, IsLinkNext = true) })
            let level = Level(Chords = chords)

            let results = checkChords testArr level

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.Type MissingLinkNextChordNotes "Correct issue type"

        testCase "Detects chords with weird fingering" <| fun _ ->
            // Dummy chord notes
            let cn = ResizeArray(seq { Note(String = 1y, Time = 1000, Sustain = 100) })
            let chords =
                ResizeArray(seq {
                    Chord(ChordId = 1s, ChordNotes = cn)
                    Chord(ChordId = 2s, ChordNotes = cn)
                    Chord(ChordId = 3s, ChordNotes = cn)
                })
            let level = Level(Chords = chords)

            let results = checkChords testArr level

            Expect.hasLength results 2 "Two issues created"
            Expect.all results (fun x -> x.Type = PossiblyWrongChordFingering) "Correct issue types"

        testCase "Detects chords with barre over open strings" <| fun _ ->
            // Dummy chord notes
            let cn = ResizeArray(seq { Note(String = 1y, Time = 1000, Sustain = 100) })
            let chords =
                ResizeArray(seq {
                    Chord(ChordId = 4s, ChordNotes = cn)
                    Chord(ChordId = 5s, ChordNotes = cn)
                })
            let level = Level(Chords = chords)

            let results = checkChords testArr level

            Expect.hasLength results 2 "Two issues created"
            Expect.all results (fun x -> x.Type = BarreOverOpenStrings) "Correct issue types"
    ]

[<Tests>]
let handshapeTests =
    testList "Arrangement Checker (Handshapes)" [
        testCase "Detects fingering that does not match anchor position" <| fun _ ->
            let hs = ResizeArray(seq { HandShape(0s, 1000, 1500) })
            let anchors = ResizeArray(seq { Anchor(2y, 500) })
            let level = Level(HandShapes = hs, Anchors = anchors)

            let results = checkHandshapes testArr level

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.Type FingeringAnchorMismatch "Correct issue type"
    ]

[<Tests>]
let anchorTests =
    testList "Arrangement Checker (Anchors)" [
        testCase "Ignores anchor exactly on note" <| fun _ ->
            let anchors = ResizeArray(seq { Anchor(1y, 100) })
            let notes = ResizeArray(seq { Note(Time = 100, Fret = 1y) })
            let level = Level(Notes = notes, Anchors = anchors)

            let results = checkAnchors testArr level

            Expect.hasLength results 0 "No issues created"

        testCase "Detects anchor before note" <| fun _ ->
            let anchors = ResizeArray(seq { Anchor(1y, 99) })
            let notes = ResizeArray(seq { Note(Time = 100, Fret = 1y) })
            let level = Level(Notes = notes, Anchors = anchors)

            let results = checkAnchors testArr level

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.Type (AnchorNotOnNote -1) "Correct issue type"

        testCase "Detects anchor after chord" <| fun _ ->
            let anchors = ResizeArray(seq { Anchor(1y, 102) })
            let chords = ResizeArray(seq { Chord(Time = 100) })
            let level = Level(Chords = chords, Anchors = anchors)

            let results = checkAnchors testArr level

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.Type (AnchorNotOnNote 2) "Correct issue type"

        testCase "Detects anchor inside handshape" <| fun _ ->
            let anchors = ResizeArray(seq { Anchor(1y, 200) })
            let handShapes = ResizeArray(seq { HandShape(StartTime = 100, EndTime = 400) })
            let level = Level(HandShapes = handShapes, Anchors = anchors)

            let results = checkAnchors testArr level

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.Type AnchorInsideHandShape "Correct issue type"

        testCase "No false positive for anchor at the start of handshape" <| fun _ ->
            let anchors = ResizeArray(seq { Anchor(1y, 100) })
            let handShapes = ResizeArray(seq { HandShape(StartTime = 100, EndTime = 400) })
            let level = Level(HandShapes = handShapes, Anchors = anchors)

            let results = checkAnchors testArr level

            Expect.isEmpty results "No issues created"

        testCase "Detects anchor inside handshape at section boundary" <| fun _ ->
            let anchors = ResizeArray(seq { Anchor(1y, 8000) })
            let handShapes = ResizeArray(seq { HandShape(StartTime = 7000, EndTime = 9000) })
            let level = Level(HandShapes = handShapes, Anchors = anchors)

            let results = checkAnchors testArr level

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.Type AnchorInsideHandShapeAtPhraseBoundary "Correct issue type"

        testCase "Ignores anchors on phrases that will be moved (handshape check)" <| fun _ ->
            let anchors = ResizeArray(seq { Anchor(1y, 6500) })
            let handShapes = ResizeArray(seq { HandShape(StartTime = 6000, EndTime = 6550) })
            let level = Level(HandShapes = handShapes, Anchors = anchors)

            let results = checkAnchors testArr level

            Expect.isEmpty results "No issues created"

        testCase "Detects anchor near the end of an unpitched slide" <| fun _ ->
            let anchors = ResizeArray(seq { Anchor(1y, 500) })
            let notes = ResizeArray(seq { Note(Time = 100, Sustain = 397, SlideUnpitchTo = 5y) })
            let level = Level(Notes = notes, Anchors = anchors)

            let results = checkAnchors testArr level

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.Type AnchorCloseToUnpitchedSlide "Correct issue type"

        testCase "Ignores anchors on phrases that will be moved (unpitched slide check)" <| fun _ ->
            let anchors = ResizeArray(seq { Anchor(1y, 6500) })
            let notes = ResizeArray(seq { Note(Time = 6200, Sustain = 300, SlideUnpitchTo = 5y) })
            let level = Level(Notes = notes, Anchors = anchors)

            let results = checkAnchors testArr level

            Expect.isEmpty results "No issues created"
    ]

[<Tests>]
let phraseTests =
    testList "Arrangement Checker (Phrases)" [
        testCase "Detects non-empty first phrase" <| fun _ ->
            let sections = ResizeArray(seq { Section("riff", 1500, 1s); Section("noguitar", 2000, 2s) })
            let phrases = ResizeArray(seq { Phrase("COUNT", 0uy, PhraseMask.None); Phrase("riff", 0uy, PhraseMask.None); Phrase("END", 0uy, PhraseMask.None) })
            let phraseIterations = ResizeArray(seq { PhraseIteration(1000, 0); PhraseIteration(1500, 1); PhraseIteration(2000, 2) })
            let notes = ResizeArray(seq { Note(Time = 1100) })
            let levels = ResizeArray(seq { Level(0y, Notes = notes) })
            let phraseTestArr = InstrumentalArrangement(Sections = sections, Phrases = phrases, PhraseIterations = phraseIterations, Levels = levels)

            let results = checkPhrases phraseTestArr

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.Type FirstPhraseNotEmpty "Correct issue type"

        testCase "Detects missing END phrase" <| fun _ ->
            let phrases = ResizeArray(seq { Phrase("COUNT", 0uy, PhraseMask.None); Phrase("riff", 0uy, PhraseMask.None) })
            let phraseIterations = ResizeArray(seq { PhraseIteration(1000, 0); PhraseIteration(1500, 1) })
            let notes = ResizeArray(seq { Note(Time = 1600) })
            let levels = ResizeArray(seq { Level(0y, Notes = notes) })
            let noEndTestArr = InstrumentalArrangement(Sections = sections, Phrases = phrases, PhraseIterations = phraseIterations, Levels = levels)

            let results = checkPhrases noEndTestArr

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.Type NoEndPhrase "Correct issue type"
    ]

[<Tests>]
let generalTests =
    testList "Arrangement Checker (General)" [
        testCase "Does not throw exceptions when checking an arrangement without notes" <| fun _ ->
            let phrases = ResizeArray(seq { Phrase("A", 0uy, PhraseMask.None); Phrase("END", 0uy, PhraseMask.None) })
            let phraseIterations = ResizeArray(seq { PhraseIteration(500, 0); PhraseIteration(2500, 1) })
            let arr = InstrumentalArrangement(Phrases = phrases, PhraseIterations = phraseIterations)

            let issues = ArrangementChecker.checkInstrumental arr

            Expect.isEmpty issues "No issues were returned"
    ]
