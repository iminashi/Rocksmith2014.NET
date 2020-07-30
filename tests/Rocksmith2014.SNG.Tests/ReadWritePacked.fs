module Rocksmith2014.SNG.Tests.ReadWritePacked

open Expecto
open Rocksmith2014.Common
open Rocksmith2014.SNG

let testFileLevels = 12

let testRead file platform =
    let sng = SNGFile.readPacked file platform
    Expect.equal sng.Levels.Length testFileLevels (sprintf "Read %i levels" testFileLevels)

let testWrite source target platform =
    SNGFile.readPacked source platform
    |> SNGFile.savePacked target platform

[<Tests>]
let readWriteTests =
  testList "Read/Write Packed Files" [

    testCase "Can read packed PC SNG file" <| fun _ -> testRead "packed_pc.sng" PC
    testCase "Can read packed Mac SNG file" <| fun _ -> testRead "packed_mac.sng" Mac

    testCase "Can write packed PC SNG file" <| fun _ ->
      testWrite "packed_pc.sng" "test_write_packed_pc.sng" PC
      
      testRead "test_write_packed_pc.sng" PC

    testCase "Can write packed Mac SNG file" <| fun _ ->
      testWrite "packed_mac.sng" "test_write_packed_mac.sng" Mac

      testRead "test_write_packed_mac.sng" Mac
  ]
