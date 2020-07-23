namespace Rocksmith2014.SNG

open Interfaces
open System.IO

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
