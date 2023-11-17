module AttributesTests

open Expecto
open Rocksmith2014.Common
open Rocksmith2014.Common.Manifest
open Rocksmith2014.Conversion
open Rocksmith2014.DLCProject
open Rocksmith2014.DLCProject.ArrangementPropertiesOverride
open Rocksmith2014.DLCProject.Manifest.AttributesCreation
open Rocksmith2014.SNG
open System

let private testArr = Rocksmith2014.XML.InstrumentalArrangement.Load("instrumental.xml")
let private testSng = ConvertInstrumental.xmlToSng testArr

let testProps : (ArrPropFlags * (ArrangementProperties -> byte)) list  =
    [
        ArrPropFlags.BarreChords, fun p -> p.barreChords
        ArrPropFlags.Bends, fun p -> p.bends
        ArrPropFlags.DoubleStops, fun p -> p.doubleStops
        ArrPropFlags.DropDPower, fun p -> p.dropDPower
        ArrPropFlags.FifthsAndOctaves, fun p -> p.fifthsAndOctaves
        ArrPropFlags.FingerPicking, fun p -> p.fingerPicking
        ArrPropFlags.Harmonics, fun p -> p.harmonics
        ArrPropFlags.PinchHarmonics, fun p -> p.pinchHarmonics
        ArrPropFlags.SlapPop, fun p -> p.slapPop
        ArrPropFlags.Sustain, fun p -> p.sustain
        ArrPropFlags.Tapping, fun p -> p.tapping
        ArrPropFlags.TwoFingerPicking, fun p -> p.twoFingerPicking
        ArrPropFlags.PalmMutes, fun p -> p.palmMutes
        ArrPropFlags.FretHandMutes, fun p -> p.fretHandMutes
        ArrPropFlags.Hopo, fun p -> p.hopo
        ArrPropFlags.NonStandardChords, fun p -> p.nonStandardChords
        ArrPropFlags.OpenChords, fun p -> p.openChords
        ArrPropFlags.PowerChords, fun p -> p.powerChords
        ArrPropFlags.Slides, fun p -> p.slides
        ArrPropFlags.UnpitchedSlides, fun p -> p.unpitchedSlides
        ArrPropFlags.Syncopation, fun p -> p.syncopation
        ArrPropFlags.Tremolo, fun p -> p.tremolo
        ArrPropFlags.Vibrato, fun p -> p.vibrato
    ]

[<Tests>]
let attributeTests =
    testList "Attribute Tests" [
        testCase "Partition is set correctly" <| fun _ ->
            let lead2 = { testLead with MasterID = 12346; PersistentID = Guid.NewGuid() }
            let project = { testProject with Arrangements = [ Instrumental testLead; Instrumental lead2 ] }

            let attr1 = createAttributes project (FromInstrumental(testLead, testSng))
            let attr2 = createAttributes project (FromInstrumental(lead2, testSng))

            Expect.equal attr1.SongPartition (Nullable(1)) "Partition for first lead arrangement is 1"
            Expect.equal attr2.SongPartition (Nullable(2)) "Partition for second lead arrangement is 2"
            Expect.equal attr2.SongAsset "urn:application:musicgame-song:sometest_lead2" "Song asset is correct"

        testCase "Chord templates are created" <| fun _ ->
            let emptyNameId = testSng.Chords |> Array.findIndex (fun c -> String.IsNullOrEmpty c.Name)

            let attr = createAttributes testProject (FromInstrumental(testLead, testSng))

            Expect.isNonEmpty attr.ChordTemplates "Chord templates array is not empty"
            Expect.isFalse (attr.ChordTemplates |> Array.exists (fun (c: ChordTemplate) -> c.ChordId = int16 emptyNameId)) "Chord template with empty name is removed"

        testCase "Sections are created" <| fun _ ->
            let attr = createAttributes testProject (FromInstrumental(testLead, testSng))

            Expect.equal attr.Sections.Length testSng.Sections.Length "Section count is same"
            Expect.equal attr.Sections.[0].UIName "$[34298] Riff [1]" "UI name is correct"

        testCase "Phrases are created" <| fun _ ->
            let attr = createAttributes testProject (FromInstrumental(testLead, testSng))

            Expect.equal attr.Phrases.Length testSng.Phrases.Length "Phrase count is same"

        testCase "Phrase iterations are created" <| fun _ ->
            let attr = createAttributes testProject (FromInstrumental(testLead, testSng))

            Expect.equal attr.PhraseIterations.Length testSng.PhraseIterations.Length "Phrase iteration count is same"

        testCase "Chords are created" <| fun _ ->
            let attr = createAttributes testProject (FromInstrumental(testLead, testSng))

            Expect.isNonEmpty attr.Chords "Chords map is not empty"

        testCase "Techniques are created" <| fun _ ->
            let attr = createAttributes testProject (FromInstrumental(testLead, testSng))

            Expect.isNonEmpty attr.Techniques "Technique map is not empty"

        testCase "Arrangement properties are set correctly" <| fun _ ->
            let attr = createAttributes testProject (FromInstrumental(testLead, testSng))

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

            let attr = createAttributes project (FromInstrumental(testLead, testSng))

            match attr.ArrangementProperties with
            | Some ap ->
                Expect.equal ap.represent 0uy "Represent property is not set"
                Expect.equal ap.bonusArr 0uy "Bonus arrangement property is not set"
            | None -> failwith "Arrangement properties do not exist"

        testCase "Bonus arrangement property is set for bonus arrangement" <| fun _ ->
            let testLead = { testLead with Priority = ArrangementPriority.Bonus }
            let project = { testProject with Arrangements = [ Instrumental testLead ] }

            let attr = createAttributes project (FromInstrumental(testLead, testSng))

            match attr.ArrangementProperties with
            | Some ap ->
                Expect.equal ap.represent 0uy "Represent property is not set"
                Expect.equal ap.bonusArr 1uy "Bonus arrangement property is set"
            | None -> failwith "Arrangement properties do not exist"

        testCase "DNA times are calculated" <| fun _ ->
            let attr = createAttributes testProject (FromInstrumental(testLead, testSng))

            Expect.isGreaterThan attr.DNA_Riffs.Value 0. "DNA riffs is greater than zero"
            Expect.isGreaterThan attr.DNA_Chords.Value 0. "DNA chords is greater than zero"
            Expect.isGreaterThan attr.DNA_Solo.Value 0. "DNA solo is greater than zero"

        testCase "Tones are set correctly" <| fun _ ->
            let attr = createAttributes testProject (FromInstrumental(testLead, testSng))

            Expect.equal attr.Tone_Base "Base_Tone" "Base tone name is correct"
            Expect.equal attr.Tone_A "Tone_1" "Tone A name is correct"
            Expect.equal attr.Tone_B "Tone_2" "Tone B name is correct"
            Expect.equal attr.Tone_C "Tone_3" "Tone C name is correct"
            Expect.equal attr.Tone_D "Tone_4" "Tone D name is correct"
            Expect.equal attr.Tone_Multiplayer "" "Tone Multiplayer name is an empty string"
            Expect.hasLength attr.Tones 2 "Tones array contains two tones"

        testCase "URN attributes are set correctly" <| fun _ ->
            let attr = createAttributes testProject (FromInstrumental(testLead, testSng))

            Expect.equal attr.AlbumArt "urn:image:dds:album_sometest" "AlbumArt is correct"
            Expect.equal attr.ManifestUrn "urn:database:json-db:sometest_lead" "ManifestUrn is correct"
            Expect.equal attr.BlockAsset "urn:emergent-world:sometest" "BlockAsset is correct"
            Expect.equal attr.ShowlightsXML "urn:application:xml:sometest_showlights" "ShowlightsXML is correct"
            Expect.equal attr.SongAsset "urn:application:musicgame-song:sometest_lead" "SongAsset is correct"
            Expect.equal attr.SongXml "urn:application:xml:sometest_lead" "SongXml is correct"

        testCase "Various attributes are set correctly (Common)" <| fun _ ->
            let attr = createAttributes testProject (FromInstrumental(testLead, testSng))

            Expect.equal attr.MasterID_PS3 (Nullable(-1)) "MasterID_PS3 is correct"
            Expect.equal attr.MasterID_XBox360 (Nullable(-1)) "MasterID_XBox360 is correct"
            Expect.isNull attr.JapaneseArtistName "Japanese artist name is null"
            Expect.isNull attr.JapaneseSongName "Japanese song name is null"
            Expect.equal attr.ArrangementSort (Nullable(0)) "ArrangementSort is 0"
            Expect.equal attr.RelativeDifficulty (Nullable(0)) "RelativeDifficulty is 0"
            Expect.equal attr.DLCKey "SomeTest" "DLCKey is correct"
            Expect.equal attr.SongKey "SomeTest" "SongKey is correct"
            Expect.equal attr.SKU "RS2" "SKU is correct"
            Expect.isTrue attr.Shipping "Shipping is true"

        testCase "Various attributes are set correctly (Instrumental)" <| fun _ ->
            let expectedId = testLead.PersistentID.ToString("N").ToUpperInvariant()

            let attr = createAttributes testProject (FromInstrumental(testLead, testSng))

            Expect.equal attr.MasterID_RDV 12345 "MasterID is correct"
            Expect.equal attr.PersistentID expectedId "PersistentID is correct"
            Expect.equal attr.FullName "SomeTest_Lead" "FullName is correct"
            Expect.equal attr.PreviewBankPath "song_sometest_preview.bnk" "PreviewBankPath is correct"
            Expect.equal attr.SongBank "song_sometest.bnk" "SongBank is correct"
            Expect.equal attr.SongEvent "Play_SomeTest" "SongEvent is correct"
            Expect.equal attr.ArrangementType (Nullable(0)) "ArrangementType is correct"
            Expect.equal attr.LastConversionDateTime testSng.MetaData.LastConversionDateTime "LastConversionDateTime is correct"

        testCase "Note count/score related attributes are set correctly (Instrumental)" <| fun _ ->
            let attr = createAttributes testProject (FromInstrumental(testLead, testSng))

            Expect.equal attr.NotesEasy (Nullable(float32 testSng.NoteCounts.Easy)) "NotesEasy is correct"
            Expect.equal attr.NotesMedium (Nullable(float32 testSng.NoteCounts.Medium)) "NotesMedium is correct"
            Expect.equal attr.NotesHard (Nullable(float32 testSng.NoteCounts.Hard)) "NotesHard is correct"
            Expect.equal attr.Score_MaxNotes (Nullable(float32 testSng.NoteCounts.Hard)) "Score_MaxNotes is correct"
            Expect.equal attr.MaxPhraseDifficulty (Nullable(testSng.Levels.Length - 1)) "MaxPhraseDifficulty is correct"
            Expect.equal attr.TargetScore (Nullable(100_000)) "TargetScore is correct"
            Expect.equal attr.Score_PNV.Value (100_000.f / float32 testSng.NoteCounts.Hard) "Score_PNV is correct"
            Expect.isGreaterThan attr.EasyMastery.Value 0. "EasyMastery is greater than zero"
            Expect.isGreaterThan attr.MediumMastery.Value 0. "MediumMastery is greater than zero"

        testCase "Various metadata attributes are set correctly (Instrumental)" <| fun _ ->
            let attr = createAttributes testProject (FromInstrumental(testLead, testSng))

            Expect.equal attr.AlbumName "Album" "Album name is correct"
            Expect.equal attr.AlbumNameSort "AlbumSort" "Album name sort is correct"
            Expect.equal attr.ArtistName "Artist" "Artist name is correct"
            Expect.equal attr.ArtistNameSort "ArtistSort" "Artist name sort is correct"
            Expect.equal attr.SongName "Title" "Song name is correct"
            Expect.equal attr.SongNameSort "TitleSort" "Song name sort is correct"
            Expect.equal attr.SongYear (Nullable(2020)) "Year is correct"
            Expect.equal attr.SongLength (Nullable(testSng.MetaData.SongLength)) "Song length is correct"
            Expect.equal attr.SongAverageTempo (Nullable(160.541f)) "Song average tempo is correct"
            Expect.equal attr.SongOffset (Nullable(-10.f)) "Song offset is correct"

        testCase "Various attributes are set correctly (Instrumental with custom audio)" <| fun _ ->
            let testArr = { testLead with CustomAudio = Some { Path = "Test.wem"; Volume = 0. } }
            let testProject = { testProject with Arrangements = [ Instrumental testArr ] }
            let attr = createAttributes testProject (FromInstrumental(testArr, testSng))

            Expect.equal attr.PreviewBankPath "song_sometest_preview.bnk" "PreviewBankPath is correct"
            Expect.equal attr.SongBank "song_sometest_lead.bnk" "SongBank is correct"
            Expect.equal attr.SongEvent "Play_SomeTest_lead" "SongEvent is correct"

        testCase "Various attributes are set correctly (Vocals)" <| fun _ ->
            let project = { testProject with Arrangements = [ Vocals testVocals ] }

            let attr = createAttributes project (FromVocals testVocals)

            Expect.equal attr.MasterID_RDV 54321 "MasterID is correct"
            Expect.equal attr.FullName "SomeTest_Vocals" "FullName is correct"
            Expect.equal attr.SongEvent "Play_SomeTest" "SongEvent is correct"
            Expect.equal attr.InputEvent "Play_Tone_Standard_Mic" "InputEvent is correct"
            Expect.equal attr.JapaneseVocal (Nullable()) "JapaneseVocal is null"

        testCase "Various attributes are set correctly (Japanese Vocals)" <| fun _ ->
            let project = { testProject with Arrangements = [ Vocals testJVocals ] }

            let attr = createAttributes project (FromVocals testJVocals)

            Expect.equal attr.ArrangementName "Vocals" "ArrangementName is correct"
            Expect.equal attr.FullName "SomeTest_JVocals" "FullName is correct"
            Expect.equal attr.JapaneseVocal (Nullable(true)) "JapaneseVocal is true"

        testCase "Maximum scroll speed is correct" <| fun _ ->
            let attr = createAttributes testProject (FromInstrumental(testLead, testSng))
            let dvd = attr.DynamicVisualDensity[(testArr.Levels.Count - 1)..]

            Expect.allEqual dvd (float32 testLead.ScrollSpeed) "Maximum scroll speed is correct"

        testCase "CapoFret, Tuning and CentOffset are set correctly" <| fun _ ->
            let tuning = Tuning.FromArray([| -1s; -2s; -3s; -4s; -5s; -6s |])
            let project = { testProject with Arrangements = [ Instrumental testLeadCapo ] }

            let attr = createAttributes project (FromInstrumental(testLeadCapo, testSng))

            Expect.equal attr.CapoFret (Nullable(5.)) "Capo fret is correct"
            Expect.equal attr.Tuning (Some(tuning)) "Tuning is correct"
            Expect.equal attr.CentOffset (Nullable(50.)) "Cent offset is correct"

        testCase "Japanese artist and song name are set correctly" <| fun _ ->
            let project =
                { testProject with
                      JapaneseArtistName = Some "アーティスト"
                      JapaneseTitle = Some "タイトル" }

            let attr = createAttributes project (FromInstrumental(testLead, testSng))

            Expect.equal attr.JapaneseArtistName "アーティスト" "Japanese artist name is correct"
            Expect.equal attr.JapaneseSongName "タイトル" "Japanese song name is correct"

        testCase "Overridden arrangement properties are set correctly" <| fun _ ->
            for (prop, f) in testProps do
                let inst = { testLead with ArrangementProperties = Some prop }
                let project = { testProject with Arrangements = [ Instrumental inst ] }

                let attr = createAttributes project (FromInstrumental(inst, testSng))

                Expect.equal (f attr.ArrangementProperties.Value) 1uy "Arrangement property was set correctly"

        testCase "Sort values are created if they are missing from the project" <| fun _ ->
            let ss v sv = { Value = v; SortValue = sv }
            let project =
                { testProject with
                    ArtistName = ss "B.B. King" ""
                    Title = ss "The Title" null
                    AlbumName = ss "B.B. King" "   " }

            let attr = createAttributes project (FromInstrumental(testLead, testSng))

            // Should be unchanged
            Expect.equal attr.AlbumNameSort "B.B. King" "Album name sort was set correctly"
            // Should use official sort value
            Expect.equal attr.ArtistNameSort "BB King" "Artist name sort was set correctly"
            // Article should be removed
            Expect.equal attr.SongNameSort "Title" "Song name sort was set correctly"
    ]
