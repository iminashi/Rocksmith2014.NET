namespace Rocksmith2014.SNG

open Rocksmith2014.Common.Interfaces

type FingerPrint =
    { ChordId : int32
      StartTime : float32
      EndTime : float32
      FirstNoteTime : float32
      LastNoteTime : float32 }

    interface IBinaryWritable with
        member this.Write(writer) =
            writer.WriteInt32 this.ChordId
            writer.WriteSingle this.StartTime
            writer.WriteSingle this.EndTime
            writer.WriteSingle this.FirstNoteTime
            writer.WriteSingle this.LastNoteTime

    static member Read(reader : IBinaryReader) =
        { ChordId = reader.ReadInt32() 
          StartTime = reader.ReadSingle()
          EndTime = reader.ReadSingle()
          FirstNoteTime = reader.ReadSingle()
          LastNoteTime = reader.ReadSingle() }
