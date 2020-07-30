namespace Rocksmith2014.SNG

open Rocksmith2014.Common.Interfaces

type ChordNotes =
    { Mask : NoteMask[]
      BendData : BendData32[]
      SlideTo : int8[]
      SlideUnpitchTo : int8[] 
      Vibrato : int16[] }
    
    interface IBinaryWritable with
        member this.Write(writer) =
            this.Mask |> Array.iter (LanguagePrimitives.EnumToValue >> writer.WriteUInt32)
            this.BendData |> Array.iter (fun b -> (b :> IBinaryWritable).Write writer)
            this.SlideTo |> Array.iter writer.WriteInt8
            this.SlideUnpitchTo |> Array.iter writer.WriteInt8
            this.Vibrato |> Array.iter writer.WriteInt16
    
    static member Read(reader : IBinaryReader) =
        { Mask = Array.init 6 (fun _ -> reader.ReadUInt32() |> LanguagePrimitives.EnumOfValue)
          BendData = Array.init 6 (fun _ -> BendData32.Read reader)
          SlideTo = Array.init 6 (fun _ -> reader.ReadInt8())
          SlideUnpitchTo = Array.init 6 (fun _ -> reader.ReadInt8())
          Vibrato = Array.init 6 (fun _ -> reader.ReadInt16()) }
