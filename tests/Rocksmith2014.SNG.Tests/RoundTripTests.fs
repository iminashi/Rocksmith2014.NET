module Rocksmith2014.SNG.Tests.RoundTripTests

open Expecto
open Rocksmith2014.SNG.Types
open Rocksmith2014.SNG.Interfaces
open System.IO
open Generators

/// Writes the value into a memory stream and reads it back with the given function.
let roundTrip<'a when 'a :> IBinaryWritable> (x:'a) (read : BinaryReader -> 'a) =
    use stream = new MemoryStream()
    use writer = new BinaryWriter(stream)
    (x :> IBinaryWritable).Write(writer)

    stream.Position <- 0L
    use reader = new BinaryReader(stream)
    read reader

/// Does a round-trip on the given value and tests if the result is equal to it.
let testEqual<'a when 'a : equality and 'a :> IBinaryWritable> (read : BinaryReader -> 'a) (expected:'a) =
    let actual = roundTrip expected read
    Expect.equal actual expected "Equal after round-trip"

let config = { FsCheckConfig.defaultConfig with arbitrary = [typeof<Overrides>; typeof<Generators>] }
let testProp name = testPropertyWithConfig config name

[<Tests>]
let roundTripTests =
  testList "Round-Trips (Create → Write → Read)" [

    testProp "Beats" <| testEqual Beat.Read
    testProp "Phrase" <| testEqual Phrase.Read
    testProp "Chord" <| testEqual Chord.Read
    testProp "Bend Value" <| testEqual BendValue.Read

    testCase "Bend Data 32" <| fun _ ->
      let bd =
        { BendValues = Array.init 32 (fun i -> BendValue.Create(66.66f + float32 i, 1.f / float32 i))
          UsedCount = 32 }
      
      testEqual BendData32.Read bd

    testCase "Chord Note" <| fun _ ->
      let cn =
        { Mask = Array.init 6 (uint32 >> LanguagePrimitives.EnumOfValue)
          BendData = Array.init 6 (fun _ -> { BendValues = Array.init 32 (fun i -> BendValue.Create(106.777f + float32 i, 1.f / float32 i)); UsedCount = 5})
          SlideTo = Array.init 6 sbyte
          SlideUnpitchTo = Array.init 6 sbyte
          Vibrato = Array.init 6 int16 }
      
      testEqual ChordNote.Read cn

    testProp "Vocal" <| testEqual Vocal.Read
    testProp "Symbols Header" <| testEqual SymbolsHeader.Read
    testProp "Symbols Texture" <| testEqual SymbolsTexture.Read
    testProp "Symbols Rectangle" <| testEqual Rect.Read

    testCase "Symbol Definition" <| fun _ ->
      let def =
        { Symbol = "金"
          Outer = { yMin = 1.888f; xMin = 1.015f; yMax = 1.99f; xMax = 1.1f }
          Inner = { yMin = 0.888f; xMin = 0.015f; yMax = 0.99f; xMax = 0.1f } }
      
      testEqual SymbolDefinition.Read def

    testCase "Phrase Iteration" <| fun _ ->
      let pi =
        { PhraseId = 42
          StartTime = 10.111f
          NextPhraseTime = 20.222f
          Difficulty = Array.init 3 id }
      
      testEqual PhraseIteration.Read pi

    testProp "Phrase Extra Info" <| testEqual PhraseExtraInfo.Read

    testCase "New Linked Difficulty" <| fun _ ->
      let info =
        { LevelBreak = 16
          NLDPhrases = Array.init 4 id }
      
      testEqual NewLinkedDifficulty.Read info

    testCase "Action" <| fun _ ->
      let action =
        { Time = 70.0f
          ActionName = "NOT USED IN RS2014 <_<" }
      
      testEqual Action.Read action

    testProp "Event" <| testEqual Event.Read
    testProp "Tone" <| testEqual Tone.Read
    testProp "DNA" <| testEqual DNA.Read

    testCase "Section" <| fun _ ->
      let section =
        { Name = "tapping"
          Number = 2
          StartTime = 50.0f
          EndTime = 62.7f
          StartPhraseIterationId = 5
          EndPhraseIterationId = 6
          StringMask = Array.init 36 sbyte }
      
      testEqual Section.Read section

    testCase "Anchor" <| fun _ ->
      let a =
        { StartBeatTime = 10.0f
          EndBeatTime = 20.0f
          FirstNoteTime = 11.0f
          LastNoteTime = 17.0f
          FretId = 12y
          Width = 4
          PhraseIterationId = 7 }
      
      testEqual Anchor.Read a

    testProp "Anchor Extension" <| testEqual AnchorExtension.Read
    testProp "Finger Print" <| testEqual FingerPrint.Read
      
    testCase "Note" <| fun _ ->
      let n =
        { Mask = NoteMask.FretHandMute ||| NoteMask.UnpitchedSlide
          Flags = 1u
          Hash = 45684265u
          Time = 7.8f
          StringIndex = 1y
          FretId = 7y
          AnchorFretId = 7y
          AnchorWidth = 5y
          ChordId = -1
          ChordNotesId = -1
          PhraseId = 2
          PhraseIterationId = 4
          FingerPrintId = Array.init 2 int16
          NextIterNote = 2s
          PrevIterNote = 4s
          ParentPrevNote = 1s
          SlideTo = -1y
          SlideUnpitchTo = 24y
          LeftHand = -1y
          Tap = 0y
          PickDirection = 0y
          Slap = 1y
          Pluck = 0y
          Vibrato = 80s
          Sustain = 44.0f
          MaxBend = 1.0f
          BendData = Array.init 2 (fun i -> { Time = 66.66f; Step = 1.f / float32 i; Unk3 = 0s; Unk4 = 0y; Unk5 = 0y }) }
      
      testEqual Note.Read n

    testCase "Meta Data" <| fun _ ->
      let md =
        { MaxScore = 100000.0
          MaxNotesAndChords = 456.0
          MaxNotesAndChordsReal = 452.0
          PointsPerNote = 100.0
          FirstBeatLength = 88.0f
          StartTime = 10.0f
          CapoFretId = -1y
          LastConversionDateTime = "6-11-18 18:36"
          Part = 1s
          SongLength = 520.0f
          Tuning = Array.init 6 int16
          Unk11FirstNoteTime = 15.0f
          Unk12FirstNoteTime = 15.0f
          MaxDifficulty = 22 }
      
      testEqual MetaData.Read md
  ]