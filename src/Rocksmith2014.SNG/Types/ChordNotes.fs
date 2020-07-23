namespace Rocksmith2014.SNG

open System.IO
open Interfaces

type ChordNotes =
    { Mask : NoteMask[]
      BendData : BendData32[]
      SlideTo : int8[]
      SlideUnpitchTo : int8[] 
      Vibrato : int16[] }
    
    interface IBinaryWritable with
        member this.Write(writer) =
            this.Mask |> Array.iter (LanguagePrimitives.EnumToValue >> writer.Write)
            this.BendData |> Array.iter (fun b -> (b :> IBinaryWritable).Write writer)
            this.SlideTo |> Array.iter writer.Write
            this.SlideUnpitchTo |> Array.iter writer.Write
            this.Vibrato |> Array.iter writer.Write
    
    static member Read(reader : BinaryReader) =
        { Mask = Array.init 6 (fun _ -> reader.ReadUInt32() |> LanguagePrimitives.EnumOfValue)
          BendData = Array.init 6 (fun _ -> BendData32.Read reader)
          SlideTo = Array.init 6 (fun _ -> reader.ReadSByte())
          SlideUnpitchTo = Array.init 6 (fun _ -> reader.ReadSByte())
          Vibrato = Array.init 6 (fun _ -> reader.ReadInt16()) }
