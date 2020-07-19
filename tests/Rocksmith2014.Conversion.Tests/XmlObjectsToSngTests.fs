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
    arr.PhraseIterations.Add(PhraseIteration(Time = 3000, PhraseId = 1))
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
      Expect.equal sng.PhraseIterationLinks 3 "Phrase iteration links is set correctly"

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
  ]
