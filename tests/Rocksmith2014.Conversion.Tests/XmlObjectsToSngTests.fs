module Rocksmith2014.Conversion.Tests.XmlObjectsToSngTests

open Expecto
open Rocksmith2014
open Rocksmith2014.XML
open Rocksmith2014.Conversion
//open Rocksmith2014.SNG.Types

let testArr =
    let arr = InstrumentalArrangement()
    arr.PhraseIterations.Add(PhraseIteration(Time = 0, PhraseId = 1))
    arr.PhraseIterations.Add(PhraseIteration(Time = 1, PhraseId = 1))
    arr.PhraseIterations.Add(PhraseIteration(Time = 2, PhraseId = 1))
    arr

[<Tests>]
let sngToXmlConversionTests =
  testList "XML Objects → SNG Objects" [

    testCase "Vocal" <| fun _ ->
      let v = Vocal(54_132, 22_222, "Hello", 77uy)

      let sng = XmlToSng.convertVocal v

      Expect.equal sng.Time 54.132f "Time is same"
      Expect.equal sng.Length 22.222f "Length is same"
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

        Expect.equal sng.Time 456.465f "Time is same"
        Expect.equal sng.Step bv.Step "Step is same"
  ]