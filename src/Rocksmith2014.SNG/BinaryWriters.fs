module Rocksmith2014.SNG.BinaryWriters

open System
open System.IO
open System.Buffers.Binary
open Microsoft.FSharp.NativeInterop
open Interfaces

#nowarn "9"

type LittleEndianBinaryWriter(stream: Stream) =
    interface IBinaryWriter with
        member _.WriteInt8(value) = stream.WriteByte(value |> byte)

        member _.WriteInt16(value) =
            let length = 2
            let buffer = NativePtr.stackalloc<byte>(length) |> NativePtr.toVoidPtr
            let bufferSpan = Span<byte>(buffer, length)
            BinaryPrimitives.WriteInt16LittleEndian(bufferSpan, value)
            stream.Write(ReadOnlySpan(buffer, length))

        member _.WriteInt32(value) =
            let length = 4
            let buffer = NativePtr.stackalloc<byte>(length) |> NativePtr.toVoidPtr
            let bufferSpan = Span<byte>(buffer, length)
            BinaryPrimitives.WriteInt32LittleEndian(bufferSpan, value)
            stream.Write(ReadOnlySpan(buffer, length))

        member _.WriteUInt32(value) =
            let length = 4
            let buffer = NativePtr.stackalloc<byte>(length) |> NativePtr.toVoidPtr
            let bufferSpan = Span<byte>(buffer, length)
            BinaryPrimitives.WriteUInt32LittleEndian(bufferSpan, value)
            stream.Write(ReadOnlySpan(buffer, length))

        member _.WriteSingle(value) =
            let length = 4
            let buffer = NativePtr.stackalloc<byte>(length) |> NativePtr.toVoidPtr
            let bufferSpan = Span<byte>(buffer, length)
            BinaryPrimitives.WriteInt32LittleEndian(bufferSpan, BitConverter.SingleToInt32Bits(value))
            stream.Write(ReadOnlySpan(buffer, length))

        member _.WriteDouble(value) =
            let length = 8
            let buffer = NativePtr.stackalloc<byte>(length) |> NativePtr.toVoidPtr
            let bufferSpan = Span<byte>(buffer, length)
            BinaryPrimitives.WriteInt64LittleEndian(bufferSpan, BitConverter.DoubleToInt64Bits(value))
            stream.Write(ReadOnlySpan(buffer, length))

        member _.WriteBytes(value) =
            stream.Write(ReadOnlySpan(value))

type BigEndianBinaryWriter(stream: Stream) =
    interface IBinaryWriter with
        member _.WriteInt8(value) = stream.WriteByte(value |> byte)

        member _.WriteInt16(value) =
            let length = 2
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            let bufferSpan = Span<byte>(buffer, length)
            BinaryPrimitives.WriteInt16BigEndian(bufferSpan, value)
            stream.Write(ReadOnlySpan(buffer, length))

        member _.WriteInt32(value) =
            let length = 4
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            let bufferSpan = Span<byte>(buffer, length)
            BinaryPrimitives.WriteInt32BigEndian(bufferSpan, value)
            stream.Write(ReadOnlySpan(buffer, length))

        member _.WriteUInt32(value) =
            let length = 4
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            let bufferSpan = Span<byte>(buffer, length)
            BinaryPrimitives.WriteUInt32BigEndian(bufferSpan, value)
            stream.Write(ReadOnlySpan(buffer, length))

        member _.WriteSingle(value) =
            let length = 4
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            let bufferSpan = Span<byte>(buffer, length)
            BinaryPrimitives.WriteInt32BigEndian(bufferSpan, BitConverter.SingleToInt32Bits value)
            stream.Write(ReadOnlySpan(buffer, length))

        member _.WriteDouble(value) =
            let length = 8
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            let bufferSpan = Span<byte>(buffer, length)
            BinaryPrimitives.WriteInt64BigEndian(bufferSpan, BitConverter.DoubleToInt64Bits value)
            stream.Write(ReadOnlySpan(buffer, length))

        member _.WriteBytes(value) =
            stream.Write(ReadOnlySpan(value))

let getWriter stream platform =
    match platform with
    | PC | Mac -> LittleEndianBinaryWriter(stream) :> IBinaryWriter