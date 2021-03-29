module PackageBuildingTests

open Expecto
open Rocksmith2014.Common
open Rocksmith2014.DLCProject
open Rocksmith2014.DLCProject.PackageBuilder
open Rocksmith2014.DD
open System.IO
open Rocksmith2014.PSARC

let buildConfig =
    { Platforms = [ PC; Mac ]
      Author = "Author"
      AppId = "123456"
      GenerateDD = true
      DDConfig = { PhraseSearch = WithThreshold 80 }
      ApplyImprovements = true
      SaveDebugFiles = false
      AudioConversionTask = async { return () } }

let [<Literal>] BuildDir = "./project/build"
let psarcPathWin = $"{BuildDir}/test_v1_p.psarc"
let psarcPathMac = $"{BuildDir}/test_v1_m.psarc"

let fileSelector pathPart extension filename =
    String.contains pathPart filename && String.endsWith extension filename

let testCommonContents contents =
    let expect filename filetype =
        Expect.contains contents filename $"PSARC contains {filetype} file"

    Expect.hasCountOf contents 3u (fileSelector "gfxassets/album_art" ".dds") "PSARC contains three album art files"
    Expect.hasCountOf contents 4u (fileSelector "manifests" ".json") "PSARC contains four JSON manifests"
    expect "appid.appid" "app ID"
    expect "integrationtest_aggregategraph.nt" "aggregate graph"
    expect "flatmodels/rs/rsenumerable_root.flat" "flatmodel root"
    expect "flatmodels/rs/rsenumerable_song.flat" "flatmodel song"
    expect "gamexblocks/nsongs/integrationtest.xblock" "x-block"
    expect "manifests/songs_dlc_integrationtest/songs_dlc_integrationtest.hsan" "manifest header"
    expect "songs/arr/integrationtest_showlights.xml" "showlights"
    expect "assets/ui/lyrics/integrationtest/lyrics_integrationtest.dds" "custom font"

[<Tests>]
let tests =
    testSequenced <| testList "Package Building Tests" [
        testAsync "Packages can be built for PC and Mac platforms" {
            if Directory.Exists BuildDir then Directory.Delete(BuildDir, true)
            Directory.CreateDirectory(BuildDir) |> ignore

            let! project = DLCProject.load "./project/Integration_test.rs2dlc"
            do! PackageBuilder.buildPackages $"{BuildDir}/test_v1" buildConfig project

            Expect.isTrue (File.Exists psarcPathWin) "PC package was built"
            Expect.isTrue (File.Exists psarcPathMac) "Mac package was built" }

        testAsync "PC package contains correct files" {
            use psarc = PSARC.ReadFile psarcPathWin

            let contents = psarc.Manifest

            Expect.hasCountOf contents 2u (fileSelector "audio/windows" ".wem") "PSARC contains two audio files"
            Expect.hasCountOf contents 2u (fileSelector "audio/windows" ".bnk") "PSARC contains two sound bank files"
            Expect.hasCountOf contents 4u (fileSelector "songs/bin/generic" ".sng") "PSARC contains four SNG files"
            testCommonContents contents }

        testAsync "Mac package contains correct files" {
            use psarc = PSARC.ReadFile psarcPathMac

            let contents = psarc.Manifest

            Expect.hasCountOf contents 2u (fileSelector "audio/mac" ".wem") "PSARC contains two audio files"
            Expect.hasCountOf contents 2u (fileSelector "audio/mac" ".bnk") "PSARC contains two sound bank files"
            Expect.hasCountOf contents 4u (fileSelector "songs/bin/macos" ".sng") "PSARC contains four SNG files"
            testCommonContents contents }

        testAsync "App ID file contains correct app ID" {
            use psarc = PSARC.ReadFile psarcPathWin

            use mem = new MemoryStream()
            do! psarc.InflateFile("appid.appid", mem)
            let appid = using (new StreamReader(mem)) (fun r -> r.ReadToEnd())

            Expect.equal appid buildConfig.AppId "App ID was the one defined in the build configuration" }
    ]
