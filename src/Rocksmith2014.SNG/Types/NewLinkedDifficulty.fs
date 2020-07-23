namespace Rocksmith2014.SNG

open Interfaces
open System.IO
open BinaryHelpers

type NewLinkedDifficulty =
    { LevelBreak : int32
      NLDPhrases : int32[] }

    interface IBinaryWritable with
        member this.Write(writer) =
            writer.Write this.LevelBreak
            writer.Write this.NLDPhrases.Length
            this.NLDPhrases |> Array.iter writer.Write

    static member Read(reader : BinaryReader) =
        { LevelBreak = reader.ReadInt32()
          NLDPhrases = readArray reader (fun r -> r.ReadInt32()) }
