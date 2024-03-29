namespace Rocksmith2014.SNG

open Rocksmith2014.Common

type PhraseIteration =
    { PhraseId: int32
      StartTime: float32
      EndTime: float32
      Difficulty: int32 array }

    interface IBinaryWritable with
        member this.Write(writer) =
            writer.WriteInt32 this.PhraseId
            writer.WriteSingle this.StartTime
            writer.WriteSingle this.EndTime
            this.Difficulty |> Array.iter writer.WriteInt32

    static member Read(reader: IBinaryReader) =
        { PhraseId = reader.ReadInt32()
          StartTime = reader.ReadSingle()
          EndTime = reader.ReadSingle()
          Difficulty = Array.init 3 (fun _ -> reader.ReadInt32()) }
