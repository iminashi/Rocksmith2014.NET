module Rocksmith2014.Conversion.Tests.InstrumentalConversionTest

open Expecto
open Rocksmith2014.XML
open Rocksmith2014
open Rocksmith2014.Conversion.Utils
open Rocksmith2014.Conversion

[<Tests>]
let sngToXmlConversionTests =
  testList "XML Files → SNG Files" [

    testCase "Instrumental Conversion (Notes Only)" <| fun _ ->
        let xml = InstrumentalArrangement.Load("instrumental_1level_notesonly.xml")
        
        let sng = ConvertInstrumental.xmlToSng xml

        // Test note counts
        Expect.equal sng.MetaData.MaxNotesAndChords 17.0 "Total number of notes is 17"
        Expect.equal sng.MetaData.MaxNotesAndChordsReal 16.0 "Total number of notes - ignored notes is 16"
        Expect.equal sng.Levels.[0].NotesInPhraseIterationsAll.[1] 10 "Number of notes in phrase iteration #1 is 10"
        Expect.equal sng.Levels.[0].NotesInPhraseIterationsAll.[2] 7 "Number of notes in phrase iteration #2 is 7"
        Expect.equal sng.Levels.[0].NotesInPhraseIterationsExclIgnored.[2] 6 "Number of notes (excluding ignored) in phrase iteration #2 is 6"

        // Test beat phrase iterations
        Expect.equal sng.Beats.[4].PhraseIteration 0 "Beat #4 for is in phrase iteration 0"
        Expect.equal sng.Beats.[5].PhraseIteration 1 "Beat #5 for is in phrase iteration 1"
        Expect.equal sng.Beats.[12].PhraseIteration 1 "Beat #12 for is in phrase iteration 1"
        Expect.equal sng.Beats.[13].PhraseIteration 2 "Beat #13 for is in phrase iteration 2"

        // Test various properties of the notes
        Expect.equal sng.Levels.[0].Notes.[0].AnchorFretId 2y "Note #0 is anchored on fret 2"
        Expect.isTrue (sng.Levels.[0].Notes.[2].Mask ?= SNG.NoteMask.Open) "Note #2 has open bit set"
        Expect.equal sng.Levels.[0].Notes.[6].FingerPrintId.[1] 0s "Note #6 is inside arpeggio (Chord ID 0)"
        Expect.equal sng.Levels.[0].Notes.[9].Sustain 0.750f "Note #9 has 0.750s sustain"
        Expect.equal sng.Levels.[0].Notes.[10].MaxBend 1.f "Note #10 max bend is 1.0"
        Expect.equal sng.Levels.[0].Notes.[10].BendData.Length 1 "Note #10 has one bend value"
        Expect.equal sng.Levels.[0].Notes.[11].SlideUnpitchTo 14y "Note #11 has unpitched slide to fret 14"
        Expect.isTrue (sng.Levels.[0].Notes.[15].Mask ?= SNG.NoteMask.Parent) "Note #15 has parent bit set"
        Expect.equal sng.Levels.[0].Notes.[16].Vibrato 80s "Note #16 has vibrato set to 80"
  ]
