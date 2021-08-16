module XblockTests

open Expecto
open Rocksmith2014.Common
open Rocksmith2014.DLCProject
open Rocksmith2014.DLCProject.XBlock
open System.IO

let private project = { testProject with Arrangements = [ Vocals testJVocals; Instrumental testLead; Vocals testVocals ] }

let private propertyEqual name value (p: Property) =
    p.Name = name && p.Set.Value = value

let private hasProperty name value (entity: Entity) =
    entity.Properties
    |> Array.exists (propertyEqual name value)

[<Tests>]
let xblockTests =
    testList "XBlock Tests" [
        test "Can be created" {
            let x = create PC project

            Expect.isNonEmpty x.EntitySet "Entity set has been populated" }

        test "Entity set contents is correct" {
            let x = create PC project
            let entitySet = x.EntitySet

            Expect.all entitySet (fun x -> x.ModelName = "RSEnumerable_Song") "Model name is correct"
            Expect.all entitySet (hasProperty "SoundBank" "urn:audio:wwise-sound-bank:song_sometest") "Contains sound bank property with correct URN"
            Expect.all entitySet (hasProperty "PreviewSoundBank" "urn:audio:wwise-sound-bank:song_sometest_preview") "Contains preview sound bank property with correct URN"
            Expect.all entitySet (hasProperty "AlbumArtSmall" "urn:image:dds:album_sometest_64") "Contains album art small property with correct URN"
            Expect.all entitySet (hasProperty "AlbumArtMedium" "urn:image:dds:album_sometest_128") "Contains album art medium property with correct URN"
            Expect.all entitySet (hasProperty "AlbumArtLarge" "urn:image:dds:album_sometest_256") "Contains album art large property with correct URN"
            Expect.all entitySet (hasProperty "ShowLightsXMLAsset" "urn:application:xml:sometest_showlights") "Contains showlights XML asset property with correct URN"
            Expect.all entitySet (hasProperty "Header" "urn:database:hsan-db:songs_dlc_sometest") "Contains header property with correct URN"
            Expect.equal entitySet.[0].Name "SomeTest_JVocals" "JVocals entity name is correct"
            Expect.exists entitySet.[0].Properties (propertyEqual "Manifest" "urn:database:json-db:sometest_jvocals") "JVocals entity manifest property is correct"
            Expect.exists entitySet.[0].Properties (propertyEqual "SngAsset" "urn:application:musicgame-song:sometest_jvocals") "JVocals entity SNG asset property is correct"
            Expect.exists entitySet.[0].Properties (propertyEqual "LyricArt" "urn:image:dds:lyrics_sometest") "JVocals entity lyric art property is correct"
            Expect.equal entitySet.[1].Name "SomeTest_Lead" "Lead entity name is correct"
            Expect.exists entitySet.[1].Properties (propertyEqual "Manifest" "urn:database:json-db:sometest_lead") "Lead entity manifest property is correct"
            Expect.exists entitySet.[1].Properties (propertyEqual "SngAsset" "urn:application:musicgame-song:sometest_lead") "Lead entity SNG asset property is correct"
            Expect.exists entitySet.[1].Properties (propertyEqual "LyricArt" "") "Lead entity lyric art property is empty"
            Expect.equal entitySet.[2].Name "SomeTest_Vocals" "Vocals entity name is correct"
            Expect.exists entitySet.[2].Properties (propertyEqual "Manifest" "urn:database:json-db:sometest_vocals") "Vocals entity manifest property is correct"
            Expect.exists entitySet.[2].Properties (propertyEqual "SngAsset" "urn:application:musicgame-song:sometest_vocals") "Vocals entity SNG asset property is correct"
            Expect.exists entitySet.[2].Properties (propertyEqual "LyricArt" "") "Vocals entity lyric art property is empty" }

        test "Custom audio entity is correct" {
            let testArr = { testLead with CustomAudio = Some { Path = "Test.wem"; Volume = 0. } }
            let project = { project with Arrangements = [ Instrumental testArr ] }
            let x = create PC project
            let entitySet = x.EntitySet

            Expect.all entitySet (hasProperty "SoundBank" "urn:audio:wwise-sound-bank:song_sometest_lead") "Contains sound bank property with correct URN" }

        test "Can be serialized" {
            let set = { Value = "urn:database:hsan-db:songs_dlc_test" }
            let property =
                { Name = "Header"
                  Set = set }
            let entity =
                { Id = "2b689bf502f744d39aced3f4728aa6b0"
                  ModelName = "RSEnumerable_Song"
                  Name = "Test_Bass"
                  Iterations = 0
                  Properties = [| property |] }
            let game = { EntitySet = [| entity |] }

            use stream = new MemoryStream()
            serialize stream game
            stream.Position <- 0L
            let xml = using (new StreamReader(stream)) (fun reader -> reader.ReadToEnd())

            Expect.isNotEmpty xml "XML string is not empty" }

        test "Can be deserialized" {
            use file = File.OpenRead "test.xblock"

            let xblock = deserialize file

            Expect.equal xblock.EntitySet.[0].ModelName "RSEnumerable_Song" "Model name is correct" }
    ]
