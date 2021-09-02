namespace Rocksmith2014.SNG

open Rocksmith2014.Common
open System

[<CustomEquality; NoComparison>]
type BendData32 =
    { BendValues : BendValue[]
      UsedCount : int32 }

    interface IBinaryWritable with
        member this.Write(writer) =
            this.BendValues |> Array.iter (fun b -> (b :> IBinaryWritable).Write writer)
            writer.WriteInt32 this.UsedCount

    static member Read(reader: IBinaryReader) =
        { BendValues = Array.init 32 (fun _ -> BendValue.Read reader)
          UsedCount = reader.ReadInt32() }

    interface IEquatable<BendData32> with
        // Since bend values have timestamps, in theory, different chords should never have the same bend values
        member this.Equals other =
            (this.UsedCount = 0 && other.UsedCount = 0)
            ||
            (this.UsedCount = other.UsedCount && 
             ReadOnlySpan(this.BendValues, 0, this.UsedCount).SequenceEqual(ReadOnlySpan(other.BendValues, 0, other.UsedCount)))

    override this.Equals other =
        match other with
        | :? BendData32 as bd ->
            (this :> IEquatable<_>).Equals bd
        | _ ->
            false

    override this.GetHashCode () =
        if this.UsedCount = 0
        then 0
        else this.BendValues.GetHashCode()

    static member Empty = { BendValues = Array.replicate 32 BendValue.Empty; UsedCount = 0 }
