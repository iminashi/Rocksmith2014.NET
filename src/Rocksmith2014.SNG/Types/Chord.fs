namespace Rocksmith2014.SNG

open System.IO
open Interfaces
open BinaryHelpers

type Chord =
    { Mask : ChordMask
      Frets : int8[]
      Fingers : int8[]
      Notes : int32[]
      Name : string }
    
    interface IBinaryWritable with
        member this.Write(writer) =
            writer.Write (LanguagePrimitives.EnumToValue(this.Mask))
            this.Frets |> Array.iter writer.Write
            this.Fingers |> Array.iter writer.Write
            this.Notes |> Array.iter writer.Write
            writeZeroTerminatedUTF8String 32 this.Name writer
    
    static member Read(reader : BinaryReader) =
        { Mask = reader.ReadUInt32() |> LanguagePrimitives.EnumOfValue
          Frets = Array.init 6 (fun _ -> reader.ReadSByte())
          Fingers = Array.init 6 (fun _ -> reader.ReadSByte())
          Notes = Array.init 6 (fun _ -> reader.ReadInt32())
          Name = readZeroTerminatedUTF8String 32 reader }
