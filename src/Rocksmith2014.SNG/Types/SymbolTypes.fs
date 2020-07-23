namespace Rocksmith2014.SNG

open Interfaces
open System.IO
open BinaryHelpers

type SymbolsHeader = 
    { ID : int32
      Unk2 : int32 // Always zero
      Unk3 : int32 // Always zero
      Unk4 : int32 // Always zero
      Unk5 : int32 // Always zero
      Unk6 : int32 // Always zero
      Unk7 : int32 // Always zero
      Unk8 : int32 } // Always 2
    
    interface IBinaryWritable with
        member this.Write(writer) =
            writer.Write this.ID
            writer.Write this.Unk2
            writer.Write this.Unk3
            writer.Write this.Unk4
            writer.Write this.Unk5
            writer.Write this.Unk6
            writer.Write this.Unk7
            writer.Write this.Unk8
    
    static member Read(reader : BinaryReader) =
        { ID = reader.ReadInt32()
          Unk2 = reader.ReadInt32()
          Unk3 = reader.ReadInt32()
          Unk4 = reader.ReadInt32()
          Unk5 = reader.ReadInt32()
          Unk6 = reader.ReadInt32()
          Unk7 = reader.ReadInt32()
          Unk8 = reader.ReadInt32() }

    static member Default = { ID = 0; Unk2 = 0; Unk3 = 0; Unk4 = 0; Unk5 = 0; Unk6 = 0; Unk7 = 0; Unk8 = 2 }

type SymbolsTexture =
    { Font : string
      FontPathLength : int32 
      // Unknown value (int32): always zero
      Width : int32
      Height : int32 }

    interface IBinaryWritable with
        member this.Write(writer) =
            writeZeroTerminatedUTF8String 128 this.Font writer
            writer.Write this.FontPathLength
            // Write zero for unknown value
            writer.Write 0
            writer.Write this.Width
            writer.Write this.Height

    static member Read(reader : BinaryReader) =
        { Font = readZeroTerminatedUTF8String 128 reader
          FontPathLength = reader.ReadInt32()
          //Read unknown value before width
          Width = (reader.ReadInt32() |> ignore; reader.ReadInt32())
          Height = reader.ReadInt32() }

[<Struct>]
type Rect =
    { yMin : float32
      xMin : float32
      yMax : float32
      xMax : float32 }

    interface IBinaryWritable with
        member this.Write(writer) =
            writer.Write this.yMin
            writer.Write this.xMin
            writer.Write this.yMax
            writer.Write this.xMax

    static member Read(reader : BinaryReader) =
        { yMin = reader.ReadSingle()
          xMin = reader.ReadSingle()
          yMax = reader.ReadSingle()
          xMax = reader.ReadSingle() }

type SymbolDefinition =
    { Symbol : string
      Outer : Rect
      Inner : Rect }

    interface IBinaryWritable with
        member this.Write(writer) =
            writeZeroTerminatedUTF8String 12 this.Symbol writer
            (this.Outer :> IBinaryWritable).Write writer
            (this.Inner :> IBinaryWritable).Write writer

    static member Read(reader : BinaryReader) =
        { Symbol = readZeroTerminatedUTF8String 12 reader
          Outer = Rect.Read reader
          Inner = Rect.Read reader }
