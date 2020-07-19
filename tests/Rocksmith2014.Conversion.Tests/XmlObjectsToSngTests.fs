module Rocksmith2014.Conversion.Tests.XmlObjectsToSngTests

open Expecto
open Rocksmith2014
open Rocksmith2014.XML
open Rocksmith2014.Conversion
open System.Globalization
open System

/// Testing function that converts a time in milliseconds into seconds without floating point arithmetic.
let timeConversion (time:int) =
    Single.Parse(Utils.TimeCodeToString(time), NumberFormatInfo.InvariantInfo)

let testArr =
    let arr = InstrumentalArrangement()
    arr.SongLength <- 4784_455
    arr.PhraseIterations.Add(PhraseIteration(Time = 1000, PhraseId = 1))
    arr.PhraseIterations.Add(PhraseIteration(Time = 2000, PhraseId = 1))
    arr.PhraseIterations.Add(PhraseIteration(Time = 3000, PhraseId = 77))
    arr.PhraseIterations.Add(PhraseIteration(Time = 7554_100, PhraseId = 2))
    arr.PhraseIterations.Add(PhraseIteration(Time = 7555_000, PhraseId = 3))

    arr.Sections.Add(Section("1", 1000, 1s))
    arr.Sections.Add(Section("2", 4000, 1s))
    arr.Sections.Add(Section("3", 8000_000, 1s))

    arr.Events.Add(Event("e0", 1000))
    arr.Events.Add(Event("dna_none", 2500))
    arr.Events.Add(Event("dna_solo", 3500))
    arr.Events.Add(Event("dna_chord", 4500))
    arr.Events.Add(Event("dna_riff", 5500))

    let lvl = Level(0y)
    lvl.Anchors.Add(Anchor(8y, 1000))
    lvl.Anchors.Add(Anchor(7y, 2000, 5y))

    arr.Levels.Add(lvl)
    arr

[<Tests>]
let sngToXmlConversionTests =
  testList "XML Objects → SNG Objects" [

    testCase "Vocal" <| fun _ ->
      let v = Vocal(54_132, 22_222, "Hello", 77uy)

      let sng = XmlToSng.convertVocal v

      Expect.equal sng.Time (timeConversion v.Time) "Time is same"
      Expect.equal sng.Length (timeConversion v.Length) "Length is same"
      Expect.equal sng.Lyric v.Lyric "Lyric is same"
      Expect.equal sng.Note (int v.Note) "Note is same"

    testCase "Phrase" <| fun _ ->
      let ph = Phrase("ttt", 15uy, PhraseMask.Disparity ||| PhraseMask.Ignore ||| PhraseMask.Solo)

      let sng = XmlToSng.convertPhrase 1 testArr ph

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

      let sng = XmlToSng.convertChord ct

      Expect.equal sng.Name ct.Name "Name is same"
      Expect.sequenceEqual sng.Fingers ct.Fingers "Fingers are same"
      Expect.sequenceEqual sng.Frets ct.Frets "Fingers are same"
      // TODO: Test notes

    testCase "Chord Template (Arpeggio)" <| fun _ ->
        let ct = ChordTemplate(DisplayName = "E-arp")

        let sng = XmlToSng.convertChord ct

        Expect.equal sng.Mask SNG.Types.ChordMask.Arpeggio "Arpeggio is set"

    testCase "Chord Template (Nop)" <| fun _ ->
        let ct = ChordTemplate(DisplayName = "E-nop")

        let sng = XmlToSng.convertChord ct

        Expect.equal sng.Mask SNG.Types.ChordMask.Nop "Nop is set"

    testCase "Bend Value" <| fun _ ->
        let bv = BendValue(456465, 99.f)

        let sng = XmlToSng.convertBendValue bv

        Expect.equal sng.Time (timeConversion bv.Time) "Time is same"
        Expect.equal sng.Step bv.Step "Step is same"

    testCase "Phrase Iteration" <| fun _ ->
        let pi = PhraseIteration(2000, 8, [| 88; 99; 77 |])

        let sng = XmlToSng.convertPhraseIteration 1 testArr pi

        Expect.equal sng.StartTime (timeConversion pi.Time) "Start time is same"
        Expect.equal sng.NextPhraseTime (timeConversion (testArr.PhraseIterations.[2].Time)) "Next phrase time is correct"
        Expect.equal sng.PhraseId pi.PhraseId "Phrase ID is same"
        Expect.equal sng.Difficulty.[0] (int pi.HeroLevels.Easy) "Easy difficulty level is same"
        Expect.equal sng.Difficulty.[1] (int pi.HeroLevels.Medium) "Medium difficulty level is same"
        Expect.equal sng.Difficulty.[2] (int pi.HeroLevels.Hard) "Hard difficulty level is same"

    testCase "Phrase Iteration (Last)" <| fun _ ->
        let pi = PhraseIteration(3000, 8, [| 88; 99; 77 |])

        let sng = XmlToSng.convertPhraseIteration (testArr.PhraseIterations.Count - 1) testArr pi

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

        let sng = XmlToSng.convertSection 0 testArr s

        Expect.equal sng.Name s.Name "Name is same"
        Expect.equal sng.StartTime (timeConversion s.Time) "Start time is same"
        Expect.equal sng.Number (int s.Number) "Number is same"
        Expect.equal sng.EndTime (timeConversion testArr.Sections.[1].Time) "End time is correct"

    testCase "Section (Last)" <| fun _ ->
        let s = Section("section", 4000_003, 2s)

        let sng = XmlToSng.convertSection (testArr.Sections.Count - 1) testArr s

        Expect.equal sng.EndTime (timeConversion testArr.SongLength) "End time is same as song length"

    testCase "Section (Phrase Iteration Start/End)" <| fun _ ->
        let s = Section("section", 1000, 1s)

        let sng = XmlToSng.convertSection 0 testArr s

        Expect.equal sng.StartPhraseIterationId 0 "Start phrase iteration ID is correct"
        Expect.equal sng.EndPhraseIterationId 2 "End phrase iteration ID is correct"
        // TODO: Test string mask

    testCase "Anchor" <| fun _ ->
        let a = Anchor(1y, 2000, 5y)
        let lvl = 0
        let i = 0

        let sng = XmlToSng.convertAnchor i lvl testArr a

        Expect.equal sng.FretId a.Fret "Fret is same"
        Expect.equal sng.Width (int a.Width) "Width is same"
        Expect.equal sng.StartTime (timeConversion a.Time) "Start time is same"
        Expect.equal sng.EndTime (timeConversion (testArr.Levels.[lvl].Anchors.[i + 1].Time)) "End time is correct"
        Expect.equal sng.PhraseIterationId 1 "Phrase iteration ID is correct"
        // TODO: Test first/last note times

    testCase "Hand Shape" <| fun _ ->
        let hs = HandShape(1s, 222, 333)

        let sng = XmlToSng.convertHandshape hs

        Expect.equal sng.ChordId (int hs.ChordId) "Chord ID is same"
        Expect.equal sng.StartTime (timeConversion hs.StartTime) "Start time is same"
        Expect.equal sng.EndTime (timeConversion hs.EndTime) "End time is same"
        // TODO: Test first/last note times

    testCase "Note" <| fun _ ->
        let note = Note(
                    Mask = NoteMask.Pluck,
                    Fret = 12y,
                    String = 3y,
                    Time = 5555,
                    Sustain = 4444,
                    SlideTo = 14y,
                    Tap = 2y,
                    Vibrato = 80uy,
                    LeftHand = 2y,
                    BendValues = ResizeArray(seq { BendValue(5556, 1.f) }))
        testArr.Levels.[0].Notes.Add(note)

        let sng = XmlToSng.convertNote 0 testArr note

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
        Expect.equal sng.PickDirection 0y "Pick direction is same"
        Expect.equal sng.LeftHand note.LeftHand "Left hand is same"
        Expect.equal sng.AnchorFretId 7y "Anchor fret is correct"
        Expect.equal sng.AnchorWidth 5y "Anchor width is correct"
        Expect.equal sng.BendData.Length note.BendValues.Count "Bend value count is correct"
        Expect.equal sng.MaxBend note.MaxBend "Max bend is same"
        Expect.equal sng.PhraseId 77 "Phrase ID is correct"

    testCase "Events to DNAs" <| fun _ ->
        let dnas = XmlToSng.createDNAs testArr

        Expect.equal dnas.Length 4 "DNA count is correct"
        Expect.equal dnas.[3].DnaId 2 "Last DNA ID is correct"
  ]
