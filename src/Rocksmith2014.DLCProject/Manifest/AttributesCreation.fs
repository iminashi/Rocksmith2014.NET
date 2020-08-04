module Rocksmith2014.DLCProject.Manifest.AttributesCreation

open Rocksmith2014.DLCProject
open Rocksmith2014
open Rocksmith2014.SNG
open Rocksmith2014.Common.Attributes
open System
open System.Collections.Generic

module internal A =
    let bti b = if b then 1 else 0

    let getMasterId = function
        | Vocals v -> v.MasterID
        | Instrumental i -> i.MasterID
        | Showlights -> failwith "No"

    let getPersistentId (arr: Arrangement) =
        let id =
            match arr with
            | Vocals v -> v.PersistentID.ToString()
            | Instrumental i -> i.PersistentID.ToString()
            | Showlights -> failwith "No"
        id.Replace("-", "").ToUpperInvariant()

    let getName (arr: Arrangement) generic =
        match arr with
        | Vocals v -> if v.Japanese && not generic then "JVocals" else "Vocals"
        | Showlights -> "Showlights"
        | Instrumental i -> i.ArrangementName.ToString()

    let getJapaneseVocal = function
        | Vocals v when v.Japanese -> Nullable(true)
        | _ -> Nullable()

    let calculateDifficulties (metaData: XML.MetaData) (sng: SNG) =
        let arrProp = metaData.ArrangementProperties
        let techCoeff = bti arrProp.NonStandardChords +
                        3 * bti arrProp.BarreChords +
                        (bti arrProp.PowerChords ||| bti arrProp.DoubleStops) +
                        bti arrProp.DropDPower +
                        2 * bti arrProp.OpenChords +
                        bti arrProp.FingerPicking +
                        bti arrProp.TwoFingerPicking +
                        bti arrProp.PalmMutes +
                        2 * bti arrProp.Harmonics +
                        3 * bti arrProp.PinchHarmonics +
                        bti arrProp.Hopo +
                        bti arrProp.Tremolo +
                        (if arrProp.PathBass then 4 else 1) * bti arrProp.Slides +
                        bti arrProp.UnpitchedSlides +
                        3 * bti arrProp.Bends +
                        4 * bti arrProp.Tapping +
                        2 * bti arrProp.Vibrato +
                        bti arrProp.FretHandMutes +
                        bti arrProp.SlapPop +
                        bti arrProp.Sustain +
                        bti arrProp.FifthsAndOctaves +
                        bti arrProp.Syncopation

        // Arrangements with few/no techniques get very low values otherwise
        let techCoeff =
            if techCoeff <= 5 then
                techCoeff + 4
            else
                techCoeff

        // In official content maximum value for SongDiffHard is 1.0
        Math.Round(float (techCoeff * sng.NoteCounts.Hard) / float sng.MetaData.SongLength / 100.0, 9),
        Math.Round(float (techCoeff * sng.NoteCounts.Medium) / float sng.MetaData.SongLength / 50.0, 9),
        Math.Round(float (techCoeff * sng.NoteCounts.Easy) / float sng.MetaData.SongLength / 25.0, 9)

    let convertPhraseIterations (sng: SNG) =
        sng.PhraseIterations
        |> Array.map (fun pi ->
            let phrase = sng.Phrases.[pi.PhraseId]
            { PhraseIndex = pi.PhraseId
              MaxDifficulty = phrase.MaxDifficulty
              Name = phrase.Name
              StartTime = pi.StartTime
              EndTime = pi.EndTime })

    let createDVD (arrangement: Arrangement) =
        // TODO
        Array.replicate 20 2.f

let initBase dlcKey (project: DLCProject) (arrangement: Arrangement) (attr: Attributes) =
    attr.AlbumArt <- sprintf "urn:image:dds:album_%s" dlcKey
    attr.ArrangementName <- A.getName arrangement true
    attr.DLCKey <- project.DLCKey
    attr.JapaneseArtistName <- Option.toObj project.JapaneseArtistName
    attr.JapaneseSongName <- Option.toObj project.JapaneseTitle
    attr.JapaneseVocal <- A.getJapaneseVocal arrangement
    attr.ManifestUrn <- sprintf "urn:database:json-db:%s_%s" dlcKey "something" // TODO
    attr.MasterID_RDV <- A.getMasterId arrangement
    attr.PersistentID <- A.getPersistentId arrangement
    attr.SongKey <- project.DLCKey

    attr

let initAttributesCommon dlcKey (project: DLCProject) (arrangement: Arrangement) (attr: Attributes) =
    attr.ArrangementSort <- 0 |> Nullable // Always zero
    attr.BlockAsset <- sprintf "urn:emergent-world:%s" dlcKey
    attr.DynamicVisualDensity <- A.createDVD arrangement
    attr.FullName <- sprintf "%s_%s" project.DLCKey (A.getName arrangement false)
    attr.MasterID_PS3 <- -1 |> Nullable
    attr.MasterID_XBox360 <- -1 |> Nullable
    attr.PreviewBankPath <- sprintf "song_%s_preview.bnk" dlcKey
    attr.RelativeDifficulty <- 0 |> Nullable // Always zero
    attr.ShowlightsXML <- sprintf "urn:application:xml:%s_showlights" dlcKey
    attr.SongAsset <- sprintf "urn:application:musicgame-song:%s_%s" dlcKey "jvocals/vocals" // TODO
    attr.SongBank <- sprintf "song_%s.bnk" dlcKey
    attr.SongEvent <- sprintf "Play_%s" project.DLCKey
    attr.SongXml <- sprintf "urn:application:xml:%s_%s" dlcKey "jvocals/vocals" // TODO

    attr

let initSongCommon xmlMetaData (project: DLCProject) (instrumental: Instrumental) (sng: SNG) (attr: Attributes) =
    let dHard, dMedium, dEasy = A.calculateDifficulties xmlMetaData sng

    attr.AlbumName <- project.AlbumName
    attr.AlbumNameSort <- project.AlbumNameSort
    attr.ArtistName <- project.ArtistName
    attr.ArtistNameSort <- project.ArtistNameSort
    attr.BassPick <- if xmlMetaData.ArrangementProperties.BassPick then Nullable(1) else Nullable()
    attr.CentOffset <- Nullable(project.CentOffset)
    attr.DNA_Chords <- Nullable(0.f) // TODO
    attr.DNA_Riffs <- Nullable(0.f) // TODO
    attr.DNA_Solo <- Nullable(0.f) // TODO
    attr.EasyMastery <- Math.Round(float sng.NoteCounts.Easy / float sng.NoteCounts.Hard, 9) |> Nullable
    attr.MediumMastery <- Math.Round(float sng.NoteCounts.Medium / float sng.NoteCounts.Hard, 9) |> Nullable
    attr.NotesEasy <- float32 sng.NoteCounts.Easy |> Nullable
    attr.NotesHard <- float32 sng.NoteCounts.Hard |> Nullable
    attr.NotesMedium <- float32 sng.NoteCounts.Medium |> Nullable
    attr.SongDiffEasy <- dEasy |> Nullable
    attr.SongDiffHard <- dHard |> Nullable
    attr.SongDiffMed <- dMedium |> Nullable
    attr.SongDifficulty <- dHard |> Nullable
    attr.SongLength <- sng.MetaData.SongLength |> Nullable
    attr.SongName <- project.Title
    attr.SongNameSort <- project.TitleSort
    attr.SongYear <- project.Year |> Nullable
    attr.Tuning <- Tuning.FromArray(xmlMetaData.Tuning.Strings) |> Some

    attr

let initSongComplete (project: DLCProject) (instrumental: Instrumental) (sng: SNG) (attr: Attributes) =
    let partition =
        let rec getPartition list part =
            match list with
            | (Instrumental head)::_ when head = instrumental -> part
            | (Instrumental head)::tail when head.ArrangementName = instrumental.ArrangementName ->
                getPartition tail (part + 1)
            | _::t ->  getPartition t part
            | [] -> part

        getPartition project.Arrangements 1

    //member val ArrangementProperties : ArrangementProperties = // TODO
    attr.ArrangementType <- instrumental.ArrangementName |> LanguagePrimitives.EnumToValue |> Nullable
    attr.Chords <- Dictionary() // TODO
    attr.ChordTemplates <- [||] // TODO
    attr.LastConversionDateTime <- sng.MetaData.LastConversionDateTime
    attr.MaxPhraseDifficulty <- sng.Levels.Length |> Nullable
    attr.PhraseIterations <- A.convertPhraseIterations sng
    attr.Phrases <- [||] // TODO
    attr.Score_MaxNotes <- sng.NoteCounts.Hard |> float32 |> Nullable
    attr.Score_PNV <- (100000.f / float32 sng.NoteCounts.Hard) |> Nullable
    attr.Sections <- [||] // TODO
    attr.SongAverageTempo <- 0.f |> Nullable // TODO
    attr.SongOffset <- -sng.MetaData.StartTime |> Nullable
    attr.SongPartition <- partition |> Nullable
    attr.TargetScore <- 100000 |> Nullable
    attr.Techniques <- Dictionary() // TODO
    attr.Tone_A <- "" // TODO
    attr.Tone_B <- "" // TODO
    attr.Tone_Base <- "" // TODO
    attr.Tone_C <- "" // TODO
    attr.Tone_D <- "" // TODO
    attr.Tone_Multiplayer <- String.Empty
    attr.Tones <- [||] // TODO

    attr

type AttributesConversion =
| VocalsConversion of Vocals
| InstrumentalConversion of inst: Instrumental * sng: SNG

let private create isHeader (project: DLCProject) (conversion: AttributesConversion) =
    let attributes = Attributes()
    let dlcKey = project.DLCKey.ToLowerInvariant()

    match conversion with
    | VocalsConversion v ->
        let arr = Vocals v
        let attr =
            initBase dlcKey project arr attributes

        if isHeader then
            attr
        else
            initAttributesCommon dlcKey project arr attr |> ignore
            attr.InputEvent <- "Play_Tone_Standard_Mic"
            attr

    | InstrumentalConversion (inst, sng) ->
        let arr = Instrumental inst
        let xmlMetaData = XML.MetaData.Read(inst.XML)

        let attr =
            initBase dlcKey project arr attributes
            |> initSongCommon xmlMetaData project inst sng

        if isHeader then
            attr.Representative <- xmlMetaData.ArrangementProperties.Represent |> A.bti |> Nullable
            attr.RouteMask <- inst.RouteMask |> LanguagePrimitives.EnumToValue |> Nullable
            attr
        else
            attr
            |> initAttributesCommon dlcKey project arr
            |> initSongComplete project inst sng

let createAttributes = create false
let createAttributesHeader = create true