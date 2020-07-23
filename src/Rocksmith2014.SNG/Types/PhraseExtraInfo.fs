namespace Rocksmith2014.SNG

open Interfaces
open System.IO

/// Leftover from RS1, not used in RS2014.
type PhraseExtraInfo =
    { PhraseId : int32
      Difficulty : int32
      Empty : int32
      LevelJump : int8
      Redundant : int16 }
      // 1 byte padding

    interface IBinaryWritable with
        member this.Write(writer) =
            writer.Write this.PhraseId
            writer.Write this.Difficulty
            writer.Write this.Empty
            writer.Write this.LevelJump
            writer.Write this.Redundant
            // Write a single byte of padding
            writer.Write 0y

    static member Read(reader : BinaryReader) =
        let info =
            { PhraseId = reader.ReadInt32()
              Difficulty = reader.ReadInt32()
              Empty = reader.ReadInt32()
              LevelJump = reader.ReadSByte()
              Redundant = reader.ReadInt16() }
        // Read a single byte of padding
        reader.ReadSByte() |> ignore
        info