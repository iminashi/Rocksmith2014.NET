namespace Rocksmith2014.SNG

open Interfaces
open System.IO

type AnchorExtension =
    { BeatTime : float32
      FretId : int8 }
      // Unknown values:
      // (int32), always zero
      // (int16), always zero
      // (int8),  always zero

    interface IBinaryWritable with
        member this.Write(writer) =
            writer.Write this.BeatTime
            writer.Write this.FretId
            // Write zeros for unknown values
            writer.Write 0
            writer.Write 0s
            writer.Write 0y

    static member Read(reader : BinaryReader) =
        let time = reader.ReadSingle()
        let fret = reader.ReadSByte()

        // Read unknown values
        reader.ReadInt32() |> ignore; reader.ReadInt16() |> ignore; reader.ReadSByte() |> ignore

        { BeatTime = time
          FretId = fret }
