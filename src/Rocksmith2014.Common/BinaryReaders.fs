module Rocksmith2014.Common.BinaryReaders

open System
open System.IO
open System.Buffers.Binary
open Microsoft.FSharp.NativeInterop
open Interfaces
open System.Buffers

#nowarn "9"

// TODO: Error handling

type LittleEndianBinaryReader(stream: Stream) =
    interface IBinaryReader with
        member _.ReadInt8() = stream.ReadByte() |> int8

        member _.ReadInt16() =
            let length = 2
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            let bytesRead = stream.Read(Span<byte>(buffer, length))
            BinaryPrimitives.ReadInt16LittleEndian(ReadOnlySpan(buffer, length))

        member _.ReadUInt16() =
            let length = 2
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            let bytesRead = stream.Read(Span<byte>(buffer, length))
            BinaryPrimitives.ReadUInt16LittleEndian(ReadOnlySpan(buffer, length))

        member _.ReadUInt24() =
            let length = 4
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            let bytesRead = stream.Read(Span<byte>(buffer, 3))
            BinaryPrimitives.ReadUInt32LittleEndian(ReadOnlySpan(buffer, length))

        member _.ReadInt32() =
            let length = 4
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            let bytesRead = stream.Read(Span<byte>(buffer, length))
            BinaryPrimitives.ReadInt32LittleEndian(ReadOnlySpan(buffer, length))

        member _.ReadUInt32() =
            let length = 4
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            let bytesRead = stream.Read(Span<byte>(buffer, length))
            BinaryPrimitives.ReadUInt32LittleEndian(ReadOnlySpan(buffer, length))
        
        member _.ReadUInt40() =
            let length = 8
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            let bytesRead = stream.Read(Span<byte>(buffer, 5))
            BinaryPrimitives.ReadUInt64LittleEndian(ReadOnlySpan(buffer, length))

        member _.ReadUInt64() =
            let length = 8
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            let bytesRead = stream.Read(Span<byte>(buffer, length))
            BinaryPrimitives.ReadUInt64LittleEndian(ReadOnlySpan(buffer, length))

        member _.ReadSingle() =
            let length = 4
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            let bytesRead = stream.Read(Span<byte>(buffer, length))
            BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(ReadOnlySpan(buffer, length)))

        member _.ReadDouble() =
            let length = 8
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            let bytesRead = stream.Read(Span<byte>(buffer, length))
            BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64LittleEndian(ReadOnlySpan(buffer, length)))

        member _.ReadBytes(count) =
            let buffer = Array.zeroCreate<byte> count
            let bytesRead = stream.Read(buffer.AsSpan())
            buffer

        member _.ReadSpan(span) =
            stream.Read(span) |> ignore

type BigEndianBinaryReader(stream: Stream) =
    interface IBinaryReader with
        member _.ReadInt8() = stream.ReadByte() |> int8

        member _.ReadInt16() =
            let length = 2
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            let bytesRead = stream.Read(Span<byte>(buffer, length))
            BinaryPrimitives.ReadInt16BigEndian(ReadOnlySpan(buffer, length))

        member _.ReadUInt16() =
            let length = 2
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            let bytesRead = stream.Read(Span<byte>(buffer, length))
            BinaryPrimitives.ReadUInt16BigEndian(ReadOnlySpan(buffer, length))

        member _.ReadUInt24() =
            let length = 4
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            let span = Span<byte>(buffer, length)
            let bytesRead = stream.Read(span.Slice(1, 3))
            BinaryPrimitives.ReadUInt32BigEndian(ReadOnlySpan(buffer, length))

        member _.ReadInt32() =
            let length = 4
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            let bytesRead = stream.Read(Span<byte>(buffer, length))
            BinaryPrimitives.ReadInt32BigEndian(ReadOnlySpan(buffer, length))

        member _.ReadUInt32() =
            let length = 4
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            let bytesRead = stream.Read(Span<byte>(buffer, length))
            BinaryPrimitives.ReadUInt32BigEndian(ReadOnlySpan(buffer, length))

        member _.ReadUInt40() =
             let length = 8
             let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
             let span = Span<byte>(buffer, length)
             let bytesRead = stream.Read(span.Slice(3, 5))
             BinaryPrimitives.ReadUInt64BigEndian(ReadOnlySpan(buffer, length))

        member _.ReadUInt64() =
             let length = 8
             let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
             let bytesRead = stream.Read(Span<byte>(buffer, length))
             BinaryPrimitives.ReadUInt64BigEndian(ReadOnlySpan(buffer, length))

        member _.ReadSingle() =
            let length = 4
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            let bytesRead = stream.Read(Span<byte>(buffer, length))
            BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32BigEndian(ReadOnlySpan(buffer, length)))

        member _.ReadDouble() =
            let length = 8
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            let bytesRead = stream.Read(Span<byte>(buffer, length))
            BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64BigEndian(ReadOnlySpan(buffer, length)))

        member _.ReadBytes(count) =
            let buffer = Array.zeroCreate<byte> count
            let bytesRead = stream.Read(buffer.AsSpan())
            buffer

        member _.ReadSpan(span) =
            stream.Read(span) |> ignore

/// Returns a binary reader that matches the given platform.
let getReader stream platform =
    match platform with
    | PC | Mac -> LittleEndianBinaryReader(stream) :> IBinaryReader
