namespace Rocksmith2014.SNG

open Rocksmith2014.Common.Interfaces

[<Struct>]
type DNA =
    { Time : float32
      DnaId : int32 }

    interface IBinaryWritable with
        member this.Write(writer) =
            writer.WriteSingle this.Time
            writer.WriteInt32 this.DnaId

    static member Read(reader : IBinaryReader) =
        { Time = reader.ReadSingle()
          DnaId = reader.ReadInt32() }

module DNA =
    [<Literal>]
    let None = 0

    [<Literal>]
    let Solo = 1

    [<Literal>]
    let Riff = 2

    [<Literal>]
    let Chord = 3
