namespace Rocksmith2014.SNG

open Rocksmith2014.Common
open BinaryHelpers

type Phrase =
    { Solo: int8
      Disparity: int8
      Ignore: int8
      // 1 byte padding
      MaxDifficulty: int32
      IterationCount: int32
      Name: string }

    interface IBinaryWritable with
        member this.Write(writer) =
            writer.WriteInt8 this.Solo
            writer.WriteInt8 this.Disparity
            writer.WriteInt8 this.Ignore
            // Write a single byte of padding
            writer.WriteInt8 0y
            writer.WriteInt32 this.MaxDifficulty
            writer.WriteInt32 this.IterationCount
            writeZeroTerminatedUTF8String 32 this.Name writer

    static member Read(reader: IBinaryReader) =
        // Read a single byte of padding
        let readPadding () = reader.ReadInt8() |> ignore

        { Solo = reader.ReadInt8()
          Disparity = reader.ReadInt8()
          Ignore = reader.ReadInt8() 
          MaxDifficulty = (readPadding(); reader.ReadInt32())
          IterationCount = reader.ReadInt32()
          Name = readZeroTerminatedUTF8String 32 reader }
