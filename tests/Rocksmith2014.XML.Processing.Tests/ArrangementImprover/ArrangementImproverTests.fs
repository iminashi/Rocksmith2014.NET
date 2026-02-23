module Rocksmith2014.XML.Processing.Tests.ArrangementImproverTests

open Expecto
open Rocksmith2014.XML
open Rocksmith2014.XML.Processing

[<Tests>]
let crowdEventTests =
    testList "Arrangement Improver (Crowd Events)" [
        testCase "Creates crowd events" <| fun _ ->
            let notes = ![ Note(Time = 10000) ]
            let level = Level(Notes = notes)
            let arr = InstrumentalArrangement(Levels = ![ level ])
            arr.MetaData.SongLength <- 120_000

            ArrangementImprover.addCrowdEvents arr

            Expect.isNonEmpty arr.Events "Events were created"

        testCase "No events are created when already present" <| fun _ ->
            let notes = ![ Note(Time = 10000) ]
            let level = Level(Notes = notes)
            let events = ![ Event("e1", 1000); Event("E3", 10000); Event("D3", 20000) ]
            let arr = InstrumentalArrangement(Events = events, Levels = ![ level ])
            arr.MetaData.SongLength <- 120_000

            ArrangementImprover.addCrowdEvents arr

            Expect.hasLength arr.Events 3 "No new events were created"
    ]

[<Tests>]
let beatRemoverTests =
    testList "Arrangement Improver (Beat Remover)" [
        testCase "Removes beats" <| fun _ ->
            let beats = ![ Ebeat(5000, 1s); Ebeat(6000, 1s); Ebeat(7000, 1s); Ebeat(8000, 1s) ]
            let arr = InstrumentalArrangement(Ebeats = beats)
            arr.MetaData.SongLength <- 6000

            ArrangementImprover.removeExtraBeats arr

            Expect.hasLength arr.Ebeats 2 "Two beats were removed"

        testCase "Moves the beat after the end close to it to the end" <| fun _ ->
            let beats = ![ Ebeat(5000, 1s); Ebeat(6000, 1s); Ebeat(7000, 1s); Ebeat(8000, 1s) ]
            let arr = InstrumentalArrangement(Ebeats = beats)
            arr.MetaData.SongLength <- 6900

            ArrangementImprover.removeExtraBeats arr

            Expect.hasLength arr.Ebeats 3 "One beat was removed"
            Expect.equal arr.Ebeats.[2].Time 6900 "Last beat was moved to the correct time"

        testCase "Moves the beat before the end close to it to the end" <| fun _ ->
            let beats = ![ Ebeat(5000, 1s); Ebeat(6000, 1s); Ebeat(7000, 1s); Ebeat(8000, 1s) ]
            let arr = InstrumentalArrangement(Ebeats = beats)
            arr.MetaData.SongLength <- 6100

            ArrangementImprover.removeExtraBeats arr

            Expect.hasLength arr.Ebeats 2 "Two beats were removed"
            Expect.equal arr.Ebeats.[1].Time 6100 "Last beat was moved to the correct time"
    ]

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

[<Tests>]
let customEventTests =
    testList "Arrangement Improver (Custom Events)" [
        testCase "Anchor width 3 event" <| fun _ ->
            let anchor = Anchor(1y, 100)
            let arr =
                InstrumentalArrangement(
                    Events = ![ Event("w3", 100) ],
                    Levels = ![ Level(Anchors = ![ anchor ]) ]
                )

            CustomEvents.improve arr

            Expect.equal anchor.Width 3y "Anchor has correct width"

        testCase "Anchor width 3 event can change fret" <| fun _ ->
            let anchor = Anchor(21y, 180)
            let arr =
                InstrumentalArrangement(
                    Events = ![ Event("w3-22", 100) ],
                    Levels = ![ Level(Anchors = ![ anchor ]) ]
                )

            CustomEvents.improve arr

            Expect.equal anchor.Width 3y "Anchor has correct width"
            Expect.equal anchor.Fret 22y "Anchor has correct fret"

        testCase "Remove beats event" <| fun _ ->
            let beats = ![ Ebeat(100, -1s); Ebeat(200, -1s); Ebeat(300, -1s); Ebeat(400, -1s); Ebeat(500, -1s); ]
            let arr =
                InstrumentalArrangement(
                    Events = ![ Event("removebeats", 400) ],
                    Ebeats = beats
                )

            CustomEvents.improve arr

            Expect.hasLength arr.Ebeats 3 "Two beats were removed"

        testCase "Slide-out event works for normal chord" <| fun _ ->
            let templates = ![ ChordTemplate("", "", [| 1y; 3y; -1y; -1y; -1y; -1y; |], [| 1y; 3y; -1y; -1y; -1y; -1y; |]) ]
            let cn = ![
                Note(String = 0y, Fret = 1y, Sustain = 1000, SlideUnpitchTo = 7y)
                Note(String = 1y, Fret = 3y, Sustain = 1000, SlideUnpitchTo = 9y)
            ]
            let chords = ![ Chord(ChordNotes = cn) ]
            let hs = HandShape(0s, 0, 1000)
            let arr =
                InstrumentalArrangement(
                    Phrases = ![ Phrase("", 0uy, PhraseMask.None) ],
                    PhraseIterations = ![ PhraseIteration(0, 0) ],
                    ChordTemplates = templates,
                    Events = ![ Event("so", 0) ],
                    Levels = ![ Level(Chords = chords, HandShapes = ![ hs ]) ]
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
            let templates = ![ ChordTemplate("", "", [| -1y; -1y; 2y; 2y; -1y; -1y; |], [| -1y; -1y; 5y; 5y; -1y; -1y; |]) ]
            let cn = ![
                Note(String = 2y, Fret = 5y, Sustain = 1000, IsLinkNext = true)
                Note(String = 3y, Fret = 5y, Sustain = 1000, IsLinkNext = true)
            ]
            let chords = ![ Chord(ChordNotes = cn, IsLinkNext = true) ]
            let notes = ![
                Note(Time = 1000, String = 2y, Fret = 5y, Sustain = 500, SlideUnpitchTo = 12y)
                Note(Time = 1000, String = 3y, Fret = 5y, Sustain = 500, SlideUnpitchTo = 12y)
            ]
            let hs = HandShape(0s, 0, 1500) // Includes sustain of slide-out notes
            let arr =
                InstrumentalArrangement(
                    Phrases = ![ Phrase("", 0uy, PhraseMask.None) ],
                    PhraseIterations = ![ PhraseIteration(0, 0) ],
                    ChordTemplates = templates,
                    Events = ![ Event("so", 1000) ],
                    Levels = ![ Level(Notes = notes, Chords = chords, HandShapes = ![ hs ]) ]
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
            let beats = ![ Ebeat(500, -1s); Ebeat(1000, -1s); Ebeat(1500, -1s); Ebeat(2500, -1s) ]
            let chords = ![ Chord(Time = 1000); Chord(ChordId = 1s, Time = 2000) ]
            let hs1 = HandShape(0s, 1000, 2000)
            let hs2 = HandShape(1s, 2000, 3000)
            let arr =
                InstrumentalArrangement(
                    Ebeats = beats,
                    Levels = ![ Level(Chords = chords, HandShapes = ![ hs1; hs2 ]) ]
                )

            HandShapeAdjuster.shortenHandshapes arr

            Expect.isTrue (hs1.EndTime < 2000) "Handshape was shortened"
            Expect.isTrue (hs1.StartTime < hs1.EndTime) "Handshape end comes after the start"

        testCase "Shortens length of a really short handshape" <| fun _ ->
            let beats = ![ Ebeat(500, -1s); Ebeat(1000, -1s); Ebeat(1500, -1s); Ebeat(2500, -1s) ]
            let chords = ![ Chord(Time = 1950); Chord(ChordId = 1s, Time = 2000) ]
            let hs1 = HandShape(0s, 1950, 2000)
            let hs2 = HandShape(1s, 2000, 3000)
            let arr =
                InstrumentalArrangement(
                    Ebeats = beats,
                    Levels = ![ Level(Chords = chords, HandShapes = ![ hs1; hs2 ]) ]
                )

            HandShapeAdjuster.shortenHandshapes arr

            Expect.isTrue (hs1.EndTime < 2000) "Handshape was shortened"
            Expect.isTrue (hs1.StartTime < hs1.EndTime) "Handshape end comes after the start"

        testCase "Does not fail on handshapes that exceed the last beat" <| fun _ ->
            let beats = ![ Ebeat(500, -1s); Ebeat(1000, -1s); Ebeat(1500, -1s); Ebeat(2500, -1s) ]
            let hs1 = HandShape(0s, 2500, 2600)
            let hs2 = HandShape(0s, 2600, 2800)
            let arr =
                InstrumentalArrangement(
                    Ebeats = beats,
                    Levels = ![ Level(HandShapes = ![ hs1; hs2 ]) ]
                )

            HandShapeAdjuster.shortenHandshapes arr

            Expect.isTrue (hs1.EndTime < 2600) "Handshape was shortened"

        testCase "Lengthens handshape when chord is at end of handshape" <| fun _ ->
            let beats = ![ Ebeat(500, -1s); Ebeat(1000, -1s); Ebeat(1500, -1s); Ebeat(2500, -1s) ]
            let chords = ![ Chord(Time = 1000); Chord(Time = 2000) ]
            let hs1 = HandShape(0s, 1000, 2000)
            let arr =
                InstrumentalArrangement(
                    Ebeats = beats,
                    Levels = ![ Level(Chords = chords, HandShapes = ![ hs1 ]) ]
                )

            HandShapeAdjuster.lengthenHandshapes arr

            Expect.equal hs1.EndTime 2250 "Handshape was lengthened by time of 16th note"

        testCase "Lengthens handshape when chord is at end of handshape and next note is very close" <| fun _ ->
            let beats = ![ Ebeat(500, -1s); Ebeat(1000, -1s); Ebeat(1500, -1s); Ebeat(2500, -1s) ]
            let chords = ![ Chord(Time = 1000); Chord(Time = 2000) ]
            let notes = ![ Note(Time = 2050) ]
            let hs1 = HandShape(0s, 1000, 2000)
            let arr =
                InstrumentalArrangement(
                    Ebeats = beats,
                    Levels = ![ Level(Chords = chords, Notes = notes, HandShapes = ![ hs1 ]) ]
                )

            HandShapeAdjuster.lengthenHandshapes arr

            Expect.equal hs1.EndTime 2025 "Handshape was lengthened"

        testCase "Lengthens handshape when chord is at end of handshape and next anchor is very close" <| fun _ ->
            let beats = ![ Ebeat(500, -1s); Ebeat(1000, -1s); Ebeat(1500, -1s); Ebeat(2500, -1s) ]
            let chords = ![ Chord(Time = 1000); Chord(Time = 2000) ]
            let anchors = ![ Anchor(4y, 2100) ]
            let hs1 = HandShape(0s, 1000, 2000)
            let arr =
                InstrumentalArrangement(
                    Ebeats = beats,
                    Levels = ![ Level(Chords = chords, Anchors = anchors, HandShapes = ![ hs1 ]) ]
                )

            HandShapeAdjuster.lengthenHandshapes arr

            Expect.equal hs1.EndTime 2050 "Handshape was lengthened"

        testCase "Test handshape handshape lengthening with two handshapes (lengthen both)" <| fun _ ->
            let beats = ![ for i in 1..20 -> Ebeat(i * 500, -1s) ]
            let chords = ![ Chord(Time = 1000); Chord(Time = 2090); Chord(Time = 3000); Chord(Time = 3500) ]
            let hs1 = HandShape(0s, 1000, 2100)
            let hs2 = HandShape(0s, 3000, 3500)
            let arr =
                InstrumentalArrangement(
                    Ebeats = beats,
                    Levels = ![ Level(Chords = chords, HandShapes = ![ hs1; hs2 ]) ]
                )

            HandShapeAdjuster.lengthenHandshapes arr

            Expect.equal hs1.EndTime 2225 "1st handshape was lengthened"
            Expect.equal hs2.EndTime 3625 "2nd handshape was lengthened"

        testCase "Test handshape handshape lengthening with two handshapes (lengthen second)" <| fun _ ->
            let beats = ![ for i in 1..20 -> Ebeat(i * 500, -1s) ]
            let chords = ![ Chord(Time = 1000); Chord(Time = 2000); Chord(Time = 2150); Chord(Time = 2500) ]
            let hs1 = HandShape(0s, 1000, 2100)
            let hs2 = HandShape(0s, 2150, 2500)
            let arr =
                InstrumentalArrangement(
                    Ebeats = beats,
                    Levels = ![ Level(Chords = chords, HandShapes = ![ hs1; hs2 ]) ]
                )

            HandShapeAdjuster.lengthenHandshapes arr

            Expect.equal hs1.EndTime 2100 "1st handshape was not lengthened"
            Expect.equal hs2.EndTime 2625 "2nd handshape was lengthened"
    ]

let anchorMoverTests =
    testList "Arrangement Improver (Anchor mover)" [
        testCase "Anchor before note is moved" <| fun _ ->
            let anchors = ![ Anchor(1y, 99) ]
            let notes = ![ Note(Time = 100, Fret = 1y) ]
            let level = Level(Notes = notes, Anchors = anchors)
            let arr = InstrumentalArrangement(Levels = ![ level ])

            AnchorMover.improve arr

            Expect.equal level.Anchors[0].Time 100 "Anchor was moved by 1ms"

        testCase "Anchor after note by 5ms is moved" <| fun _ ->
            let anchors = ![ Anchor(1y, 105) ]
            let notes = ![ Note(Time = 100, Fret = 1y) ]
            let level = Level(Notes = notes, Anchors = anchors)
            let arr = InstrumentalArrangement(Levels = ![ level ])

            AnchorMover.improve arr

            Expect.equal level.Anchors[0].Time 100 "Anchor was moved"

        testCase "Anchor after note by 6ms is not moved" <| fun _ ->
            let anchors = ![ Anchor(1y, 106) ]
            let notes = ![ Note(Time = 100, Fret = 1y) ]
            let level = Level(Notes = notes, Anchors = anchors)
            let arr = InstrumentalArrangement(Levels = ![ level ])

            AnchorMover.improve arr

            Expect.equal level.Anchors[0].Time 106 "Anchor was not moved"

        testCase "Anchor after chord is moved" <| fun _ ->
            let anchors = ![ Anchor(1y, 102) ]
            let chords = ![ Chord(Time = 100) ]
            let level = Level(Chords = chords, Anchors = anchors)
            let arr = InstrumentalArrangement(Levels = ![ level ])

            AnchorMover.improve arr

            Expect.equal level.Anchors[0].Time 100 "Anchor was moved by 2ms"

        testCase "Anchor on note that is very close to another note is not moved" <| fun _ ->
            let anchors = ![ Anchor(1y, 100) ]
            let notes = ![ Note(Time = 100, Fret = 1y); Note(Time = 103, Fret = 3y) ]
            let level = Level(Notes = notes, Anchors = anchors)
            let arr = InstrumentalArrangement(Levels = ![ level ])

            AnchorMover.improve arr

            Expect.equal level.Anchors[0].Time 100 "Anchor was not moved"

        testCase "Anchor at the end of a slide that is very close to another note is not moved" <| fun _ ->
            let anchors = ![ Anchor(1y, 100); Anchor(3y, 300) ]
            let notes = ![ Note(Time = 100, Sustain = 200, Fret = 1y, SlideTo = 3y); Note(Time = 303, Fret = 3y) ]
            let level = Level(Notes = notes, Anchors = anchors)
            let arr = InstrumentalArrangement(Levels = ![ level ])

            AnchorMover.improve arr

            Expect.equal level.Anchors[1].Time 300 "Anchor was not moved"
    ]

[<Tests>]
let applyAllTests =
    testList "Arrangement Improver (Apply All Fixes)" [
        testCase "Extra anchors are not created when moving phrases" <| fun _ ->
            let beats = ![ Ebeat(900, 0s); Ebeat(1000, -1s); Ebeat(1200, -1s) ]
            let phrases = ![ Phrase("mover2", 0uy, PhraseMask.None); Phrase("END", 0uy, PhraseMask.None) ]
            let iterations = ![ PhraseIteration(1000, 0); PhraseIteration(1900, 1) ]
            let notes = ![ Note(Time = 1000); Note(Time = 1200) ]
            let anchors = ![ Anchor(1y, 1200) ]
            let arr =
                InstrumentalArrangement(
                    Phrases = phrases,
                    PhraseIterations = iterations,
                    Levels = ![ Level(Notes = notes, Anchors = anchors) ],
                    Ebeats = beats
                )
            arr.MetaData.SongLength <- 2000

            ArrangementImprover.applyAll arr

            Expect.hasLength anchors 1 "No new anchors were created"
            Expect.equal anchors.[0].Time 1200 "Anchor is at correct position"
    ]

[<Tests>]
let unnecessaryNoteRemoverTests =
    testList "Arrangement Improver (Note Remover)" [
        testCase "Removes notes without sustain after a linknext note" <| fun _ ->
            let anchors = ![ Anchor(1y, 100) ]
            let notes = ![
                Note(Time = 100, Fret = 1y, Sustain = 100, IsLinkNext = true)
                Note(Time = 150, Fret = 3y, String = 1y)
                Note(Time = 200, Fret = 1y, Sustain = 0)
            ]
            let level = Level(Notes = notes, Anchors = anchors)
            let arr = InstrumentalArrangement(Levels = ![ level ])

            ArrangementImprover.removeUnnecessaryNotes arr

            Expect.hasLength arr.Levels[0].Notes 2 "One note was removed"
            Expect.equal arr.Levels[0].Notes[0].Time 100 "First note was not removed"
            Expect.isFalse arr.Levels[0].Notes[0].IsLinkNext "Linknext was removed from first note"
            Expect.equal arr.Levels[0].Notes[1].Time 150 "Unrelated note was not removed"

        testCase "Does not remove note with sustain after a linknext note" <| fun _ ->
            let anchors = ![ Anchor(1y, 100) ]
            let notes = ![
                Note(Time = 100, Fret = 1y, Sustain = 100, IsLinkNext = true)
                Note(Time = 200, Fret = 1y, Sustain = 5)
            ]
            let level = Level(Notes = notes, Anchors = anchors)
            let arr = InstrumentalArrangement(Levels = ![ level ])

            ArrangementImprover.removeUnnecessaryNotes arr

            Expect.hasLength arr.Levels[0].Notes 2 "No notes were removed"
            Expect.isTrue arr.Levels[0].Notes[0].IsLinkNext "Linknext was not removed from first note"

        testCase "Removes note without sustain after a chord" <| fun _ ->
            let anchors = ![ Anchor(1y, 100) ]
            let cn = ![
                Note(Time = 100, String = 2y, Fret = 1y, Sustain = 100, IsLinkNext = true)
                Note(Time = 100, String = 3y, Fret = 3y, Sustain = 100, IsLinkNext = true)
            ]
            let chords = ![
                Chord(Time = 100, ChordId = 0s, IsLinkNext = true, ChordNotes = cn)
            ]
            let notes = ![
                Note(Time = 200, Fret = 1y, String = 2y, Sustain = 0)
                Note(Time = 200, Fret = 3y, String = 3y, Sustain = 100)
            ]
            let level = Level(Notes = notes, Chords = chords, Anchors = anchors)
            let arr = InstrumentalArrangement(Levels = ![ level ])

            ArrangementImprover.removeUnnecessaryNotes arr

            Expect.hasLength arr.Levels[0].Notes 1 "One note was removed"
            Expect.equal arr.Levels[0].Notes[0].String 3y "Note with sustain was not removed"
            Expect.isTrue arr.Levels[0].Chords[0].IsLinkNext "Linknext was not removed from chord"

        testCase "Removes note without sustain after a chord slide" <| fun _ ->
            let anchors = ![ Anchor(1y, 100); Anchor(3y, 200) ]
            let cn = ![
                Note(Time = 100, String = 2y, Fret = 1y, Sustain = 100, SlideTo = 3y, IsLinkNext = true)
                Note(Time = 100, String = 3y, Fret = 3y, Sustain = 100, SlideTo = 5y, IsLinkNext = true)
            ]
            let chords = ![
                Chord(Time = 100, ChordId = 0s, IsLinkNext = true, ChordNotes = cn)
            ]
            let notes = ![
                Note(Time = 200, Fret = 3y, String = 2y, Sustain = 100)
                Note(Time = 200, Fret = 5y, String = 3y, Sustain = 0)
            ]
            let level = Level(Notes = notes, Chords = chords, Anchors = anchors)
            let arr = InstrumentalArrangement(Levels = ![ level ])

            ArrangementImprover.removeUnnecessaryNotes arr

            Expect.hasLength arr.Levels[0].Notes 1 "One note was removed"
            Expect.equal arr.Levels[0].Notes[0].Fret 3y "Note with sustain was not removed"
            Expect.isTrue arr.Levels[0].Chords[0].IsLinkNext "Linknext was not removed from chord"

        testCase "Removes all notes without sustain after a chord slide" <| fun _ ->
            let anchors = ![ Anchor(1y, 100); Anchor(3y, 200) ]
            let cn = ![
                Note(Time = 100, String = 2y, Fret = 1y, Sustain = 100, SlideTo = 3y, IsLinkNext = true)
                Note(Time = 100, String = 3y, Fret = 3y, Sustain = 100, SlideTo = 5y, IsLinkNext = true)
            ]
            let chords = ![
                Chord(Time = 100, ChordId = 0s, IsLinkNext = true, ChordNotes = cn)
            ]
            let notes = ![
                Note(Time = 200, Fret = 3y, String = 2y, Sustain = 0)
                Note(Time = 200, Fret = 5y, String = 3y, Sustain = 0)
            ]
            let level = Level(Notes = notes, Chords = chords, Anchors = anchors)
            let arr = InstrumentalArrangement(Levels = ![ level ])

            ArrangementImprover.removeUnnecessaryNotes arr

            Expect.hasLength arr.Levels[0].Notes 0 "All notes without sustain were removed"
            Expect.isFalse arr.Levels[0].Chords[0].IsLinkNext "Linknext was removed from the chord"
    ]
