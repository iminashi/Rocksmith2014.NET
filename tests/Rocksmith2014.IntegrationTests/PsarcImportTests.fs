module PsarcImportTests

open Expecto
open Rocksmith2014.DLCProject
open System.IO
open System

let [<Literal>] PCImportDir = "./psarc/pc"
let [<Literal>] MacImportDir = "./psarc/mac"

let expectedArrangements = [ "bass"; "lead"; "vocals"; "jvocals"; "showlights" ]
let expectedTones = [ "bass"; "guitar" ]

let testProject project path =
    Expect.isTrue (File.Exists path) "Project file exists"
    Expect.equal project.DLCKey "IntegrationTest" "DLC key is correct"
    Expect.equal project.ArtistName.Value "Integration" "Artist name is correct"
    Expect.equal project.Title.Value "The Test" "Title is correct"
    Expect.equal project.Version "5.4" "Version is correct"
    Expect.equal project.AudioFile.Volume -0.2 "Audio volume is correct"
    Expect.equal project.AudioPreviewFile.Volume 0.3 "Preview audio volume is correct"

    Expect.hasLength project.Tones expectedTones.Length "Project has two tones"
    Expect.all project.Tones (fun t -> expectedTones |> List.contains t.Key) "Tone keys are correct"

    Expect.hasLength project.Arrangements expectedArrangements.Length "Project has five arrangements"
    project.Arrangements
    |> List.iter (function
        | Instrumental ({ RouteMask = RouteMask.Bass } as bass) ->
            Expect.isTrue bass.BassPicked "Bass is picked"
            Expect.equal bass.PersistentID (Guid.Parse("5a1eca1a5a234bf29df3eb61adf20746")) "Bass persistent ID is correct"
        | Instrumental ({ RouteMask = RouteMask.Lead } as lead) ->
            Expect.equal lead.BaseTone "guitar" "Lead base tone is correct"
            Expect.equal lead.MasterID 1670570628 "Lead master ID is correct"
        | Vocals ({ Japanese = true } as jvocals) ->
            Expect.stringEnds jvocals.CustomFont.Value "lyrics.dds" "J-Vocals has custom font"
        | _ ->
            ())

let testFiles importPath =
    expectedArrangements
    |> List.iter (fun arr ->
        let file = $"arr_{arr}.xml"
        Expect.isTrue (File.Exists (Path.Combine(importPath, file))) $"Arrangement file {file} exists")

    Expect.isTrue (File.Exists (Path.Combine(importPath, "integrationtest.wem"))) "Main audio file exists"
    Expect.isTrue (File.Exists (Path.Combine(importPath, "integrationtest_preview.wem"))) "Preview audio file exists"
    Expect.isTrue (File.Exists (Path.Combine(importPath, "lyrics.dds"))) "Custom lyrics art file exists"
    Expect.isTrue (File.Exists (Path.Combine(importPath, "lyrics.glyphs.xml"))) "Custom lyrics glyphs file exists"
    Expect.isTrue (File.Exists (Path.Combine(importPath, "cover.dds"))) "Cover art file exists"

[<Tests>]
let pcTests =
    testSequenced <| testList "PC PSARC Import Tests" [
        testAsync "PSARC can be imported" {
            if Directory.Exists PCImportDir then Directory.Delete(PCImportDir, true)
            Directory.CreateDirectory(PCImportDir) |> ignore

            let! project, path = PsarcImporter.import ignore "./psarc/test_p.psarc" PCImportDir

            Expect.equal project.JapaneseArtistName (Some "アーティスト") "Japanese artist name is correct"
            Expect.equal project.JapaneseTitle (Some "曲名") "Japanese title is correct"
            testProject project path }

        testCase "Project files were created" <| fun _ ->
            testFiles PCImportDir
    ]

[<Tests>]
let macTests =
    testSequenced <| testList "Mac PSARC Import Tests" [
        testAsync "PSARC can be imported" {
            if Directory.Exists MacImportDir then Directory.Delete(MacImportDir, true)
            Directory.CreateDirectory(MacImportDir) |> ignore

            let! project, path = PsarcImporter.import ignore "./psarc/test_m.psarc" MacImportDir

            Expect.isNone project.JapaneseArtistName "Japanese artist name is not set"
            Expect.isNone project.JapaneseTitle "Japanese title is not set"
            testProject project path }

        testCase "Project files were created" <| fun _ ->
            testFiles MacImportDir
    ]
