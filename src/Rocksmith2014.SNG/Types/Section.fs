namespace Rocksmith2014.SNG

open Interfaces
open BinaryHelpers
open System.IO

type Section =
    { Name : string
      Number : int32
      StartTime : float32
      EndTime : float32
      StartPhraseIterationId : int32
      EndPhraseIterationId : int32
      StringMask : int8[] }

    interface IBinaryWritable with
        member this.Write(writer) =
            writeZeroTerminatedUTF8String 32 this.Name writer
            writer.Write this.Number
            writer.Write this.StartTime
            writer.Write this.EndTime
            writer.Write this.StartPhraseIterationId
            writer.Write this.EndPhraseIterationId
            this.StringMask |> Array.iter writer.Write

    static member Read(reader : BinaryReader) =
        { Name = readZeroTerminatedUTF8String 32 reader
          Number = reader.ReadInt32()
          StartTime = reader.ReadSingle()
          EndTime = reader.ReadSingle()
          StartPhraseIterationId = reader.ReadInt32()
          EndPhraseIterationId = reader.ReadInt32()
          StringMask = Array.init 36 (fun _ -> reader.ReadSByte()) }
