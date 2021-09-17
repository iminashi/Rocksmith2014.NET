namespace Rocksmith2014.SNG

open Rocksmith2014.Common
open System.Runtime.CompilerServices
open BinaryHelpers

[<IsReadOnly; Struct>]
type NewLinkedDifficulty =
    { LevelBreak: int32
      NLDPhrases: int32 array }

    interface IBinaryWritable with
        member this.Write(writer) =
            writer.WriteInt32 this.LevelBreak
            writer.WriteInt32 this.NLDPhrases.Length
            this.NLDPhrases |> Array.iter writer.WriteInt32

    static member Read(reader: IBinaryReader) =
        { LevelBreak = reader.ReadInt32()
          NLDPhrases = readArray reader (fun r -> r.ReadInt32()) }
