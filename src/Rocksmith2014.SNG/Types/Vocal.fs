namespace Rocksmith2014.SNG

open Rocksmith2014.Common.Interfaces
open BinaryHelpers

type Vocal =
    { Time : float32
      Note : int32
      Length : float32
      Lyric : string }
     
    interface IBinaryWritable with
        member this.Write(writer) =
            writer.WriteSingle this.Time
            writer.WriteInt32 this.Note
            writer.WriteSingle this.Length
            writeZeroTerminatedUTF8String 48 this.Lyric writer
    
    static member Read(reader : IBinaryReader) =
        { Time = reader.ReadSingle()
          Note = reader.ReadInt32()
          Length = reader.ReadSingle()
          Lyric = readZeroTerminatedUTF8String 48 reader }
