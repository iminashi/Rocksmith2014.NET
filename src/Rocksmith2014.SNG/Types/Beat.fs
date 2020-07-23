namespace Rocksmith2014.SNG

open Interfaces
open System.IO

type Beat =
    { Time : float32
      Measure : int16
      Beat : int16
      PhraseIteration : int32
      Mask : BeatMask }
    
    interface IBinaryWritable with
        member this.Write(writer) =
            writer.Write this.Time
            writer.Write this.Measure
            writer.Write this.Beat
            writer.Write this.PhraseIteration
            writer.Write (LanguagePrimitives.EnumToValue this.Mask)
    
    static member Read(reader : BinaryReader) =
        { Time = reader.ReadSingle()
          Measure = reader.ReadInt16()
          Beat = reader.ReadInt16()
          PhraseIteration = reader.ReadInt32()
          Mask = LanguagePrimitives.EnumOfValue(reader.ReadInt32()) }
