module InstrumentalCheckerTests

open Expecto
open Rocksmith2014.XML
open Rocksmith2014.XML.Processing.Types
open Rocksmith2014.XML.Processing.InstrumentalChecker
open Rocksmith2014.XML.Processing
open TestArrangement

[<Tests>]
let eventTests =
    testList "Arrangement Checker (Events)" [
        testCase "Detects missing applause end event" <| fun _ ->
            let xml = InstrumentalArrangement()
            xml.Events.Add(Event("E3", 1000))

            let results = checkCrowdEventPlacement xml

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.IssueType ApplauseEventWithoutEnd "Correct issue type"

        testCase "Detects unexpected crowd speed event" <| fun _ ->
            let xml = InstrumentalArrangement()
            xml.Events.Add(Event("E3", 1000))
            xml.Events.Add(Event("e2", 2000))
            xml.Events.Add(Event("E13", 5000))

            let results = checkCrowdEventPlacement xml

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.IssueType (EventBetweenIntroApplause "e2") "Correct issue type"

        testCase "Detects unexpected intro applause event" <| fun _ ->
            let xml = InstrumentalArrangement()
            xml.Events.Add(Event("E3", 1000))
            xml.Events.Add(Event("E3", 2000))
            xml.Events.Add(Event("E13", 5000))

            let results = checkCrowdEventPlacement xml

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.IssueType (EventBetweenIntroApplause "E3") "Correct issue type"

        testCase "Detects unexpected outro applause event" <| fun _ ->
            let xml = InstrumentalArrangement()
            xml.Events.Add(Event("E3", 1000))
            xml.Events.Add(Event("D3", 2000))
            xml.Events.Add(Event("E13", 5000))

            let results = checkCrowdEventPlacement xml

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.IssueType (EventBetweenIntroApplause "D3") "Correct issue type"

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
let handshapeTests =
    testList "Arrangement Checker (Handshapes)" [
        testCase "Detects fingering that does not match anchor position" <| fun _ ->
            let hs = ResizeArray(seq { HandShape(0s, 1000, 1500) })
            let anchors = ResizeArray(seq { Anchor(2y, 500) })
            let level = Level(HandShapes = hs, Anchors = anchors)

            let results = checkHandshapes testArr level

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.IssueType FingeringAnchorMismatch "Correct issue type"

        testCase "Logic that checks if fingering does not match anchor position ignores fingerings using thumb" <| fun _ ->
            let hs = ResizeArray(seq { HandShape(6s, 1000, 1500); HandShape(7s, 2000, 2500) })
            let anchors = ResizeArray(seq { Anchor(5y, 1000); Anchor(1y, 2000) })
            let level = Level(HandShapes = hs, Anchors = anchors)

            let results = checkHandshapes testArr level

            Expect.isEmpty results "An issue was found in check results"
    ]

[<Tests>]
let anchorTests =
    testList "Arrangement Checker (Anchors)" [
        testCase "Detects anchor inside handshape" <| fun _ ->
            let anchors = ResizeArray(seq { Anchor(1y, 200) })
            let handShapes = ResizeArray(seq { HandShape(StartTime = 100, EndTime = 400) })
            let level = Level(HandShapes = handShapes, Anchors = anchors)

            let results = checkAnchors testArr level

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.IssueType AnchorInsideHandShape "Correct issue type"

        testCase "No false positive for anchor at the start of handshape" <| fun _ ->
            let anchors = ResizeArray(seq { Anchor(1y, 100) })
            let handShapes = ResizeArray(seq { HandShape(StartTime = 100, EndTime = 400) })
            let level = Level(HandShapes = handShapes, Anchors = anchors)

            let results = checkAnchors testArr level

            Expect.isEmpty results "An issue was found in check results"

        testCase "Detects anchor inside handshape at section boundary" <| fun _ ->
            let anchors = ResizeArray(seq { Anchor(1y, 8000) })
            let handShapes = ResizeArray(seq { HandShape(StartTime = 7000, EndTime = 9000) })
            let level = Level(HandShapes = handShapes, Anchors = anchors)

            let results = checkAnchors testArr level

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.IssueType AnchorInsideHandShapeAtPhraseBoundary "Correct issue type"

        testCase "Ignores anchors on phrases that will be moved (handshape check)" <| fun _ ->
            let anchors = ResizeArray(seq { Anchor(1y, 6500) })
            let handShapes = ResizeArray(seq { HandShape(StartTime = 6000, EndTime = 6550) })
            let level = Level(HandShapes = handShapes, Anchors = anchors)

            let results = checkAnchors testArr level

            Expect.isEmpty results "An issue was found in check results"

        testCase "Detects anchor near the end of an unpitched slide" <| fun _ ->
            let anchors = ResizeArray(seq { Anchor(1y, 500) })
            let notes = ResizeArray(seq { Note(Time = 100, Sustain = 397, SlideUnpitchTo = 5y) })
            let level = Level(Notes = notes, Anchors = anchors)

            let results = checkAnchors testArr level

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.IssueType AnchorCloseToUnpitchedSlide "Correct issue type"

        testCase "Ignores anchors on phrases that will be moved (unpitched slide check)" <| fun _ ->
            let anchors = ResizeArray(seq { Anchor(1y, 6500) })
            let notes = ResizeArray(seq { Note(Time = 6200, Sustain = 300, SlideUnpitchTo = 5y) })
            let level = Level(Notes = notes, Anchors = anchors)

            let results = checkAnchors testArr level

            Expect.isEmpty results "An issue was found in check results"
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
            Expect.equal results.Head.IssueType FirstPhraseNotEmpty "Correct issue type"

        testCase "Detects missing END phrase" <| fun _ ->
            let phrases = ResizeArray(seq { Phrase("COUNT", 0uy, PhraseMask.None); Phrase("riff", 0uy, PhraseMask.None) })
            let phraseIterations = ResizeArray(seq { PhraseIteration(1000, 0); PhraseIteration(1500, 1) })
            let notes = ResizeArray(seq { Note(Time = 1600) })
            let levels = ResizeArray(seq { Level(0y, Notes = notes) })
            let noEndTestArr = InstrumentalArrangement(Sections = sections, Phrases = phrases, PhraseIterations = phraseIterations, Levels = levels)

            let results = checkPhrases noEndTestArr

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.IssueType NoEndPhrase "Correct issue type"

        testCase "Detects more than 100 phrases" <| fun _ ->
            let phrases =
                ResizeArray(seq {
                    Phrase("COUNT", 0uy, PhraseMask.None)
                    Phrase("riff", 0uy, PhraseMask.None)
                    Phrase("END", 0uy, PhraseMask.None) })

            let phraseIterations =
                seq {
                    yield PhraseIteration(1000, 0)
                    for i in 1..99 -> PhraseIteration(1000 + i * 100, 1)
                    yield PhraseIteration(9000, 2)
                } |> ResizeArray

            let notes = ResizeArray(seq { Note(Time = 1600) })
            let levels = ResizeArray(seq { Level(0y, Notes = notes) })
            let noEndTestArr = InstrumentalArrangement(Sections = sections, Phrases = phrases, PhraseIterations = phraseIterations, Levels = levels)

            let results = checkPhrases noEndTestArr

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.IssueType MoreThan100Phrases "Correct issue type"
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
