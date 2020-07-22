module Rocksmith2014.Conversion.Tests.XmlObjectsToSngTests

open Expecto
open Rocksmith2014
open Rocksmith2014.XML
open Rocksmith2014.Conversion
open Rocksmith2014.Conversion.Utils
open System.Globalization
open System
open Rocksmith2014.Conversion.XmlToSng

/// Testing function that converts a time in milliseconds into seconds without floating point arithmetic.
let timeConversion (time:int) =
    Single.Parse(Utils.TimeCodeToString(time), NumberFormatInfo.InvariantInfo)

let createTestArr () =
    let arr = InstrumentalArrangement()
    arr.SongLength <- 4784_455

    let f1 = [| 1y;1y;1y;1y;1y;1y |]
    let f2 = [| 1y;1y;-1y;-1y;-1y;-1y |]
    arr.ChordTemplates.Add(ChordTemplate("A", "A", f1, f1))
    arr.ChordTemplates.Add(ChordTemplate("A", "A-arp", f2, f2))

    arr.PhraseIterations.Add(PhraseIteration(Time = 1000, PhraseId = 1))
    arr.PhraseIterations.Add(PhraseIteration(Time = 2000, PhraseId = 1))
    arr.PhraseIterations.Add(PhraseIteration(Time = 3000, PhraseId = 77))
    arr.PhraseIterations.Add(PhraseIteration(Time = 7554_100, PhraseId = 2))
    arr.PhraseIterations.Add(PhraseIteration(Time = 7555_000, PhraseId = 3))

    arr.Tuning.SetTuning(-1s, 2s, 4s, -5s, 3s, -2s)

    arr.Sections.Add(Section("1", 1000, 1s))
    arr.Sections.Add(Section("2", 4000, 1s))
    arr.Sections.Add(Section("3", 8000_000, 1s))

    arr.Events.Add(Event("e0", 1000))
    arr.Events.Add(Event("dna_none", 2500))
    arr.Events.Add(Event("dna_solo", 3500))
    arr.Events.Add(Event("dna_chord", 4500))
    arr.Events.Add(Event("dna_riff", 5500))

    arr.Ebeats.Add(Ebeat(1000, 0s))
    arr.Ebeats.Add(Ebeat(1200, -1s))

    let lvl = Level(0y)
    lvl.Anchors.Add(Anchor(8y, 1000))
    lvl.Anchors.Add(Anchor(7y, 2000, 5y))

    arr.Levels.Add(lvl)
    arr

let sharedAccData = XmlToSng.AccuData.Init (createTestArr())

let createNoteTimes (level:XML.Level) =
    let chords =
        level.Chords
        |> Seq.map (fun c -> c.Time)
    level.Notes
    |> Seq.map (fun n -> n.Time)
    |> Seq.append chords
    |> Seq.sort
    |> Seq.toArray

let createNoteConvertFunction (accuData:XmlToSng.AccuData) (arr:InstrumentalArrangement) (level:Level) =
    let noteTimes = createNoteTimes level
    let hs = XmlToSng.createFingerprintMap noteTimes level
    let piNotes = XmlToSng.divideNoteTimesPerPhraseIteration noteTimes arr
    XmlToSng.convertNote() piNotes hs accuData arr

[<Tests>]
let sngToXmlConversionTests =
  testList "XML Objects → SNG Objects" [

    testCase "Beat (Strong)" <| fun _ ->
      let b = Ebeat(3666, 2s)
      let convert = XmlToSng.convertBeat ()
      let testArr = createTestArr()

      let sng = convert testArr b

      Expect.equal sng.Time (timeConversion b.Time) "Time is same"
      Expect.equal sng.Measure b.Measure "Measure is correct"
      Expect.equal sng.Beat 0s "Beat is correct"
      Expect.equal sng.PhraseIteration 2 "Phrase iteration is correct"
      Expect.isTrue ((sng.Mask &&& SNG.Types.BeatMask.FirstBeatOfMeasure) <> SNG.Types.BeatMask.None) "First beat flag is set"
      Expect.isTrue ((sng.Mask &&& SNG.Types.BeatMask.EvenMeasure) <> SNG.Types.BeatMask.None) "Even measure flag is set"

    testCase "Beat (Weak)" <| fun _ ->
      let b = Ebeat(3666, -1s)
      let convert = XmlToSng.convertBeat ()
      let testArr = createTestArr()

      let sng = convert testArr b

      Expect.isTrue ((sng.Mask &&& SNG.Types.BeatMask.FirstBeatOfMeasure) = SNG.Types.BeatMask.None) "First beat flag is not set"

    testCase "Beats" <| fun _ ->
      let b0 = Ebeat(3000, 1s)
      let b1 = Ebeat(3100, -1s)
      let b2 = Ebeat(3200, -1s)
      let b3 = Ebeat(3300, 2s)
      let b4 = Ebeat(3400, -1s)
      let convert = XmlToSng.convertBeat ()
      let testArr = createTestArr()

      let sngB0 = convert testArr b0
      let sngB1 = convert testArr b1
      let sngB2 = convert testArr b2
      let sngB3 = convert testArr b3
      let sngB4 = convert testArr b4

      Expect.isTrue ((sngB0.Mask &&& SNG.Types.BeatMask.FirstBeatOfMeasure) <> SNG.Types.BeatMask.None) "B0: First beat flag is set"
      Expect.isTrue ((sngB0.Mask &&& SNG.Types.BeatMask.EvenMeasure) = SNG.Types.BeatMask.None) "B0: Even measure flag is not set"
      Expect.equal sngB1.Beat 1s "B1: Is second beat of measure"
      Expect.equal sngB2.Measure 1s "B2: Is in measure 1"
      Expect.equal sngB3.Measure 2s "B2: Is in measure 1"
      Expect.isTrue ((sngB3.Mask &&& SNG.Types.BeatMask.EvenMeasure) <> SNG.Types.BeatMask.None) "B3: Even measure flag is set"
      Expect.equal sngB4.Measure 2s "B4: Is in measure 2"
      Expect.equal sngB4.Beat 1s "B4: Is second beat of measure"

    testCase "Vocal" <| fun _ ->
      let v = Vocal(54_132, 22_222, "Hello", 77uy)

      let sng = XmlToSng.convertVocal v

      Expect.equal sng.Time (timeConversion v.Time) "Time is same"
      Expect.equal sng.Length (timeConversion v.Length) "Length is same"
      Expect.equal sng.Lyric v.Lyric "Lyric is same"
      Expect.equal sng.Note (int v.Note) "Note is same"

    testCase "Phrase" <| fun _ ->
      let ph = Phrase("ttt", 15uy, PhraseMask.Disparity ||| PhraseMask.Ignore ||| PhraseMask.Solo)
      let testArr = createTestArr()

      let sng = XmlToSng.convertPhrase testArr 1 ph

      Expect.equal sng.Name ph.Name "Name is same"
      Expect.equal sng.MaxDifficulty (int ph.MaxDifficulty) "Max difficulty is same"
      Expect.equal sng.Solo 1y "Solo is set correctly"
      Expect.equal sng.Disparity 1y "Disparity is set correctly"
      Expect.equal sng.Ignore 1y "Ignore is set correctly"
      Expect.equal sng.PhraseIterationLinks 2 "Phrase iteration links is set correctly"

    testCase "Chord Template" <| fun _ ->
      let ct = ChordTemplate(Name = "EEE")
      ct.SetFingering(1y,2y,3y,4y,5y,6y)
      ct.SetFrets(1y,2y,3y,4y,5y,6y)
      let testArr = createTestArr()

      let sng = XmlToSng.convertChord testArr ct

      Expect.equal sng.Name ct.Name "Name is same"
      Expect.sequenceEqual sng.Fingers ct.Fingers "Fingers are same"
      Expect.sequenceEqual sng.Frets ct.Frets "Fingers are same"

    testCase "Chord Template (MIDI Notes)" <| fun _ ->
      let ct = ChordTemplate(Name = "EEE")
      ct.SetFrets(1y,2y,3y,4y,5y,6y)
      let testArr = createTestArr()

      let sng = XmlToSng.convertChord testArr ct

      Expect.equal sng.Notes.[0] 40 "MIDI note 1 is correct (40 + 1 fret - 1 tuning)"
      Expect.equal sng.Notes.[1] 49 "MIDI note 2 is correct (45 + 2 fret + 2 tuning)"
      Expect.equal sng.Notes.[2] 57 "MIDI note 3 is correct (50 + 3 fret + 4 tuning)"
      Expect.equal sng.Notes.[3] 54 "MIDI note 4 is correct (55 + 4 fret - 5 tuning)"
      Expect.equal sng.Notes.[4] 67 "MIDI note 5 is correct (59 + 5 fret + 3 tuning)"
      Expect.equal sng.Notes.[5] 68 "MIDI note 6 is correct (64 + 6 fret - 2 tuning)"

    testCase "Chord Template (Arpeggio)" <| fun _ ->
        let ct = ChordTemplate(DisplayName = "E-arp")
        let testArr = createTestArr()

        let sng = XmlToSng.convertChord testArr ct

        Expect.equal sng.Mask SNG.Types.ChordMask.Arpeggio "Arpeggio is set"

    testCase "Chord Template (Nop)" <| fun _ ->
        let ct = ChordTemplate(DisplayName = "E-nop")
        let testArr = createTestArr()

        let sng = XmlToSng.convertChord testArr ct

        Expect.equal sng.Mask SNG.Types.ChordMask.Nop "Nop is set"

    testCase "Bend Value" <| fun _ ->
        let bv = BendValue(456465, 99.f)

        let sng = XmlToSng.convertBendValue bv

        Expect.equal sng.Time (timeConversion bv.Time) "Time is same"
        Expect.equal sng.Step bv.Step "Step is same"

    testCase "Phrase Iteration" <| fun _ ->
        let pi = PhraseIteration(2000, 8, [| 88; 99; 77 |])
        let testArr = createTestArr()

        let sng = XmlToSng.convertPhraseIteration testArr 1 pi

        Expect.equal sng.StartTime (timeConversion pi.Time) "Start time is same"
        Expect.equal sng.NextPhraseTime (timeConversion (testArr.PhraseIterations.[2].Time)) "Next phrase time is correct"
        Expect.equal sng.PhraseId pi.PhraseId "Phrase ID is same"
        Expect.equal sng.Difficulty.[0] (int pi.HeroLevels.Easy) "Easy difficulty level is same"
        Expect.equal sng.Difficulty.[1] (int pi.HeroLevels.Medium) "Medium difficulty level is same"
        Expect.equal sng.Difficulty.[2] (int pi.HeroLevels.Hard) "Hard difficulty level is same"

    testCase "Phrase Iteration (Last)" <| fun _ ->
        let pi = PhraseIteration(3000, 8, [| 88; 99; 77 |])
        let testArr = createTestArr()

        let sng = XmlToSng.convertPhraseIteration testArr (testArr.PhraseIterations.Count - 1) pi

        Expect.equal sng.NextPhraseTime (timeConversion testArr.SongLength) "Next phrase time is equal to song length"

    testCase "New Linked Difficulty" <| fun _ ->
        let phrases = [| 1; 2; 3 |]
        let nld = NewLinkedDiff(5y, phrases)

        let sng = XmlToSng.convertNLD nld

        Expect.equal sng.LevelBreak (int nld.LevelBreak) "Level break is same"
        Expect.sequenceEqual sng.NLDPhrases phrases "Phrase IDs are same"

    testCase "Event" <| fun _ ->
        let ev = Event("name", 777_777)

        let sng = XmlToSng.convertEvent ev

        Expect.equal sng.Name ev.Code "Name is same"
        Expect.equal sng.Time (timeConversion ev.Time) "Time code is same"

    testCase "Tone" <| fun _ ->
        let tone = ToneChange("dist", 456_123, 3uy)

        let sng = XmlToSng.convertTone tone

        Expect.equal sng.ToneId (int tone.Id) "ID is same"
        Expect.equal sng.Time (timeConversion tone.Time) "Time code is same"

    testCase "Section" <| fun _ ->
        let s = Section("section", 7554_003, 2s)
        let testArr = createTestArr()

        let sng = XmlToSng.convertSection testArr 0 s

        Expect.equal sng.Name s.Name "Name is same"
        Expect.equal sng.StartTime (timeConversion s.Time) "Start time is same"
        Expect.equal sng.Number (int s.Number) "Number is same"
        Expect.equal sng.EndTime (timeConversion testArr.Sections.[1].Time) "End time is correct"

    testCase "Section (Last)" <| fun _ ->
        let s = Section("section", 4000_003, 2s)
        let testArr = createTestArr()

        let sng = XmlToSng.convertSection testArr (testArr.Sections.Count - 1) s

        Expect.equal sng.EndTime (timeConversion testArr.SongLength) "End time is same as song length"

    testCase "Section (Phrase Iteration Start/End, 1 Phrase Iteration)" <| fun _ ->
        let s = Section("section", 8000, 1s)
        let testArr = createTestArr()

        let sng = XmlToSng.convertSection testArr 0 s

        Expect.equal sng.StartPhraseIterationId 2 "Start phrase iteration ID is correct"
        Expect.equal sng.EndPhraseIterationId 2 "End phrase iteration ID is correct"

    testCase "Section (Phrase Iteration Start/End, 3 Phrase Iterations)" <| fun _ ->
        let s = Section("section", 1000, 1s)
        let testArr = createTestArr()

        let sng = XmlToSng.convertSection testArr 0 s

        Expect.equal sng.StartPhraseIterationId 0 "Start phrase iteration ID is correct"
        Expect.equal sng.EndPhraseIterationId 2 "End phrase iteration ID is correct"
        // TODO: Test string mask

    testCase "Anchor" <| fun _ ->
        let i = 0
        let testArr = createTestArr()
        let a = testArr.Levels.[0].Anchors.[i]
        let noteTimes = [| 1000; 1500; 2100; 3000 |]

        let sng = XmlToSng.convertAnchor noteTimes testArr.Levels.[0] testArr i a

        Expect.equal sng.FretId a.Fret "Fret is same"
        Expect.equal sng.Width (int a.Width) "Width is same"
        Expect.equal sng.StartTime (timeConversion a.Time) "Start time is same"
        Expect.equal sng.EndTime (timeConversion (testArr.Levels.[0].Anchors.[i + 1].Time)) "End time is correct"
        Expect.equal sng.PhraseIterationId 0 "Phrase iteration ID is correct"
        Expect.equal sng.FirstNoteTime 1.f "First note time is correct"
        Expect.equal sng.LastNoteTime 1.5f "Last note time is correct"

    testCase "Hand Shape" <| fun _ ->
        let hs = HandShape(1s, 222, 333)
        let noteTimes = Set(seq { 250; 222; 300; 280 })
        let fpMap = Map(seq { 1s, noteTimes })

        let sng = XmlToSng.convertHandshape fpMap hs

        Expect.equal sng.ChordId (int hs.ChordId) "Chord ID is same"
        Expect.equal sng.StartTime (timeConversion hs.StartTime) "Start time is same"
        Expect.equal sng.EndTime (timeConversion hs.EndTime) "End time is same"
        Expect.equal sng.FirstNoteTime 0.222f "First note time is correct"
        Expect.equal sng.LastNoteTime 0.3f "Last note time is correct"

    testCase "Note" <| fun _ ->
        let note = Note(Mask = NoteMask.Pluck,
                        Fret = 12y,
                        String = 3y,
                        Time = 5555,
                        Sustain = 4444,
                        SlideTo = 14y,
                        Tap = 2y,
                        Vibrato = 80uy,
                        LeftHand = 2y,
                        BendValues = ResizeArray(seq { BendValue(5556, 1.f) }))
        
        let testLevel = Level()
        testLevel.Notes.Add(note)
        testLevel.Anchors.Add(Anchor(7y, 5555, 5y))
        let testArr = createTestArr()
        testArr.Levels.[0] <- testLevel

        let noteTimes = createNoteTimes testLevel
        let piNotes = XmlToSng.divideNoteTimesPerPhraseIteration noteTimes testArr
        let convert = XmlToSng.convertNote() piNotes Map.empty sharedAccData testArr

        let sng = convert 0 (XmlToSng.XmlNote note)

        Expect.equal sng.ChordId -1 "Chord ID is -1"
        Expect.equal sng.ChordNotesId -1 "Chord notes ID is -1"
        Expect.equal sng.FretId note.Fret "Fret is same"
        Expect.equal sng.StringIndex note.String "String is same"
        Expect.equal sng.Time (timeConversion note.Time) "Time is same"
        Expect.equal sng.Sustain (timeConversion note.Sustain) "Sustain is same"
        Expect.equal sng.SlideTo note.SlideTo "Slide is same"
        Expect.equal sng.SlideUnpitchTo note.SlideUnpitchTo "Unpitched slide is same"
        Expect.equal sng.Tap note.Tap "Tap is same"
        Expect.equal sng.Slap -1y "Slap is set correctly"
        Expect.equal sng.Pluck 1y "Pluck is set correctly"
        Expect.equal sng.Vibrato (int16 note.Vibrato) "Vibrato is same"
        Expect.equal sng.PickDirection -1y "Pick direction is correct"
        Expect.equal sng.LeftHand note.LeftHand "Left hand is same"
        Expect.equal sng.AnchorFretId 7y "Anchor fret is correct"
        Expect.equal sng.AnchorWidth 5y "Anchor width is correct"
        Expect.equal sng.BendData.Length note.BendValues.Count "Bend value count is correct"
        Expect.equal sng.MaxBend note.MaxBend "Max bend is same"
        Expect.equal sng.PhraseId 77 "Phrase ID is correct"
        Expect.equal sng.PhraseIterationId 2 "Phrase iteration ID is correct"

    testCase "Note (Next/Previous Note IDs)" <| fun _ ->
        let note0 = Note(Fret = 12y,
                         String = 3y,
                         Time = 1000,
                         Sustain = 500)
        let note1 = Note(Fret = 12y,
                         String = 3y,
                         Time = 1500,
                         Sustain = 100)

        let testLevel = Level()
        testLevel.Notes.Add(note0)
        testLevel.Notes.Add(note1)
        testLevel.Anchors.Add(Anchor(12y, 1000))
        let testArr = createTestArr()
        testArr.Levels.[0] <- testLevel

        let noteTimes = createNoteTimes testLevel
        let piNotes = XmlToSng.divideNoteTimesPerPhraseIteration noteTimes testArr
        let convert = XmlToSng.convertNote() piNotes Map.empty sharedAccData testArr

        let sngNote0 = convert 0 (XmlToSng.XmlNote note0)
        let sngNote1 = convert 0 (XmlToSng.XmlNote note1)

        Expect.equal sngNote0.PrevIterNote -1s "Previous note index of first note is -1"
        Expect.equal sngNote0.NextIterNote 1s "Next note index of first note is 1"
        Expect.equal sngNote1.PrevIterNote 0s "Previous note index of second note is 0"
        Expect.equal sngNote1.NextIterNote -1s "Next note index of second note is -1"
        Expect.equal sngNote0.ParentPrevNote -1s "Parent note index of first note is -1"
        Expect.equal sngNote1.ParentPrevNote -1s "Parent note index of second note is -1"

    testCase "Note (Mask 1/2)" <| fun _ ->
        let note = Note(Mask = (NoteMask.Accent ||| NoteMask.Tremolo ||| NoteMask.FretHandMute ||| NoteMask.HammerOn |||
                                NoteMask.Harmonic ||| NoteMask.Ignore ||| NoteMask.PalmMute ||| NoteMask.PinchHarmonic |||
                                NoteMask.Pluck ||| NoteMask.PullOff ||| NoteMask.RightHand ||| NoteMask.Slap),
                        Fret = 0y,
                        String = 3y,
                        Time = 1000,
                        Sustain = 500)

        let testLevel = Level()
        testLevel.Notes.Add(note)
        testLevel.Anchors.Add(Anchor(12y, 1000))
        let testArr = createTestArr()
        testArr.Levels.[0] <- testLevel

        let noteTimes = createNoteTimes testLevel
        let piNotes = XmlToSng.divideNoteTimesPerPhraseIteration noteTimes testArr
        let convert = XmlToSng.convertNote() piNotes Map.empty sharedAccData testArr

        let sngNote = convert 0 (XmlToSng.XmlNote note)

        Expect.isTrue (sngNote.Mask ?= SNG.Types.NoteMask.Single) "Single note has single flag"
        Expect.isTrue (sngNote.Mask ?= SNG.Types.NoteMask.Open) "Open string note has open flag"
        Expect.isTrue (sngNote.Mask ?= SNG.Types.NoteMask.Sustain) "Sustained note has sustain flag"
        Expect.isTrue (sngNote.Mask ?= SNG.Types.NoteMask.Accent) "Accented note has accent flag"
        Expect.isTrue (sngNote.Mask ?= SNG.Types.NoteMask.Tremolo) "Tremolo note has tremolo flag"
        Expect.isTrue (sngNote.Mask ?= SNG.Types.NoteMask.Mute) "Muted note has mute flag"
        Expect.isTrue (sngNote.Mask ?= SNG.Types.NoteMask.HammerOn) "Hammer-on note has hammer-on flag"
        Expect.isTrue (sngNote.Mask ?= SNG.Types.NoteMask.Harmonic) "Harmonic note has harmonic flag"
        Expect.isTrue (sngNote.Mask ?= SNG.Types.NoteMask.Ignore) "Ignored note has ignore flag"
        Expect.isTrue (sngNote.Mask ?= SNG.Types.NoteMask.PalmMute) "Palm-muted note has palm-mute flag"
        Expect.isTrue (sngNote.Mask ?= SNG.Types.NoteMask.PinchHarmonic) "Pinch harmonic note has pinch harmonic flag"
        Expect.isTrue (sngNote.Mask ?= SNG.Types.NoteMask.Pluck) "Plucked note has pluck flag"
        Expect.isTrue (sngNote.Mask ?= SNG.Types.NoteMask.PullOff) "Pull-off note has pull-off flag"
        Expect.isTrue (sngNote.Mask ?= SNG.Types.NoteMask.RightHand) "Right hand note has right hand flag"
        Expect.isTrue (sngNote.Mask ?= SNG.Types.NoteMask.Slap) "Slapped note has slap flag"

    testCase "Note (Mask 2/2)" <| fun _ ->
        let note = Note(Mask = NoteMask.None,
                        Fret = 2y,
                        String = 3y,
                        Time = 1000,
                        SlideTo = 5y,
                        SlideUnpitchTo = 5y,
                        Tap = 1y,
                        Vibrato = 40uy,
                        LeftHand = 1y,
                        BendValues = ResizeArray(seq { BendValue(1000, 1.f) }))

        let testLevel = Level()
        testLevel.Notes.Add(note)
        testLevel.Anchors.Add(Anchor(12y, 1000))
        let testArr = createTestArr()
        testArr.Levels.[0] <- testLevel

        let noteTimes = createNoteTimes testLevel
        let piNotes = XmlToSng.divideNoteTimesPerPhraseIteration noteTimes testArr
        let convert = XmlToSng.convertNote() piNotes Map.empty sharedAccData testArr

        let sngNote = convert 0 (XmlToSng.XmlNote note)

        Expect.isFalse (sngNote.Mask ?= SNG.Types.NoteMask.Open) "Non-open string note does not have open flag"
        Expect.isFalse (sngNote.Mask ?= SNG.Types.NoteMask.Sustain) "Non-sustained note does not have sustain flag"
        Expect.isTrue (sngNote.Mask ?= SNG.Types.NoteMask.Slide) "Slide note has slide flag"
        Expect.isTrue (sngNote.Mask ?= SNG.Types.NoteMask.UnpitchedSlide) "Unpitched slide note has unpitched slide flag"
        Expect.isTrue (sngNote.Mask ?= SNG.Types.NoteMask.Tap) "Tapped note has tap flag"
        Expect.isTrue (sngNote.Mask ?= SNG.Types.NoteMask.Vibrato) "Vibrato note has vibrato flag"
        Expect.isTrue (sngNote.Mask ?= SNG.Types.NoteMask.Bend) "Bend note has bend flag"
        Expect.isTrue (sngNote.Mask ?= SNG.Types.NoteMask.LeftHand) "Note with left hand has left hand flag"

    testCase "Note (Link Next)" <| fun _ ->
        let parent = Note(Mask = NoteMask.LinkNext,
                          Fret = 12y,
                          String = 3y,
                          Time = 1000,
                          Sustain = 500)
        let child = Note(Mask = NoteMask.Tremolo,
                         Fret = 12y,
                         String = 3y,
                         Time = 1500,
                         Sustain = 100)

        let testLevel = Level()
        testLevel.Notes.Add(parent)
        testLevel.Notes.Add(child)
        testLevel.Anchors.Add(Anchor(12y, 1000))
        let testArr = createTestArr()
        testArr.Levels.[0] <- testLevel

        let noteTimes = createNoteTimes testLevel
        let piNotes = XmlToSng.divideNoteTimesPerPhraseIteration noteTimes testArr
        let convert = XmlToSng.convertNote() piNotes Map.empty sharedAccData testArr

        let sngParent = convert 0 (XmlToSng.XmlNote parent)
        let sngChild = convert 0 (XmlToSng.XmlNote child)

        Expect.isTrue (sngParent.Mask ?= SNG.Types.NoteMask.Parent) "Parent has correct mask set"
        Expect.isTrue (sngChild.Mask ?= SNG.Types.NoteMask.Child) "Child has correct mask set"
        Expect.equal sngChild.ParentPrevNote 0s "Child's parent note index is correct"

    testCase "Note (Hand Shape ID)" <| fun _ ->
        let note = Note(Mask = NoteMask.LinkNext,
                        Fret = 12y,
                        String = 3y,
                        Time = 1000,
                        Sustain = 500)

        let testLevel = Level()
        testLevel.Notes.Add(note)
        testLevel.Anchors.Add(Anchor(12y, 1000))
        testLevel.HandShapes.Add(HandShape(0s, 1000, 1500))
        let testArr = createTestArr()
        testArr.Levels.[0] <- testLevel

        let convert = createNoteConvertFunction sharedAccData testArr testLevel

        let sng = convert 0 (XmlToSng.XmlNote note)

        Expect.equal (sng.FingerPrintId.[0]) 0s "Fingerprint ID is correct"

    testCase "Note (Hand Shape ID, Arpeggio)" <| fun _ ->
        let note = Note(Mask = NoteMask.LinkNext,
                        Fret = 12y,
                        String = 3y,
                        Time = 1000,
                        Sustain = 500)

        let testLevel = Level()
        testLevel.Notes.Add(note)
        testLevel.Anchors.Add(Anchor(12y, 1000))
        testLevel.HandShapes.Add(HandShape(1s, 1000, 1500))
        let testArr = createTestArr()
        testArr.Levels.[0] <- testLevel

        let convert = createNoteConvertFunction sharedAccData testArr testLevel

        let sng = convert 0 (XmlToSng.XmlNote note)

        Expect.equal (sng.FingerPrintId.[1]) 1s "Arpeggio fingerprint ID is correct"
        Expect.isTrue (sng.Mask ?= SNG.Types.NoteMask.Arpeggio) "Arpeggio bit is set"

    testCase "Events to DNAs" <| fun _ ->
        let testArr = createTestArr()

        let dnas = XmlToSng.createDNAs testArr

        Expect.equal dnas.Length 4 "DNA count is correct"
        Expect.equal dnas.[3].DnaId 2 "Last DNA ID is correct"

    testCase "Meta Data" <| fun _ ->
        let testArr = createTestArr()

        let md = XmlToSng.convertMetaData sharedAccData testArr

        Expect.equal md.MaxScore 10_000.0 "Max score is correct"
        Expect.equal md.StartTime 1.0f "Start time is correct"
        Expect.equal md.CapoFretId -1y "Capo fret is correct"
        Expect.equal md.Part testArr.Part "Part is same"
        Expect.equal md.SongLength (timeConversion testArr.SongLength) "Song length is same"
        Expect.sequenceEqual md.Tuning testArr.Tuning.Strings "Tuning is same"

    testCase "Chord" <| fun _ ->
        let chord = Chord(Time = 1250, ChordId = 0s)
        let chordNotes = ResizeArray(seq { Note(Sustain = 500); Note(Sustain = 500) })
        chord.ChordNotes <- chordNotes

        let testLevel = Level()
        testLevel.Chords.Add(chord)
        testLevel.Anchors.Add(Anchor(12y, 1000))
        testLevel.HandShapes.Add(HandShape(1s, 1000, 1500))
        let testArr = createTestArr()
        testArr.Levels.[0] <- testLevel

        let convert = createNoteConvertFunction sharedAccData testArr testLevel

        let sng = convert 0 (XmlToSng.XmlChord chord)

        Expect.equal sng.ChordId (int chord.ChordId) "Chord ID is same"
        Expect.equal sng.Sustain (timeConversion chord.ChordNotes.[1].Sustain) "Sustain is same"
        Expect.isTrue (sng.Mask ?= SNG.Types.NoteMask.Chord) "Chord has chord flag"
        Expect.isFalse (sng.Mask ?= SNG.Types.NoteMask.DoubleStop) "Double stop flag is not set"
        Expect.isFalse (sng.Mask ?= SNG.Types.NoteMask.Arpeggio) "Arpeggio flag is not set"
        Expect.isTrue (sng.Mask ?= SNG.Types.NoteMask.Strum) "Strum flag is set"
        Expect.isTrue (sng.Mask ?= SNG.Types.NoteMask.Sustain) "Sustain flag is set"
        Expect.isTrue (sng.Mask ?= SNG.Types.NoteMask.ChordNotes) "Chord notes flag is set"

    testCase "Chord (Double stop, arpeggio, no chord notes)" <| fun _ ->
        let chord = Chord(Time = 1250, ChordId = 1s)

        let testLevel = Level()
        testLevel.Chords.Add(chord)
        testLevel.Anchors.Add(Anchor(12y, 1000))
        testLevel.HandShapes.Add(HandShape(1s, 1000, 1500))
        let testArr = createTestArr()
        testArr.Levels.[0] <- testLevel

        let convert = createNoteConvertFunction sharedAccData testArr testLevel

        let sng = convert 0 (XmlToSng.XmlChord chord)

        Expect.equal sng.ChordId (int chord.ChordId) "Chord ID is same"
        Expect.isTrue (sng.Mask ?= SNG.Types.NoteMask.Chord) "Chord has chord flag"
        Expect.isTrue (sng.Mask ?= SNG.Types.NoteMask.DoubleStop) "Double stop flag is set"
        Expect.isTrue (sng.Mask ?= SNG.Types.NoteMask.Arpeggio) "Arpeggio flag is set"
        Expect.isFalse (sng.Mask ?= SNG.Types.NoteMask.Strum) "Strum flag is not set"
        Expect.isFalse (sng.Mask ?= SNG.Types.NoteMask.ChordNotes) "Chord notes flag is not set"

    testCase "Chord (Mask)" <| fun _ ->
        let chord = Chord(Mask = (ChordMask.FretHandMute ||| ChordMask.PalmMute ||| ChordMask.Ignore
                                  ||| ChordMask.HighDensity ||| ChordMask.Accent ||| ChordMask.LinkNext),
                          Time = 1250,
                          ChordId = 1s)

        let testLevel = Level()
        testLevel.Chords.Add(chord)
        testLevel.Anchors.Add(Anchor(12y, 1000))
        testLevel.HandShapes.Add(HandShape(1s, 1000, 1500))
        let testArr = createTestArr()
        testArr.Levels.[0] <- testLevel

        let convert = createNoteConvertFunction sharedAccData testArr testLevel

        let sng = convert 0 (XmlToSng.XmlChord chord)

        Expect.isTrue (sng.Mask ?= SNG.Types.NoteMask.Accent) "Accented chord has accent flag"
        Expect.isTrue (sng.Mask ?= SNG.Types.NoteMask.FretHandMute) "Fret-hand-muted chord has fret-hand mute flag"
        Expect.isTrue (sng.Mask ?= SNG.Types.NoteMask.PalmMute) "Palm-muted chord has palm-mute flag"
        Expect.isTrue (sng.Mask ?= SNG.Types.NoteMask.Ignore) "Ignored chord has ignore flag"
        Expect.isTrue (sng.Mask ?= SNG.Types.NoteMask.HighDensity) "High-density chord has high-density flag"
        Expect.isTrue (sng.Mask ?= SNG.Types.NoteMask.Parent) "Link-next chord has parent flag"

    testCase "Chord notes are created when needed" <| fun _ ->
        let chord = Chord(Mask = ChordMask.None,
                          Time = 1250,
                          ChordId = 1s)
        let chordNotes = ResizeArray(seq { Note(String = 0y, Sustain = 500, Vibrato = 80uy); Note(String = 1y, Sustain = 500) })
        chord.ChordNotes <- chordNotes

        let testLevel = Level()
        testLevel.Chords.Add(chord)
        testLevel.Anchors.Add(Anchor(12y, 1000))
        testLevel.HandShapes.Add(HandShape(1s, 1000, 1500))
        let testArr = createTestArr()
        testArr.Levels.[0] <- testLevel

        let accuData = XmlToSng.AccuData.Init(testArr)
        let convert = createNoteConvertFunction accuData testArr testLevel

        let sng = convert 0 (XmlToSng.XmlChord chord)

        Expect.equal accuData.ChordNotes.Count 1 "One chord notes object created"
        Expect.allEqual accuData.ChordNotes.[0].SlideTo -1y "All slides are -1"
        Expect.allEqual accuData.ChordNotes.[0].SlideUnpitchTo -1y "All unpitched slides are -1"
        Expect.equal accuData.ChordNotes.[0].Vibrato.[0] 80s "Vibrato is correct"
        Expect.isTrue (sng.Mask ?= SNG.Types.NoteMask.ChordNotes) "Chord notes flag is set"

    testCase "Chord notes are not created when not needed" <| fun _ ->
        let chord = Chord(Mask = ChordMask.None,
                          Time = 1250,
                          ChordId = 1s)
        // Create chord notes that have no techniques that would require SNG chord notes
        let chordNotes = ResizeArray(seq { Note(Fret = 1y); Note(Fret = 1y) })
        chord.ChordNotes <- chordNotes

        let testLevel = Level()
        testLevel.Chords.Add(chord)
        testLevel.Anchors.Add(Anchor(12y, 1000))
        testLevel.HandShapes.Add(HandShape(1s, 1000, 1500))
        let testArr = createTestArr()
        testArr.Levels.[0] <- testLevel

        let accuData = XmlToSng.AccuData.Init(testArr)
        let convert = createNoteConvertFunction accuData testArr testLevel

        let sng = convert 0 (XmlToSng.XmlChord chord)

        Expect.equal accuData.ChordNotes.Count 0 "No chord notes object created"
        Expect.isFalse (sng.Mask ?= SNG.Types.NoteMask.ChordNotes) "Chord notes flag is not set"

    testCase "Anchor extensions are created for slide notes" <| fun _ ->
        let note = Note(Time = 1100, Sustain = 200, SlideTo = 8y)

        let testArr = createTestArr()
        let testLevel = Level()
        testLevel.Notes.Add(note)
        testLevel.Anchors.Add(Anchor(12y, 1000))
        testArr.Levels.[0] <- testLevel

        let accuData = XmlToSng.AccuData.Init(testArr)
        let convert = createNoteConvertFunction accuData testArr testLevel

        let sng = convert 0 (XmlToSng.XmlNote note)

        Expect.equal accuData.AnchorExtensions.Count 1 "Anchor extension was created"
        Expect.equal accuData.AnchorExtensions.[0].BeatTime 1.3f "Time is correct"
        Expect.equal accuData.AnchorExtensions.[0].FretId note.SlideTo "Fret is correct"

    testCase "Level" <| fun _ ->
        let testArr = createTestArr()
        testArr.Phrases.Add(Phrase("default", 0uy, PhraseMask.None))
        testArr.Phrases.Add(Phrase("phrase1", 0uy, PhraseMask.None))
        let testLevel = testArr.Levels.[0]
        testLevel.HandShapes.Add(HandShape(0s, 1000, 1200))
        testLevel.HandShapes.Add(HandShape(1s, 1000, 1200))
        testLevel.Notes.Add(Note(Time = 1100, Sustain = 200, SlideTo = 8y))
        testLevel.Chords.Add(Chord(Time = 1250, ChordId = 1s))
        let accuData = AccuData.Init(testArr)

        let sng = XmlToSng.convertLevel accuData testArr testLevel

        Expect.equal sng.Difficulty (int testLevel.Difficulty) "Difficulty is same"
        Expect.equal sng.Anchors.Length testLevel.Anchors.Count "Anchor count is same"
        Expect.equal sng.HandShapes.Length 1 "Handshape count is correct"
        Expect.equal sng.Arpeggios.Length 1 "Arpeggio count is correct"
        Expect.equal sng.AverageNotesPerIteration.Length testArr.Phrases.Count "Phrase count is correct"
        Expect.equal sng.Notes.Length 2 "Note count is correct"
        Expect.equal sng.AverageNotesPerIteration.[1] 1.f "Average notes in phrase #2 is one (two notes / two iterations)"
        Expect.equal accuData.FirstNoteTime 1100 "First note time is correct"
        Expect.equal accuData.NoteCounts.Easy 2 "Note count, easy is correct"
  ]
