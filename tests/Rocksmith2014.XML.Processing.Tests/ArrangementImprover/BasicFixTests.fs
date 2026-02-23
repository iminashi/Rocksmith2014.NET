module Rocksmith2014.XML.Processing.Tests.BasicFixTests

open Expecto
open Rocksmith2014.XML
open Rocksmith2014.XML.Processing

[<Tests>]
let basicFixTests =
    testList "Arrangement Improver (Basic Fixes)" [
        testCase "Filters characters in phrase names" <| fun _ ->
            let phrases = ![
                Phrase("\"TEST\"", 0uy, PhraseMask.None)
                Phrase("'TEST'_(2)", 0uy, PhraseMask.None)
            ]
            let arr = InstrumentalArrangement(Phrases = phrases)

            BasicFixes.validatePhraseNames arr

            Expect.equal phrases.[0].Name "TEST" "First phrase name was changed"
            Expect.equal phrases.[1].Name "TEST_2" "Second phrase name was changed"

        testCase "Ignore is added to 23rd and 24th fret notes" <| fun _ ->
            let notes = ![ Note(Time = 1000, Fret = 5y); Note(Time = 1200, Fret = 23y); Note(Time = 1300, Fret = 24y) ]
            let arr = InstrumentalArrangement(Levels = ![ Level(Notes = notes) ])

            BasicFixes.addIgnores arr

            Expect.isFalse notes.[0].IsIgnore "First note is not ignored"
            Expect.isTrue notes.[1].IsIgnore "Second note is ignored"
            Expect.isTrue notes.[2].IsIgnore "Third note is ignored"

        testCase "Ignore is added to 7th fret harmonic with sustain" <| fun _ ->
            let notes = ![
                Note(Time = 1000, Fret = 7y, Sustain = 500, IsHarmonic = true)
                Note(Time = 2000, Fret = 7y, Sustain = 0, IsHarmonic = true)
            ]
            let arr = InstrumentalArrangement(Levels = ![ Level(Notes = notes) ])

            BasicFixes.addIgnores arr

            Expect.isTrue notes.[0].IsIgnore "First note is ignored"
            Expect.isFalse notes.[1].IsIgnore "Second note is not ignored"

        testCase "Ignore is added to chord with 23rd and 24th fret notes" <| fun _ ->
            let noFingers = [| -1y; -1y; -1y; -1y; -1y; -1y |]
            let templates = ![
                ChordTemplate("", "", noFingers, [| -1y; 0y; 23y; -1y; -1y; -1y; |])
                ChordTemplate("", "", noFingers, [| -1y; -1y; -1y; -1y; 22y; 24y; |])
            ]
            let chords = ![ Chord(Time = 1000, ChordId = 0s); Chord(Time = 1200, ChordId = 1s) ]
            let arr = InstrumentalArrangement(Levels = ![ Level(Chords = chords) ], ChordTemplates = templates)

            BasicFixes.addIgnores arr

            Expect.isTrue chords.[0].IsIgnore "First chord is ignored"
            Expect.isTrue chords.[1].IsIgnore "Second chord is ignored"

        testCase "Ignore is added to chord with 7th fret harmonic with sustain" <| fun _ ->
            let noFingers = [| -1y; -1y; -1y; -1y; -1y; -1y |]
            let templates = ![
                ChordTemplate("", "", noFingers, [| -1y; 7y; 7y; -1y; -1y; -1y; |])
            ]
            let cn = ![
                Note(Time = 1000, Sustain = 500, Fret = 7y, String = 1y, IsHarmonic = true)
                Note(Time = 1000, Sustain = 500, Fret = 7y, String = 2y, IsHarmonic = true)
            ]
            let chords = ![ Chord(Time = 1000, ChordId = 0s, ChordNotes = cn) ]
            let arr = InstrumentalArrangement(Levels = ![ Level(Chords = chords) ], ChordTemplates = templates)

            BasicFixes.addIgnores arr

            Expect.isTrue chords.[0].IsIgnore "Chord is ignored"

        testCase "Incorrect linknext is removed (next note on same string not found)" <| fun _ ->
            let notes = ![ Note(Time = 1000, Fret = 5y, IsLinkNext = true); Note(Time = 1500, String = 4y, Fret = 5y) ]
            let arr = InstrumentalArrangement(Levels = ![ Level(Notes = notes) ])

            BasicFixes.fixLinkNexts arr

            Expect.isFalse notes.[0].IsLinkNext "Linknext was removed"

        testCase "Incorrect linknext is removed (next note too far)" <| fun _ ->
            let notes = ![ Note(Time = 1000, Fret = 5y, IsLinkNext = true); Note(Time = 2000, Fret = 5y) ]
            let arr = InstrumentalArrangement(Levels = ![ Level(Notes = notes) ])

            BasicFixes.fixLinkNexts arr

            Expect.isFalse notes.[0].IsLinkNext "Linknext was removed"

        testCase "Incorrect linknext fret is corrected" <| fun _ ->
            let notes = ![
                Note(Time = 1000, Fret = 5y, Sustain = 500, IsLinkNext = true)
                Note(Time = 1500, Fret = 6y)
            ]
            let arr = InstrumentalArrangement(Levels = ![ Level(Notes = notes) ])

            BasicFixes.fixLinkNexts arr

            Expect.isTrue notes.[0].IsLinkNext "Linknext was not removed"
            Expect.equal notes.[1].Fret 5y "Fret was corrected"

        testCase "Incorrect linknext fret is corrected (Slide)" <| fun _ ->
            let notes = ![
                Note(Time = 1000, Fret = 5y, Sustain = 500, IsLinkNext = true, SlideTo = 9y)
                Note(Time = 1500, Fret = 10y)
            ]
            let arr = InstrumentalArrangement(Levels = ![ Level(Notes = notes) ])

            BasicFixes.fixLinkNexts arr

            Expect.isTrue notes.[0].IsLinkNext "Linknext was not removed"
            Expect.equal notes.[1].Fret 9y "Fret was corrected"

        testCase "Incorrect linknext fret is corrected (Unpitched slide)" <| fun _ ->
            let notes = ![
                Note(Time = 1000, Fret = 5y, Sustain = 500, IsLinkNext = true, SlideUnpitchTo = 9y)
                Note(Time = 1500, Fret = 10y)
            ]
            let arr = InstrumentalArrangement(Levels = ![ Level(Notes = notes) ])

            BasicFixes.fixLinkNexts arr

            Expect.isTrue notes.[0].IsLinkNext "Linknext was not removed"
            Expect.equal notes.[1].Fret 9y "Fret was corrected"

        testCase "Incorrect linknext fret is corrected (Bend)" <| fun _ ->
            let bv = ![ BendValue(1200, 2.0f) ]
            let notes = ![
                Note(Time = 1000, Fret = 5y, Sustain = 500, IsLinkNext = true, BendValues = bv)
                Note(Time = 1500, Fret = 10y)
            ]
            let arr = InstrumentalArrangement(Levels = ![ Level(Notes = notes) ])

            BasicFixes.fixLinkNexts arr

            Expect.isTrue notes.[1].IsBend "Second note has bend"
            Expect.equal notes.[1].MaxBend 2.0f "Max bend is correct"
            Expect.equal notes.[1].BendValues[0].Step 2.0f "Bend value step is correct"
            Expect.equal notes.[1].BendValues[0].Time 1500 "Bend value time is correct"

        testCase "Overlapping bend values are removed" <| fun _ ->
            let bv1 = ![ BendValue(1200, 2.0f); BendValue(1200, 1.0f) ]
            let bv2 = ![ BendValue(2100, 2.0f); BendValue(2100, 2.0f) ]
            let notes = ![
                Note(Time = 1000, Fret = 5y, Sustain = 500, IsLinkNext = true, BendValues = bv1)
            ]
            let chords = ![
                Chord(Time = 2000, ChordNotes = ![ Note(Time = 2000, Sustain = 500, BendValues = bv2) ])
            ]
            let arr = InstrumentalArrangement(Levels = ![ Level(Notes = notes, Chords = chords) ])

            BasicFixes.removeOverlappingBendValues arr

            Expect.hasLength notes[0].BendValues 1 "Bend value was removed from note"
            Expect.hasLength chords[0].ChordNotes[0].BendValues 1 "Bend value was removed from chord note"

        testCase "Muted strings are removed from non-muted chords" <| fun _ ->
            let templates = ![
                ChordTemplate("", "", [| 1y; 3y; 4y; -1y; -1y; -1y |], [| 1y; 3y; 3y; -1y; -1y; -1y; |])
                ChordTemplate("", "", [| -1y; -1y; -1y; -1y; -1y; -1y |], [| 0y; 0y; 0y; -1y; -1y; -1y; |])
            ]
            let cn1 = ![
                Note(Time = 1000, String = 0y, Fret = 1y)
                Note(Time = 1000, String = 1y, Fret = 3y, IsFretHandMute = true)
                Note(Time = 1000, String = 2y, Fret = 3y)
            ]
            let cn2 = ![
                Note(Time = 1200, String = 0y, Fret = 0y, IsFretHandMute = true)
                Note(Time = 1200, String = 1y, Fret = 0y, IsFretHandMute = true)
                Note(Time = 1200, String = 2y, Fret = 0y, IsFretHandMute = true)
            ]
            let chords = ![
                Chord(Time = 1000, ChordId = 0s, ChordNotes = cn1)
                // Chord with all muted notes, but not marked as muted
                Chord(Time = 1200, ChordId = 1s, ChordNotes = cn2)
            ]
            let arr = InstrumentalArrangement(Levels = ![ Level(Chords = chords) ], ChordTemplates = templates)

            BasicFixes.removeMutedNotesFromChords arr

            Expect.hasLength chords.[0].ChordNotes 2 "Chord note was removed from first chord"
            Expect.isFalse (chords.[0].ChordNotes.Exists(fun n -> n.IsFretHandMute)) "Fret-hand mute was removed"
            Expect.hasLength chords.[1].ChordNotes 3 "Chord notes were not removed from second chord"
            Expect.equal templates[0].Fingers[1] -1y "Fingering was removed from first chord template"
            Expect.equal templates[0].Frets[1] -1y "String was removed from first chord template"
            Expect.sequenceContainsOrder templates[1].Frets [| 0y; 0y; 0y; -1y; -1y; -1y; |] "Second chord template was not modified"

        testCase "Redundant anchors are removed" <| fun _ ->
            let anchors = ![
                Anchor(1y, 1000, 4y)
                Anchor(1y, 2000, 4y)

                Anchor(5y, 3000, 4y)
                Anchor(5y, 4000, 6y)
            ]
            let arr = InstrumentalArrangement(Levels = ![ Level(Anchors = anchors) ])

            BasicFixes.removeRedundantAnchors arr

            let expectedResult =
                [
                    Anchor(1y, 1000, 4y)
                    Anchor(5y, 3000, 4y)
                    Anchor(5y, 4000, 6y)
                ]

            Expect.hasLength arr.Levels[0].Anchors 3 "One anchor was removed"
            Expect.sequenceContainsOrder arr.Levels[0].Anchors expectedResult "Anchors are correct"

        testCase "Identical anchor at phrase time is not removed" <| fun _ ->
            let anchors = ![
                Anchor(1y, 1000, 4y)
                Anchor(1y, 2000, 4y)
                Anchor(1y, 3000, 4y)
                Anchor(1y, 4000, 4y)

                Anchor(1y, 5000, 5y)
            ]
            let arr = InstrumentalArrangement(Levels = ![ Level(Anchors = anchors) ])
            arr.PhraseIterations <- ![ PhraseIteration(1000, 0); PhraseIteration(4000, 0) ]

            BasicFixes.removeRedundantAnchors arr

            let expectedResult =
                [
                    Anchor(1y, 1000, 4y)
                    Anchor(1y, 4000, 4y)
                    Anchor(1y, 5000, 5y)
                ]

            Expect.sequenceContainsOrder arr.Levels[0].Anchors expectedResult "Anchors are correct"
    ]
