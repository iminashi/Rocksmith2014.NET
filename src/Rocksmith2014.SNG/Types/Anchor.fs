namespace Rocksmith2014.SNG

open Interfaces

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
            writer.WriteSingle this.StartTime
            writer.WriteSingle this.EndTime
            writer.WriteSingle this.FirstNoteTime
            writer.WriteSingle this.LastNoteTime
            writer.WriteInt8 this.FretId
            // Write three bytes of padding
            writer.WriteInt16 0s; writer.WriteInt8 0y
            writer.WriteInt32 this.Width
            writer.WriteInt32 this.PhraseIterationId

    static member Read(reader : IBinaryReader) =
        // Read three bytes of padding
        let readPadding () =
            reader.ReadInt16() |> ignore; reader.ReadInt8() |> ignore

        { StartTime = reader.ReadSingle()
          EndTime = reader.ReadSingle()
          FirstNoteTime = reader.ReadSingle()
          LastNoteTime = reader.ReadSingle()
          FretId = reader.ReadInt8()
          Width = (readPadding(); reader.ReadInt32())
          PhraseIterationId = reader.ReadInt32() }
