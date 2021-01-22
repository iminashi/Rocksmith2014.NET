module Rocksmith2014.SNG.Tests.ReadWritePacked

open Expecto
open Rocksmith2014.Common
open Rocksmith2014.SNG

let testFileLevels = 12

let testRead file platform = async {
    let! sng = SNG.readPackedFile file platform
    Expect.equal sng.Levels.Length testFileLevels (sprintf "Read %i levels" testFileLevels) }

let testWrite source target platform = async {
    let! sng = SNG.readPackedFile source platform
    do! SNG.savePackedFile target platform sng }

[<Tests>]
let readWriteTests =
    testList "Read/Write Packed Files" [
        testAsync "Can read packed PC SNG file" { do! testRead "packed_pc.sng" PC }
        testAsync "Can read packed Mac SNG file" { do! testRead "packed_mac.sng" Mac }
        
        testAsync "Can write packed PC SNG file" {
            do! testWrite "packed_pc.sng" "test_write_packed_pc.sng" PC
            
            do! testRead "test_write_packed_pc.sng" PC }
        
        testAsync "Can write packed Mac SNG file" {
            do! testWrite "packed_mac.sng" "test_write_packed_mac.sng" Mac
            
            do! testRead "test_write_packed_mac.sng" Mac }
    ]
