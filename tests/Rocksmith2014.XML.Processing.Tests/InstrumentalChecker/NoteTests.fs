module NoteTests

open Expecto
open Rocksmith2014.XML
open Rocksmith2014.XML.Processing.Types
open Rocksmith2014.XML.Processing.InstrumentalChecker
open TestArrangement

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

            Expect.isEmpty results "An issue was found in check results"

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

            Expect.isEmpty results "An issue was found in check results"

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

            Expect.isEmpty results "An issue was found in check results"

        testCase "Detects hammer-on into same fret" <| fun _ ->
            let notes = ResizeArray(seq { Note(Fret = 1y, Time = 1300)
                                          Note(Fret = 1y, Time = 1800, IsHammerOn = true) })
            let level = Level(Notes = notes)
            let arr = InstrumentalArrangement(Phrases = phrases, Levels = ResizeArray([ level ]))

            let results = checkNotes arr level

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.Type HopoIntoSameNote "Correct issue type"

        testCase "No false positive for hammer-on from nowhere" <| fun _ ->
            let notes = ResizeArray(seq {
                Note(Fret = 5y, String = 2y, Time = 1000)
                Note(Fret = 3y, String = 3y, Time = 1100)
                Note(Fret = 5y, String = 3y, Time = 1200)
                Note(Fret = 3y, String = 3y, Time = 1300, IsPullOff = true)
                Note(Fret = 5y, String = 2y, Time = 1400, IsHammerOn = true) })
            let level = Level(Notes = notes)
            let arr = InstrumentalArrangement(Phrases = phrases, Levels = ResizeArray([ level ]))

            let results = checkNotes arr level

            Expect.isEmpty results "An issue was found in check results"

        testCase "Detects pull-off into same fret" <| fun _ ->
            let notes = ResizeArray(seq { Note(Fret = 1y, Time = 1300)
                                          Note(Fret = 1y, Time = 1800, IsPullOff = true) })
            let level = Level(Notes = notes)
            let arr = InstrumentalArrangement(Phrases = phrases, Levels = ResizeArray([ level ]))

            let results = checkNotes arr level

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.Type HopoIntoSameNote "Correct issue type"

        testCase "Detects finger change during slide" <| fun _ ->
            let notes = ResizeArray(seq { Note(Fret = 1y, Time = 1300, IsLinkNext = true, SlideTo = 5y, Sustain = 500)
                                          Note(Fret = 5y, Time = 1800) })
            let anchors = ResizeArray(seq { Anchor(1y, 1300); Anchor(3y, 1800)})
            let level = Level(Notes = notes, Anchors = anchors)
            let arr = InstrumentalArrangement(Phrases = phrases, Levels = ResizeArray([ level ]))

            let results = checkNotes arr level

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.Type FingerChangeDuringSlide "Correct issue type"

        testCase "Detects position shift into pull-off" <| fun _ ->
            let notes = ResizeArray(seq { Note(Fret = 10y, Time = 1300)
                                          Note(Fret = 5y, Time = 1800, IsPullOff = true) })
            let anchors = ResizeArray(seq { Anchor(10y, 1300); Anchor(5y, 1800)})
            let level = Level(Notes = notes, Anchors = anchors)
            let arr = InstrumentalArrangement(Phrases = phrases, Levels = ResizeArray([ level ]))

            let results = checkNotes arr level

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.Type PositionShiftIntoPullOff "Correct issue type"

        testCase "Position shift into open string pull-off is ignored" <| fun _ ->
            let notes = ResizeArray(seq { Note(Fret = 3y, Time = 1300, SlideUnpitchTo = 1y)
                                          Note(Fret = 0y, Time = 1800, IsPullOff = true) })
            let anchors = ResizeArray(seq { Anchor(3y, 1300); Anchor(1y, 1800)})
            let level = Level(Notes = notes, Anchors = anchors)
            let arr = InstrumentalArrangement(Phrases = phrases, Levels = ResizeArray([ level ]))

            let results = checkNotes arr level

            Expect.hasLength results 0 "No issues should be created"

        testCase "Overlapping bend values are detected" <| fun _ ->
            let notes = ResizeArray(seq {
                Note(Fret = 3y, BendValues = ResizeArray([ BendValue(500, 1.0f); BendValue(500, 1.0f) ]))
            })
            let level = Level(Notes = notes)
            let arr = InstrumentalArrangement(Levels = ResizeArray([ level ]))

            let results = checkNotes arr level

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.Type OverlappingBendValues "Correct issue type"

        testCase "Natural harmonic with bend is detected" <| fun _ ->
            let notes = ResizeArray(seq {
                Note(Fret = 12y, IsHarmonic = true, Sustain = 1000, MaxBend = 2.0f, BendValues = ResizeArray([ BendValue(500, 2.0f) ]))
            })
            let level = Level(Notes = notes)
            let arr = InstrumentalArrangement(Levels = ResizeArray([ level ]))

            let results = checkNotes arr level

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.Type NaturalHarmonicWithBend "Correct issue type"
    ]
