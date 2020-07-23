namespace Rocksmith2014.SNG

open Interfaces
open System.IO

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
