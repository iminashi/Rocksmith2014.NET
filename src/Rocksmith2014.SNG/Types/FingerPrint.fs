namespace Rocksmith2014.SNG

open Rocksmith2014.Common.Interfaces

type FingerPrint =
    { ChordId : int32
      StartTime : float32
      EndTime : float32
      FirstNoteTime : float32
      /// Defines the time after which the handshape fingering can start moving to the next fingering (-1 = handshape end time).
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
