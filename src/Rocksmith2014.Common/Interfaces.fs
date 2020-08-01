module Rocksmith2014.Common.Interfaces

open System

type IBinaryReader =
    abstract member ReadInt8 : unit -> int8
    abstract member ReadInt16 : unit -> int16
    abstract member ReadUInt16 : unit -> uint16
    abstract member ReadUInt24 : unit -> uint32
    abstract member ReadInt32 : unit -> int32
    abstract member ReadUInt32 : unit -> uint32
    abstract member ReadUInt40 : unit -> uint64
    abstract member ReadUInt64 : unit -> uint64
    abstract member ReadSingle : unit -> float32
    abstract member ReadDouble : unit -> float
    abstract member ReadBytes : int -> byte array
    abstract member ReadSpan : Span<byte> -> unit

type IBinaryWriter =
    abstract member WriteInt8 : int8 -> unit
    abstract member WriteInt16 : int16 -> unit
    abstract member WriteUInt16 : uint16 -> unit
    abstract member WriteUInt24 : uint32 -> unit
    abstract member WriteInt32 : int32 -> unit
    abstract member WriteUInt32 : uint32 -> unit
    abstract member WriteUInt64 : uint64 -> unit
    abstract member WriteSingle : float32 -> unit
    abstract member WriteDouble : float -> unit
    abstract member WriteBytes : byte array -> unit

type IBinaryWritable =
    abstract member Write : writer:IBinaryWriter -> unit
