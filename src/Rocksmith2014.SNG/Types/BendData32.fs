namespace Rocksmith2014.SNG

open Interfaces
open System.IO

type BendData32 =
    { BendValues : BendValue[]
      UsedCount : int32 }

    interface IBinaryWritable with
        member this.Write(writer) =
            this.BendValues |> Array.iter (fun b -> (b :> IBinaryWritable).Write writer)
            writer.Write this.UsedCount

    static member Read(reader : BinaryReader) =
        { BendValues = Array.init 32 (fun _ -> BendValue.Read reader)
          UsedCount = reader.ReadInt32() }

module BendData32 =
    let Empty = { BendValues = Array.replicate 32 BendValue.Empty; UsedCount = 0 }
