module PackageBuildingTests

open Expecto
open System
open System.IO
open Rocksmith2014.Common
open Rocksmith2014.Common.Manifest
open Rocksmith2014.DLCProject
open Rocksmith2014.DLCProject.PackageBuilder
open Rocksmith2014.DD
open Rocksmith2014.PSARC
open Rocksmith2014.SNG

let buildConfig =
    { Platforms = [ PC; Mac ]
      BuilderVersion = "1.0"
      Author = "Author"
      AppId = AppId 123456UL
      GenerateDD = true
      DDConfig = { PhraseSearchThreshold = Some 80; LevelCountGeneration = LevelCountGeneration.Simple }
      ApplyImprovements = true
      SaveDebugFiles = false
      AudioConversionTask = async { return () }
      IdResetConfig = None
      ProgressReporter = None }

let projectPath =
    Path.Combine(AppContext.BaseDirectory, "project", "Integration_Test.rs2dlc")

let buildDir =
    Path.Combine(AppContext.BaseDirectory, "build")

let psarcPath =
    Path.Combine(buildDir, "test_v1")

let psarcPathWin =
    Path.Combine(buildDir, "test_v1_p.psarc")

let psarcPathMac =
    Path.Combine(buildDir, "test_v1_m.psarc")

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
        testTask "Packages can be built for PC and Mac platforms" {
            if Directory.Exists(buildDir) then Directory.Delete(buildDir, true)
            Directory.CreateDirectory(buildDir) |> ignore

            let! project = DLCProject.load projectPath
            do! buildPackages (WithoutPlatformOrExtension psarcPath) buildConfig project |> Async.StartAsTask

            Expect.isTrue (File.Exists(psarcPathWin)) "PC package was built"
            Expect.isTrue (File.Exists(psarcPathMac)) "Mac package was built"
        }

        testAsync "PC package contains correct files" {
            use psarc = PSARC.ReadFile(psarcPathWin)

            let contents = psarc.Manifest

            Expect.hasCountOf contents 2u (fileSelector "audio/windows" ".wem") "PSARC contains two audio files"
            Expect.hasCountOf contents 2u (fileSelector "audio/windows" ".bnk") "PSARC contains two soundbank files"
            Expect.hasCountOf contents 4u (fileSelector "songs/bin/generic" ".sng") "PSARC contains four SNG files"
            testCommonContents contents
        }

        testAsync "Mac package contains correct files" {
            use psarc = PSARC.ReadFile(psarcPathMac)

            let contents = psarc.Manifest

            Expect.hasCountOf contents 2u (fileSelector "audio/mac" ".wem") "PSARC contains two audio files"
            Expect.hasCountOf contents 2u (fileSelector "audio/mac" ".bnk") "PSARC contains two soundbank files"
            Expect.hasCountOf contents 4u (fileSelector "songs/bin/macos" ".sng") "PSARC contains four SNG files"
            testCommonContents contents
        }

        testTask "App ID file contains correct app ID" {
            use psarc = PSARC.ReadFile(psarcPathWin)

            use! stream = psarc.GetEntryStream("appid.appid")
            let appid = using (new StreamReader(stream)) (fun r -> r.ReadToEnd() |> AppId.ofString)

            Expect.equal appid.Value buildConfig.AppId "App ID was the one defined in the build configuration"
        }

        testTask "Mac package contains correct SNG file" {
            use psarc = PSARC.ReadFile(psarcPathMac)

            use! stream = psarc.GetEntryStream("songs/bin/macos/integrationtest_lead.sng")
            let! sng = SNG.fromStream stream Mac |> Async.StartAsTask

            Expect.exists sng.Sections (fun s -> s.Name = "melody") "SNG contains melody section"
            Expect.isGreaterThan sng.Levels.Length 1 "SNG contains DD levels"
        }

        testTask "Mac package contains correct manifest file" {
            use psarc = PSARC.ReadFile(psarcPathMac)

            use! stream = psarc.GetEntryStream "manifests/songs_dlc_integrationtest/integrationtest_bass.json"
            let! mani = (Manifest.fromJsonStream stream).AsTask()
            let attr = Manifest.getSingletonAttributes mani

            Expect.exists attr.Tones (fun t -> t.Key = "bass") "Attributes contain a tone with key bass"
        }

        testTask "Mac package contains correct soundbank file" {
            use psarc = PSARC.ReadFile(psarcPathMac)

            use mem = new MemoryStream()
            do! psarc.InflateFile("audio/mac/song_integrationtest_preview.bnk", mem)
            let volume = SoundBank.readVolume mem Mac
            let id = SoundBank.readFileId mem Mac

            match volume with
            | Ok vol -> Expect.equal vol 0.3f "Volume is correct"
            | Error e -> failwith e

            match id with
            | Ok id -> Expect.exists psarc.Manifest (String.contains (string id)) $"PSARC contains audio file with correct ID"
            | Error e -> failwith e
        }

        testTask "PC package contains correct lead tone" {
            use psarc = PSARC.ReadFile(psarcPathWin)

            use! file = psarc.GetEntryStream("manifests/songs_dlc_integrationtest/integrationtest_lead.json")
            let! manifest = Manifest.fromJsonStream(file).AsTask()
            let attributes = Manifest.getSingletonAttributes manifest

            Expect.equal attributes.Tones.[0].Key "guitar" "Tone key is correct"
            Expect.equal attributes.Tones.[0].ToneDescriptors.[0] "$[35751]MULTI-EFFECT" "First tone descriptor is correct"
        }
    ]
