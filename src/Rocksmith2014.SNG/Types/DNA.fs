namespace Rocksmith2014.SNG

open Interfaces
open System.IO

[<Struct>]
type DNA =
    { Time : float32
      DnaId : int32 }

    interface IBinaryWritable with
        member this.Write(writer) =
            writer.Write this.Time
            writer.Write this.DnaId

    static member Read(reader : BinaryReader) =
        { Time = reader.ReadSingle()
          DnaId = reader.ReadInt32() }
