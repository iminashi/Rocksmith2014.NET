namespace Rocksmith2014.SNG

open Rocksmith2014.Common
open System
open BinaryHelpers

type MetaData =
    { MaxScore: float
      MaxNotesAndChords: float
      MaxNotesAndChordsReal: float
      PointsPerNote: float
      FirstBeatLength: float32
      StartTime: float32
      CapoFretId: int8
      LastConversionDateTime: string
      Part: int16
      SongLength: float32
      Tuning: int16 array
      // First note time appears twice, always the same value
      FirstNoteTime: float32
      MaxDifficulty: int32 }

    interface IBinaryWritable with
        member this.Write(writer) =
            writer.WriteDouble this.MaxScore
            writer.WriteDouble this.MaxNotesAndChords
            writer.WriteDouble this.MaxNotesAndChordsReal
            writer.WriteDouble this.PointsPerNote
            writer.WriteSingle this.FirstBeatLength
            writer.WriteSingle this.StartTime
            writer.WriteInt8 this.CapoFretId
            writeZeroTerminatedUTF8String 32 this.LastConversionDateTime writer
            writer.WriteInt16 this.Part
            writer.WriteSingle this.SongLength
            writer.WriteInt32 this.Tuning.Length
            this.Tuning |> Array.iter writer.WriteInt16
            // Write twice
            writer.WriteSingle this.FirstNoteTime
            writer.WriteSingle this.FirstNoteTime
            writer.WriteInt32 this.MaxDifficulty

    static member Read(reader: IBinaryReader) =
        { MaxScore = reader.ReadDouble()
          MaxNotesAndChords = reader.ReadDouble()
          MaxNotesAndChordsReal = reader.ReadDouble()
          PointsPerNote = reader.ReadDouble()
          FirstBeatLength = reader.ReadSingle()
          StartTime = reader.ReadSingle()
          CapoFretId = reader.ReadInt8()
          LastConversionDateTime = readZeroTerminatedUTF8String 32 reader
          Part = reader.ReadInt16()
          SongLength = reader.ReadSingle()
          Tuning = readArray reader (fun r -> r.ReadInt16())
          // Read twice
          FirstNoteTime = (reader.ReadSingle() |> ignore; reader.ReadSingle())
          MaxDifficulty = reader.ReadInt32() }

module MetaData =
    let Empty =
        { MaxScore = 0.
          MaxNotesAndChords = 0.
          MaxNotesAndChordsReal = 0.
          PointsPerNote = 0.
          FirstBeatLength = 0.f
          StartTime = 0.f
          CapoFretId = -1y
          LastConversionDateTime = String.Empty
          Part = 0s
          SongLength = 0.f
          Tuning = [||]
          FirstNoteTime = 0.f
          MaxDifficulty = 0 }
