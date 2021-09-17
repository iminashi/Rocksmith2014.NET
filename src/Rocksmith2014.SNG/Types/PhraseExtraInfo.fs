namespace Rocksmith2014.SNG

open Rocksmith2014.Common

/// Leftover from RS1, not used in RS2014.
type PhraseExtraInfo =
    { PhraseId: int32
      Difficulty: int32
      Empty: int32
      LevelJump: int8
      Redundant: int16 }
    // 1 byte padding

    interface IBinaryWritable with
        member this.Write(writer) =
            writer.WriteInt32 this.PhraseId
            writer.WriteInt32 this.Difficulty
            writer.WriteInt32 this.Empty
            writer.WriteInt8 this.LevelJump
            writer.WriteInt16 this.Redundant
            // Write a single byte of padding
            writer.WriteInt8 0y

    static member Read(reader: IBinaryReader) =
        let info =
            { PhraseId = reader.ReadInt32()
              Difficulty = reader.ReadInt32()
              Empty = reader.ReadInt32()
              LevelJump = reader.ReadInt8()
              Redundant = reader.ReadInt16() }
        // Read a single byte of padding
        reader.ReadInt8() |> ignore
        info
