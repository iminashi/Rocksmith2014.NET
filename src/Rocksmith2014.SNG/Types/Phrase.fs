namespace Rocksmith2014.SNG

open Interfaces
open System.IO
open BinaryHelpers

type Phrase =
    { Solo : int8
      Disparity : int8 
      Ignore : int8
      // 1 byte padding
      MaxDifficulty : int32
      PhraseIterationLinks : int32
      Name : string }

    interface IBinaryWritable with
        member this.Write(writer) =
            writer.Write this.Solo
            writer.Write this.Disparity
            writer.Write this.Ignore
            // Write a single byte of padding
            writer.Write 0y
            writer.Write this.MaxDifficulty
            writer.Write this.PhraseIterationLinks
            writeZeroTerminatedUTF8String 32 this.Name writer

    static member Read(reader : BinaryReader) =
        // Read a single byte of padding
        let readPadding() = reader.ReadSByte() |> ignore

        { Solo = reader.ReadSByte()
          Disparity = reader.ReadSByte()
          Ignore = reader.ReadSByte() 
          MaxDifficulty = (readPadding(); reader.ReadInt32())
          PhraseIterationLinks = reader.ReadInt32()
          Name = readZeroTerminatedUTF8String 32 reader }
