namespace Rocksmith2014.SNG

open Rocksmith2014.Common
open System

[<CustomEquality; NoComparison>]
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

    static member Read(reader: IBinaryReader) =
        { Mask = Array.init 6 (fun _ -> reader.ReadUInt32() |> LanguagePrimitives.EnumOfValue)
          BendData = Array.init 6 (fun _ -> BendData32.Read reader)
          SlideTo = Array.init 6 (fun _ -> reader.ReadInt8())
          SlideUnpitchTo = Array.init 6 (fun _ -> reader.ReadInt8())
          Vibrato = Array.init 6 (fun _ -> reader.ReadInt16()) }

    interface IEquatable<ChordNotes> with
        member this.Equals other =
            let rec test index =
                index = 6
                ||
                (this.Mask.[index] = other.Mask.[index] &&
                 this.SlideTo.[index] = other.SlideTo.[index] &&
                 this.SlideUnpitchTo.[index] = other.SlideUnpitchTo.[index] &&
                 this.Vibrato.[index] = other.Vibrato.[index] &&
                 this.BendData.[index] = other.BendData.[index] &&
                 test (index + 1))
            test 0

    override this.Equals other =
        match other with
        | :? ChordNotes as cn -> (this :> IEquatable<_>).Equals cn
        | _ -> false

    override this.GetHashCode () =
        int this.Mask.[0]
        + (int this.Mask.[1] * 2)
        + (int this.Mask.[2] * 3)
        + (int this.Mask.[3] * 4)
        + (int this.Mask.[4] * 5)
        + (int this.Mask.[5] * 6)
