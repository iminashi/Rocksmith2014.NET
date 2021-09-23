module ToolkitImportTests

open Expecto
open Rocksmith2014.DLCProject
open System
open System.IO

let toPath =
    let templatePath = Path.GetFullPath("toolkit-old.dlc.xml")
    let templateDir = Path.GetDirectoryName(templatePath)

    fun fileName -> Path.Combine(templateDir, fileName)

[<Tests>]
let tests =
    testList "Toolkit Template Import Tests" [
        test "Old template can be imported" {
            let templatePath = Path.GetFullPath("toolkit-old.dlc.xml")

            let project = ToolkitImporter.import templatePath
            
            Expect.equal project.Version "1.5" "Version is correct"
            Expect.equal project.ArtistName.Value "Artist" "Artist name is correct"
            Expect.equal project.AudioFile.Path (toPath "audio.wem") "Audio file is correct"
            Expect.equal project.AudioFile.Volume -2. "Audio volume is correct"
            Expect.hasLength project.Arrangements 1 "One arrangement was imported"
            
            match project.Arrangements.Head with
            | Instrumental inst ->
                Expect.equal inst.XML (toPath "PART REAL_GUITAR_RS2.xml") "XML file path is correct"    
                Expect.equal inst.PersistentID (Guid.Parse("5f5d33c8-e88e-485c-8ca8-d4aed6e80a31")) "Persistent ID is correct"
                Expect.equal inst.ScrollSpeed 1.0 "Scroll speed is correct"
                Expect.equal inst.BaseTone "Default" "Base tone is correct"
            | _ ->
                failwith "Wrong arrangement type."

            Expect.hasLength project.Tones 1 "One tone was imported"
        }

        test "New template can be imported" {
            let templatePath = Path.GetFullPath("toolkit-new.dlc.xml")

            let project = ToolkitImporter.import templatePath

            Expect.equal project.Version "5" "Version is correct"
            Expect.equal project.ArtistName.Value "Artist" "Artist name is correct"
            Expect.equal project.JapaneseArtistName.Value "アーティスト" "Japanese artist name is correct"
            Expect.equal project.AudioFile.Path (toPath "test.wav") "Audio file is correct"
            Expect.equal project.AudioFile.Volume -7. "Audio volume is correct"
            Expect.hasLength project.Arrangements 5 "Five arrangement were imported"
            
            match project.Arrangements.[0] with
            | Instrumental inst ->
                Expect.equal inst.XML (toPath "Arr_Lead_RS2.xml") "XML file path is correct"    
                Expect.equal inst.PersistentID (Guid.Parse("6a7916c7-6f39-4362-a45e-29025952d0a8")) "Persistent ID is correct"
                Expect.equal inst.Priority ArrangementPriority.Main "First arrangement is main lead"
                Expect.equal inst.Name ArrangementName.Lead "First arrangement is main lead"
                Expect.equal inst.ScrollSpeed 1.3 "Scroll speed is correct"
                Expect.equal inst.BaseTone "clean" "Base tone is correct"
            | _ ->
                failwith "Wrong arrangement type."

            match project.Arrangements.[1] with
            | Instrumental inst ->
                Expect.equal inst.XML (toPath "Arr_BonusLead_RS2.xml") "XML file path is correct"    
                Expect.equal inst.Priority ArrangementPriority.Alternative "Second arrangement is alternative lead"
                Expect.equal inst.Name ArrangementName.Lead "Second arrangement is alternative lead"
                Expect.equal inst.TuningPitch 450. "Tuning pitch is correct"
            | _ ->
                failwith "Wrong arrangement type."

            match project.Arrangements.[3] with
            | Instrumental inst ->
                Expect.equal inst.XML (toPath "Arr_Bass_RS2.xml") "XML file path is correct"    
                Expect.equal inst.Priority ArrangementPriority.Main "Fourth arrangement is main bass"
                Expect.equal inst.Name ArrangementName.Bass "Fourth arrangement is main bass"
            | _ ->
                failwith "Wrong arrangement type."

            match project.Arrangements.[4] with
            | Vocals voc ->
                Expect.equal voc.XML (toPath "PART JVOCALS_RS2.xml") "XML file path is correct"    
                Expect.isTrue voc.Japanese "Fifth arrangement is Japanese vocals"
                Expect.equal voc.CustomFont.Value (toPath "lyrics.dds") "Custom font is correct"
            | _ ->
                failwith "Wrong arrangement type."

            Expect.hasLength project.Tones 3 "Three tones were imported"
        }
    ]
