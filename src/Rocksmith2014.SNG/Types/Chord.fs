namespace Rocksmith2014.SNG

open Rocksmith2014.Common
open BinaryHelpers

type Chord =
    { Mask : ChordMask
      Frets : int8[]
      Fingers : int8[]
      Notes : int32[]
      Name : string }

    interface IBinaryWritable with
        member this.Write(writer) =
            writer.WriteUInt32 (LanguagePrimitives.EnumToValue(this.Mask))
            this.Frets |> Array.iter writer.WriteInt8
            this.Fingers |> Array.iter writer.WriteInt8
            this.Notes |> Array.iter writer.WriteInt32
            writeZeroTerminatedUTF8String 32 this.Name writer

    static member Read(reader: IBinaryReader) =
        { Mask = reader.ReadUInt32() |> LanguagePrimitives.EnumOfValue
          Frets = Array.init 6 (fun _ -> reader.ReadInt8())
          Fingers = Array.init 6 (fun _ -> reader.ReadInt8())
          Notes = Array.init 6 (fun _ -> reader.ReadInt32())
          Name = readZeroTerminatedUTF8String 32 reader }
