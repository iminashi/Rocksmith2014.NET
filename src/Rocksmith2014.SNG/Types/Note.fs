namespace Rocksmith2014.SNG

open Interfaces
open BinaryHelpers
open System.IO

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
