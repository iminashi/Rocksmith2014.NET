module Rocksmith2014.SNG.Interfaces

open System.IO

type IBinaryWritable =
    abstract member Write : writer:BinaryWriter -> unit
