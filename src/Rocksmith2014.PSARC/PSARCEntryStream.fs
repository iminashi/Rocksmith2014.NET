// fsharplint:disable MemberNames
namespace Rocksmith2014.PSARC

open System.IO
open System
open Rocksmith2014.Common
open ICSharpCode.SharpZipLib.Zip.Compression
open System.Buffers

/// In-memory read stream for a PSARC entry.
type PSARCEntryStream(source: Stream, entry: Entry, blockSize: int, blockSizeTable: uint32 array) =
    inherit Stream()

    let length = int entry.Length
    let buffer = ArrayPool<byte>.Shared.Rent length
    let mutable zIndex = int entry.ZIndexBegin
    let mutable position = 0

    let fillBuffer () =
        let mutable bufferPosition = 0

        while bufferPosition < length do
            match int blockSizeTable.[zIndex] with
            | 0 ->
                // Raw, full cluster used
                source.Read(buffer, bufferPosition, blockSize) |> ignore
                bufferPosition <- bufferPosition + blockSize
            | size ->
                let zipBuffer = Array.zeroCreate<byte> size
                source.Read(zipBuffer, 0, size) |> ignore

                // Check for zlib header
                if zipBuffer.[0] = 0x78uy && zipBuffer.[1] = 0xDAuy then
                    try
                        let inf = Inflater()
                        inf.SetInput zipBuffer
                        let inflatedSize = inf.Inflate(buffer, bufferPosition, buffer.Length - bufferPosition)
                        bufferPosition <- bufferPosition + inflatedSize
                    with :? AggregateException as ex when ex.InnerException.Message.StartsWith("Unknown block") ->
                        // Assume it is uncompressed data, needed for unpacking audio.psarc
                        Span(zipBuffer).CopyTo(Span(buffer, bufferPosition, buffer.Length - bufferPosition))
                        bufferPosition <- bufferPosition + size
                else
                    Span(zipBuffer).CopyTo(Span(buffer, bufferPosition, buffer.Length - bufferPosition))
                    bufferPosition <- bufferPosition + size

            zIndex <- zIndex + 1

    do
        source.Position <- int64 entry.Offset
        fillBuffer()

    override _.Read(target: Span<byte>) =
        if position = length then
            0
        else
            let bytesToRead = min (length - position) target.Length
            Span(buffer, position, bytesToRead).CopyTo(target)
            position <- position + bytesToRead
            bytesToRead

    override this.Read(target, offset, count) =
        this.Read(Span(target, offset, count))

    override _.ReadByte() =
        if position = length then
            -1
        else
            let byteRead = int buffer.[position]
            position <- position + 1
            byteRead

    override _.Write(_buffer, _offset, _count) = ()
    override _.get_CanRead() = true
    override _.get_CanWrite() = false
    override _.get_CanSeek() = true
    override _.get_Length() = int64 length
    override _.get_Position() = int64 position
    override _.set_Position(newPosition) = position <- int newPosition
    override _.SetLength(_length) = ()
    override _.Flush() = ()
    override _.Seek(offset, origin) =
        match origin with
        | SeekOrigin.Begin ->
            position <- min (int offset) length
        | SeekOrigin.Current ->
            position <- min (position + int offset) length
        | SeekOrigin.End ->
            position <- min (length + int offset) length
        | _ ->
            failwith "Invalid seek origin."
        int64 position

    override _.Dispose(disposing) =
        if disposing then ArrayPool.Shared.Return buffer
        base.Dispose(disposing)
