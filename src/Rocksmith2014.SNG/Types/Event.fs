namespace Rocksmith2014.SNG

open Interfaces
open BinaryHelpers
open System.IO

type Event =
    { Time : float32
      Name : string }

    interface IBinaryWritable with
        member this.Write(writer) =
            writer.Write this.Time
            writeZeroTerminatedUTF8String 256 this.Name writer

    static member Read(reader : BinaryReader) =
        { Time = reader.ReadSingle()
          Name = readZeroTerminatedUTF8String 256 reader }
