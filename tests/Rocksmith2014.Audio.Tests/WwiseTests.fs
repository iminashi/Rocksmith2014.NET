module WwiseTests

open Expecto
open Rocksmith2014.Audio
open System
open System.IO

let actualCliPath =
    // Skip the conversion tests if running in CI
    if Environment.GetEnvironmentVariable("CI") <> "true" then
        try
            Some <| Wwise.getCLIPath ()
        with _ ->
            None
    else
        None

let testConversion testFile = async {
    let wemPath = Path.ChangeExtension(testFile, "wem")
    if File.Exists(wemPath) then File.Delete(wemPath)

    do! Wwise.convertToWem actualCliPath testFile
    let info = FileInfo(wemPath)

    Expect.isTrue info.Exists "Wem file was created"
    Expect.isGreaterThan info.Length 100_000L "File is larger than 100KB" }

[<Tests>]
let wwiseTests =
    testList "Wwise Conversion Tests" [
        if actualCliPath.IsSome then
            testAsync "Wave file can be converted" { do! testConversion TestFiles.WaveFile }
            testAsync "Vorbis file can be converted" { do! testConversion TestFiles.VorbisFile }
    ]

[<Tests>]
let wwiseDetectionTests =
    testList "Wwise Detection Tests" [
        test "Detection prioritizes WWISEROOT environment variable (Windows)" {
            // Create a dummy directory and set it to the environment variable
            let baseDir = AppDomain.CurrentDomain.BaseDirectory
            let dummyWwiseDir = (Directory.CreateDirectory(Path.Combine(baseDir, "Wwise Test 2019"))).FullName
            let expectedPath = Path.Combine(dummyWwiseDir, "Authoring", "x64", "Release", "bin", "WwiseConsole.exe")
            Environment.SetEnvironmentVariable("WWISEROOT", dummyWwiseDir)

            let cliPath = WwiseFinder.findWindows ()

            Expect.equal cliPath expectedPath "WWISEROOT environment variable was used"
        }
    ]
