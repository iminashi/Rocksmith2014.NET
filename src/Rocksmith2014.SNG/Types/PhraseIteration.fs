namespace Rocksmith2014.SNG

open Interfaces
open System.IO

type PhraseIteration =
    { PhraseId : int32
      StartTime : float32
      NextPhraseTime : float32
      Difficulty : int32[] }

    interface IBinaryWritable with
        member this.Write(writer) =
            writer.Write this.PhraseId
            writer.Write this.StartTime
            writer.Write this.NextPhraseTime
            this.Difficulty |> Array.iter writer.Write

    static member Read(reader : BinaryReader) =
        { PhraseId = reader.ReadInt32()
          StartTime = reader.ReadSingle()
          NextPhraseTime = reader.ReadSingle()
          Difficulty = Array.init 3 (fun _ -> reader.ReadInt32()) }
