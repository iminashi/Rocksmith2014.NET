module ReadTests

open Expecto
open Rocksmith2014.PSARC
open System.IO

let getTempPath subDir =
    let tempPath = Path.Combine(Path.GetTempPath(), subDir)
    if Directory.Exists(tempPath) then Directory.Delete(tempPath, true)
    Directory.CreateDirectory(tempPath).FullName

[<Tests>]
let readTests =
    testList "Read and Extract Files" [
        testCase "Can read PSARC with encrypted TOC" <| fun _ ->
            use file = File.OpenRead("test_p.psarc")
            use psarc = PSARC.Read(file)
            Expect.equal psarc.Manifest.[0] "gfxassets/album_art/album_testtest_64.dds" "First file name is correct"
        
        testTask "Can extract all files from PSARC" {
            use file = File.OpenRead("test_p.psarc")
            use psarc = PSARC.Read(file)
            let tempPath = getTempPath "extractTest"
            
            do! psarc.ExtractFiles(tempPath)
        
            let fileCount = Directory.EnumerateFiles(tempPath, "*.*", SearchOption.AllDirectories) |> Seq.length
            Expect.equal fileCount psarc.TOC.Count "All files were extracted"
            Directory.Delete(tempPath, true)
        }

        testTask "Can extract partially compressed file" {
            // The test archive contains a single file where only the first block is zlib compressed
            use psarc = PSARC.OpenFile("partially_compressed_test_p.psarc")
            let tempPath = getTempPath "partiallyCompressedTest"

            do! psarc.ExtractFiles(tempPath)

            let fileCount = Directory.EnumerateFiles(tempPath, "*.*", SearchOption.AllDirectories) |> Seq.length
            Expect.equal fileCount psarc.TOC.Count "One file was extracted"
            Directory.Delete(tempPath, true)
        }
    ]
