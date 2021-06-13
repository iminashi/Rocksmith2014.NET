module BinaryHelpersTests

open Expecto
open Rocksmith2014.SNG
open Rocksmith2014.Common.BinaryWriters
open System.IO

[<Tests>]
let tests =
    testList "Binary Helpers Tests" [
        testCase "UTF8 string always includes null terminator" <| fun _ ->
            let length = 48
            let input = String.replicate length "A"
            use mem = new MemoryStream()
            let writer = LittleEndianBinaryWriter(mem)

            BinaryHelpers.writeZeroTerminatedUTF8String length input writer
            let bytes = mem.ToArray()

            Expect.equal bytes.[length - 1] 0uy "Last byte is zero"
    ]
