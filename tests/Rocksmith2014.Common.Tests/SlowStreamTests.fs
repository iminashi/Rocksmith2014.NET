// fsharplint:disable MemberNames
module SlowStreamTests

open Expecto
open Rocksmith2014.Common
open Rocksmith2014.Common.BinaryReaders
open System
open System.IO

/// A stream that returns data one byte at a time
type SlowStream() =
    inherit Stream()

    let data = Array.init<byte> 40 byte
    let mutable index = 0

    override _.Read(buffer: Span<byte>) =
        if index < data.Length then
            buffer.[0] <- data.[index]
            index <- index + 1
            1
        else
            0

    override _.Read(buffer, offset, _count) =
        if index < data.Length then
            buffer.[offset] <- data.[index]
            index <- index + 1
            1
        else
            0

    override _.Write(_buffer, _offset, _count) = ()
    override _.get_CanRead() = true
    override _.get_CanWrite() = false
    override _.get_CanSeek() = false
    override _.get_Length() = data.LongLength
    override _.get_Position() = int64 index
    override _.set_Position(pos) = index <- int pos
    override _.SetLength(_length) = ()
    override _.Flush() = ()
    override _.Seek(_offset, _origin) = 0L

[<Tests>]
let bigEndianSlowStream =
    testList "Big-Endian Binary Reader with Slow Stream" [
        test "Can read bytes" {
            use stream = new SlowStream()
            let reader = BigEndianBinaryReader(stream) :> IBinaryReader
            let expected = seq { 0uy; 1uy; 2uy; 3uy; 4uy }

            let array = reader.ReadBytes(5)

            Expect.sequenceContainsOrder array expected "Sequence of 5 bytes is correct"
        }

        test "Can read signed 16-bit integer" {
            use stream = new SlowStream()
            let reader = BigEndianBinaryReader(stream) :> IBinaryReader
            let expected = BitConverter.ToInt16([| 1uy; 0uy |], 0)

            let read = reader.ReadInt16()

            Expect.equal read expected "Signed 16-bit integer read correctly"
        }

        test "Can read unsigned 24-bit integer" {
            use stream = new SlowStream()
            let reader = BigEndianBinaryReader(stream) :> IBinaryReader
            let expected = BitConverter.ToUInt32([| 2uy; 1uy; 0uy; 0uy |], 0)

            let read = reader.ReadUInt24()

            Expect.equal read expected "Unsigned 24-bit integer read correctly"
        }

        test "Can read unsigned 64-bit integer" {
            use stream = new SlowStream()
            let reader = BigEndianBinaryReader(stream) :> IBinaryReader
            let expected = BitConverter.ToUInt64([| 7uy; 6uy; 5uy; 4uy; 3uy; 2uy; 1uy; 0uy |], 0)

            let read = reader.ReadUInt64()

            Expect.equal read expected "Unsigned 64-bit integer read correctly"
        }
    ]

[<Tests>]
let littleEndianSlowStream =
    testList "Little-Endian Binary Reader with Slow Stream" [
        test "Can read bytes" {
            use stream = new SlowStream()
            let reader = LittleEndianBinaryReader(stream) :> IBinaryReader
            let expected = seq { 0uy; 1uy; 2uy; 3uy; 4uy }

            let array = reader.ReadBytes(5)

            Expect.sequenceContainsOrder array expected "Sequence of 5 bytes is correct"
        }

        test "Can read signed 16-bit integer" {
            use stream = new SlowStream()
            let reader = LittleEndianBinaryReader(stream) :> IBinaryReader
            let expected = BitConverter.ToInt16([| 0uy; 1uy |], 0)

            let read = reader.ReadInt16()

            Expect.equal read expected "Signed 16-bit integer read correctly"
        }

        test "Can read unsigned 24-bit integer" {
            use stream = new SlowStream()
            let reader = LittleEndianBinaryReader(stream) :> IBinaryReader
            let expected = BitConverter.ToUInt32([| 0uy; 1uy; 2uy; 0uy |], 0)

            let read = reader.ReadUInt24()

            Expect.equal read expected "Unsigned 24-bit integer read correctly"
        }

        test "Can read unsigned 64-bit integer" {
            use stream = new SlowStream()
            let reader = LittleEndianBinaryReader(stream) :> IBinaryReader
            let expected = BitConverter.ToUInt64([| 0uy; 1uy; 2uy; 3uy; 4uy; 5uy; 6uy; 7uy |], 0)

            let read = reader.ReadUInt64()

            Expect.equal read expected "Unsigned 64-bit integer read correctly"
        }
    ]
