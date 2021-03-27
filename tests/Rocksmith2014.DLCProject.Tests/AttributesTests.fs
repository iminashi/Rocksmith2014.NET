module AttributesTests

open Expecto
open System
open Rocksmith2014.SNG
open Rocksmith2014.DLCProject.Manifest.AttributesCreation
open Rocksmith2014.XML
open Rocksmith2014.DLCProject
open Rocksmith2014.Conversion
open Rocksmith2014.Common
open Rocksmith2014.Common.Manifest

let private testArr = InstrumentalArrangement.Load("instrumental.xml")
let private testSng = ConvertInstrumental.xmlToSng testArr

[<Tests>]
let attributeTests =
    testList "Attribute Tests" [
        testCase "Partition is set correctly" <| fun _ ->
            let lead2 = { testLead with MasterID = 12346; PersistentID = Guid.NewGuid() }
        
            let project = { testProject with Arrangements = [ Instrumental testLead; Instrumental lead2 ] }
        
            let attr1 = createAttributes project (FromInstrumental (testLead, testSng))
            let attr2 = createAttributes project (FromInstrumental (lead2, testSng))
        
            Expect.equal attr1.SongPartition (Nullable(1)) "Partition for first lead arrangement is 1"
            Expect.equal attr2.SongPartition (Nullable(2)) "Partition for second lead arrangement is 2"
            Expect.equal attr2.SongAsset "urn:application:musicgame-song:sometest_lead2" "Song asset is correct"
        
        testCase "Chord templates are created" <| fun _ ->
            let emptyNameId = testSng.Chords |> Array.findIndex (fun c -> String.IsNullOrEmpty c.Name)
        
            let attr = createAttributes testProject (FromInstrumental (testLead, testSng))
        
            Expect.isNonEmpty attr.ChordTemplates "Chord templates array is not empty"
            Expect.isFalse (attr.ChordTemplates |> Array.exists (fun (c: Manifest.ChordTemplate) -> c.ChordId = int16 emptyNameId)) "Chord template with empty name is removed"
        
        testCase "Sections are created" <| fun _ ->
            let attr = createAttributes testProject (FromInstrumental (testLead, testSng))
        
            Expect.equal attr.Sections.Length testSng.Sections.Length "Section count is same"
            Expect.equal attr.Sections.[0].UIName "$[34298] Riff [1]" "UI name is correct"
        
        testCase "Phrases are created" <| fun _ ->
            let attr = createAttributes testProject (FromInstrumental (testLead, testSng))
        
            Expect.equal attr.Phrases.Length testSng.Phrases.Length "Phrase count is same"
        
        testCase "Chords are created" <| fun _ ->
            let attr = createAttributes testProject (FromInstrumental (testLead, testSng))
        
            Expect.isNonEmpty attr.Chords "Chords map is not empty"
        
        testCase "Techniques are created" <| fun _ ->
            let attr = createAttributes testProject (FromInstrumental (testLead, testSng))
        
            Expect.isNonEmpty attr.Techniques "Technique map is not empty"
        
        testCase "Arrangement properties are set" <| fun _ ->
            let attr = createAttributes testProject (FromInstrumental (testLead, testSng))
        
            match attr.ArrangementProperties with
            | Some ap ->
                Expect.equal ap.represent 1uy "Represent is set"
                Expect.equal ap.standardTuning 1uy "Standard tuning is set"
                Expect.equal ap.openChords 1uy "Open chords is set"
                Expect.equal ap.unpitchedSlides 1uy "Unpitched slides is set"
                Expect.equal ap.doubleStops 1uy "Double stops is set"
                Expect.equal ap.tremolo 0uy "Tremolo is not set"
                Expect.equal ap.pathLead 1uy "Path lead is set"
                Expect.equal ap.pathRhythm 0uy "Path rhythm is not set"
                Expect.equal ap.pathBass 0uy "Path bass is not set"
            | None -> failwith "Arrangement properties do not exist"

        testCase "Represent arrangement property is not set for alternative arrangement" <| fun _ ->
            let testLead = { testLead with Priority = ArrangementPriority.Alternative }
            let project = { testProject with Arrangements = [ Instrumental testLead ] }

            let attr = createAttributes project (FromInstrumental (testLead, testSng))
        
            match attr.ArrangementProperties with
            | Some ap ->
                Expect.equal ap.represent 0uy "Represent property is not set"
                Expect.equal ap.bonusArr 0uy "Bonus arrangement property is not set"
            | None -> failwith "Arrangement properties do not exist"

        testCase "Bonus arrangement property is set for bonus arrangement" <| fun _ ->
            let testLead = { testLead with Priority = ArrangementPriority.Bonus }
            let project = { testProject with Arrangements = [ Instrumental testLead ] }

            let attr = createAttributes project (FromInstrumental (testLead, testSng))
        
            match attr.ArrangementProperties with
            | Some ap ->
                Expect.equal ap.represent 0uy "Represent property is not set"
                Expect.equal ap.bonusArr 1uy "Bonus arrangement property is set"
            | None -> failwith "Arrangement properties do not exist"
        
        testCase "DNA riffs time is calculated" <| fun _ ->
            let attr = createAttributes testProject (FromInstrumental (testLead, testSng))
        
            Expect.isGreaterThan attr.DNA_Riffs.Value 0. "DNA riffs is greater than zero"
        
        testCase "Tone names are correct" <| fun _ ->
            let attr = createAttributes testProject (FromInstrumental (testLead, testSng))
        
            Expect.equal attr.Tone_Base "Base_Tone" "Base tone name is correct"
            Expect.equal attr.Tone_A "Tone_1" "Tone A name is correct"
            Expect.equal attr.Tone_B "Tone_2" "Tone B name is correct"
            Expect.equal attr.Tone_C "Tone_3" "Tone C name is correct"
        
        testCase "URN attributes are correct" <| fun _ ->
            let attr = createAttributes testProject (FromInstrumental (testLead, testSng))
        
            Expect.equal attr.AlbumArt "urn:image:dds:album_sometest" "AlbumArt is correct"
            Expect.equal attr.ManifestUrn "urn:database:json-db:sometest_lead" "ManifestUrn is correct"
            Expect.equal attr.BlockAsset "urn:emergent-world:sometest" "BlockAsset is correct"
            Expect.equal attr.ShowlightsXML "urn:application:xml:sometest_showlights" "ShowlightsXML is correct"
            Expect.equal attr.SongAsset "urn:application:musicgame-song:sometest_lead" "SongAsset is correct"
            Expect.equal attr.SongXml "urn:application:xml:sometest_lead" "SongXml is correct"
        
        testCase "Various attributes are correct (Common)" <| fun _ ->
            let attr = createAttributes testProject (FromInstrumental (testLead, testSng))
        
            Expect.equal attr.ArrangementSort (Nullable(0)) "ArrangementSort is 0"
            Expect.equal attr.RelativeDifficulty (Nullable(0)) "RelativeDifficulty is 0"
            Expect.equal attr.DLCKey "SomeTest" "DLCKey is correct"
            Expect.equal attr.SongKey "SomeTest" "SongKey is correct"
            Expect.equal attr.SKU "RS2" "SKU is correct"
            Expect.isTrue attr.Shipping "Shipping is true"
        
        testCase "Various attributes are correct (Instrumental)" <| fun _ ->
            let attr = createAttributes testProject (FromInstrumental (testLead, testSng))
        
            Expect.equal attr.MasterID_RDV 12345 "MasterID is correct"
            Expect.equal attr.FullName "SomeTest_Lead" "FullName is correct"
            Expect.equal attr.PreviewBankPath "song_sometest_preview.bnk" "PreviewBankPath is correct"
            Expect.equal attr.SongBank "song_sometest.bnk" "SongBank is correct"
            Expect.equal attr.SongEvent "Play_SomeTest" "SongEvent is correct"

        testCase "Various attributes are correct (Instrumental with custom audio)" <| fun _ ->
            let testArr = { testLead with CustomAudio = Some { Path = "Test.wem"; Volume = 0. } }
            let testProject = { testProject with Arrangements = [ Instrumental testArr ] }
            let attr = createAttributes testProject (FromInstrumental (testArr, testSng))
        
            Expect.equal attr.PreviewBankPath "song_sometest_preview.bnk" "PreviewBankPath is correct"
            Expect.equal attr.SongBank "song_sometest_lead.bnk" "SongBank is correct"
            Expect.equal attr.SongEvent "Play_SomeTest_lead" "SongEvent is correct"
        
        testCase "Various attributes are correct (Vocals)" <| fun _ ->
            let project = { testProject with Arrangements = [ Vocals testVocals ] }
        
            let attr = createAttributes project (FromVocals testVocals)
        
            Expect.equal attr.MasterID_RDV 54321 "MasterID is correct"
            Expect.equal attr.FullName "SomeTest_Vocals" "FullName is correct"
            Expect.equal attr.SongEvent "Play_SomeTest" "SongEvent is correct"
            Expect.equal attr.InputEvent "Play_Tone_Standard_Mic" "InputEvent is correct"
            Expect.equal attr.JapaneseVocal (Nullable()) "JapaneseVocal is null"
        
        testCase "Various attributes are correct (Japanese Vocals)" <| fun _ ->
            let project = { testProject with Arrangements = [ Vocals testJVocals ] }
        
            let attr = createAttributes project (FromVocals testJVocals)
        
            Expect.equal attr.ArrangementName "Vocals" "ArrangementName is correct"
            Expect.equal attr.FullName "SomeTest_JVocals" "FullName is correct"
            Expect.equal attr.JapaneseVocal (Nullable(true)) "JapaneseVocal is true"
        
        testCase "Maximum scroll speed is correct" <| fun _ ->
            let attr = createAttributes testProject (FromInstrumental (testLead, testSng))
            let dvd = attr.DynamicVisualDensity.[(testArr.Levels.Count - 1)..]
        
            Expect.allEqual dvd (float32 testLead.ScrollSpeed) "Maximum scroll speed is correct"

        testCase "CapoFret, Tuning and CentOffset are correct" <| fun _ ->
            let tuning = Tuning.FromArray([| -1s; -2s; -3s; -4s; -5s; -6s |])
            let project = { testProject with Arrangements = [ Instrumental testLeadCapo ] }

            let attr = createAttributes project (FromInstrumental (testLeadCapo, testSng))
        
            Expect.equal attr.CapoFret (Nullable(5.)) "Capo fret is correct"
            Expect.equal attr.Tuning (Some(tuning)) "Tuning is correct"
            Expect.equal attr.CentOffset (Nullable(50.)) "Cent offset is correct"
    ]
