module Rocksmith2014.DLCProject.Manifest.AttributesCreation

open Rocksmith2014
open Rocksmith2014.DLCProject
open Rocksmith2014.Common
open Rocksmith2014.Common.Manifest
open Rocksmith2014.SNG
open System
open System.Collections.Generic

(* There are two "iteration versions" of the attributes, version 2 and 3.

   Differences in version 3:
   -The attributes are sorted differently (e.g. arrangement properties are sorted alphabetically)
   -Whitespace differences
   -Chords section is (at least partially) fixed
   -The way DNA times are calculated seems to be different:
      *Version 2: Sum of all regions of DNA ID to DNA None (or the end of the song)
      *Version 3: Sum of all regions of DNA ID to different DNA ID (or the END phrase) *)

let inline private bti (b: bool) = Convert.ToInt32 b
let inline private btb (b: bool) = Convert.ToByte b

let private getJapaneseVocal = function
    | Vocals v when v.Japanese -> Nullable(true)
    | _ -> Nullable()

/// Calculates the sum of all ranges of DNAId -> DNA None (or the end of the song).
let private getDNATime (sng: SNG) dnaId =
    let rec getTotal i time =
        // Find the index of a DNA with the given ID
        match Array.FindIndex(sng.DNAs, i, (fun x -> x.DnaId = dnaId)) with
        | -1 ->
            time
        | next ->
            // Find the index of the next DNA None
            match Array.FindIndex(sng.DNAs, next, (fun x -> x.DnaId = DNA.None)) with 
            | -1 ->
                time + (sng.MetaData.SongLength - sng.DNAs.[next].Time)
            | none ->
                // Find the next DNA ID -> DNA None range
                time + getTotal none (sng.DNAs.[none].Time - sng.DNAs.[next].Time)

    getTotal 0 0.f |> float

/// Calculates the times for the three types of DNA.
let private calculateDNAs (sng: SNG) =
    Math.Round(getDNATime sng DNA.Chord, 3),
    Math.Round(getDNATime sng DNA.Riff, 3),
    Math.Round(getDNATime sng DNA.Solo, 3)

/// Calculates difficulty values for hard, medium and easy.
let private calculateDifficulties (metaData: XML.MetaData) (sng: SNG) =
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

/// Converts SNG phrase iterations into manifest phrase iterations.
let private convertPhraseIterations (sng: SNG) =
    sng.PhraseIterations
    |> Array.map (fun pi ->
        let phrase = sng.Phrases.[pi.PhraseId]
        { PhraseIndex = pi.PhraseId
          MaxDifficulty = phrase.MaxDifficulty
          Name = phrase.Name
          StartTime = pi.StartTime
          EndTime = pi.EndTime })

/// Converts SNG chord templates into manifest chord templates.
let private convertChordTemplates (sng: SNG) = [|
    for id = 0 to sng.Chords.Length - 1 do
        let chord = sng.Chords.[id]
        if String.notEmpty chord.Name && chord.Mask <> ChordMask.Arpeggio then
            { ChordId = int16 id
              ChordName = chord.Name
              Fingers = chord.Fingers
              Frets = chord.Frets } |]

/// Returns a matching UI name for a section name.
let private getSectionUIName (name: string) =
    // Official files may have names like "riff 1" or "solo7"
    match name.ToLowerInvariant() |> String.filter Char.IsLetter with
    | "fadein"     -> "$[34276] Fade In [1]"
    | "fadeout"    -> "$[34277] Fade Out [1]"
    | "buildup"    -> "$[34278] Buildup [1]"
    | "chorus"     -> "$[34279] Chorus [1]"
    | "hook"       -> "$[34280] Hook [1]"
    | "head"       -> "$[34281] Head [1]"
    | "bridge"     -> "$[34282] Bridge [1]"
    | "ambient"    -> "$[34283] Ambient [1]"
    | "breakdown"  -> "$[34284] Breakdown [1]"
    | "interlude"  -> "$[34285] Interlude [1]"
    | "intro"      -> "$[34286] Intro [1]"
    | "melody"     -> "$[34287] Melody [1]"
    | "modbridge"  -> "$[34288] Modulated Bridge [1]"
    | "modchorus"  -> "$[34289] Modulated Chorus [1]"
    | "modverse"   -> "$[34290] Modulated Verse [1]"
    | "outro"      -> "$[34291] Outro [1]"
    | "postbrdg"   -> "$[34292] Post Bridge [1]"
    | "postchorus" -> "$[34293] Post Chorus [1]"
    | "postvs"     -> "$[34294] Post Verse [1]"
    | "prebrdg"    -> "$[34295] Pre Bridge [1]"
    | "prechorus"  -> "$[34296] Pre Chorus [1]"
    | "preverse"   -> "$[34297] Pre Verse [1]"
    | "riff"       -> "$[34298] Riff [1]"
    | "rifff"      -> "$[34298] Riff [1]" // Error in official file
    | "silence"    -> "$[34299] Silence [1]"
    | "solo"       -> "$[34300] Solo [1]"
    | "transition" -> "$[34301] Transition [1]"
    | "vamp"       -> "$[34302] Vamp [1]"
    | "variation"  -> "$[34303] Variation [1]"
    | "verse"      -> "$[34304] Verse [1]"
    | "tapping"    -> "$[34305] Tapping [1]"
    | "noguitar"   -> "$[6091] No Guitar [1]"
    | name         -> failwith $"Unknown section name '{name}'."

/// Converts SNG sections into manifest sections.
let private convertSections (sng: SNG) =
    sng.Sections
    |> Array.map (fun s ->
        { Name = s.Name
          UIName = getSectionUIName s.Name
          Number = s.Number
          StartTime = s.StartTime
          EndTime = s.EndTime
          StartPhraseIterationIndex = s.StartPhraseIterationId
          EndPhraseIterationIndex = s.EndPhraseIterationId
          IsSolo = String.startsWith "solo" s.Name })

/// Converts SNG phrases into manifest phrases.
let private convertPhrases (sng: SNG) =
    sng.Phrases
    |> Array.map (fun p ->
        { MaxDifficulty = int8 p.MaxDifficulty
          Name = p.Name
          IterationCount = p.IterationCount })

/// Creates a dynamic visual density array for the arrangement.
let private createDynamicVisualDensity (levels: int) (arrangement: Arrangement) =
    match arrangement with
    | Vocals _ ->
        Array.replicate 20 2.f

    | Instrumental inst ->
        let floorLimit = 0.5 // Fastest allowed speed
        let beginSpeed = 5.0
        let endSpeed = Math.Clamp(inst.ScrollSpeed, floorLimit, beginSpeed)
        let maxLevel = (min levels 20) - 1
        let factor = if maxLevel > 0 then Math.Pow(endSpeed / beginSpeed, 1. / float maxLevel) else 1.

        Array.init 20 (fun i ->
            if i >= maxLevel then
                float32 endSpeed
            else
                float32 <| Math.Round(beginSpeed * Math.Pow(factor, float i), 1))

    | Showlights _ -> failwith "I am Error."

/// Converts XML arrangement properties into manifest arrangement properties.
let private convertArrangementProperties (arrProps: XML.ArrangementProperties) (instrumental: Instrumental) =
    { represent = btb (instrumental.Priority = ArrangementPriority.Main)
      bonusArr = btb (instrumental.Priority = ArrangementPriority.Bonus)
      standardTuning = btb arrProps.StandardTuning
      nonStandardChords = btb arrProps.NonStandardChords
      barreChords = btb arrProps.BarreChords
      powerChords = btb arrProps.PowerChords
      dropDPower = btb arrProps.DropDPower
      openChords = btb arrProps.OpenChords
      fingerPicking = btb arrProps.FingerPicking
      pickDirection = btb arrProps.PickDirection
      doubleStops = btb arrProps.DoubleStops
      palmMutes = btb arrProps.PalmMutes
      harmonics = btb arrProps.Harmonics
      pinchHarmonics = btb arrProps.PinchHarmonics
      hopo = btb arrProps.Hopo
      tremolo = btb arrProps.Tremolo
      slides = btb arrProps.Slides
      unpitchedSlides = btb arrProps.UnpitchedSlides
      bends = btb arrProps.Bends
      tapping = btb arrProps.Tapping
      vibrato = btb arrProps.Vibrato
      fretHandMutes = btb arrProps.FretHandMutes
      slapPop = btb arrProps.SlapPop
      twoFingerPicking = btb arrProps.TwoFingerPicking
      fifthsAndOctaves = btb arrProps.FifthsAndOctaves
      syncopation = btb arrProps.Syncopation
      bassPick = btb instrumental.BassPicked
      sustain = btb arrProps.Sustain
      pathLead = if instrumental.RouteMask = RouteMask.Lead then 1uy else 0uy
      pathRhythm = if instrumental.RouteMask = RouteMask.Rhythm then 1uy else 0uy
      pathBass = if instrumental.RouteMask = RouteMask.Bass then 1uy else 0uy
      routeMask = instrumental.RouteMask |> LanguagePrimitives.EnumToValue |> byte }

/// Creates a chord ID map.
let private createChordMap (sng: SNG) =
    (* Structure:

       "Difficulty level" : {
            "Phrase iteration index" : [
                Chord ID of a chord included in the manifest, i.e. a chord that has a name and is not an arpeggio.
            ]
        }

       In iteration version 2, the way this section is generated for official files is very broken:

       -For a particular difficulty level, the chord IDs of the last phrase iteration are repeated for every phrase iteration.
       -Some phrase iterations that have no chords or notes in that difficulty level are included.
       -Empty arrays may be included. *)

    let chords = Dictionary<string, Dictionary<string, int array>>()

    for lvl = 0 to sng.Levels.Length - 1 do
        let diffIds = Dictionary<string, int array>()
        for i = 0 to sng.PhraseIterations.Length - 1 do
            let pi = sng.PhraseIterations.[i]
            let chordIds =
                sng.Levels.[lvl].HandShapes
                |> Seq.filter (fun x -> 
                    (String.notEmpty sng.Chords.[x.ChordId].Name) && (x.StartTime >= pi.StartTime && x.StartTime < pi.EndTime))
                |> Seq.map (fun x -> x.ChordId)
                |> Set.ofSeq
            if chordIds.Count > 0 then
                diffIds.Add(string i, Set.toArray chordIds)

        if diffIds.Count > 0 then
            chords.Add(string lvl, diffIds)
            
    chords

/// Creates a technique ID map.
let private createTechniqueMap (sng: SNG) =
    (* The structure of the map is the same as the chord map, but with technique IDs instead of chord IDs.

       In official files, the techniques of the last phrase iteration in a difficulty level seem to be missing,
       Or they are included in the first phrase iteration in the next level. *)

    let techniques = Dictionary<string, Dictionary<string, int array>>()

    for lvl = 0 to sng.Levels.Length - 1 do
        let diffIds = Dictionary<string, int array>()
        for i = 0 to sng.PhraseIterations.Length - 2 do
            let pi = sng.PhraseIterations.[i]
            let techIds =
                sng.Levels.[lvl].Notes
                |> Seq.filter (fun x -> (x.Time > pi.StartTime && x.Time <= pi.EndTime)) // Weird division into phrase iterations intentional
                |> Seq.collect (Techniques.getTechniques sng)
                |> Set.ofSeq
            if techIds.Count > 0 then
                diffIds.Add(string i, Set.toArray techIds)

        if diffIds.Count > 0 then
            techniques.Add(string lvl, diffIds)

    techniques

/// Initializes attributes that are common to all arrangements (manifest headers).
let private initBase name dlcKey (project: DLCProject) (arrangement: Arrangement) =
    let attr = Attributes()
    attr.AlbumArt <- $"urn:image:dds:album_%s{dlcKey}"
    attr.ArrangementName <- Arrangement.getName arrangement true
    attr.DLCKey <- project.DLCKey
    attr.JapaneseArtistName <- Option.toObj project.JapaneseArtistName
    attr.JapaneseSongName <- Option.toObj project.JapaneseTitle
    attr.JapaneseVocal <- getJapaneseVocal arrangement
    attr.ManifestUrn <- $"urn:database:json-db:%s{dlcKey}_%s{name}"
    attr.MasterID_RDV <- Arrangement.getMasterId arrangement
    attr.PersistentID <- (Arrangement.getPersistentId arrangement).ToString("N").ToUpperInvariant()
    attr.SongKey <- project.DLCKey

    attr

/// Initializes attributes that are common to all arrangements (non-headers).
let private initAttributesCommon name dlcKey levels (project: DLCProject) (arrangement: Arrangement) (attr: Attributes) =
    attr.ArrangementSort <- 0 // Always zero
    attr.BlockAsset <- $"urn:emergent-world:{dlcKey}"
    attr.DynamicVisualDensity <- createDynamicVisualDensity levels arrangement
    attr.FullName <- $"{project.DLCKey}_{Arrangement.getName arrangement false}"
    attr.MasterID_PS3 <- -1
    attr.MasterID_XBox360 <- -1
    attr.PreviewBankPath <- $"song_{dlcKey}_preview.bnk"
    attr.RelativeDifficulty <- 0 // Always zero
    attr.ShowlightsXML <- $"urn:application:xml:{dlcKey}_showlights"
    attr.SongAsset <- $"urn:application:musicgame-song:%s{dlcKey}_%s{name}"
    match arrangement with
    | Instrumental { CustomAudio = Some _ } ->
        attr.SongBank <- $"song_{dlcKey}_{name}.bnk"
        attr.SongEvent <- $"Play_{project.DLCKey}_{name}"
    | _ ->
        attr.SongBank <- $"song_{dlcKey}.bnk"
        attr.SongEvent <- $"Play_{project.DLCKey}"
    attr.SongXml <- $"urn:application:xml:{dlcKey}_{name}"

    attr

/// Initializes attributes that are common to instrumental arrangement headers and non-headers.
let private initSongCommon xmlMetaData (project: DLCProject) (instrumental: Instrumental) (sng: SNG) (attr: Attributes) =
    let diffHard, diffMed, diffEasy = calculateDifficulties xmlMetaData sng
    let dnaChords, dnaRiffs, dnaSolo = calculateDNAs sng

    attr.AlbumName <- project.AlbumName.Value
    attr.AlbumNameSort <- project.AlbumName.SortValue
    attr.ArtistName <- project.ArtistName.Value
    attr.ArtistNameSort <- project.ArtistName.SortValue
    if xmlMetaData.Capo > 0y then attr.CapoFret <- Nullable(float xmlMetaData.Capo)
    attr.CentOffset <- Utils.tuningPitchToCents instrumental.TuningPitch
    attr.DNA_Chords <- dnaChords
    attr.DNA_Riffs <- dnaRiffs
    attr.DNA_Solo <- dnaSolo
    attr.EasyMastery <- Math.Round(float sng.NoteCounts.Easy / float sng.NoteCounts.Hard, 9)
    attr.MediumMastery <- Math.Round(float sng.NoteCounts.Medium / float sng.NoteCounts.Hard, 9)
    attr.NotesEasy <- float32 sng.NoteCounts.Easy
    attr.NotesHard <- float32 sng.NoteCounts.Hard
    attr.NotesMedium <- float32 sng.NoteCounts.Medium
    attr.SongDiffEasy <- diffEasy
    attr.SongDiffHard <- diffHard
    attr.SongDiffMed <- diffMed
    attr.SongDifficulty <- diffHard
    attr.SongLength <- sng.MetaData.SongLength
    attr.SongName <- project.Title.Value
    attr.SongNameSort <- project.Title.SortValue
    attr.SongYear <- project.Year
    attr.Tuning <- Tuning.FromArray(instrumental.Tuning) |> Some

    attr

let private getToneName index tones =
    List.tryItem index tones
    |> Option.defaultValue String.Empty

/// Initializes attributes unique to instrumental arrangements (non-header).
let private initSongComplete (partition: int)
                             (xmlMetaData: XML.MetaData)
                             (project: DLCProject)
                             (instrumental: Instrumental)
                             (sng: SNG)
                             (attr: Attributes) =
    let tones =
        let toneKeysUsed =
            instrumental.BaseTone::instrumental.Tones
            |> Set.ofList
        project.Tones
        |> List.filter (fun t -> toneKeysUsed.Contains t.Key)
        |> List.toArray

    attr.ArrangementProperties <- Some (convertArrangementProperties xmlMetaData.ArrangementProperties instrumental)
    attr.ArrangementType <- LanguagePrimitives.EnumToValue instrumental.Name
    attr.Chords <- createChordMap sng
    attr.ChordTemplates <- convertChordTemplates sng
    attr.LastConversionDateTime <- sng.MetaData.LastConversionDateTime
    attr.MaxPhraseDifficulty <- (sng.Levels.Length - 1)
    attr.PhraseIterations <- convertPhraseIterations sng
    attr.Phrases <- convertPhrases sng
    attr.Score_MaxNotes <- float32 sng.NoteCounts.Hard
    attr.Score_PNV <- (100_000.f / float32 sng.NoteCounts.Hard)
    attr.Sections <- convertSections sng
    attr.SongAverageTempo <- if xmlMetaData.AverageTempo <= 0.f then 120.f else xmlMetaData.AverageTempo
    attr.SongOffset <- -sng.MetaData.StartTime
    attr.SongPartition <- partition
    attr.TargetScore <- 100_000
    attr.Techniques <- createTechniqueMap sng
    attr.Tone_A <- getToneName 0 instrumental.Tones
    attr.Tone_B <- getToneName 1 instrumental.Tones
    attr.Tone_Base <- instrumental.BaseTone
    attr.Tone_C <- getToneName 2 instrumental.Tones
    attr.Tone_D <- getToneName 3 instrumental.Tones
    attr.Tone_Multiplayer <- String.Empty
    attr.Tones <- tones |> Array.map Tone.toDto

    attr

type AttributesConversion =
    | FromVocals of Vocals
    | FromInstrumental of inst: Instrumental * sng: SNG

/// Creates attributes for an arrangement.
let private create isHeader (project: DLCProject) (conversion: AttributesConversion) =
    let dlcKey = project.DLCKey.ToLowerInvariant()
    let partition = Partitioner.create project

    match conversion with
    | FromVocals v ->
        let arr = Vocals v
        let _, name = partition arr
        let attr = initBase name dlcKey project arr

        if isHeader then
            attr
        else
            initAttributesCommon name dlcKey 0 project arr attr |> ignore
            // Attribute unique to vocals
            attr.InputEvent <- "Play_Tone_Standard_Mic"
            attr

    | FromInstrumental (inst, sng) ->
        let arr = Instrumental inst
        let part, name = partition arr
        let xmlMetaData = XML.MetaData.Read(inst.XML)

        let attr =
            initBase name dlcKey project arr
            |> initSongCommon xmlMetaData project inst sng

        if isHeader then
            // Attributes unique to header
            if inst.BassPicked then attr.BassPick <- Nullable 1
            attr.Representative <- if inst.Priority = ArrangementPriority.Main then 1 else 0
            attr.RouteMask <- LanguagePrimitives.EnumToValue inst.RouteMask
            attr
        else
            attr
            |> initAttributesCommon name dlcKey sng.Levels.Length project arr
            |> initSongComplete part xmlMetaData project inst sng

/// Creates manifest attributes for an arrangement.
let createAttributes = create false

/// Creates manifest header attributes for an arrangement.
let createAttributesHeader = create true
