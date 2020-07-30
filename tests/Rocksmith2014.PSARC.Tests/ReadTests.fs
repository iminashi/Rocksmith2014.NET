module ReadTests

open Expecto
open System.IO
open Rocksmith2014.PSARC

[<Tests>]
let someTests =
  testList "Read and Extract Files" [

    testCase "Can read PSARC with encrypted TOC" <| fun _ ->
        use file = File.OpenRead("test_p.psarc")
        use psarc = PSARC.read file
        Expect.equal psarc.TOC.[0].Name "gfxassets/album_art/album_testtest_64.dds" "First file name is correct"

    testCase "Can extract all files from PSARC" <| fun _ ->
        use file = File.OpenRead("test_p.psarc")
        use psarc = PSARC.read file
        let tempPath = Path.Combine(Path.GetTempPath(), "extractTest")
        Directory.CreateDirectory(tempPath) |> ignore
        
        PSARC.extractFiles tempPath psarc

        let fileCount = Directory.EnumerateFiles(tempPath, "*.*", SearchOption.AllDirectories) |> Seq.length
        Expect.equal fileCount psarc.TOC.Count "All files were extracted"
  ]
