namespace Rocksmith2014.SNG

open Rocksmith2014.Common.Interfaces
open System.Runtime.CompilerServices

[<IsReadOnly; Struct>]
type BendValue =
    { Time : float32
      Step : float32 }
      // Unknown values:
      // (int16), always zero
      // (int8), always zero
      // (int8), often zero, but can be a random value, even in unused bend data for chord notes

    interface IBinaryWritable with
        member this.Write(writer) =
            writer.WriteSingle this.Time
            writer.WriteSingle this.Step
            // Write zero for all unknown values
            writer.WriteInt32 0

    static member Read(reader : IBinaryReader) =
        let time = reader.ReadSingle()
        let step = reader.ReadSingle()
        // Read unknown values
        reader.ReadInt32() |> ignore

        { Time = time; Step = step }

    static member Create(time, step) = { Time = time; Step = step }

module BendValue =
    let Empty = { Time = 0.f; Step = 0.f }
