module ReadTests

open Expecto
open System.IO
open Rocksmith2014.PSARC

let rec cleanDirectory (path: string) =
    Directory.EnumerateFiles path |> Seq.iter File.Delete
    let subDirs = Directory.EnumerateDirectories path
    subDirs |> Seq.iter cleanDirectory
    subDirs |> Seq.iter Directory.Delete

[<Tests>]
let someTests =
  testList "Read and Extract Files" [

    testCase "Can read PSARC with encrypted TOC" <| fun _ ->
        use file = File.OpenRead("test_p.psarc")
        use psarc = PSARC.Read file
        Expect.equal psarc.Manifest.[0] "gfxassets/album_art/album_testtest_64.dds" "First file name is correct"

    testAsync "Can extract all files from PSARC" {
        use file = File.OpenRead("test_p.psarc")
        use psarc = PSARC.Read file
        let tempPath = Path.Combine(Path.GetTempPath(), "extractTest")
        Directory.CreateDirectory(tempPath) |> ignore
        
        do! psarc.ExtractFiles tempPath

        let fileCount = Directory.EnumerateFiles(tempPath, "*.*", SearchOption.AllDirectories) |> Seq.length
        Expect.equal fileCount psarc.TOC.Count "All files were extracted"
        cleanDirectory tempPath }
  ]
