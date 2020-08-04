namespace Rocksmith2014.DLCProject.Manifest

open System
open System.Collections.Generic
open Rocksmith2014.DLCProject
open Rocksmith2014.SNG

module internal A =
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

/// Attributes common to both header and regular attributes for vocals and instrumental arrangements.
type AttributesBase(project : DLCProject, arrangement : Arrangement) =
    let dlcKey = project.DLCKey.ToLowerInvariant()

    member val AlbumArt : string = sprintf "urn:image:dds:album_%s" dlcKey
    member val ArrangementName : string = A.getName arrangement true
    member val DLC : bool = true
    member val DLCKey : string = project.DLCKey
    member val JapaneseArtistName : string = Option.toObj project.JapaneseArtistName
    member val JapaneseSongName : string = Option.toObj project.JapaneseTitle
    member val JapaneseVocal : Nullable<bool> = A.getJapaneseVocal arrangement
    member val LeaderboardChallengeRating : int = 0 // Always zero
    member val ManifestUrn : string = sprintf "urn:database:json-db:%s_%s" dlcKey "something" // TODO
    member val MasterID_RDV : int = A.getMasterId arrangement
    member val PersistentID : string = A.getPersistentId arrangement
    member val SKU : string = "RS2"
    member val Shipping : bool = true
    member val SongKey : string = project.DLCKey

type VocalsHeader(project : DLCProject, arrangement : Arrangement) =
    inherit AttributesBase(project, arrangement)

type VocalsAttributes(project : DLCProject, arrangement : Arrangement) =
    inherit AttributesBase(project, arrangement)

    let dlcKey = project.DLCKey.ToLowerInvariant()

    member val ArrangementSort : int = 0 // Always zero
    member val BlockAsset : string = sprintf "urn:emergent-world:%s" dlcKey
    member val DynamicVisualDensity : float32 array = Array.replicate 20 2.f
    member val FullName : string = sprintf "%s_%s" project.DLCKey (A.getName arrangement false)
    member val InputEvent : string = "Play_Tone_Standard_Mic"
    member val MasterID_PS3 : int = -1
    member val MasterID_XBox360 : int = -1
    member val PreviewBankPath : string = sprintf "song_%s_preview.bnk" dlcKey
    member val RelativeDifficulty : int = 0 // Always zero
    member val ShowlightsXML : string = sprintf "urn:application:xml:%s_showlights" dlcKey
    member val SongAsset : string = sprintf "urn:application:musicgame-song:%s_%s" dlcKey "jvocals/vocals" // TODO
    member val SongBank : string = sprintf "song_%s.bnk" dlcKey
    member val SongEvent : string = sprintf "Play_%s" base.SongKey
    member val SongXml : string = sprintf "urn:application:xml:%s_%s" dlcKey "jvocals/vocals" // TODO

type SongHeader(project : DLCProject, arrangement : Instrumental, sng : SNG) =
    inherit AttributesBase(project, Instrumental arrangement)

    member val AlbumName : string = project.AlbumName
    member val AlbumNameSort : string = project.AlbumNameSort
    member val ArtistName : string = project.ArtistName
    member val ArtistNameSort : string = project.ArtistNameSort
    member val BassPick : Nullable<int> = Nullable() // TODO
    member val CentOffset : float32 = 0.f // TODO
    member val DNA_Chords : float32 = 0.f // TODO
    member val DNA_Riffs : float32 = 0.f // TODO
    member val DNA_Solo : float32 = 0.f // TODO
    member val EasyMastery : float32 = 0.f // TODO
    member val MediumMastery : float32 = 0.f // TODO
    member val NotesEasy : float32 = float32 sng.NoteCounts.Easy
    member val NotesHard : float32 = float32 sng.NoteCounts.Hard
    member val NotesMedium : float32 = float32 sng.NoteCounts.Medium
    member val Representative : int = 0 // TODO
    member val RouteMask : int = arrangement.RouteMask |> LanguagePrimitives.EnumToValue
    member val SongDiffEasy : float32 = 0.f // TODO
    member val SongDiffHard : float32 = 0.f // TODO
    member val SongDiffMed : float32 = 0.f // TODO
    member val SongDifficulty : float32 = 0.f // TODO
    member val SongLength : float32 = 0.f // TODO
    member val SongName : string = project.Title
    member val SongNameSort : string = project.TitleSort
    member val SongYear : int = project.Year
    member val Tuning : Tuning = Tuning.Default // TODO

type SongAttributes(project : DLCProject, instrumental : Instrumental, header : SongHeader, sng : SNG) =
    inherit AttributesBase(project, (Instrumental instrumental))

    let dlcKey = project.DLCKey.ToLowerInvariant()

    let partition =
        let rec getPartition list part =
            match list with
            | (Instrumental head)::_ when head = instrumental -> part
            | (Instrumental head)::tail when head.ArrangementName = instrumental.ArrangementName ->
                getPartition tail (part + 1)
            | _::t ->  getPartition t part
            | [] -> part

        getPartition project.Arrangements 1

    member val AlbumName : string = header.AlbumName
    member val AlbumNameSort : string = header.AlbumNameSort
    //member val ArrangementProperties : ArrangementProperties = 
    member val ArrangementSort : int = 0 // Always zero
    member val ArrangementType : int = instrumental.ArrangementName |> LanguagePrimitives.EnumToValue
    member val ArtistName : string = header.ArtistName
    member val ArtistNameSort : string = header.ArtistNameSort
    member val BlockAsset : string = sprintf "urn:emergent-world:%s" dlcKey
    member val CentOffset : float32 = header.CentOffset
    member val Chords : Dictionary<string, Dictionary<string, int array>> = Dictionary()
    member val ChordTemplates : ChordTemplate array = [||] // TODO
    member val DNA_Chords : float32 = header.DNA_Chords
    member val DNA_Riffs : float32 = header.DNA_Riffs
    member val DNA_Solo : float32 = header.DNA_Solo
    member val DynamicVisualDensity : float32 array = [||] // TODO
    member val EasyMastery : float32 = header.EasyMastery
    member val FullName : string = sprintf "%s_%s" base.SongKey base.ArrangementName
    member val LastConversionDateTime : string = sng.MetaData.LastConversionDateTime
    member val MasterID_PS3 : int = -1
    member val MasterID_XBox360 : int = -1
    member val MaxPhraseDifficulty : int = sng.Levels.Length
    member val MediumMastery : float32 = header.MediumMastery
    member val NotesEasy : float32 = header.NotesEasy
    member val PhraseIterations : PhraseIteration array = [||] // TODO
    member val Phrases : Phrase array = [||] // TODO
    member val PreviewBankPath : string = sprintf "song_%s_preview.bnk" dlcKey
    member val RelativeDifficulty : int = 0 // Always zero
    member val Score_MaxNotes : float32 = header.NotesHard
    member val Score_PNV : float32 = 100000.f / header.NotesHard
    member val Sections : Section array = [||] // TODO
    member val ShowlightsXML : string = sprintf "urn:application:xml:%s_showlights" dlcKey
    member val SongAsset : string = sprintf "urn:application:musicgame-song:%s_%s" dlcKey "something" // TODO
    member val SongAverageTempo : float32 = 0.f // TODO
    member val SongBank : string = sprintf "song_%s.bnk" dlcKey
    member val SongDiffEasy : float32 = header.SongDiffEasy
    member val SongDiffHard : float32 = header.SongDiffHard
    member val SongDifficulty : float32 = header.SongDifficulty
    member val SongDiffMed : float32 = header.SongDiffMed
    member val SongEvent : string = sprintf "Play_%s" base.SongKey
    member val SongLength : float32 = sng.MetaData.SongLength
    member val SongName : string = project.Title
    member val SongNameSort : string = project.TitleSort
    member val SongOffset : float32 = -sng.MetaData.StartTime
    member val SongPartition : int = partition
    member val SongXml : string = sprintf "urn:application:xml:%s_%s" dlcKey "something" // TODO
    member val SongYear : int = project.Year
    member val TargetScore : int = 100000
    member val Techniques : Dictionary<string, Dictionary<string, int array>> = Dictionary() // TODO
    member val Tone_A : string = "" // TODO
    member val Tone_B : string = "" // TODO
    member val Tone_Base : string = "" // TODO
    member val Tone_C : string = "" // TODO
    member val Tone_D : string = "" // TODO
    member val Tone_Multiplayer : string = String.Empty
    member val Tones : Tone array = [||] // TODO
    member val Tuning : Tuning = header.Tuning

type AttributesContainer = { Attributes : obj }
