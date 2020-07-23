namespace Rocksmith2014.SNG

open Interfaces

type PhraseIteration =
    { PhraseId : int32
      StartTime : float32
      NextPhraseTime : float32
      Difficulty : int32[] }

    interface IBinaryWritable with
        member this.Write(writer) =
            writer.WriteInt32 this.PhraseId
            writer.WriteSingle this.StartTime
            writer.WriteSingle this.NextPhraseTime
            this.Difficulty |> Array.iter writer.WriteInt32

    static member Read(reader : IBinaryReader) =
        { PhraseId = reader.ReadInt32()
          StartTime = reader.ReadSingle()
          NextPhraseTime = reader.ReadSingle()
          Difficulty = Array.init 3 (fun _ -> reader.ReadInt32()) }
