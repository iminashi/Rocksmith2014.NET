namespace Rocksmith2014.SNG

open Rocksmith2014.Common.Interfaces
open BinaryHelpers

[<Struct>]
type Event =
    { Time : float32
      Name : string }

    interface IBinaryWritable with
        member this.Write(writer) =
            writer.WriteSingle this.Time
            writeZeroTerminatedUTF8String 256 this.Name writer

    static member Read(reader : IBinaryReader) =
        { Time = reader.ReadSingle()
          Name = readZeroTerminatedUTF8String 256 reader }
