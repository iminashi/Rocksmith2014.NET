module Rocksmith2014.Common.BinaryWriters

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
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            BinaryPrimitives.WriteInt16LittleEndian(Span<byte>(buffer, length), value)
            stream.Write(ReadOnlySpan(buffer, length))

        member _.WriteUInt16(value) =
            let length = 2
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            BinaryPrimitives.WriteUInt16LittleEndian(Span<byte>(buffer, length), value)
            stream.Write(ReadOnlySpan(buffer, length))

        member _.WriteUInt24(value) =
            let length = 4
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            BinaryPrimitives.WriteUInt32LittleEndian(Span<byte>(buffer, length), value)
            stream.Write(ReadOnlySpan(buffer, length).Slice(0, 3))

        member _.WriteInt32(value) =
            let length = 4
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            BinaryPrimitives.WriteInt32LittleEndian(Span<byte>(buffer, length), value)
            stream.Write(ReadOnlySpan(buffer, length))

        member _.WriteUInt32(value) =
            let length = 4
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            BinaryPrimitives.WriteUInt32LittleEndian(Span<byte>(buffer, length), value)
            stream.Write(ReadOnlySpan(buffer, length))

        member _.WriteUInt40(value) =
            let length = 8
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            BinaryPrimitives.WriteUInt64LittleEndian(Span<byte>(buffer, length), value)
            stream.Write(ReadOnlySpan(buffer, length).Slice(0, 5))

        member _.WriteUInt64(value) =
            let length = 8
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            BinaryPrimitives.WriteUInt64LittleEndian(Span<byte>(buffer, length), value)
            stream.Write(ReadOnlySpan(buffer, length))

        member _.WriteSingle(value) =
            let length = 4
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            BinaryPrimitives.WriteInt32LittleEndian(Span<byte>(buffer, length), BitConverter.SingleToInt32Bits value)
            stream.Write(ReadOnlySpan(buffer, length))

        member _.WriteDouble(value) =
            let length = 8
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            BinaryPrimitives.WriteInt64LittleEndian(Span<byte>(buffer, length), BitConverter.DoubleToInt64Bits value)
            stream.Write(ReadOnlySpan(buffer, length))

        member _.WriteBytes(value) = stream.Write(ReadOnlySpan(value))

type BigEndianBinaryWriter(stream: Stream) =
    interface IBinaryWriter with
        member _.WriteInt8(value) = stream.WriteByte(value |> byte)

        member _.WriteInt16(value) =
            let length = 2
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            BinaryPrimitives.WriteInt16BigEndian(Span<byte>(buffer, length), value)
            stream.Write(ReadOnlySpan(buffer, length))

        member _.WriteUInt16(value) =
            let length = 2
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            BinaryPrimitives.WriteUInt16BigEndian(Span<byte>(buffer, length), value)
            stream.Write(ReadOnlySpan(buffer, length))

        member _.WriteUInt24(value) =
            let length = 4
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            BinaryPrimitives.WriteUInt32BigEndian(Span<byte>(buffer, length), value)
            stream.Write(ReadOnlySpan(buffer, length).Slice(1, 3))

        member _.WriteInt32(value) =
            let length = 4
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            BinaryPrimitives.WriteInt32BigEndian(Span<byte>(buffer, length), value)
            stream.Write(ReadOnlySpan(buffer, length))

        member _.WriteUInt32(value) =
            let length = 4
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            BinaryPrimitives.WriteUInt32BigEndian(Span<byte>(buffer, length), value)
            stream.Write(ReadOnlySpan(buffer, length))

        member _.WriteUInt40(value) =
            let length = 8
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            BinaryPrimitives.WriteUInt64BigEndian(Span<byte>(buffer, length), value)
            stream.Write(ReadOnlySpan(buffer, length).Slice(3, 5))

        member _.WriteUInt64(value) =
            let length = 8
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            BinaryPrimitives.WriteUInt64BigEndian(Span<byte>(buffer, length), value)
            stream.Write(ReadOnlySpan(buffer, length))

        member _.WriteSingle(value) =
            let length = 4
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            BinaryPrimitives.WriteInt32BigEndian(Span<byte>(buffer, length), BitConverter.SingleToInt32Bits value)
            stream.Write(ReadOnlySpan(buffer, length))

        member _.WriteDouble(value) =
            let length = 8
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            BinaryPrimitives.WriteInt64BigEndian(Span<byte>(buffer, length), BitConverter.DoubleToInt64Bits value)
            stream.Write(ReadOnlySpan(buffer, length))

        member _.WriteBytes(value) = stream.Write(ReadOnlySpan(value))

/// Returns a binary writer that matches the given platform.
let getWriter stream platform =
    match platform with
    | PC | Mac -> LittleEndianBinaryWriter(stream) :> IBinaryWriter
