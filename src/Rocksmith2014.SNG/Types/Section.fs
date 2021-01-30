namespace Rocksmith2014.SNG

open Rocksmith2014.Common
open BinaryHelpers

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
            writer.WriteInt32 this.Number
            writer.WriteSingle this.StartTime
            writer.WriteSingle this.EndTime
            writer.WriteInt32 this.StartPhraseIterationId
            writer.WriteInt32 this.EndPhraseIterationId
            this.StringMask |> Array.iter writer.WriteInt8

    static member Read(reader: IBinaryReader) =
        { Name = readZeroTerminatedUTF8String 32 reader
          Number = reader.ReadInt32()
          StartTime = reader.ReadSingle()
          EndTime = reader.ReadSingle()
          StartPhraseIterationId = reader.ReadInt32()
          EndPhraseIterationId = reader.ReadInt32()
          StringMask = Array.init 36 (fun _ -> reader.ReadInt8()) }
