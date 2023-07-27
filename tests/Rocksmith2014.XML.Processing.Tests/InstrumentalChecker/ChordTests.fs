module ChordTests

open Expecto
open Rocksmith2014.XML
open Rocksmith2014.XML.Processing
open Rocksmith2014.XML.Processing.InstrumentalChecker
open TestArrangement

let private withSongLength (arr: InstrumentalArrangement) =
    arr |> apply (fun a -> a.MetaData.SongLength <- 500_000)

[<Tests>]
let chordTests =
    testList "Arrangement Checker (Chords)" [
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

            Expect.isEmpty results "An issue was found in check results"

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

        testCase "Detects non-muted chords with that contain muted strings" <| fun _ ->
            // Invalid chord notes (muted and not muted)
            let cn1 =
                ResizeArray(seq {
                    Note(String = 0y, Time = 1000, IsFretHandMute = true)
                    Note(String = 1y, Time = 1000)
                })
            // Valid chord notes (all muted)
            let cn2 =
                ResizeArray(seq {
                    Note(String = 0y, Time = 1000, IsFretHandMute = true)
                    Note(String = 1y, Time = 1000, IsFretHandMute = true)
                })
            let chords =
                ResizeArray(seq {
                    Chord(ChordId = 0s, ChordNotes = cn1)
                    Chord(ChordId = 0s, ChordNotes = cn2)
                })
            let level = Level(Chords = chords)

            let results = checkChords testArr level

            Expect.hasLength results 1 "One issue created"
            Expect.all results (fun x -> x.Type = MutedStringInNonMutedChord) "Correct issue types"

        testCase "Detects position shift into pull-off (double stops)" <| fun _ ->
            let cn1 =
                ResizeArray(seq {
                    Note(String = 4y, Fret = 12y, Time = 1000)
                    Note(String = 5y, Fret = 12y, Time = 1000)
                })
            let cn2 =
                ResizeArray(seq {
                    Note(String = 4y, Fret = 10y, Time = 1500, IsPullOff = true)
                    Note(String = 5y, Fret = 10y, Time = 1500, IsPullOff = true)
                })
            let chords =
                ResizeArray(seq {
                    Chord(ChordId = 0s, Time = 1000, ChordNotes = cn1)
                    Chord(ChordId = 0s, Time = 1500, ChordNotes = cn2)
                })
            let anchors = ResizeArray(seq { Anchor(12y, 1000); Anchor(10y, 1500) })
            let level = Level(Chords = chords, Anchors = anchors)
            let arr = InstrumentalArrangement(Phrases = phrases, Levels = ResizeArray([ level ])) |> withSongLength

            let results = checkChords arr level

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.Type PositionShiftIntoPullOff "Correct issue type"

        testCase "Overlapping bend values are detected for chords" <| fun _ ->
            let cn1 =
                ResizeArray(seq {
                    Note(String = 4y, Fret = 12y)
                    Note(String = 5y, Fret = 12y, BendValues = ResizeArray([ BendValue(200, 2.0f); BendValue(200, 1.0f) ]))
                })
            let chords =
                ResizeArray(seq {
                    Chord(ChordId = 0s, Time = 1000, ChordNotes = cn1)
                })
            let level = Level(Chords = chords)

            let results = checkChords testArr level

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.Type OverlappingBendValues "Correct issue type"

        testCase "Invalid strings on bass arrangement are detected for chords" <| fun _ ->
            let cn1 =
                ResizeArray(seq {
                    Note(String = 3y, Fret = 12y)
                    Note(String = 4y, Fret = 12y)
                    Note(String = 5y, Fret = 12y)
                })
            let chords =
                ResizeArray(seq {
                    Chord(ChordId = 0s, Time = 1000, ChordNotes = cn1)
                })
            let level = Level(Chords = chords)
            let arr = InstrumentalArrangement(Levels = ResizeArray([ level ])) |> withSongLength
            arr.MetaData.ArrangementProperties.PathBass <- true

            let results = checkChords arr level

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.Head.Type InvalidBassArrangementString "Correct issue type"

        testCase "Detects chord after END phrase" <| fun _ ->
            let chords = ResizeArray(seq { Chord(Time = 50_000) })
            let level = Level(Chords = chords)
            let phrases = ResizeArray(seq { Phrase("Default", 0uy, PhraseMask.None); Phrase("end", 0uy, PhraseMask.None) })
            let phraseIterations = ResizeArray(seq { PhraseIteration(1000, 0); PhraseIteration(45_000, 1) })
            let arr =
                InstrumentalArrangement(Levels = ResizeArray.singleton level, Phrases = phrases, PhraseIterations = phraseIterations)
                |> withSongLength

            let results = checkChords arr level

            Expect.hasLength results 1 "One issue created"
            Expect.equal results[0].Type NoteAfterSongEnd "Correct issue type"
            Expect.equal results[0].TimeCode 50_000 "Correct issue time"

        testCase "Detects chords with techniques that require sustain" <| fun _ ->
            let cn1 = ResizeArray(seq { Note(Fret = 1y, Time = 1_000, SlideTo = 2y, Sustain = 0) })
            let cn2 = ResizeArray(seq { Note(Fret = 1y, Time = 2_000, Vibrato = 80uy, Sustain = 1) })
            let cn3 = ResizeArray(seq { Note(Fret = 1y, Time = 3_000, IsTremolo = true, Sustain = 2) })
            let cn4 = ResizeArray(seq { Note(Fret = 1y, Time = 4_000, SlideUnpitchTo = 7y, Sustain = 4) })
            let chords = ResizeArray(seq {
                Chord(Time = 1_000, ChordNotes = cn1)
                Chord(Time = 2_000, ChordNotes = cn2)
                Chord(Time = 3_000, ChordNotes = cn3)
                Chord(Time = 4_000, ChordNotes = cn4)
            })
            let level = Level(Chords = chords)

            let results = checkChords testArr level

            Expect.hasLength results 4 "Four issue created"
            Expect.all results (fun issue -> issue.Type = TechniqueNoteWithoutSustain) "Correct issue types"
    ]
