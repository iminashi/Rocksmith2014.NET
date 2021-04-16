namespace Rocksmith2014.SNG

open Rocksmith2014.Common

type Beat =
    { Time : float32
      Measure : int16
      Beat : int16
      PhraseIteration : int32
      Mask : BeatMask }

    interface IBinaryWritable with
        member this.Write(writer) =
            writer.WriteSingle this.Time
            writer.WriteInt16 this.Measure
            writer.WriteInt16 this.Beat
            writer.WriteInt32 this.PhraseIteration
            writer.WriteInt32 (LanguagePrimitives.EnumToValue this.Mask)

    static member Read(reader: IBinaryReader) =
        { Time = reader.ReadSingle()
          Measure = reader.ReadInt16()
          Beat = reader.ReadInt16()
          PhraseIteration = reader.ReadInt32()
          Mask = LanguagePrimitives.EnumOfValue(reader.ReadInt32()) }
