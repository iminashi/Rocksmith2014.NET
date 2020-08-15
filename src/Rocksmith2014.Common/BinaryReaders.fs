module Rocksmith2014.Common.BinaryReaders

open System
open System.IO
open System.Buffers.Binary
open Microsoft.FSharp.NativeInterop
open Interfaces

#nowarn "9"

type LittleEndianBinaryReader(stream: Stream) =
    interface IBinaryReader with
        member _.ReadInt8() = stream.ReadByte() |> int8

        member this.ReadInt16() =
            let length = 2
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            (this :> IBinaryReader).ReadSpan(Span<byte>(buffer, length))
            BinaryPrimitives.ReadInt16LittleEndian(ReadOnlySpan(buffer, length))

        member this.ReadUInt16() =
            let length = 2
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            (this :> IBinaryReader).ReadSpan(Span<byte>(buffer, length))
            BinaryPrimitives.ReadUInt16LittleEndian(ReadOnlySpan(buffer, length))

        member this.ReadUInt24() =
            let length = 4
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            let span = Span<byte>(buffer, length)
            (this :> IBinaryReader).ReadSpan(span.Slice(0,3))
            span.[3] <- 0uy
            BinaryPrimitives.ReadUInt32LittleEndian(ReadOnlySpan(buffer, length))

        member this.ReadInt32() =
            let length = 4
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            (this :> IBinaryReader).ReadSpan(Span<byte>(buffer, length))
            BinaryPrimitives.ReadInt32LittleEndian(ReadOnlySpan(buffer, length))

        member this.ReadUInt32() =
            let length = 4
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            (this :> IBinaryReader).ReadSpan(Span<byte>(buffer, length))
            BinaryPrimitives.ReadUInt32LittleEndian(ReadOnlySpan(buffer, length))
        
        member this.ReadUInt40() =
            let length = 8
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            let span = Span<byte>(buffer, length)
            (this :> IBinaryReader).ReadSpan(span.Slice(0, 5))
            span.[5] <- 0uy; span.[6] <- 0uy; span.[7] <- 0uy
            BinaryPrimitives.ReadUInt64LittleEndian(ReadOnlySpan(buffer, length))

        member this.ReadUInt64() =
            let length = 8
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            (this :> IBinaryReader).ReadSpan(Span<byte>(buffer, length))
            BinaryPrimitives.ReadUInt64LittleEndian(ReadOnlySpan(buffer, length))

        member this.ReadSingle() =
            let length = 4
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            (this :> IBinaryReader).ReadSpan(Span<byte>(buffer, length))
            BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(ReadOnlySpan(buffer, length)))

        member this.ReadDouble() =
            let length = 8
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            (this :> IBinaryReader).ReadSpan(Span<byte>(buffer, length))
            BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64LittleEndian(ReadOnlySpan(buffer, length)))

        member this.ReadBytes(count) =
            let buffer = Array.zeroCreate<byte> count
            (this :> IBinaryReader).ReadSpan(buffer.AsSpan())
            buffer

        member _.ReadSpan(span) =
            let mutable bytesRead = stream.Read span
            let mutable totalRead = bytesRead
            while totalRead < span.Length && bytesRead <> 0 do
                bytesRead <- stream.Read(span.Slice totalRead)
                totalRead <- totalRead + bytesRead

type BigEndianBinaryReader(stream: Stream) =
    interface IBinaryReader with
        member _.ReadInt8() = stream.ReadByte() |> int8

        member this.ReadInt16() =
            let length = 2
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            (this :> IBinaryReader).ReadSpan(Span<byte>(buffer, length))
            BinaryPrimitives.ReadInt16BigEndian(ReadOnlySpan(buffer, length))

        member this.ReadUInt16() =
            let length = 2
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            (this :> IBinaryReader).ReadSpan(Span<byte>(buffer, length))
            BinaryPrimitives.ReadUInt16BigEndian(ReadOnlySpan(buffer, length))

        member this.ReadUInt24() =
            let length = 4
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            let span = Span<byte>(buffer, length)
            (this :> IBinaryReader).ReadSpan(span.Slice(1, 3))
            span.[0] <- 0uy
            BinaryPrimitives.ReadUInt32BigEndian(ReadOnlySpan(buffer, length))

        member this.ReadInt32() =
            let length = 4
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            (this :> IBinaryReader).ReadSpan(Span<byte>(buffer, length))
            BinaryPrimitives.ReadInt32BigEndian(ReadOnlySpan(buffer, length))

        member this.ReadUInt32() =
            let length = 4
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            (this :> IBinaryReader).ReadSpan(Span<byte>(buffer, length))
            BinaryPrimitives.ReadUInt32BigEndian(ReadOnlySpan(buffer, length))

        member this.ReadUInt40() =
             let length = 8
             let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
             let span = Span<byte>(buffer, length)
             (this :> IBinaryReader).ReadSpan(span.Slice(3, 5))
             span.[0] <- 0uy; span.[1] <- 0uy; span.[2] <- 0uy
             BinaryPrimitives.ReadUInt64BigEndian(ReadOnlySpan(buffer, length))

        member this.ReadUInt64() =
             let length = 8
             let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
             (this :> IBinaryReader).ReadSpan(Span<byte>(buffer, length))
             BinaryPrimitives.ReadUInt64BigEndian(ReadOnlySpan(buffer, length))

        member this.ReadSingle() =
            let length = 4
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            (this :> IBinaryReader).ReadSpan(Span<byte>(buffer, length))
            BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32BigEndian(ReadOnlySpan(buffer, length)))

        member this.ReadDouble() =
            let length = 8
            let buffer = NativePtr.stackalloc<byte> length |> NativePtr.toVoidPtr
            (this :> IBinaryReader).ReadSpan(Span<byte>(buffer, length))
            BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64BigEndian(ReadOnlySpan(buffer, length)))

        member this.ReadBytes(count) =
            let buffer = Array.zeroCreate<byte> count
            (this :> IBinaryReader).ReadSpan(buffer.AsSpan())
            buffer

        member _.ReadSpan(span) =
            let mutable bytesRead = stream.Read span
            let mutable totalRead = bytesRead
            while totalRead < span.Length && bytesRead <> 0 do
                bytesRead <- stream.Read(span.Slice totalRead)
                totalRead <- totalRead + bytesRead

/// Returns a binary reader that matches the given platform.
let getReader stream platform =
    match platform with
    | PC | Mac -> LittleEndianBinaryReader(stream) :> IBinaryReader
