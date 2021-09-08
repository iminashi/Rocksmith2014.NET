namespace Rocksmith2014.SNG

open Rocksmith2014.Common
open BinaryHelpers

type Note =
    { Mask : NoteMask
      Flags : uint32
      Hash : uint32
      Time : float32
      StringIndex : int8
      Fret : int8
      AnchorFret : int8
      AnchorWidth : int8
      ChordId : int32
      ChordNotesId : int32
      PhraseId : int32
      PhraseIterationId : int32
      FingerPrintId : int16 array
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
      BendData : BendValue array }

    interface IBinaryWritable with
        member this.Write(writer) =
            writer.WriteUInt32 (LanguagePrimitives.EnumToValue(this.Mask))
            writer.WriteUInt32 this.Flags
            writer.WriteUInt32 this.Hash
            writer.WriteSingle this.Time
            writer.WriteInt8 this.StringIndex
            writer.WriteInt8 this.Fret
            writer.WriteInt8 this.AnchorFret
            writer.WriteInt8 this.AnchorWidth
            writer.WriteInt32 this.ChordId
            writer.WriteInt32 this.ChordNotesId
            writer.WriteInt32 this.PhraseId
            writer.WriteInt32 this.PhraseIterationId
            this.FingerPrintId |> Array.iter writer.WriteInt16
            writer.WriteInt16 this.NextIterNote
            writer.WriteInt16 this.PrevIterNote
            writer.WriteInt16 this.ParentPrevNote
            writer.WriteInt8 this.SlideTo
            writer.WriteInt8 this.SlideUnpitchTo
            writer.WriteInt8 this.LeftHand
            writer.WriteInt8 this.Tap
            writer.WriteInt8 this.PickDirection
            writer.WriteInt8 this.Slap
            writer.WriteInt8 this.Pluck
            writer.WriteInt16 this.Vibrato
            writer.WriteSingle this.Sustain
            writer.WriteSingle this.MaxBend
            writeArray writer this.BendData

    static member Read(reader: IBinaryReader) =
        { Mask = LanguagePrimitives.EnumOfValue(reader.ReadUInt32())
          Flags = reader.ReadUInt32()
          Hash = reader.ReadUInt32()
          Time = reader.ReadSingle()
          StringIndex = reader.ReadInt8()
          Fret = reader.ReadInt8()
          AnchorFret = reader.ReadInt8()
          AnchorWidth = reader.ReadInt8()
          ChordId = reader.ReadInt32()
          ChordNotesId = reader.ReadInt32()
          PhraseId = reader.ReadInt32()
          PhraseIterationId = reader.ReadInt32()
          FingerPrintId = Array.init 2 (fun _ -> reader.ReadInt16())
          NextIterNote = reader.ReadInt16()
          PrevIterNote = reader.ReadInt16()
          ParentPrevNote = reader.ReadInt16()
          SlideTo = reader.ReadInt8()
          SlideUnpitchTo = reader.ReadInt8()
          LeftHand = reader.ReadInt8()
          Tap = reader.ReadInt8()
          PickDirection = reader.ReadInt8()
          Slap = reader.ReadInt8()
          Pluck = reader.ReadInt8()
          Vibrato = reader.ReadInt16()
          Sustain = reader.ReadSingle()
          MaxBend = reader.ReadSingle()
          BendData = readArray reader BendValue.Read }
