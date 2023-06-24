module Rocksmith2014.XML.Processing.Tests.ArrangementImproverTests

open Expecto
open Rocksmith2014.XML
open Rocksmith2014.XML.Processing

let ra (x: 'a list) = ResizeArray(x)

[<Tests>]
let crowdEventTests =
    testList "Arrangement Improver (Crowd Events)" [
        testCase "Creates crowd events" <| fun _ ->
            let notes = ra [ Note(Time = 10000) ]
            let level = Level(Notes = notes)
            let arr = InstrumentalArrangement(Levels = ra [ level ])
            arr.MetaData.SongLength <- 120_000

            ArrangementImprover.addCrowdEvents arr

            Expect.isNonEmpty arr.Events "Events were created"

        testCase "No events are created when already present" <| fun _ ->
            let notes = ra [ Note(Time = 10000) ]
            let level = Level(Notes = notes)
            let events = ra [ Event("e1", 1000); Event("E3", 10000); Event("D3", 20000) ]
            let arr = InstrumentalArrangement(Events = events, Levels = ra [ level ])
            arr.MetaData.SongLength <- 120_000

            ArrangementImprover.addCrowdEvents arr

            Expect.hasLength arr.Events 3 "No new events were created"
    ]

let private f = Array.create 6 -1y

[<Tests>]
let chordNameTests =
    testList "Arrangement Improver (Chord Names)" [
        testCase "Fixes minor chord names" <| fun _ ->
            let c1 = ChordTemplate("Emin", "Emin", f, f)
            let c2 = ChordTemplate("Amin7", "Amin7", f, f)
            let chords = ra [ c1; c2 ]
            let arr = InstrumentalArrangement(ChordTemplates = chords)

            ArrangementImprover.processChordNames arr

            Expect.equal c1.Name "Em" "Name was fixed"
            Expect.equal c1.DisplayName "Em" "DisplayName was fixed"
            Expect.isFalse (chords |> Seq.exists(fun c -> c.Name.Contains("min") || c.DisplayName.Contains("min"))) "All chords were fixed"

        testCase "Fixes -arp chord names" <| fun _ ->
            let c = ChordTemplate("E-arp", "E-arp", f, f)
            let chords = ra [ c ]
            let arr = InstrumentalArrangement(ChordTemplates = chords)

            ArrangementImprover.processChordNames arr

            Expect.equal c.Name "E" "Name was fixed"
            Expect.equal c.DisplayName "E-arp" "DisplayName was not changed"

        testCase "Fixes -nop chord names" <| fun _ ->
            let c = ChordTemplate("CMaj7-nop", "CMaj7-nop", f, f)
            let chords = ra [ c ]
            let arr = InstrumentalArrangement(ChordTemplates = chords)

            ArrangementImprover.processChordNames arr

            Expect.equal c.Name "CMaj7" "Name was fixed"
            Expect.equal c.DisplayName "CMaj7-nop" "DisplayName was not changed"

        testCase "Can convert chords to arpeggios" <| fun _ ->
            let c = ChordTemplate("CminCONV", "CminCONV", f, f)
            let chords = ra [ c ]
            let arr = InstrumentalArrangement(ChordTemplates = chords)

            ArrangementImprover.processChordNames arr

            Expect.equal c.Name "Cm" "Name was fixed"
            Expect.equal c.DisplayName "Cm-arp" "DisplayName was fixed"

        testCase "Fixes empty chord names" <| fun _ ->
            let c = ChordTemplate(" ", " ", f, f)
            let chords = ra [ c ]
            let arr = InstrumentalArrangement(ChordTemplates = chords)

            ArrangementImprover.processChordNames arr

            Expect.stringHasLength c.Name 0 "Name was fixed"
            Expect.stringHasLength c.DisplayName 0 "DisplayName was fixed"
    ]

[<Tests>]
let beatRemoverTests =
    testList "Arrangement Improver (Beat Remover)" [
        testCase "Removes beats" <| fun _ ->
            let beats = ra [ Ebeat(5000, 1s); Ebeat(6000, 1s); Ebeat(7000, 1s); Ebeat(8000, 1s) ]
            let arr = InstrumentalArrangement(Ebeats = beats)
            arr.MetaData.SongLength <- 6000

            ArrangementImprover.removeExtraBeats arr

            Expect.hasLength arr.Ebeats 2 "Two beats were removed"

        testCase "Moves the beat after the end close to it to the end" <| fun _ ->
            let beats = ra [ Ebeat(5000, 1s); Ebeat(6000, 1s); Ebeat(7000, 1s); Ebeat(8000, 1s) ]
            let arr = InstrumentalArrangement(Ebeats = beats)
            arr.MetaData.SongLength <- 6900

            ArrangementImprover.removeExtraBeats arr

            Expect.hasLength arr.Ebeats 3 "One beat was removed"
            Expect.equal arr.Ebeats.[2].Time 6900 "Last beat was moved to the correct time"

        testCase "Moves the beat before the end close to it to the end" <| fun _ ->
            let beats = ra [ Ebeat(5000, 1s); Ebeat(6000, 1s); Ebeat(7000, 1s); Ebeat(8000, 1s) ]
            let arr = InstrumentalArrangement(Ebeats = beats)
            arr.MetaData.SongLength <- 6100

            ArrangementImprover.removeExtraBeats arr

            Expect.hasLength arr.Ebeats 2 "Two beats were removed"
            Expect.equal arr.Ebeats.[1].Time 6100 "Last beat was moved to the correct time"
    ]

[<Tests>]
let eofFixTests =
    testList "Arrangement Improver (EOF Fixes)" [
        testCase "Adds LinkNext to chords missing the attribute" <| fun _ ->
            let chord = Chord(ChordNotes = ra [ Note(IsLinkNext = true) ])
            let levels = ra [ Level(Chords = ra [ chord ]) ]
            let arr = InstrumentalArrangement(Levels = levels)

            EOFFixes.fixChordNotes arr

            Expect.isTrue chord.IsLinkNext "LinkNext was enabled"

        testCase "Fixes varying sustain of chord notes" <| fun _ ->
            let correctSustain = 500
            let chord = Chord(ChordNotes = ra [ Note(Sustain = 0); Note(String = 1y, Sustain = correctSustain); Note(String = 2y, Sustain = 85) ])
            let levels = ra [ Level(Chords = ra [ chord ]) ]
            let arr = InstrumentalArrangement(Levels = levels)

            EOFFixes.fixChordNotes arr

            Expect.all levels[0].Chords (fun c -> c.ChordNotes |> Seq.forall (fun cn -> cn.Sustain = correctSustain)) "Sustain was changed"

        testCase "Removes incorrect chord note linknexts" <| fun _ ->
            let cn = ra [ Note(IsLinkNext = true) ]
            let chords = ra [ Chord(ChordNotes = cn, IsLinkNext = true) ]
            let levels = ra [ Level(Chords = chords) ]
            let arr = InstrumentalArrangement(Levels = levels)

            EOFFixes.removeInvalidChordNoteLinkNexts arr

            Expect.isFalse cn.[0].IsLinkNext "LinkNext was removed from chord note"

        testCase "Chord note linknext is not removed when there is 1ms gap" <| fun _ ->
            let cn =
                ra [ Note(String = 0y, Sustain = 499, IsLinkNext = true)
                     Note(String = 1y, Sustain = 499, IsLinkNext = true) ]
            let chords = ra [ Chord(ChordNotes = cn, IsLinkNext = true) ]
            let notes = ra [ Note(String = 0y, Time = 500) ]
            let levels = ra [ Level(Chords = chords, Notes = notes) ]
            let arr = InstrumentalArrangement(Levels = levels)

            EOFFixes.removeInvalidChordNoteLinkNexts arr

            Expect.isTrue cn.[0].IsLinkNext "First chord note has LinkNext"
            Expect.isFalse cn.[1].IsLinkNext "Second chord note does not have LinkNext"

        testCase "Fixes incorrect crowd events" <| fun _ ->
            let events = ra [ Event("E0", 100); Event("E1", 200); Event("E2", 300) ]
            let arr = InstrumentalArrangement(Events = events)

            EOFFixes.fixCrowdEvents arr

            Expect.hasLength arr.Events 3 "Number of events is unchanged"
            Expect.exists arr.Events (fun e -> e.Code = "e0") "E0 -> e0"
            Expect.exists arr.Events (fun e -> e.Code = "e1") "E1 -> e1"
            Expect.exists arr.Events (fun e -> e.Code = "e2") "E2 -> e2"

        testCase "Does not change correct crowd events" <| fun _ ->
            let events = ra [ Event("E3", 100); Event("E13", 200); Event("D3", 300); Event("E13", 400); ]
            let arr = InstrumentalArrangement(Events = events)

            EOFFixes.fixCrowdEvents arr

            Expect.hasLength arr.Events 4 "Number of events is unchanged"
            Expect.equal arr.Events.[0].Code "E3" "Event #1 code unchanged"
            Expect.equal arr.Events.[1].Code "E13" "Event #2 code unchanged"
            Expect.equal arr.Events.[2].Code "D3" "Event #3 code unchanged"
            Expect.equal arr.Events.[3].Code "E13" "Event #4 code unchanged"

        testCase "Fixes incorrect handshape lengths" <| fun _ ->
            let cn = ra [ Note(IsLinkNext = true, SlideTo = 5y, Sustain = 1000) ]
            let chord = Chord(ChordNotes = cn, IsLinkNext = true)
            let hs = HandShape(0s, 0, 1500)
            let levels = ra [ Level(Chords = ra [ chord ], HandShapes = ra [ hs ]) ]
            let arr = InstrumentalArrangement(Levels = levels)

            EOFFixes.fixChordSlideHandshapes arr

            Expect.equal hs.EndTime 1000 "Handshape end time is correct"

        testCase "Moves anchor to the beginning of phrase" <| fun _ ->
            let anchor = Anchor(5y, 700)
            let anchors = ra [ anchor ]
            let levels = ra [ Level(Anchors = anchors) ]
            let phraseIterations = ra [ PhraseIteration(100, 0); PhraseIteration(650, 0); PhraseIteration(1000, 1) ]
            let arr = InstrumentalArrangement(Levels = levels, PhraseIterations = phraseIterations)

            EOFFixes.fixPhraseStartAnchors arr

            Expect.hasLength anchors 1 "Anchor was not copied"
            Expect.equal anchor.Time 650 "Anchor time is correct"

        testCase "Copies active anchor to the beginning of phrase" <| fun _ ->
            let anchor = Anchor(5y, 400, 7y)
            let anchors = ra [ anchor ]
            let levels = ra [ Level(Anchors = anchors) ]
            let phraseIterations =
                ra [ PhraseIteration(100, 0)
                     PhraseIteration(400, 0)
                     PhraseIteration(650, 0)
                     PhraseIteration(1000, 1) ]
            let arr = InstrumentalArrangement(Levels = levels, PhraseIterations = phraseIterations)

            EOFFixes.fixPhraseStartAnchors arr

            Expect.hasLength anchors 2 "Anchor was copied"
            Expect.equal anchor.Time 400 "Existing anchor time is correct"
            Expect.equal anchors.[1].Time 650 "New anchor time is correct"
            Expect.equal anchors.[1].Fret anchor.Fret "New anchor fret is correct"
            Expect.equal anchors.[1].Width anchor.Width "New anchor width is correct"
    ]

[<Tests>]
let phraseMoverTests =
    testList "Arrangement Improver (Phrase Mover)" [
        testCase "Can move phrase to next note" <| fun _ ->
            let iter = PhraseIteration(1000, 0)
            let notes = ra [ Note(Time = 1200) ]
            let arr =
                InstrumentalArrangement(
                    Phrases = ra [ Phrase("mover1", 0uy, PhraseMask.None) ],
                    PhraseIterations = ra [ iter ],
                    Levels = ra [ Level(Notes = notes) ]
                )

            PhraseMover.improve arr

            Expect.equal iter.Time 1200 "Phrase iteration was moved to correct time"

        testCase "Can move phrase to chord" <| fun _ ->
            let iter = PhraseIteration(1000, 0)
            let notes = ra [ Note(Time = 1200) ]
            let chords = ra [ Chord(Time = 1600) ]
            let arr =
                InstrumentalArrangement(
                    Phrases = ra [ Phrase("mover2", 0uy, PhraseMask.None) ],
                    PhraseIterations = ra [ iter ],
                    Levels = ra [ Level(Notes = notes, Chords = chords) ]
                )

            PhraseMover.improve arr

            Expect.equal iter.Time 1600 "Phrase iteration was moved to correct time"

        testCase "Can move phrase beyond multiple notes at the same time code" <| fun _ ->
            let iter = PhraseIteration(1000, 0)
            let notes = ra [ Note(Time = 1200); Note(String = 1y, Time = 1200); Note(String = 2y, Time = 1200); Note(Time = 2500) ]
            let arr =
                InstrumentalArrangement(
                    Phrases = ra [ Phrase("mover2", 0uy, PhraseMask.None) ],
                    PhraseIterations = ra [ iter ],
                    Levels = ra [ Level(Notes = notes) ]
                )

            PhraseMover.improve arr

            Expect.equal iter.Time 2500 "Phrase iteration was moved to correct time"

        testCase "Can move a phrase on the same time code as a note" <| fun _ ->
            let iter = PhraseIteration(1000, 0)
            let notes = ra [ Note(Time = 1000); Note(Time = 7500) ]
            let arr =
                InstrumentalArrangement(
                    Phrases = ra [ Phrase("mover2", 0uy, PhraseMask.None) ],
                    PhraseIterations = ra [ iter ],
                    Levels = ra [ Level(Notes = notes) ]
                )

            PhraseMover.improve arr

            Expect.equal iter.Time 7500 "Phrase iteration was moved to correct time"

        testCase "Section is also moved" <| fun _ ->
            let iter = PhraseIteration(1000, 0)
            let section = Section("", 1000, 1s)
            let notes = ra [ Note(Time = 7500) ]
            let arr =
                InstrumentalArrangement(
                    Phrases = ra [ Phrase("mover1", 0uy, PhraseMask.None) ],
                    PhraseIterations = ra [ iter ],
                    Sections = ra [ section ],
                    Levels = ra [ Level(Notes = notes) ]
                )

            PhraseMover.improve arr

            Expect.equal section.Time 7500 "Section was moved to correct time"

        testCase "Anchor is also moved" <| fun _ ->
            let notes = ra [ Note(Time = 7500) ]
            let anchors = ra [ Anchor(Time = 1000) ]
            let arr =
                InstrumentalArrangement(
                    Phrases = ra [ Phrase("mover1", 0uy, PhraseMask.None) ],
                    PhraseIterations = ra [ PhraseIteration(1000, 0) ],
                    Levels = ra [ Level(Notes = notes, Anchors = anchors) ]
                )

            PhraseMover.improve arr

            Expect.hasLength anchors 1 "One anchor exists"
            Expect.exists anchors (fun a -> a.Time = 7500) "Anchor was moved to correct time"

        testCase "Throws an exception when no integer given" <| fun _ ->
            let arr =
                InstrumentalArrangement(
                    Phrases = ra [ Phrase("mover", 0uy, PhraseMask.None) ],
                    PhraseIterations = ra [ PhraseIteration(1000, 0) ]
                )

            Expect.throwsC
                (fun _ -> PhraseMover.improve arr)
                (fun ex -> Expect.stringContains ex.Message "Unable to parse" "Correct exception was thrown")
    ]

[<Tests>]
let customEventTests =
    testList "Arrangement Improver (Custom Events)" [
        testCase "Anchor width 3 event" <| fun _ ->
            let anchor = Anchor(1y, 100)
            let arr =
                InstrumentalArrangement(
                    Events = ra [ Event("w3", 100) ],
                    Levels = ra [ Level(Anchors = ra [ anchor ]) ]
                )

            CustomEvents.improve arr

            Expect.equal anchor.Width 3y "Anchor has correct width"

        testCase "Anchor width 3 event can change fret" <| fun _ ->
            let anchor = Anchor(21y, 180)
            let arr =
                InstrumentalArrangement(
                    Events = ra [ Event("w3-22", 100) ],
                    Levels = ra [ Level(Anchors = ra [ anchor ]) ]
                )

            CustomEvents.improve arr

            Expect.equal anchor.Width 3y "Anchor has correct width"
            Expect.equal anchor.Fret 22y "Anchor has correct fret"

        testCase "Remove beats event" <| fun _ ->
            let beats = ra [ Ebeat(100, -1s); Ebeat(200, -1s); Ebeat(300, -1s); Ebeat(400, -1s); Ebeat(500, -1s); ]
            let arr =
                InstrumentalArrangement(
                    Events = ra [ Event("removebeats", 400) ],
                    Ebeats = beats
                )

            CustomEvents.improve arr

            Expect.hasLength arr.Ebeats 3 "Two beats were removed"

        testCase "Slide-out event works for normal chord" <| fun _ ->
            let templates = ra [ ChordTemplate("", "", [| 1y; 3y; -1y; -1y; -1y; -1y; |], [| 1y; 3y; -1y; -1y; -1y; -1y; |]) ]
            let cn =
                ra [ Note(String = 0y, Fret = 1y, Sustain = 1000, SlideUnpitchTo = 7y)
                     Note(String = 1y, Fret = 3y, Sustain = 1000, SlideUnpitchTo = 9y) ]
            let chords = ra [ Chord(ChordNotes = cn) ]
            let hs = HandShape(0s, 0, 1000)
            let arr =
                InstrumentalArrangement(
                    Phrases = ra [ Phrase("", 0uy, PhraseMask.None) ],
                    PhraseIterations = ra [ PhraseIteration(0, 0) ],
                    ChordTemplates = templates,
                    Events = ra [ Event("so", 0) ],
                    Levels = ra [ Level(Chords = chords, HandShapes = ra [ hs ]) ]
                )

            CustomEvents.improve arr

            Expect.hasLength arr.ChordTemplates 2 "A chord template was created"
            Expect.equal arr.ChordTemplates.[1].Frets.[0] 7y "Fret is correct"
            Expect.equal arr.ChordTemplates.[1].Frets.[1] 9y "Fret is correct"
            Expect.equal arr.ChordTemplates.[1].Fingers.[0] 1y "Fingering is correct"
            Expect.equal arr.ChordTemplates.[1].Fingers.[1] 3y "Fingering is correct"
            Expect.hasLength arr.Levels.[0].HandShapes 2 "A hand shape was created"
            Expect.equal arr.Levels.[0].HandShapes.[1].EndTime 1001 "Second handshape ends at end of sustain + 1ms"
            Expect.isTrue (hs.EndTime < 1000) "First handshape was shortened"

        testCase "Slide-out event works for link-next chord" <| fun _ ->
            let templates = ra [ ChordTemplate("", "", [| -1y; -1y; 2y; 2y; -1y; -1y; |], [| -1y; -1y; 5y; 5y; -1y; -1y; |]) ]
            let cn =
                ra [ Note(String = 2y, Fret = 5y, Sustain = 1000, IsLinkNext = true)
                     Note(String = 3y, Fret = 5y, Sustain = 1000, IsLinkNext = true) ]
            let chords = ra [ Chord(ChordNotes = cn, IsLinkNext = true) ]
            let notes =
                ra [ Note(Time = 1000, String = 2y, Fret = 5y, Sustain = 500, SlideUnpitchTo = 12y)
                     Note(Time = 1000, String = 3y, Fret = 5y, Sustain = 500, SlideUnpitchTo = 12y) ]
            let hs = HandShape(0s, 0, 1500) // Includes sustain of slide-out notes
            let arr =
                InstrumentalArrangement(
                    Phrases = ra [ Phrase("", 0uy, PhraseMask.None) ],
                    PhraseIterations = ra [ PhraseIteration(0, 0) ],
                    ChordTemplates = templates,
                    Events = ra [ Event("so", 1000) ],
                    Levels = ra [ Level(Notes = notes, Chords = chords, HandShapes = ra [ hs ]) ]
                )

            CustomEvents.improve arr

            Expect.hasLength arr.ChordTemplates 2 "A chord template was created"
            Expect.equal arr.ChordTemplates.[1].Frets.[2] 12y "Fret is correct"
            Expect.equal arr.ChordTemplates.[1].Frets.[3] 12y "Fret is correct"
            Expect.equal arr.ChordTemplates.[1].Fingers.[2] 2y "Fingering is correct"
            Expect.equal arr.ChordTemplates.[1].Fingers.[3] 2y "Fingering is correct"
            Expect.hasLength arr.Levels.[0].HandShapes 2 "A handshape was created"
            Expect.equal arr.Levels.[0].HandShapes.[1].EndTime 1501 "Second handshape ends at end of sustain + 1ms"
            Expect.isTrue (hs.EndTime < 1500) "First handshape was shortened"
    ]

[<Tests>]
let handShapeAdjusterTests =
    testList "Arrangement Improver (Hand Shape Adjuster)" [
        testCase "Shortens handshape length" <| fun _ ->
            let beats = ra [ Ebeat(500, -1s); Ebeat(1000, -1s); Ebeat(1500, -1s); Ebeat(2500, -1s) ]
            let chords = ra [ Chord(Time = 1000); Chord(ChordId = 1s, Time = 2000) ]
            let hs1 = HandShape(0s, 1000, 2000)
            let hs2 = HandShape(1s, 2000, 3000)
            let arr =
                InstrumentalArrangement(
                    Ebeats = beats,
                    Levels = ra [ Level(Chords = chords, HandShapes = ra [ hs1; hs2 ]) ]
                )

            HandShapeAdjuster.improve arr

            Expect.isTrue (hs1.EndTime < 2000) "Handshape was shortened"
            Expect.isTrue (hs1.StartTime < hs1.EndTime) "Handshape end comes after the start"

        testCase "Shortens length of a really short handshape" <| fun _ ->
            let beats = ra [ Ebeat(500, -1s); Ebeat(1000, -1s); Ebeat(1500, -1s); Ebeat(2500, -1s) ]
            let chords = ra [ Chord(Time = 1950); Chord(ChordId = 1s, Time = 2000) ]
            let hs1 = HandShape(0s, 1950, 2000)
            let hs2 = HandShape(1s, 2000, 3000)
            let arr =
                InstrumentalArrangement(
                    Ebeats = beats,
                    Levels = ra [ Level(Chords = chords, HandShapes = ra [ hs1; hs2 ]) ]
                )

            HandShapeAdjuster.improve arr

            Expect.isTrue (hs1.EndTime < 2000) "Handshape was shortened"
            Expect.isTrue (hs1.StartTime < hs1.EndTime) "Handshape end comes after the start"

        testCase "Does not fail on handshapes that exceed the last beat" <| fun _ ->
            let beats = ra [ Ebeat(500, -1s); Ebeat(1000, -1s); Ebeat(1500, -1s); Ebeat(2500, -1s) ]
            let hs1 = HandShape(0s, 2500, 2600)
            let hs2 = HandShape(0s, 2600, 2800)
            let arr =
                InstrumentalArrangement(
                    Ebeats = beats,
                    Levels = ra [ Level(HandShapes = ra [ hs1; hs2 ]) ]
                )

            HandShapeAdjuster.improve arr

            Expect.isTrue (hs1.EndTime < 2600) "Handshape was shortened"
    ]

[<Tests>]
let basicFixTests =
    testList "Arrangement Improver (Basic Fixes)" [
        testCase "Filters characters in phrase names" <| fun _ ->
            let phrases =
                ra [ Phrase("\"TEST\"", 0uy, PhraseMask.None)
                     Phrase("'TEST'_(2)", 0uy, PhraseMask.None) ]
            let arr = InstrumentalArrangement(Phrases = phrases)

            BasicFixes.validatePhraseNames arr

            Expect.equal phrases.[0].Name "TEST" "First phrase name was changed"
            Expect.equal phrases.[1].Name "TEST_2" "Second phrase name was changed"

        testCase "Ignore is added to 23rd and 24th fret notes" <| fun _ ->
            let notes = ra [ Note(Time = 1000, Fret = 5y); Note(Time = 1200, Fret = 23y); Note(Time = 1300, Fret = 24y) ]
            let arr = InstrumentalArrangement(Levels = ra [ Level(Notes = notes) ])

            BasicFixes.addIgnores arr

            Expect.isFalse notes.[0].IsIgnore "First note is not ignored"
            Expect.isTrue notes.[1].IsIgnore "Second note is ignored"
            Expect.isTrue notes.[2].IsIgnore "Third note is ignored"

        testCase "Ignore is added to 7th fret harmonic with sustain" <| fun _ ->
            let notes = ra [
                Note(Time = 1000, Fret = 7y, Sustain = 500, IsHarmonic = true)
                Note(Time = 2000, Fret = 7y, Sustain = 0, IsHarmonic = true)
            ]
            let arr = InstrumentalArrangement(Levels = ra [ Level(Notes = notes) ])

            BasicFixes.addIgnores arr

            Expect.isTrue notes.[0].IsIgnore "First note is ignored"
            Expect.isFalse notes.[1].IsIgnore "Second note is not ignored"

        testCase "Ignore is added to chord with 23rd and 24th fret notes" <| fun _ ->
            let noFingers = [| -1y; -1y; -1y; -1y; -1y; -1y |]
            let templates = ra [
                ChordTemplate("", "", noFingers, [| -1y; 0y; 23y; -1y; -1y; -1y; |])
                ChordTemplate("", "", noFingers, [| -1y; -1y; -1y; -1y; 22y; 24y; |])
            ]
            let chords = ra [ Chord(Time = 1000, ChordId = 0s); Chord(Time = 1200, ChordId = 1s) ]
            let arr = InstrumentalArrangement(Levels = ra [ Level(Chords = chords) ], ChordTemplates = templates)

            BasicFixes.addIgnores arr

            Expect.isTrue chords.[0].IsIgnore "First chord is ignored"
            Expect.isTrue chords.[1].IsIgnore "Second chord is ignored"

        testCase "Ignore is added to chord with 7th fret harmonic with sustain" <| fun _ ->
            let noFingers = [| -1y; -1y; -1y; -1y; -1y; -1y |]
            let templates = ra [
                ChordTemplate("", "", noFingers, [| -1y; 7y; 7y; -1y; -1y; -1y; |])
            ]
            let cn = ra [
                Note(Time = 1000, Sustain = 500, Fret = 7y, String = 1y, IsHarmonic = true)
                Note(Time = 1000, Sustain = 500, Fret = 7y, String = 2y, IsHarmonic = true)
            ]
            let chords = ra [ Chord(Time = 1000, ChordId = 0s, ChordNotes = cn) ]
            let arr = InstrumentalArrangement(Levels = ra [ Level(Chords = chords) ], ChordTemplates = templates)

            BasicFixes.addIgnores arr

            Expect.isTrue chords.[0].IsIgnore "Chord is ignored"

        testCase "Incorrect linknext is removed (next note on same string not found)" <| fun _ ->
            let notes = ra [ Note(Time = 1000, Fret = 5y, IsLinkNext = true); Note(Time = 1500, String = 4y, Fret = 5y) ]
            let arr = InstrumentalArrangement(Levels = ra [ Level(Notes = notes) ])

            BasicFixes.fixLinkNexts arr

            Expect.isFalse notes.[0].IsLinkNext "Linknext was removed"

        testCase "Incorrect linknext is removed (next note too far)" <| fun _ ->
            let notes = ra [ Note(Time = 1000, Fret = 5y, IsLinkNext = true); Note(Time = 2000, Fret = 5y) ]
            let arr = InstrumentalArrangement(Levels = ra [ Level(Notes = notes) ])

            BasicFixes.fixLinkNexts arr

            Expect.isFalse notes.[0].IsLinkNext "Linknext was removed"

        testCase "Incorrect linknext fret is corrected" <| fun _ ->
            let notes = ra [
                Note(Time = 1000, Fret = 5y, Sustain = 500, IsLinkNext = true)
                Note(Time = 1500, Fret = 6y)
            ]
            let arr = InstrumentalArrangement(Levels = ra [ Level(Notes = notes) ])

            BasicFixes.fixLinkNexts arr

            Expect.isTrue notes.[0].IsLinkNext "Linknext was not removed"
            Expect.equal notes.[1].Fret 5y "Fret was corrected"

        testCase "Incorrect linknext fret is corrected (Slide)" <| fun _ ->
            let notes = ra [
                Note(Time = 1000, Fret = 5y, Sustain = 500, IsLinkNext = true, SlideTo = 9y)
                Note(Time = 1500, Fret = 10y)
            ]
            let arr = InstrumentalArrangement(Levels = ra [ Level(Notes = notes) ])

            BasicFixes.fixLinkNexts arr

            Expect.isTrue notes.[0].IsLinkNext "Linknext was not removed"
            Expect.equal notes.[1].Fret 9y "Fret was corrected"

        testCase "Incorrect linknext fret is corrected (Unpitched slide)" <| fun _ ->
            let notes = ra [
                Note(Time = 1000, Fret = 5y, Sustain = 500, IsLinkNext = true, SlideUnpitchTo = 9y);
                Note(Time = 1500, Fret = 10y)
            ]
            let arr = InstrumentalArrangement(Levels = ra [ Level(Notes = notes) ])

            BasicFixes.fixLinkNexts arr

            Expect.isTrue notes.[0].IsLinkNext "Linknext was not removed"
            Expect.equal notes.[1].Fret 9y "Fret was corrected"

        testCase "Incorrect linknext fret is corrected (Bend)" <| fun _ ->
            let bv = ra [ BendValue(1200, 2.0f) ]
            let notes = ra [
                Note(Time = 1000, Fret = 5y, Sustain = 500, IsLinkNext = true, BendValues = bv)
                Note(Time = 1500, Fret = 10y)
            ]
            let arr = InstrumentalArrangement(Levels = ra [ Level(Notes = notes) ])

            BasicFixes.fixLinkNexts arr

            Expect.isTrue notes.[1].IsBend "Second note has bend"
            Expect.equal notes.[1].MaxBend 2.0f "Max bend is correct"
            Expect.equal notes.[1].BendValues[0].Step 2.0f "Bend value step is correct"
            Expect.equal notes.[1].BendValues[0].Time 1500 "Bend value time is correct"

        testCase "Overlapping bend values are removed" <| fun _ ->
            let bv1 = ra [ BendValue(1200, 2.0f); BendValue(1200, 1.0f) ]
            let bv2 = ra [ BendValue(2100, 2.0f); BendValue(2100, 2.0f) ]
            let notes = ra [
                Note(Time = 1000, Fret = 5y, Sustain = 500, IsLinkNext = true, BendValues = bv1)
            ]
            let chords = ra [
                Chord(Time = 2000, ChordNotes = ra [ Note(Time = 2000, Sustain = 500, BendValues = bv2) ])
            ]
            let arr = InstrumentalArrangement(Levels = ra [ Level(Notes = notes, Chords = chords) ])

            BasicFixes.removeOverlappingBendValues arr

            Expect.hasLength notes[0].BendValues 1 "Bend value was removed from note"
            Expect.hasLength chords[0].ChordNotes[0].BendValues 1 "Bend value was removed from chord note"

        testCase "Muted strings are removed from non-muted chords" <| fun _ ->
            let templates = ra [
                ChordTemplate("", "", [| 1y; 3y; 4y; -1y; -1y; -1y |], [| 1y; 3y; 3y; -1y; -1y; -1y; |])
                ChordTemplate("", "", [| -1y; -1y; -1y; -1y; -1y; -1y |], [| 0y; 0y; 0y; -1y; -1y; -1y; |])
            ]
            let cn1 = ra [
                Note(Time = 1000, String = 0y, Fret = 1y)
                Note(Time = 1000, String = 1y, Fret = 3y, IsFretHandMute = true)
                Note(Time = 1000, String = 2y, Fret = 3y)
            ]
            let cn2 = ra [
                Note(Time = 1200, String = 0y, Fret = 0y, IsFretHandMute = true)
                Note(Time = 1200, String = 1y, Fret = 0y, IsFretHandMute = true)
                Note(Time = 1200, String = 2y, Fret = 0y, IsFretHandMute = true)
            ]
            let chords = ra [
                Chord(Time = 1000, ChordId = 0s, ChordNotes = cn1)
                // Chord with all muted notes, but not marked as muted
                Chord(Time = 1200, ChordId = 1s, ChordNotes = cn2) ]
            let arr = InstrumentalArrangement(Levels = ra [ Level(Chords = chords) ], ChordTemplates = templates)

            BasicFixes.removeMutedNotesFromChords arr

            Expect.hasLength chords.[0].ChordNotes 2 "Chord note was removed from first chord"
            Expect.isFalse (chords.[0].ChordNotes.Exists(fun n -> n.IsFretHandMute)) "Fret-hand mute was removed"
            Expect.hasLength chords.[1].ChordNotes 3 "Chord notes were not removed from second chord"
            Expect.equal templates[0].Fingers[1] -1y "Fingering was removed from first chord template"
            Expect.equal templates[0].Frets[1] -1y "String was removed from first chord template"
            Expect.sequenceContainsOrder templates[1].Frets [| 0y; 0y; 0y; -1y; -1y; -1y; |] "Second chord template was not modified"
    ]

[<Tests>]
let applyAllTests =
    testList "Arrangement Improver (Apply All Fixes)" [
        testCase "Extra anchors are not created when moving phrases" <| fun _ ->
            let beats = ra [ Ebeat(900, 0s); Ebeat(1000, -1s); Ebeat(1200, -1s) ]
            let phrases = ra [ Phrase("mover2", 0uy, PhraseMask.None); Phrase("END", 0uy, PhraseMask.None) ]
            let iterations = ra [ PhraseIteration(1000, 0); PhraseIteration(1900, 1) ]
            let notes = ra [ Note(Time = 1000); Note(Time = 1200) ]
            let anchors = ra [ Anchor(1y, 1200) ]
            let arr =
                InstrumentalArrangement(
                    Phrases = phrases,
                    PhraseIterations = iterations,
                    Levels = ra [ Level(Notes = notes, Anchors = anchors) ],
                    Ebeats = beats
                )
            arr.MetaData.SongLength <- 2000

            ArrangementImprover.applyAll arr

            Expect.hasLength anchors 1 "No new anchors were created"
            Expect.equal anchors.[0].Time 1200 "Anchor is at correct position"
    ]
