module Rocksmith2014.Conversion.Tests.InstrumentalConversionTest

open Expecto
open Rocksmith2014.XML
open Rocksmith2014
open Rocksmith2014.Conversion.Utils
open Rocksmith2014.Conversion

[<Tests>]
let sngToXmlConversionTests =
    testList "XML Files → SNG" [
        testCase "Instrumental Conversion (Notes Only)" <| fun _ ->
            let xml = InstrumentalArrangement.Load("instrumental_1level_notesonly.xml")
            
            let sng = ConvertInstrumental.xmlToSng xml
            let level = sng.Levels.[0]
        
            // Test note counts
            Expect.equal sng.MetaData.MaxNotesAndChords 17.0 "Total number of notes is 17"
            Expect.equal sng.MetaData.MaxNotesAndChordsReal 16.0 "Total number of notes - ignored notes is 16"
            Expect.equal level.NotesInPhraseIterationsAll.[1] 10 "Number of notes in phrase iteration #1 is 10"
            Expect.equal level.NotesInPhraseIterationsAll.[2] 7 "Number of notes in phrase iteration #2 is 7"
            Expect.equal level.NotesInPhraseIterationsExclIgnored.[2] 6 "Number of notes (excluding ignored) in phrase iteration #2 is 6"
        
            // Test beat phrase iterations
            Expect.equal sng.Beats.[4].PhraseIteration 0 "Beat #4 for is in phrase iteration 0"
            Expect.equal sng.Beats.[5].PhraseIteration 1 "Beat #5 for is in phrase iteration 1"
            Expect.equal sng.Beats.[12].PhraseIteration 1 "Beat #12 for is in phrase iteration 1"
            Expect.equal sng.Beats.[13].PhraseIteration 2 "Beat #13 for is in phrase iteration 2"
        
            // Test various properties of the notes
            Expect.equal level.Notes.[0].AnchorFretId 2y "Note #0 is anchored on fret 2"
            Expect.isTrue (level.Notes.[2].Mask ?= SNG.NoteMask.Open) "Note #2 has open bit set"
            Expect.equal level.Notes.[6].FingerPrintId.[1] 0s "Note #6 is inside arpeggio (Chord ID 0)"
            Expect.equal level.Notes.[9].Sustain 0.750f "Note #9 has 0.750s sustain"
            Expect.equal level.Notes.[10].MaxBend 1.f "Note #10 max bend is 1.0"
            Expect.equal level.Notes.[10].BendData.Length 1 "Note #10 has one bend value"
            Expect.equal level.Notes.[11].SlideUnpitchTo 14y "Note #11 has unpitched slide to fret 14"
            Expect.isTrue (level.Notes.[15].Mask ?= SNG.NoteMask.Parent) "Note #15 has parent bit set"
            Expect.equal level.Notes.[16].Vibrato 80s "Note #16 has vibrato set to 80"
        
        testCase "Instrumental Conversion (Chords Only)" <| fun _ ->
            let xml = InstrumentalArrangement.Load("instrumental_1level_chordsonly.xml")
            
            let sng = ConvertInstrumental.xmlToSng xml
            let level = sng.Levels.[0]
        
            // Test note counts
            Expect.equal sng.MetaData.MaxNotesAndChords 8.0 "Total number of notes is 8"
            Expect.equal sng.MetaData.MaxNotesAndChordsReal 7.0 "Total number of notes - ignored notes is 7"
            Expect.equal level.NotesInPhraseIterationsAll.[1] 7 "Number of notes in phrase iteration #1 is 7"
            Expect.equal level.NotesInPhraseIterationsAll.[2] 1 "Number of notes in phrase iteration #2 is 1"
            Expect.equal level.NotesInPhraseIterationsExclIgnored.[2] 0 "Number of notes (excluding ignored) in phrase iteration #2 is 0"
        
            // Test chord notes
            Expect.equal sng.ChordNotes.Length 2 "Number of chord notes generated is 2"
            Expect.isTrue (sng.ChordNotes.[0].Mask.[3] ?= SNG.NoteMask.Open) "Chord notes #0 has open bit set on string 3"
            Expect.isTrue (sng.ChordNotes.[0].Mask.[4] ?= SNG.NoteMask.Open) "Chord notes #0 has open bit set on string 4"
            Expect.isTrue (sng.ChordNotes.[1].Mask.[2] ?= SNG.NoteMask.Sustain) "Chord notes #1 has sustain bit set on string 2"
        
            // Test various properties of the chords
            Expect.equal level.Notes.[0].FingerPrintId.[0] 0s "Chord #0 is inside hand shape (Chord ID 0)"
            Expect.isTrue (level.Notes.[0].Mask ?= SNG.NoteMask.ChordPanel) "Chord #0 has chord panel bit set"
            Expect.equal level.Notes.[1].FingerPrintId.[0] 0s "Chord #1 is inside hand shape (Chord ID 0)"
            Expect.isFalse (level.Notes.[2].Mask ?= SNG.NoteMask.ChordPanel) "Chord #2 does not have chord panel bit set"
            Expect.equal level.Notes.[4].FingerPrintId.[0] 1s "Chord #4 is inside hand shape (Chord ID 1)"
            Expect.isTrue (level.Notes.[4].Mask ?= SNG.NoteMask.DoubleStop) "Chord #4 has double stop bit set"
            Expect.equal level.Notes.[6].FingerPrintId.[0] 2s "Chord #6 is inside hand shape (Chord ID 2)"
            Expect.equal level.Notes.[6].Sustain 0.750f "Chord #6 has 0.75s sustain"
            Expect.isTrue (level.Notes.[7].Mask ?= SNG.NoteMask.Ignore) "Chord #7 has ignore bit set"

        testCase "Instrumental Conversion (Chord notes whose hash values may clash)" <| fun _ ->
            let xml = InstrumentalArrangement.Load "chordnotes.xml"

            let sng = ConvertInstrumental.xmlToSng xml

            Expect.hasLength sng.ChordNotes 2 "Two chord notes were created"
    ]
