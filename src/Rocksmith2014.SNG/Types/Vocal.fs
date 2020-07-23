namespace Rocksmith2014.SNG

open System.IO
open Interfaces
open BinaryHelpers

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
