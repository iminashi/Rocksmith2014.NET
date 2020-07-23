namespace Rocksmith2014.SNG

open Interfaces
open System.IO

type Anchor =
    { StartTime : float32
      EndTime : float32
      FirstNoteTime : float32
      LastNoteTime : float32
      FretId : int8
      // 3 bytes padding
      Width : int32
      PhraseIterationId : int32 }

    interface IBinaryWritable with
        member this.Write(writer) =
            writer.Write this.StartTime
            writer.Write this.EndTime
            writer.Write this.FirstNoteTime
            writer.Write this.LastNoteTime
            writer.Write this.FretId
            // Write three bytes of padding
            writer.Write (0s); writer.Write (0y)
            writer.Write this.Width
            writer.Write this.PhraseIterationId

    static member Read(reader : BinaryReader) =
        // Read three bytes of padding
        let readPadding () =
            reader.ReadInt16() |> ignore; reader.ReadSByte() |> ignore

        { StartTime = reader.ReadSingle()
          EndTime = reader.ReadSingle()
          FirstNoteTime = reader.ReadSingle()
          LastNoteTime = reader.ReadSingle()
          FretId = reader.ReadSByte()
          Width = (readPadding(); reader.ReadInt32())
          PhraseIterationId = reader.ReadInt32() }
