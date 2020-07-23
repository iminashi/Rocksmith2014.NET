module Rocksmith2014.SNG.Interfaces

type IBinaryReader =
    abstract member ReadInt8 : unit -> int8
    abstract member ReadInt16 : unit -> int16
    abstract member ReadInt32 : unit -> int32
    abstract member ReadUInt32 : unit -> uint32
    abstract member ReadSingle : unit -> float32
    abstract member ReadDouble : unit -> float
    abstract member ReadBytes : int -> byte array

type IBinaryWriter =
    abstract member WriteInt8 : int8 -> unit
    abstract member WriteInt16 : int16 -> unit
    abstract member WriteInt32 : int32 -> unit
    abstract member WriteUInt32 : uint32 -> unit
    abstract member WriteSingle : float32 -> unit
    abstract member WriteDouble : float -> unit
    abstract member WriteBytes : byte array -> unit

type IBinaryWritable =
    abstract member Write : writer:IBinaryWriter -> unit
