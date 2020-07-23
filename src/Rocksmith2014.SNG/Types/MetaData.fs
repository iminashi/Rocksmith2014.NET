namespace Rocksmith2014.SNG

open Interfaces
open BinaryHelpers
open System
open System.IO

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
      // First note time appears twice, always the same value
      FirstNoteTime : float32
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
            // Write twice
            writer.Write this.FirstNoteTime
            writer.Write this.FirstNoteTime
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
