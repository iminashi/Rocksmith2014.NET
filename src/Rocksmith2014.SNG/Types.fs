module Rocksmith2014.SNG.Types

open System.IO
open Interfaces
open BinaryHelpers
open System

type Platform = PC | Mac

[<Flags>]
type BeatMask =
    | None               = 0b00
    | FirstBeatOfMeasure = 0b01
    | EvenMeasure        = 0b10

[<Flags>]
type ChordMask =
    | None     = 0b00u
    | Arpeggio = 0b01u
    | Nop      = 0b10u

[<Flags>]
type NoteMask =
    | None           = 0b00000000_00000000_00000000_00000000u
  //| Unused         = 0b00000000_00000000_00000000_00000001u 
    | Chord          = 0b00000000_00000000_00000000_00000010u
    | Open           = 0b00000000_00000000_00000000_00000100u
    // Fret-hand mute for chords.
    | FretHandMute   = 0b00000000_00000000_00000000_00001000u
    | Tremolo        = 0b00000000_00000000_00000000_00010000u
    | Harmonic       = 0b00000000_00000000_00000000_00100000u
    | PalmMute       = 0b00000000_00000000_00000000_01000000u
    | Slap           = 0b00000000_00000000_00000000_10000000u
    | Pluck          = 0b00000000_00000000_00000001_00000000u
    | HammerOn       = 0b00000000_00000000_00000010_00000000u
    | PullOff        = 0b00000000_00000000_00000100_00000000u
    | Slide          = 0b00000000_00000000_00001000_00000000u
    | Bend           = 0b00000000_00000000_00010000_00000000u
    | Sustain        = 0b00000000_00000000_00100000_00000000u
    | Tap            = 0b00000000_00000000_01000000_00000000u
    | PinchHarmonic  = 0b00000000_00000000_10000000_00000000u
    | Vibrato        = 0b00000000_00000001_00000000_00000000u
    // Fret-hand mute for notes.
    | Mute           = 0b00000000_00000010_00000000_00000000u
    | Ignore         = 0b00000000_00000100_00000000_00000000u
    | LeftHand       = 0b00000000_00001000_00000000_00000000u
    | RightHand      = 0b00000000_00010000_00000000_00000000u
    | HighDensity    = 0b00000000_00100000_00000000_00000000u
    | UnpitchedSlide = 0b00000000_01000000_00000000_00000000u
    | Single         = 0b00000000_10000000_00000000_00000000u
    | ChordNotes     = 0b00000001_00000000_00000000_00000000u
    | DoubleStop     = 0b00000010_00000000_00000000_00000000u
    | Accent         = 0b00000100_00000000_00000000_00000000u
    | Parent         = 0b00001000_00000000_00000000_00000000u
    | Child          = 0b00010000_00000000_00000000_00000000u
    | Arpeggio       = 0b00100000_00000000_00000000_00000000u
  //| Unused         = 0b01000000_00000000_00000000_00000000u
    | Strum          = 0b10000000_00000000_00000000_00000000u

/// Mask bits that need to be considered when converting a note to XML.
let noteTechniqueMask = 
    NoteMask.Accent ||| NoteMask.HammerOn ||| NoteMask.Harmonic ||| NoteMask.Ignore ||| NoteMask.Mute
    ||| NoteMask.PalmMute ||| NoteMask.Parent ||| NoteMask.PinchHarmonic ||| NoteMask.Pluck
    ||| NoteMask.PullOff ||| NoteMask.RightHand ||| NoteMask.Slap ||| NoteMask.Tremolo

/// Mask bits that need to be considered when converting a chord to XML.
let chordTechniqueMask =
    NoteMask.Accent ||| NoteMask.FretHandMute ||| NoteMask.HighDensity
    ||| NoteMask.Ignore ||| NoteMask.PalmMute ||| NoteMask.Parent      

type Beat =
    { Time : float32
      Measure : int16
      Beat : int16
      PhraseIteration : int32
      Mask : BeatMask }
    
    interface IBinaryWritable with
        member this.Write(writer) =
            writer.Write this.Time
            writer.Write this.Measure
            writer.Write this.Beat
            writer.Write this.PhraseIteration
            writer.Write (LanguagePrimitives.EnumToValue this.Mask)
    
    static member Read(reader : BinaryReader) =
        { Time = reader.ReadSingle()
          Measure = reader.ReadInt16()
          Beat = reader.ReadInt16()
          PhraseIteration = reader.ReadInt32()
          Mask = LanguagePrimitives.EnumOfValue(reader.ReadInt32()) }

type Phrase =
    { Solo : int8
      Disparity : int8 
      Ignore : int8
      // 1 byte padding
      MaxDifficulty : int32
      PhraseIterationLinks : int32
      Name : string }

    interface IBinaryWritable with
        member this.Write(writer) =
            writer.Write this.Solo
            writer.Write this.Disparity
            writer.Write this.Ignore
            // Write a single byte of padding
            writer.Write 0y
            writer.Write this.MaxDifficulty
            writer.Write this.PhraseIterationLinks
            writeZeroTerminatedUTF8String 32 this.Name writer

    static member Read(reader : BinaryReader) =
        let solo = reader.ReadSByte()
        let disp = reader.ReadSByte()
        let ign = reader.ReadSByte() 
        // Read a single byte of padding
        reader.ReadSByte() |> ignore

        { Solo = solo
          Disparity = disp
          Ignore = ign
          MaxDifficulty = reader.ReadInt32()
          PhraseIterationLinks = reader.ReadInt32()
          Name = readZeroTerminatedUTF8String 32 reader }

type Chord =
    { Mask : ChordMask
      Frets : int8[]
      Fingers : int8[]
      Notes : int32[]
      Name : string }
    
    interface IBinaryWritable with
        member this.Write(writer) =
            writer.Write (LanguagePrimitives.EnumToValue(this.Mask))
            this.Frets |> Array.iter writer.Write
            this.Fingers |> Array.iter writer.Write
            this.Notes |> Array.iter writer.Write
            writeZeroTerminatedUTF8String 32 this.Name writer
    
    static member Read(reader : BinaryReader) =
        { Mask = reader.ReadUInt32() |> LanguagePrimitives.EnumOfValue
          Frets = Array.init 6 (fun _ -> reader.ReadSByte())
          Fingers = Array.init 6 (fun _ -> reader.ReadSByte())
          Notes = Array.init 6 (fun _ -> reader.ReadInt32())
          Name = readZeroTerminatedUTF8String 32 reader }

[<Struct>]
type BendValue =
    { Time : float32
      Step : float32 
      Unk3 : int16
      Unk4 : int8
      Unk5 : int8 }

    interface IBinaryWritable with
        member this.Write(writer) =
            writer.Write this.Time
            writer.Write this.Step
            writer.Write this.Unk3
            writer.Write this.Unk4
            writer.Write this.Unk5

    static member Read(reader : BinaryReader) =
        { Time = reader.ReadSingle()
          Step = reader.ReadSingle()
          Unk3 = reader.ReadInt16()
          Unk4 = reader.ReadSByte()
          Unk5 = reader.ReadSByte() }

    static member Create(time, step) = { Time = time; Step = step; Unk3 = 0s; Unk4 = 0y; Unk5 = 0y}

type BendData32 =
    { BendValues : BendValue[]
      UsedCount : int32 }

    interface IBinaryWritable with
        member this.Write(writer) =
            this.BendValues |> Array.iter (fun b -> (b :> IBinaryWritable).Write writer)
            writer.Write this.UsedCount

    static member Read(reader : BinaryReader) =
        { BendValues = Array.init 32 (fun _ -> BendValue.Read reader)
          UsedCount = reader.ReadInt32() }

type ChordNote =
    { Mask : NoteMask[]
      BendData : BendData32[]
      SlideTo : int8[]
      SlideUnpitchTo : int8[] 
      Vibrato : int16[] }
    
    interface IBinaryWritable with
        member this.Write(writer) =
            this.Mask |> Array.iter (LanguagePrimitives.EnumToValue >> writer.Write)
            this.BendData |> Array.iter (fun b -> (b :> IBinaryWritable).Write writer)
            this.SlideTo |> Array.iter writer.Write
            this.SlideUnpitchTo |> Array.iter writer.Write
            this.Vibrato |> Array.iter writer.Write
    
    static member Read(reader : BinaryReader) =
        { Mask = Array.init 6 (fun _ -> reader.ReadUInt32() |> LanguagePrimitives.EnumOfValue)
          BendData = Array.init 6 (fun _ -> BendData32.Read reader)
          SlideTo = Array.init 6 (fun _ -> reader.ReadSByte())
          SlideUnpitchTo = Array.init 6 (fun _ -> reader.ReadSByte())
          Vibrato = Array.init 6 (fun _ -> reader.ReadInt16()) }

type Vocal =
    { Time : float32
      Note : int32
      Length : float32
      Lyric : string }
     
    interface IBinaryWritable with
        member this.Write(writer) =
            writer.Write this.Time
            writer.Write this.Note
            writer.Write this.Length
            writeZeroTerminatedUTF8String 48 this.Lyric writer
    
    static member Read(reader : BinaryReader) =
        { Time = reader.ReadSingle()
          Note = reader.ReadInt32()
          Length = reader.ReadSingle()
          Lyric = readZeroTerminatedUTF8String 48 reader }

type SymbolsHeader = 
    { Unk1ID : int32
      Unk2 : int32
      Unk3 : int32
      Unk4 : int32
      Unk5 : int32
      Unk6 : int32
      Unk7 : int32
      Unk8 : int32 }
    
    interface IBinaryWritable with
        member this.Write(writer) =
            writer.Write this.Unk1ID
            writer.Write this.Unk2
            writer.Write this.Unk3
            writer.Write this.Unk4
            writer.Write this.Unk5
            writer.Write this.Unk6
            writer.Write this.Unk7
            writer.Write this.Unk8
    
    static member Read(reader : BinaryReader) =
        { Unk1ID = reader.ReadInt32()
          Unk2 = reader.ReadInt32()
          Unk3 = reader.ReadInt32()
          Unk4 = reader.ReadInt32()
          Unk5 = reader.ReadInt32()
          Unk6 = reader.ReadInt32()
          Unk7 = reader.ReadInt32()
          Unk8 = reader.ReadInt32() }

type SymbolsTexture =
    { Font : string
      FontPathLength : int32 
      Unk1 : int32
      Width : int32
      Height : int32 }

    interface IBinaryWritable with
        member this.Write(writer) =
            writeZeroTerminatedUTF8String 128 this.Font writer
            writer.Write this.FontPathLength
            writer.Write this.Unk1
            writer.Write this.Width
            writer.Write this.Height

    static member Read(reader : BinaryReader) =
        { Font = readZeroTerminatedUTF8String 128 reader
          FontPathLength = reader.ReadInt32()
          Unk1 = reader.ReadInt32()
          Width = reader.ReadInt32()
          Height = reader.ReadInt32() }

[<Struct>]
type Rect =
    { yMin : float32
      xMin : float32
      yMax : float32
      xMax : float32 }

    interface IBinaryWritable with
        member this.Write(writer) =
            writer.Write this.yMin
            writer.Write this.xMin
            writer.Write this.yMax
            writer.Write this.xMax

    static member Read(reader : BinaryReader) =
        { yMin = reader.ReadSingle()
          xMin = reader.ReadSingle()
          yMax = reader.ReadSingle()
          xMax = reader.ReadSingle() }

type SymbolDefinition =
    { Symbol : string
      Outer : Rect
      Inner : Rect }

    interface IBinaryWritable with
        member this.Write(writer) =
            writeZeroTerminatedUTF8String 12 this.Symbol writer
            (this.Outer :> IBinaryWritable).Write writer
            (this.Inner :> IBinaryWritable).Write writer

    static member Read(reader : BinaryReader) =
        { Symbol = readZeroTerminatedUTF8String 12 reader
          Outer = Rect.Read reader
          Inner = Rect.Read reader }

type PhraseIteration =
    { PhraseId : int32
      StartTime : float32
      NextPhraseTime : float32
      Difficulty : int32[] }

    interface IBinaryWritable with
        member this.Write(writer) =
            writer.Write this.PhraseId
            writer.Write this.StartTime
            writer.Write this.NextPhraseTime
            this.Difficulty |> Array.iter writer.Write

    static member Read(reader : BinaryReader) =
        { PhraseId = reader.ReadInt32()
          StartTime = reader.ReadSingle()
          NextPhraseTime = reader.ReadSingle()
          Difficulty = Array.init 3 (fun _ -> reader.ReadInt32()) }

type PhraseExtraInfo =
    { PhraseId : int32
      Difficulty : int32
      Empty : int32
      LevelJump : int8
      Redundant : int16 }
      // 1 byte padding

    interface IBinaryWritable with
        member this.Write(writer) =
            writer.Write this.PhraseId
            writer.Write this.Difficulty
            writer.Write this.Empty
            writer.Write this.LevelJump
            writer.Write this.Redundant
            // Write a single byte of padding
            writer.Write 0y

    static member Read(reader : BinaryReader) =
        let info =
            { PhraseId = reader.ReadInt32()
              Difficulty = reader.ReadInt32()
              Empty = reader.ReadInt32()
              LevelJump = reader.ReadSByte()
              Redundant = reader.ReadInt16() }
        // Read a single byte of padding
        reader.ReadSByte() |> ignore
        info

type NewLinkedDifficulty =
    { LevelBreak : int32
      PhraseCount : int32
      NLDPhrases : int32[] }

    interface IBinaryWritable with
        member this.Write(writer) =
            writer.Write this.LevelBreak
            writer.Write this.PhraseCount
            this.NLDPhrases |> Array.iter writer.Write

    static member Read(reader : BinaryReader) =
        let lb = reader.ReadInt32()
        let pc = reader.ReadInt32()
        let nld = Array.init pc (fun _ -> reader.ReadInt32())
        { LevelBreak = lb; PhraseCount = pc; NLDPhrases = nld }

type Action = 
    { Time : float32
      ActionName : string }

    interface IBinaryWritable with
        member this.Write(writer) =
            writer.Write this.Time
            writeZeroTerminatedUTF8String 256 this.ActionName writer

    static member Read(reader : BinaryReader) =
        { Time = reader.ReadSingle()
          ActionName = readZeroTerminatedUTF8String 256 reader }

type Event =
    { Time : float32
      Name : string }

    interface IBinaryWritable with
        member this.Write(writer) =
            writer.Write this.Time
            writeZeroTerminatedUTF8String 256 this.Name writer

    static member Read(reader : BinaryReader) =
        { Time = reader.ReadSingle()
          Name = readZeroTerminatedUTF8String 256 reader }

[<Struct>]
type Tone =
    { Time : float32
      ToneId : int32 }

    interface IBinaryWritable with
        member this.Write(writer) =
            writer.Write this.Time
            writer.Write this.ToneId

    static member Read(reader : BinaryReader) =
        { Time = reader.ReadSingle()
          ToneId = reader.ReadInt32() }

[<Struct>]
type DNA =
    { Time : float32
      DnaId : int32 }

    interface IBinaryWritable with
        member this.Write(writer) =
            writer.Write this.Time
            writer.Write this.DnaId

    static member Read(reader : BinaryReader) =
        { Time = reader.ReadSingle()
          DnaId = reader.ReadInt32() }

type Section =
    { Name : string
      Number : int32
      StartTime : float32
      EndTime : float32
      StartPhraseIterationId : int32
      EndPhraseIterationId : int32
      StringMask : int8[] }

    interface IBinaryWritable with
        member this.Write(writer) =
            writeZeroTerminatedUTF8String 32 this.Name writer
            writer.Write this.Number
            writer.Write this.StartTime
            writer.Write this.EndTime
            writer.Write this.StartPhraseIterationId
            writer.Write this.EndPhraseIterationId
            this.StringMask |> Array.iter writer.Write

    static member Read(reader : BinaryReader) =
        { Name = readZeroTerminatedUTF8String 32 reader
          Number = reader.ReadInt32()
          StartTime = reader.ReadSingle()
          EndTime = reader.ReadSingle()
          StartPhraseIterationId = reader.ReadInt32()
          EndPhraseIterationId = reader.ReadInt32()
          StringMask = Array.init 36 (fun _ -> reader.ReadSByte()) }

type Anchor =
    { StartBeatTime : float32
      EndBeatTime : float32
      FirstNoteTime : float32
      LastNoteTime : float32
      FretId : int8
      // 3 bytes padding
      Width : int32
      PhraseIterationId : int32 }

    interface IBinaryWritable with
        member this.Write(writer) =
            writer.Write this.StartBeatTime
            writer.Write this.EndBeatTime
            writer.Write this.FirstNoteTime
            writer.Write this.LastNoteTime
            writer.Write this.FretId
            // Write three bytes of padding
            writer.Write (0y); writer.Write (0y); writer.Write (0y)
            writer.Write this.Width
            writer.Write this.PhraseIterationId

    static member Read(reader : BinaryReader) =
        let startB = reader.ReadSingle()
        let endB = reader.ReadSingle()
        let firstN = reader.ReadSingle()
        let lastN = reader.ReadSingle()
        let fret = reader.ReadSByte()
        // Read three bytes of padding
        reader.ReadSByte() |> ignore; reader.ReadSByte() |> ignore; reader.ReadSByte() |> ignore

        { StartBeatTime = startB; EndBeatTime = endB
          FirstNoteTime = firstN; LastNoteTime = lastN
          FretId = fret
          Width = reader.ReadInt32()
          PhraseIterationId = reader.ReadInt32() }

type AnchorExtension =
    { BeatTime : float32
      FretId : int8
      Unk2 : int32
      Unk3 : int16
      Unk4 : int8 }

    interface IBinaryWritable with
        member this.Write(writer) =
            writer.Write this.BeatTime
            writer.Write this.FretId
            writer.Write this.Unk2
            writer.Write this.Unk3
            writer.Write this.Unk4

    static member Read(reader : BinaryReader) =
        { BeatTime = reader.ReadSingle()
          FretId = reader.ReadSByte()
          Unk2 = reader.ReadInt32()
          Unk3 = reader.ReadInt16()
          Unk4 = reader.ReadSByte() }

type FingerPrint =
    { ChordId : int32
      StartTime : float32
      EndTime : float32
      FirstNoteTime : float32
      LastNoteTime : float32 }

    interface IBinaryWritable with
        member this.Write(writer) =
            writer.Write this.ChordId
            writer.Write this.StartTime
            writer.Write this.EndTime
            writer.Write this.FirstNoteTime
            writer.Write this.LastNoteTime

    static member Read(reader : BinaryReader) =
        { ChordId = reader.ReadInt32() 
          StartTime = reader.ReadSingle()
          EndTime = reader.ReadSingle()
          FirstNoteTime = reader.ReadSingle()
          LastNoteTime = reader.ReadSingle() }

type Note =
    { Mask : NoteMask
      Flags : uint32
      Hash : uint32
      Time : float32
      StringIndex : int8
      FretId : int8
      AnchorFretId : int8
      AnchorWidth : int8
      ChordId : int32
      ChordNotesId : int32
      PhraseId : int32
      PhraseIterationId : int32
      FingerPrintId : int16[]
      NextIterNote : int16
      PrevIterNote : int16
      ParentPrevNote : int16
      SlideTo : int8
      SlideUnpitchTo : int8
      LeftHand : int8
      Tap : int8
      PickDirection : int8
      Slap : int8
      Pluck : int8
      Vibrato : int16
      Sustain : float32
      MaxBend : float32
      BendData : BendValue[] }

    interface IBinaryWritable with
        member this.Write(writer) =
            writer.Write(LanguagePrimitives.EnumToValue(this.Mask))
            writer.Write this.Flags
            writer.Write this.Hash
            writer.Write this.Time
            writer.Write this.StringIndex
            writer.Write this.FretId
            writer.Write this.AnchorFretId
            writer.Write this.AnchorWidth
            writer.Write this.ChordId
            writer.Write this.ChordNotesId
            writer.Write this.PhraseId
            writer.Write this.PhraseIterationId
            this.FingerPrintId |> Array.iter writer.Write
            writer.Write this.NextIterNote
            writer.Write this.PrevIterNote
            writer.Write this.ParentPrevNote
            writer.Write this.SlideTo
            writer.Write this.SlideUnpitchTo
            writer.Write this.LeftHand
            writer.Write this.Tap
            writer.Write this.PickDirection
            writer.Write this.Slap
            writer.Write this.Pluck
            writer.Write this.Vibrato
            writer.Write this.Sustain
            writer.Write this.MaxBend
            writeArray writer this.BendData

    static member Read(reader : BinaryReader) =
        { Mask = LanguagePrimitives.EnumOfValue(reader.ReadUInt32())
          Flags = reader.ReadUInt32()
          Hash = reader.ReadUInt32()
          Time = reader.ReadSingle()
          StringIndex = reader.ReadSByte()
          FretId = reader.ReadSByte()
          AnchorFretId = reader.ReadSByte()
          AnchorWidth = reader.ReadSByte()
          ChordId = reader.ReadInt32()
          ChordNotesId = reader.ReadInt32()
          PhraseId = reader.ReadInt32()
          PhraseIterationId = reader.ReadInt32()
          FingerPrintId = Array.init 2 (fun _ -> reader.ReadInt16())
          NextIterNote = reader.ReadInt16()
          PrevIterNote = reader.ReadInt16()
          ParentPrevNote = reader.ReadInt16()
          SlideTo = reader.ReadSByte()
          SlideUnpitchTo = reader.ReadSByte()
          LeftHand = reader.ReadSByte()
          Tap = reader.ReadSByte()
          PickDirection = reader.ReadSByte()
          Slap = reader.ReadSByte()
          Pluck = reader.ReadSByte()
          Vibrato = reader.ReadInt16()
          Sustain = reader.ReadSingle()
          MaxBend = reader.ReadSingle()
          BendData = readArray reader BendValue.Read }

type Level =
    { Difficulty : int32
      Anchors : Anchor[]
      AnchorExtensions : AnchorExtension[]
      HandShapes : FingerPrint[]
      Arpeggios : FingerPrint[]
      Notes : Note[]
      PhraseCount : int32
      AverageNotesPerIteration : float32[]
      PhraseIterationCount1 : int32
      NotesInIteration1 : int32[]
      PhraseIterationCount2 : int32
      NotesInIteration2 : int32[] }

    interface IBinaryWritable with
        member this.Write(writer) =
            writer.Write(this.Difficulty)
            writeArray writer this.Anchors
            writeArray writer this.AnchorExtensions
            writeArray writer this.HandShapes
            writeArray writer this.Arpeggios
            writeArray writer this.Notes
            writer.Write this.PhraseCount
            this.AverageNotesPerIteration |> Array.iter writer.Write
            writer.Write this.PhraseIterationCount1
            this.NotesInIteration1 |> Array.iter writer.Write
            writer.Write this.PhraseIterationCount2
            this.NotesInIteration2 |> Array.iter writer.Write

    static member Read(reader : BinaryReader) =
        let diff = reader.ReadInt32()
        let anchors = readArray reader Anchor.Read
        let anchorExts = readArray reader AnchorExtension.Read
        let handshapes = readArray reader FingerPrint.Read
        let arpeggios = readArray reader FingerPrint.Read
        let notes = readArray reader Note.Read
        let phraseCount = reader.ReadInt32()
        let avn = Array.init phraseCount (fun _ -> reader.ReadSingle())

        let phraseICount1 = reader.ReadInt32()
        let npi1 = Array.init phraseICount1 (fun _ -> reader.ReadInt32())

        let phraseICount2 = reader.ReadInt32()
        let npi2 = Array.init phraseICount2 (fun _ -> reader.ReadInt32())

        { Difficulty = diff
          Anchors = anchors
          AnchorExtensions = anchorExts
          HandShapes = handshapes
          Arpeggios = arpeggios
          Notes = notes
          PhraseCount = phraseCount
          AverageNotesPerIteration = avn
          PhraseIterationCount1 = phraseICount1
          NotesInIteration1 = npi1
          PhraseIterationCount2 = phraseICount2
          NotesInIteration2 = npi2 }

type MetaData =
    { MaxScore : float
      MaxNotesAndChords : float
      MaxNotesAndChordsReal : float
      PointsPerNote : float
      FirstBeatLength : float32
      StartTime : float32
      CapoFretId : int8
      LastConversionDateTime : string
      Part : int16
      SongLength : float32
      Tuning : int16[]
      Unk11FirstNoteTime : float32
      Unk12FirstNoteTime : float32
      MaxDifficulty : int32 }

    interface IBinaryWritable with
        member this.Write(writer) =
            writer.Write this.MaxScore
            writer.Write this.MaxNotesAndChords
            writer.Write this.MaxNotesAndChordsReal
            writer.Write this.PointsPerNote
            writer.Write this.FirstBeatLength
            writer.Write this.StartTime
            writer.Write this.CapoFretId
            writeZeroTerminatedUTF8String 32 this.LastConversionDateTime writer
            writer.Write this.Part
            writer.Write this.SongLength
            writer.Write this.Tuning.Length
            this.Tuning |> Array.iter writer.Write
            writer.Write this.Unk11FirstNoteTime
            writer.Write this.Unk12FirstNoteTime
            writer.Write this.MaxDifficulty

    static member Read(reader : BinaryReader) =
        { MaxScore = reader.ReadDouble()
          MaxNotesAndChords = reader.ReadDouble()
          MaxNotesAndChordsReal = reader.ReadDouble()
          PointsPerNote = reader.ReadDouble()
          FirstBeatLength = reader.ReadSingle()
          StartTime = reader.ReadSingle()
          CapoFretId = reader.ReadSByte()
          LastConversionDateTime = readZeroTerminatedUTF8String 32 reader
          Part = reader.ReadInt16()
          SongLength = reader.ReadSingle()
          Tuning = readArray reader (fun r -> r.ReadInt16())
          Unk11FirstNoteTime = reader.ReadSingle()
          Unk12FirstNoteTime = reader.ReadSingle()
          MaxDifficulty = reader.ReadInt32() }

type SNG =
    { Beats : Beat[]
      Phrases : Phrase[]
      Chords : Chord[]
      ChordNotes : ChordNote[]
      Vocals : Vocal[]
      SymbolsHeaders : SymbolsHeader[]
      SymbolsTextures : SymbolsTexture[]
      SymbolDefinitions : SymbolDefinition[]
      PhraseIterations : PhraseIteration[]
      PhraseExtraInfo : PhraseExtraInfo[]
      NewLinkedDifficulties : NewLinkedDifficulty[]
      Actions : Action[]
      Events : Event[]
      Tones : Tone[]
      DNAs : DNA[]
      Sections : Section[]
      Levels : Level[]
      MetaData : MetaData }

    interface IBinaryWritable with
        member this.Write(writer) =
            let inline write a = writeArray writer a

            write this.Beats
            write this.Phrases
            write this.Chords
            write this.ChordNotes
            write this.Vocals
            if this.Vocals.Length > 0 then
                write this.SymbolsHeaders
                write this.SymbolsTextures
                write this.SymbolDefinitions
            write this.PhraseIterations
            write this.PhraseExtraInfo
            write this.NewLinkedDifficulties
            write this.Actions
            write this.Events
            write this.Tones
            write this.DNAs
            write this.Sections
            write this.Levels
            (this.MetaData :> IBinaryWritable).Write writer

    static member Read(reader : BinaryReader) =
        let inline read f = readArray reader f

        let beats = read Beat.Read
        let phrases = read Phrase.Read
        let chords = read Chord.Read
        let chordNotes = read ChordNote.Read
        let vocals = read Vocal.Read

        { Beats = beats
          Phrases = phrases
          Chords = chords
          ChordNotes = chordNotes
          Vocals = vocals
          SymbolsHeaders = if vocals.Length > 0 then read SymbolsHeader.Read else [||]
          SymbolsTextures = if vocals.Length > 0 then read SymbolsTexture.Read else [||]
          SymbolDefinitions = if vocals.Length > 0 then read SymbolDefinition.Read else [||]
          PhraseIterations = read PhraseIteration.Read
          PhraseExtraInfo = read PhraseExtraInfo.Read
          NewLinkedDifficulties = read NewLinkedDifficulty.Read
          Actions = read Action.Read
          Events = read Event.Read
          Tones = read Tone.Read
          DNAs = read DNA.Read
          Sections = read Section.Read
          Levels = read Level.Read
          MetaData = MetaData.Read reader }
