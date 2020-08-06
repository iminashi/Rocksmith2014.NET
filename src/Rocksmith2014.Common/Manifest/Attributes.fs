namespace Rocksmith2014.Common.Manifest

open System
open System.Collections.Generic

type Attributes() =   
    member val AlbumArt : string = "" with get, set

    member val AlbumName : string = null with get, set
    
    member val AlbumNameSort : string = null with get, set

    member val ArrangementName : string = "" with get, set
    
    member val ArrangementProperties : ArrangementProperties option = None with get, set

    // Always zero
    member val ArrangementSort : Nullable<int> = Nullable() with get, set
    
    member val ArrangementType : Nullable<int> = Nullable() with get, set 
    
    member val ArtistName : string = null with get, set
    
    member val ArtistNameSort : string = null with get, set

    member val BassPick : Nullable<int> = Nullable() with get, set 
    
    member val BlockAsset : string = null with get, set
    
    member val CentOffset : Nullable<float> = Nullable() with get, set 
    
    member val Chords : Dictionary<string, Dictionary<string, int array>> = null with get, set
    
    member val ChordTemplates : ChordTemplate array = null with get, set

    member val DLC : bool = true with get, set
    
    member val DLCKey : string = "" with get, set
    
    member val DNA_Chords : Nullable<float> = Nullable() with get, set 
    
    member val DNA_Riffs : Nullable<float> = Nullable() with get, set 
    
    member val DNA_Solo : Nullable<float> = Nullable() with get, set 

    member val DynamicVisualDensity : float32 array = null with get, set

    member val EasyMastery : Nullable<float> = Nullable() with get, set 
    
    member val FullName : string = null with get, set
    
    member val InputEvent : string = null with get, set

    member val JapaneseArtistName : string = null with get, set
    
    member val JapaneseSongName : string = null with get, set
    
    member val JapaneseVocal : Nullable<bool> = Nullable() with get, set

    member val LastConversionDateTime : string = null with get, set

    // Always zero
    member val LeaderboardChallengeRating : int = 0

    member val ManifestUrn : string = "" with get, set

    member val MasterID_PS3 : Nullable<int> = Nullable() with get, set

    member val MasterID_RDV : int = 0 with get, set
    
    member val MasterID_XBox360 : Nullable<int> = Nullable() with get, set
    
    member val MaxPhraseDifficulty : Nullable<int> = Nullable() with get, set
    
    member val MediumMastery : Nullable<float> = Nullable() with get, set
    
    member val NotesEasy : Nullable<float32> = Nullable() with get, set

    member val NotesHard : Nullable<float32>  = Nullable() with get, set

    member val NotesMedium : Nullable<float32>  = Nullable() with get, set

    member val PersistentID : string = "" with get, set
    
    member val PhraseIterations : PhraseIteration array = null with get, set
    
    member val Phrases : Phrase array = null with get, set
    
    member val PreviewBankPath : string = null with get, set

    // Always zero
    member val RelativeDifficulty : Nullable<int> = Nullable() with get, set

    member val Representative : Nullable<int>  = Nullable() with get, set

    member val RouteMask : Nullable<int>  = Nullable() with get, set

    member val Score_MaxNotes : Nullable<float32> = Nullable() with get, set
    
    member val Score_PNV : Nullable<float32> = Nullable() with get, set
    
    member val Sections : Section array = null with get, set

    member val Shipping : bool = true
    
    member val ShowlightsXML : string = null with get, set

    member val SKU : string = "RS2"
    
    member val SongAsset : string = null with get, set
    
    member val SongAverageTempo : Nullable<float32> = Nullable() with get, set
    
    member val SongBank : string = null with get, set
    
    member val SongDiffEasy : Nullable<float> = Nullable() with get, set
    
    member val SongDiffHard : Nullable<float> = Nullable() with get, set
    
    member val SongDifficulty : Nullable<float> = Nullable() with get, set
    
    member val SongDiffMed : Nullable<float> = Nullable() with get, set
    
    member val SongEvent : string = null with get, set

    member val SongKey : string = "" with get, set
    
    member val SongLength : Nullable<float32> = Nullable() with get, set
    
    member val SongName : string = null with get, set
    
    member val SongNameSort : string = null with get, set
    
    member val SongOffset : Nullable<float32> = Nullable() with get, set
    
    member val SongPartition : Nullable<int> = Nullable() with get, set
    
    member val SongXml : string = null with get, set
    
    member val SongYear : Nullable<int> = Nullable() with get, set
    
    member val TargetScore : Nullable<int> = Nullable() with get, set
    
    member val Techniques : Dictionary<string, Dictionary<string, int array>> = null with get, set
    
    member val Tone_A : string = null with get, set
    
    member val Tone_B : string = null with get, set
    
    member val Tone_Base : string = null with get, set
    
    member val Tone_C : string = null with get, set
    
    member val Tone_D : string = null with get, set
    
    member val Tone_Multiplayer : string = null with get, set
    
    member val Tones : Tone array = null with get, set
    
    member val Tuning : Tuning option = None with get, set

type AttributesContainer = { Attributes : Attributes }
