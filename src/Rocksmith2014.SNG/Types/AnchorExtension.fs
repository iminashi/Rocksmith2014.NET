namespace Rocksmith2014.SNG

open Rocksmith2014.Common.Interfaces
open System.Runtime.CompilerServices

[<IsReadOnly; Struct>]
type AnchorExtension =
    { BeatTime : float32
      FretId : int8 }
      // Unknown values:
      // (int32), always zero
      // (int16), always zero
      // (int8),  always zero

    interface IBinaryWritable with
        member this.Write(writer) =
            writer.WriteSingle this.BeatTime
            writer.WriteInt8 this.FretId
            // Write zeros for unknown values
            writer.WriteInt32 0
            writer.WriteInt16 0s
            writer.WriteInt8 0y

    static member Read(reader : IBinaryReader) =
        let time = reader.ReadSingle()
        let fret = reader.ReadInt8()

        // Read unknown values
        reader.ReadInt32() |> ignore; reader.ReadInt16() |> ignore; reader.ReadInt8() |> ignore

        { BeatTime = time
          FretId = fret }
