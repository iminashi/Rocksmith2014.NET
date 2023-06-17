module BigEndianBinaryReadWrite

open Expecto
open FsCheck
open Rocksmith2014.Common
open Rocksmith2014.Common.BinaryReaders
open Rocksmith2014.Common.BinaryWriters
open System.IO
open Generators

let writer (s: Stream) = BigEndianBinaryWriter(s) :> IBinaryWriter
let reader (s: Stream) = BigEndianBinaryReader(s) :> IBinaryReader

[<Tests>]
let bigEndianTests =
    testList "Big-Endian Binary Writer/Reader" [
        testProp "Int16" <| fun v ->
            use mem = new MemoryStream()
            writer(mem).WriteInt16(v)

            mem.Position <- 0L
            let res = reader(mem).ReadInt16()

            Expect.equal res v "Same value"

        testProp "UInt16" <| fun (DoNotSize v) ->
            use mem = new MemoryStream()
            writer(mem).WriteUInt16(v)

            mem.Position <- 0L
            let res = reader(mem).ReadUInt16()

            Expect.equal res v "Same value"

        testProp "UInt24" <| fun (Custom.UInt24 v) ->
            use mem = new MemoryStream()
            writer(mem).WriteUInt24(v)

            mem.Position <- 0L
            let res = reader(mem).ReadUInt24()

            Expect.equal res v "Same value"

        testProp "Int32" <| fun v ->
            use mem = new MemoryStream()
            writer(mem).WriteInt32(v)

            mem.Position <- 0L
            let res = reader(mem).ReadInt32()

            Expect.equal res v "Same value"

        testProp "UInt32" <| fun (DoNotSize v) ->
            use mem = new MemoryStream()
            writer(mem).WriteUInt32(v)

            mem.Position <- 0L
            let res = reader(mem).ReadUInt32()

            Expect.equal res v "Same value"

        testProp "UInt40" <| fun (Custom.UInt40 v) ->
            use mem = new MemoryStream()
            writer(mem).WriteUInt40(v)

            mem.Position <- 0L
            let res = reader(mem).ReadUInt40()

            Expect.equal res v "Same value"

        testProp "UInt64" <| fun (DoNotSize v) ->
            use mem = new MemoryStream()
            writer(mem).WriteUInt64(v)

            mem.Position <- 0L
            let res = reader(mem).ReadUInt64()

            Expect.equal res v "Same value"

        testProp "Single" <| fun v ->
            use mem = new MemoryStream()
            writer(mem).WriteSingle(v)

            mem.Position <- 0L
            let res = reader(mem).ReadSingle()

            Expect.equal res v "Same value"

        testProp "Double" <| fun v ->
            use mem = new MemoryStream()
            writer(mem).WriteDouble(v)

            mem.Position <- 0L
            let res = reader(mem).ReadDouble()

            Expect.equal res v "Same value"
    ]
