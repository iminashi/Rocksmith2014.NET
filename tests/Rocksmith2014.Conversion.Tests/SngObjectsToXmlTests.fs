module Rocksmith2014.Conversion.Tests.SngObjectsToXmlTests

open Expecto
open Rocksmith2014.Common
open Rocksmith2014.Conversion
open Rocksmith2014.SNG

let emptyMetaData =
    { MaxScore = 0.0
      MaxNotesAndChords = 0.0
      MaxNotesAndChordsReal = 0.0
      PointsPerNote = 0.0
      FirstBeatLength = 0.0f
      StartTime = 10.0f
      CapoFretId = -1y
      LastConversionDateTime = ""
      Part = 1s
      SongLength = 0.0f
      Tuning = [||]
      FirstNoteTime = 10.0f
      MaxDifficulty = 29 }

let testSng =
    let template =
      { Mask = ChordMask.None
        Frets = [|-1y;0y;2y;2y;2y;0y|]
        Fingers = [|-1y;-1y;1y;2y;3y;-1y|]
        Notes = [||]
        Name = "A" }

    let cn =
      { Mask = Array.replicate 6 NoteMask.None
        BendData = Array.replicate 6 { UsedCount = 0; BendValues = Array.replicate 32 (BendValue.Create(0.f, 0.f)) }
        SlideTo = Array.replicate 6 -1y
        SlideUnpitchTo = Array.replicate 6 -1y
        Vibrato = Array.replicate 6 0s }

    { Beats = [||]
      Phrases = [||]
      Chords = [| template |]
      ChordNotes = [| cn |]
      Vocals = [||]
      SymbolsHeaders = [||]
      SymbolsTextures = [||]
      SymbolDefinitions = [||]
      PhraseIterations = [||]
      PhraseExtraInfo = [||]
      NewLinkedDifficulties = [||]
      Actions = [||]
      Events = [||]
      Tones = [||]
      DNAs = [||]
      Sections = [||]
      Levels = [||]
      MetaData = emptyMetaData
      NoteCounts = NoteCounts.Empty }


[<Tests>]
let sngToXmlConversionTests =
    testList "SNG Objects → XML Objects" [
        testCase "Beat" <| fun _ ->
            let b =
                { Time = 5468.422f
                  Measure = 5s
                  Beat = 0s
                  PhraseIteration = 0
                  Mask = BeatMask.FirstBeatOfMeasure }

            let xml = SngToXml.convertBeat b

            Expect.equal xml.Time 5468_422 "Time is same"
            Expect.equal xml.Measure b.Measure "Measure is same"

        testCase "Chord Template" <| fun _ ->
            let name = "Eb9/A#"
            let c =
                { Name = name
                  Fingers = [| -1y; 4y; 3y; 2y; 1y; -1y |]
                  Frets = [| -1y; 5y; 6y; 7y; 8y; -1y|]
                  Notes = Array.zeroCreate 6
                  Mask = ChordMask.None }

            let xml = SngToXml.convertChordTemplate c

            Expect.equal xml.DisplayName name "Display name is correct"
            Expect.equal xml.Name name "Chord name is same"
            Expect.sequenceEqual xml.Fingers c.Fingers "Fingering is same"
            Expect.sequenceEqual xml.Frets c.Frets "Frets are same"

        testCase "Chord Template (Arpeggio)" <| fun _ ->
            let name = "Eb9/A#"
            let c =
                { Name = name
                  Fingers = [| -1y; 4y; 3y; 2y; 1y; -1y |]
                  Frets = [| -1y; 5y; 6y; 7y; 8y; -1y|]
                  Notes = Array.zeroCreate 6
                  Mask = ChordMask.Arpeggio }

            let xml = SngToXml.convertChordTemplate c

            Expect.equal xml.DisplayName (name+"-arp") "Display name is correct"

        testCase "Chord Template (Nop)" <| fun _ ->
            let name = "Eb9/A#"
            let c =
                { Name = name
                  Fingers = [| -1y; 4y; 3y; 2y; 1y; -1y |]
                  Frets = [| -1y; 5y; 6y; 7y; 8y; -1y|]
                  Notes = Array.zeroCreate 6
                  Mask = ChordMask.Nop }

            let xml = SngToXml.convertChordTemplate c

            Expect.equal xml.DisplayName (name+"-nop") "Display name is correct"

        testCase "Phrase" <| fun _ ->
            let p =
                { Solo = 1y
                  Disparity = 1y
                  Ignore = 1y
                  MaxDifficulty = 25
                  IterationCount = 7
                  Name = "thelittleguitarthatcould" }

            let xml = SngToXml.convertPhrase p

            Expect.equal xml.Name p.Name "Name is same"
            Expect.equal xml.MaxDifficulty 25uy "Max difficulty is same"
            Expect.isTrue xml.IsSolo "Is solo phrase"
            Expect.isTrue xml.IsDisparity "Is disparity phrase"
            Expect.isTrue xml.IsIgnore "Is ignore phrase"

        testCase "BendValue" <| fun _ ->
            let bv = BendValue.Create(11.111f, 2.5f)

            let xml = SngToXml.convertBendValue bv

            Expect.equal xml.Step bv.Step "Step is same"
            Expect.equal xml.Time 11_111 "Time code is same"

        testCase "BendData32 (Empty)" <| fun _ ->
            let bd = { BendValues = [||]; UsedCount = 0 }

            let xml = SngToXml.convertBendData32 bd

            Expect.isNull xml "Null is returned for empty bend data"

        testCase "BendData32" <| fun _ ->
            let bd =
              { BendValues = [| BendValue.Create(11.111f, 2.5f); BendValue.Create(22.222f, 1.5f) |]
                UsedCount = 2 }

            let xml = SngToXml.convertBendData32 bd

            Expect.equal xml.Count bd.UsedCount "Count is same"
            Expect.equal xml.[0].Time 11_111 "Time code of first bend value is same"
            Expect.equal xml.[1].Step 1.5f "Step of second bend value is same"

        testCase "Vocal" <| fun _ ->
            let v =
                { Time = 87.999f
                  Note = 77
                  Length = 4.654f
                  Lyric = "end+" }

            let xml = SngToXml.convertVocal v

            Expect.equal xml.Lyric v.Lyric "Lyric is same"
            Expect.equal xml.Time 87_999 "Time code is same"
            Expect.equal xml.Length 4_654 "Length is same"
            Expect.equal xml.Note 77uy "Note is same"

        testCase "Symbol Definition" <| fun _ ->
            let sd =
                { Symbol = "轟"
                  Outer = { YMin = 0.12f; YMax = 0.77f; XMin = 0.05f; XMax = 1.7f }
                  Inner = { YMin = 4.7f; YMax = 1.11f; XMin = 55.5f; XMax = 2.8f } }

            let xml = SngToXml.convertSymbolDefinition sd

            Expect.equal xml.Symbol sd.Symbol "Symbol is same"
            Expect.equal xml.OuterYMin sd.Outer.YMin "Outer Y Min is same"
            Expect.equal xml.OuterYMax sd.Outer.YMax "Outer Y Max is same"
            Expect.equal xml.OuterXMin sd.Outer.XMin "Outer X Min is same"
            Expect.equal xml.OuterXMax sd.Outer.XMax "Outer X Max is same"
            Expect.equal xml.InnerYMin sd.Inner.YMin "Inner Y Min is same"
            Expect.equal xml.InnerYMax sd.Inner.YMax "Inner Y Max is same"
            Expect.equal xml.InnerXMin sd.Inner.XMin "Inner X Min is same"
            Expect.equal xml.InnerXMax sd.Inner.XMax "Inner X Max is same"

        testCase "Phrase Iteration" <| fun _ ->
            let pi =
                { PhraseId = 44
                  StartTime = 44.217f
                  EndTime = 45.001f
                  Difficulty = [| 5; 8; 13 |] }

            let xml = SngToXml.convertPhraseIteration pi

            Expect.equal xml.PhraseId pi.PhraseId "Phrase ID is same"
            Expect.equal xml.Time 44_217 "Time code is same"
            Expect.equal (xml.HeroLevels.Easy |> int) pi.Difficulty.[0] "Easy is same level"
            Expect.equal (xml.HeroLevels.Medium |> int) pi.Difficulty.[1] "Medium is same level"
            Expect.equal (xml.HeroLevels.Hard |> int) pi.Difficulty.[2] "Hard is same level"

        testCase "Phrase Properties" <| fun _ ->
            let pi =
                { PhraseId = 5
                  Difficulty = 3
                  Empty = 7
                  LevelJump = 1y
                  Redundant = 12s }

            let xml = SngToXml.convertPhraseExtraInfo pi

            Expect.equal xml.PhraseId pi.PhraseId "Phrase ID is same"
            Expect.equal xml.Difficulty pi.Difficulty "Difficulty is same"
            Expect.equal xml.Empty pi.Empty "Empty is same"
            Expect.equal xml.LevelJump pi.LevelJump "Level jump is same"
            Expect.equal xml.Redundant pi.Redundant "Redundant is same"

        testCase "New Linked Difficulty" <| fun _ ->
            let nld =
                { LevelBreak = 12
                  NLDPhrases = [| 2; 6; 15 |] }

            let xml = SngToXml.convertNLD nld

            Expect.equal xml.LevelBreak (nld.LevelBreak |> sbyte) "Level break is same"
            Expect.sequenceEqual xml.PhraseIds nld.NLDPhrases "Phrase IDs are same"

        testCase "Event" <| fun _ ->
            let e = { Time = 1750.735f; Name = "wedge_cutoff" }

            let xml = SngToXml.convertEvent e

            Expect.equal xml.Code e.Name "Code/name is same"
            Expect.equal xml.Time 1750_735 "Time code is same"

        testCase "Tone" <| fun _ ->
            let t = { Time = 4568.0f; ToneId = 2 }
            let attr = Manifest.Attributes(Tone_C = "tone_c")

            let xml = SngToXml.convertTone (Some attr) t

            Expect.equal xml.Id (t.ToneId |> byte) "Tone ID is same"
            Expect.equal xml.Time 4568_000 "Time code is same"
            Expect.equal xml.Name attr.Tone_C "Tone name is correct"

        testCase "Section" <| fun _ ->
            let s =
                { Name = "chorus"
                  Number = 3
                  StartTime = 123.456f
                  EndTime = 789.012f
                  StartPhraseIterationId = 4
                  EndPhraseIterationId = 5
                  StringMask = [||] }

            let xml = SngToXml.convertSection s

            Expect.equal xml.Name s.Name "Section name is same"
            Expect.equal xml.Time 123_456 "Time code is same"
            Expect.equal xml.Number 3s "Section number is same"

        testCase "Anchor" <| fun _ ->
            let a =
                { StartTime = 5.f
                  EndTime = 6.f
                  FirstNoteTime = 5.f
                  LastNoteTime = nanf
                  FretId = 14y
                  Width = 4
                  PhraseIterationId = 3 }

            let xml = SngToXml.convertAnchor a

            Expect.equal xml.Fret a.FretId "Fret is same"
            Expect.equal xml.Time 5_000 "Time code is same"
            Expect.equal xml.Width (sbyte a.Width) "Width is same"

        testCase "FingerPrint/HandShape" <| fun _ ->
            let fp =
                { ChordId = 15
                  StartTime = 999.999f
                  EndTime = 1001.001f
                  FirstNoteTime = 999.999f
                  LastNoteTime = 1001.f }

            let xml = SngToXml.convertHandShape fp

            Expect.equal xml.ChordId (int16 fp.ChordId) "Chord ID is same"
            Expect.equal xml.StartTime 999_999 "Start time is same"
            Expect.equal xml.EndTime 1001_001 "End time is same"

        testCase "Note" <| fun _ ->
            let n =
                { Mask = NoteMask.Single ||| NoteMask.HammerOn ||| NoteMask.Accent ||| NoteMask.Mute |||
                         NoteMask.Harmonic ||| NoteMask.Ignore ||| NoteMask.Parent ||| NoteMask.PalmMute |||
                         NoteMask.PinchHarmonic ||| NoteMask.Tremolo ||| NoteMask.RightHand ||| NoteMask.PullOff |||
                         NoteMask.Slap ||| NoteMask.Pluck
                  Flags = 0u
                  Hash = 1234u
                  Time = 55.55f
                  StringIndex = 4y
                  Fret = 8y
                  AnchorFret = 8y
                  AnchorWidth = 4y
                  ChordId = -1
                  ChordNotesId = -1
                  PhraseId = 7
                  PhraseIterationId = 12
                  FingerPrintId = [| -1s; -1s |]
                  NextIterNote = 16s
                  PrevIterNote = 14s
                  ParentPrevNote = 14s
                  SlideTo = 10y
                  SlideUnpitchTo = 12y
                  LeftHand = 2y
                  Tap = 3y
                  PickDirection = -1y
                  Slap = 1y
                  Pluck = 1y
                  Vibrato = 120s
                  Sustain = 15.f
                  MaxBend = 1.f
                  BendData = [| BendValue.Create(55.661f, 1.f) |] }

            let xml = SngToXml.convertNote n

            Expect.equal xml.Time 55_550 "Time code is same"
            Expect.equal xml.String n.StringIndex "String is same"
            Expect.equal xml.Fret n.Fret "Fret is same"
            Expect.equal xml.Sustain 15_000 "Sustain is same"
            Expect.equal xml.Vibrato (byte n.Vibrato) "Vibrato is same"
            Expect.equal xml.SlideTo n.SlideTo "Slide is same"
            Expect.equal xml.SlideUnpitchTo n.SlideUnpitchTo "Unpitched slide is same"
            Expect.equal xml.LeftHand n.LeftHand "Left hand is same"
            Expect.equal xml.Tap n.Tap "Tap is same"
            Expect.isTrue xml.IsSlap "Slap is same"
            Expect.isTrue xml.IsPluck "Pluck is same"
            Expect.isTrue xml.IsHammerOn "Hammer-on is same"
            Expect.isTrue xml.IsPullOff "Pull-off is same"
            Expect.isTrue xml.IsAccent "Accent is same"
            Expect.isTrue xml.IsFretHandMute "Fret-hand mute is same"
            Expect.isTrue xml.IsHarmonic "Harmonic is same"
            Expect.isTrue xml.IsIgnore "Ignore is same"
            Expect.isTrue xml.IsLinkNext "Link-next is same"
            Expect.isTrue xml.IsPalmMute "Palm-mute is same"
            Expect.isTrue xml.IsPinchHarmonic "Pinch harmonic is same"
            Expect.isTrue xml.IsTremolo "Tremolo is same"
            Expect.isTrue xml.IsRightHand "Right hand is same"
            Expect.equal xml.MaxBend n.MaxBend "Max bend is same"
            Expect.equal xml.BendValues.Count n.BendData.Length "Bend value count is same"

        testCase "Chord" <| fun _ ->
            let c =
                { Mask = NoteMask.Chord ||| NoteMask.Parent ||| NoteMask.Accent ||| NoteMask.FretHandMute |||
                         NoteMask.HighDensity ||| NoteMask.Ignore ||| NoteMask.PalmMute ||| NoteMask.ChordPanel
                  Flags = 0u
                  Hash = 1234u
                  Time = 66.66f
                  StringIndex = -1y
                  Fret = -1y
                  AnchorFret = 8y
                  AnchorWidth = 4y
                  ChordId = 0
                  ChordNotesId = 0
                  PhraseId = 7
                  PhraseIterationId = 12
                  FingerPrintId = [| 1s; -1s |]
                  NextIterNote = 16s
                  PrevIterNote = 14s
                  ParentPrevNote = 14s
                  SlideTo = -1y
                  SlideUnpitchTo = -1y
                  LeftHand = -1y
                  Tap = -1y
                  PickDirection = -1y
                  Slap = -1y
                  Pluck = -1y
                  Vibrato = 0s
                  Sustain = 0.f
                  MaxBend = 0.f
                  BendData = [||] }

            let xml = SngToXml.convertChord testSng c

            Expect.equal xml.Time 66_660 "Time code is same"
            Expect.equal xml.ChordId (int16 c.ChordId) "Chord ID is same"
            Expect.isTrue xml.IsLinkNext "Link-next is same"
            Expect.isTrue xml.IsAccent "Accent is same"
            Expect.isTrue xml.IsFretHandMute "Fret-hand mute is same"
            Expect.isTrue xml.IsHighDensity "High density is same"
            Expect.isTrue xml.IsIgnore "Ignore is same"
            Expect.isTrue xml.IsPalmMute "Palm-mute is same"
            Expect.hasLength xml.ChordNotes 5 "Chord notes were created"

        testCase "Chord (no chord notes)" <| fun _ ->
            let c =
                { Mask = NoteMask.Chord
                  Flags = 0u
                  Hash = 1234u
                  Time = 66.66f
                  StringIndex = -1y
                  Fret = -1y
                  AnchorFret = 8y
                  AnchorWidth = 4y
                  ChordId = 0
                  ChordNotesId = -1
                  PhraseId = 7
                  PhraseIterationId = 12
                  FingerPrintId = [| 1s; -1s |]
                  NextIterNote = 16s
                  PrevIterNote = 14s
                  ParentPrevNote = 14s
                  SlideTo = -1y
                  SlideUnpitchTo = -1y
                  LeftHand = -1y
                  Tap = -1y
                  PickDirection = -1y
                  Slap = -1y
                  Pluck = -1y
                  Vibrato = 0s
                  Sustain = 0.f
                  MaxBend = 0.f
                  BendData = [||] }

            let xml = SngToXml.convertChord testSng c

            Expect.isNull xml.ChordNotes "Chord notes were not created"

        testCase "Level" <| fun _ ->
            let a =
              { StartTime = 10.f
                EndTime = 11.0f
                FirstNoteTime = 10.f
                LastNoteTime = 11.f
                FretId = 4y
                Width = 7
                PhraseIterationId = 1 }

            let fp1 =
              { ChordId = 0
                StartTime = 10.5f
                EndTime = 10.75f
                FirstNoteTime = 10.5f
                LastNoteTime = 10.5f }

            let fp2 =
              { ChordId = 1
                StartTime = 10.82f
                EndTime = 10.99f
                FirstNoteTime = 10.82f
                LastNoteTime = 10.90f }

            let n =
                { Mask = NoteMask.Single ||| NoteMask.HammerOn
                  Flags = 0u
                  Hash = 1234u
                  Time = 55.55f
                  StringIndex = 4y
                  Fret = 8y
                  AnchorFret = 8y
                  AnchorWidth = 4y
                  ChordId = -1
                  ChordNotesId = -1
                  PhraseId = 7
                  PhraseIterationId = 12
                  FingerPrintId = [| -1s; -1s |]
                  NextIterNote = 16s
                  PrevIterNote = 14s
                  ParentPrevNote = 14s
                  SlideTo = 10y
                  SlideUnpitchTo = 12y
                  LeftHand = 2y
                  Tap = 3y
                  PickDirection = -1y
                  Slap = 1y
                  Pluck = 1y
                  Vibrato = 120s
                  Sustain = 15.f
                  MaxBend = 1.f
                  BendData = [||] }

            let c =
                { Mask = NoteMask.Chord
                  Flags = 0u
                  Hash = 1234u
                  Time = 66.66f
                  StringIndex = -1y
                  Fret = -1y
                  AnchorFret = 8y
                  AnchorWidth = 4y
                  ChordId = 0
                  ChordNotesId = 0
                  PhraseId = 7
                  PhraseIterationId = 12
                  FingerPrintId = [| 1s; -1s |]
                  NextIterNote = 16s
                  PrevIterNote = 14s
                  ParentPrevNote = 14s
                  SlideTo = -1y
                  SlideUnpitchTo = -1y
                  LeftHand = -1y
                  Tap = -1y
                  PickDirection = -1y
                  Slap = -1y
                  Pluck = -1y
                  Vibrato = 0s
                  Sustain = 0.f
                  MaxBend = 0.f
                  BendData = [||] }

            let lvl =
                { Difficulty = 4
                  Anchors = [| a |]
                  AnchorExtensions = [||]
                  HandShapes = [| fp1 |]
                  Arpeggios = [| fp2 |]
                  Notes = [| n; c |]
                  AverageNotesPerIteration = [| 1.0f |]
                  NotesInPhraseIterationsExclIgnored =[| 1 |]
                  NotesInPhraseIterationsAll = [| 1 |] }

            let xml = SngToXml.convertLevel testSng lvl

            Expect.equal xml.Difficulty (sbyte lvl.Difficulty) "Difficulty is same"
            Expect.equal xml.Anchors.Count lvl.Anchors.Length "Anchor count is same"
            Expect.equal xml.HandShapes.Count (lvl.HandShapes.Length + lvl.Arpeggios.Length) "Handshape count is same"
            Expect.equal (xml.Notes.Count + xml.Chords.Count) lvl.Notes.Length "Note/chord count is same"
    ]
