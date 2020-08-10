module Rocksmith2014.DLCProject.Manifest.AttributesCreation

open Rocksmith2014
open Rocksmith2014.DLCProject
open Rocksmith2014.DLCProject.Manifest.Techniques
open Rocksmith2014.Common.Manifest
open Rocksmith2014.SNG
open System
open System.Collections.Generic

// There are two "iteration versions" of the attributes, version 2 and 3:
//
// Differences in version 3:
// -The attributes are sorted differently (e.g. arrangement properties are sorted alphabetically)
// -Whitespace differences
// -Chords section is (at least partially) fixed
// -The way DNA times are calculated seems to be different:
//    *Version 2: Sum of all regions of DNA ID to DNA None (or the end of the song)
//    *Version 3: Sum of all regions of DNA ID to different DNA ID (or the END phrase)

let private bti b = if b then 1 else 0
let private btb b = if b then 1uy else 0uy

let private getJapaneseVocal = function
    | Vocals v when v.Japanese -> Nullable(true)
    | _ -> Nullable()

/// Calculates the sum of all ranges of DNAId -> DNA None (or the end of the song).
let private getDNATime (sng: SNG) dnaId =
    let rec getTotal i time =
        // Find the index of a DNA with the given ID
        let di = Array.FindIndex(sng.DNAs, i, (fun x -> x.DnaId = dnaId))
        match di with
        | -1 -> time
        | next ->
            // Find the index of the next DNA None
            let ni = Array.FindIndex(sng.DNAs, next, (fun x -> x.DnaId = DNA.None))
            match ni with 
            | -1 ->
                time + (sng.MetaData.SongLength - sng.DNAs.[next].Time)
            | none ->
                // Find the next DNA ID -> DNA None range
                time + getTotal none (sng.DNAs.[none].Time - sng.DNAs.[next].Time)
        
    getTotal 0 0.f |> float

let private calculateDNAs (sng: SNG) =
    Math.Round(getDNATime sng DNA.Chord, 3),
    Math.Round(getDNATime sng DNA.Riff, 3),
    Math.Round(getDNATime sng DNA.Solo, 3)

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

let private convertPhraseIterations (sng: SNG) =
    sng.PhraseIterations
    |> Array.map (fun pi ->
        let phrase = sng.Phrases.[pi.PhraseId]
        { PhraseIndex = pi.PhraseId
          MaxDifficulty = phrase.MaxDifficulty
          Name = phrase.Name
          StartTime = pi.StartTime
          EndTime = pi.EndTime })

let private convertChordTemplates (sng: SNG) =
    sng.Chords
    |> Seq.indexed
    |> Seq.filter(fun (_, c) -> (not <| String.IsNullOrEmpty c.Name) && (c.Mask <> ChordMask.Arpeggio))
    |> Seq.map (fun (id, c) ->
        { ChordId = int16 id
          ChordName = c.Name
          Fingers = c.Fingers
          Frets = c.Frets })
    |> Seq.toArray

let private getSectionUIName (name: string) =
    // Official files may have names like "riff 1" or "solo7"
    let n = name |> String.filter Char.IsLetter

    match n with
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
    | "silence"    -> "$[34299] Silence [1]"
    | "solo"       -> "$[34300] Solo [1]"
    | "transition" -> "$[34301] Transition [1]"
    | "vamp"       -> "$[34302] Vamp [1]"
    | "variation"  -> "$[34303] Variation [1]"
    | "verse"      -> "$[34304] Verse [1]"
    | "tapping"    -> "$[34305] Tapping [1]"
    | "noguitar"   -> "$[6091] No Guitar [1]"
    | _            -> failwith "Unknown section name."

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
          IsSolo = s.Name.StartsWith("solo", StringComparison.Ordinal) })

let private convertPhrases (sng: SNG) =
    sng.Phrases
    |> Array.map (fun p ->
        { MaxDifficulty = int8 p.MaxDifficulty
          Name = p.Name
          IterationCount = p.IterationCount })

let private createDynamicVisualDensity (levels: int) (arrangement: Arrangement) =
    match arrangement with
    | Vocals -> Array.replicate 20 2.f

    | Instrumental inst ->
        let floorLimit = 0.5 // Fastest allowed speed
        let beginSpeed = 5.0
        let endSpeed = Math.Min(beginSpeed, Math.Max(floorLimit, float inst.ScrollSpeed / 10.0))
        let maxLevel = Math.Min(levels, 20) - 1
        let factor = if maxLevel > 0 then Math.Pow(endSpeed / beginSpeed, 1. / float maxLevel) else 1.

        Array.init 20 (fun i ->
            if i >= maxLevel then
                float32 endSpeed
            else
                float32 <| Math.Round(beginSpeed * Math.Pow(factor, float i), 1))

    | Showlights -> failwith "I am Error."

let private convertArrangementProperties (arrProps: XML.ArrangementProperties) (instrumental: Instrumental) =
    { represent = btb arrProps.Represent
      bonusArr = btb arrProps.BonusArrangement
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
      bassPick = btb arrProps.BassPick
      sustain = btb arrProps.Sustain
      pathLead = if instrumental.RouteMask = RouteMask.Lead then 1uy else 0uy
      pathRhythm = if instrumental.RouteMask = RouteMask.Rhythm then 1uy else 0uy
      pathBass = if instrumental.RouteMask = RouteMask.Bass then 1uy else 0uy
      routeMask = instrumental.RouteMask |> LanguagePrimitives.EnumToValue |> byte }

let private createChordMap (sng: SNG) =
    // Structure:
    //
    // "Difficulty level" : {
    //      "Phrase iteration index" : [
    //          Chord ID of a chord included in the manifest, i.e. a chord that has a name and not an arpeggio.
    //      ]
    //  }
    //
    // In iteration version 2, the way this section is generated for official files is very broken:
    //
    // -For a particular difficulty level, the chord IDs of the last phrase iteration are repeated for every phrase iteration.
    // -Some phrase iterations that have no chords or notes in that difficulty level are included.
    // -Empty arrays may be included.

    let chords = Dictionary<string, Dictionary<string, int array>>()

    for lvl = 0 to sng.Levels.Length - 1 do
        let diffIds = Dictionary<string, int array>()
        for i = 0 to sng.PhraseIterations.Length - 1 do
            let pi = sng.PhraseIterations.[i]
            let chordIds = 
                sng.Levels.[lvl].HandShapes
                |> Seq.filter (fun x -> 
                   (not <| String.IsNullOrEmpty sng.Chords.[x.ChordId].Name) && (x.StartTime >= pi.StartTime && x.StartTime < pi.EndTime))
                |> Seq.map (fun x -> x.ChordId)
                |> Set.ofSeq
            if chordIds.Count > 0 then
                diffIds.Add(i.ToString(), chordIds |> Set.toArray)

        if diffIds.Count > 0 then
            chords.Add(lvl.ToString(), diffIds)
            
    chords

let private getTechniques (sng: SNG) (note: Note) =
    if note.Mask = NoteMask.None || note.Mask = NoteMask.Single then
        Seq.empty
    else
        let isHopo = hasFlag note NoteMask.HammerOn || hasFlag note NoteMask.PullOff
        let isPower = isPowerChord sng note

        // Some technique numbers don't seem to match the description in the lesson technique database.
        // The technique database has 45 as chord + fret hand mute, but that is already tech #2.

        // This method will miss some techniques when they are on chord notes, otherwise it should be pretty complete.

        seq { // 0: Accent
              if hasFlag note NoteMask.Accent then 0

              // 1: Bend
              if hasFlag note NoteMask.Bend then 1

              // 2: Fret-hand mute (chords only)
              if hasFlag note NoteMask.FretHandMute then 2

              // 3: Hammer-on
              if hasFlag note NoteMask.HammerOn then 3

              // 4: Harmonic
              if hasFlag note NoteMask.Harmonic then 4

              // 5: Pinch harmonic
              if hasFlag note NoteMask.PinchHarmonic then 5

              // 6: HOPO (not in technique database)
              if isHopo then 6

              // 7: Palm mute
              if hasFlag note NoteMask.PalmMute then 7

              // 8: Pluck aka Pop
              if hasFlag note NoteMask.Pluck then 8

              // 9: Pull-off
              if hasFlag note NoteMask.PullOff then 9

              // 10: Slap
              if hasFlag note NoteMask.Slap then 10

              // 11: Slide
              if hasFlag note NoteMask.Slide then 11

              // 12: Unpitched slide
              if hasFlag note NoteMask.UnpitchedSlide then 12

              // 13: Sustain (single notes)
              if hasFlag note NoteMask.Single && hasFlag note NoteMask.Sustain then 13

              // 14: Tap
              if hasFlag note NoteMask.Tap then 14

              // 15: Tremolo
              if hasFlag note NoteMask.Tremolo then 15

              // 16: Vibrato
              if hasFlag note NoteMask.Vibrato then 16

              // 17: Palm mute + accent
              if hasFlag note (NoteMask.PalmMute ||| NoteMask.Accent) then 17

              // 18: Palm mute + harmonic
              if hasFlag note (NoteMask.PalmMute ||| NoteMask.Harmonic) then 18

              // 19: Palm mute + hammer-on
              if hasFlag note (NoteMask.PalmMute ||| NoteMask.HammerOn) then 19

              // 20: Palm mute + pull off
              if hasFlag note (NoteMask.PalmMute ||| NoteMask.PullOff) then 20

              // 21: Fret hand mute + accent
              if hasFlag note (NoteMask.FretHandMute ||| NoteMask.Accent) then 21

              // 26: Tremolo + bend
              if hasFlag note (NoteMask.Bend ||| NoteMask.Tremolo) then 26

              // 27: Tremolo + slide
              if hasFlag note (NoteMask.Tremolo ||| NoteMask.Slide) then 27

              // 28: Tremolo + vibrato (used on pick scratches)
              if hasFlag note (NoteMask.Tremolo ||| NoteMask.Vibrato) then 28

              // 29: Pre-bend
              if isPreBend note then 29

              // 30: Oblique bend (compound bend in technique database)
              if isObliqueBend sng note then 30

              // 31: Compound bend (oblique bend in technique database)
              if isCompoundBend note then 31

              // 33: Double stop with adjacent strings
              if not isPower && isDoubleStopAdjacentStrings sng note then 33

              // 34: Double stop with nonadjacent strings
              if not isPower && isDoubleStopNonAdjacentStrings sng note then 34

              // 35: Two string power chord
              if isPower then 35

              // 36: Drop-D power chord (not in technique database)
              if isDropDPower sng note then 36

              // 37: Barre chord
              if isBarre sng note then 37

              // 38: Chord (with three or more strings, no sustain)
              if isChord note then 38

              // 40: Double stop HOPO (actually HOPO inside hand shape)
              if isHopo && note.FingerPrintId.[0] <> -1s then 40

              // 41: Chord slide (chord HOPO in technique database)
              if isChordSlide sng note then 41

              // 42: Chord tremolo (double stop slide in technique database)
              if isChordTremolo sng note then 42

              // 43: Chord HOPO (chord slide in technique database) 
              if isChordHammerOn sng note then 43

              // 44: Double stop slide (chord tremolo in technique database)
              if isDoubleStopSlide sng note then 44

              // 45: Double stop tremolo (double stop bend in technique database)
              if isDoubleStopTremolo sng note then 45

              // 46: Double stop bend (double stop tremolo in technique database)
              if isDoubleStopBend sng note then 46 }

              // Not used:
              // 22: Fret hand mute + pop
              // 23: Fret hand mute + slap (found in "Living in America", but not included in the tech map)
              // 24: Harmonic + pop
              // 25: Harmonic + slap
              // 32: Arpeggio
              // 39: Unknown (not included in technique database, but could be "chord zone" or "non-standard chord")

let private createTechniqueMap (sng: SNG) =
    // The structure of the map is the same as the chord map, but with technique IDs instead of chord IDs.

    // In official files, the techniques of the last phrase iteration in a difficulty level seem to be missing,
    // Or they are included in the first phrase iteration in the next level.

    let techniques = Dictionary<string, Dictionary<string, int array>>()

    for lvl = 0 to sng.Levels.Length - 1 do
        let diffIds = Dictionary<string, int array>()
        for i = 0 to sng.PhraseIterations.Length - 2 do
            let pi = sng.PhraseIterations.[i]
            let techIds = 
                sng.Levels.[lvl].Notes
                |> Seq.filter (fun x -> (x.Time > pi.StartTime && x.Time <= pi.EndTime)) // Weird division into phrase iterations intentional
                |> Seq.collect (getTechniques sng)
                |> Set.ofSeq
            if techIds.Count > 0 then
                diffIds.Add(i.ToString(), techIds |> Set.toArray)

        if diffIds.Count > 0 then
            techniques.Add(lvl.ToString(), diffIds)
            
    techniques

let private initBase name dlcKey (project: DLCProject) (arrangement: Arrangement) (attr: Attributes) =
    attr.AlbumArt <- sprintf "urn:image:dds:album_%s" dlcKey
    attr.ArrangementName <- Arrangement.getName arrangement true
    attr.DLCKey <- project.DLCKey
    attr.JapaneseArtistName <- Option.toObj project.JapaneseArtistName
    attr.JapaneseSongName <- Option.toObj project.JapaneseTitle
    attr.JapaneseVocal <- getJapaneseVocal arrangement
    attr.ManifestUrn <- sprintf "urn:database:json-db:%s_%s" dlcKey name
    attr.MasterID_RDV <- Arrangement.getMasterId arrangement
    attr.PersistentID <- (Arrangement.getPersistentId arrangement).ToString("N").ToUpperInvariant()
    attr.SongKey <- project.DLCKey

    attr

let private initAttributesCommon name dlcKey levels (project: DLCProject) (arrangement: Arrangement) (attr: Attributes) =
    attr.ArrangementSort <- 0 |> Nullable // Always zero
    attr.BlockAsset <- sprintf "urn:emergent-world:%s" dlcKey
    attr.DynamicVisualDensity <- createDynamicVisualDensity levels arrangement
    attr.FullName <- sprintf "%s_%s" project.DLCKey (Arrangement.getName arrangement false)
    attr.MasterID_PS3 <- -1 |> Nullable
    attr.MasterID_XBox360 <- -1 |> Nullable
    attr.PreviewBankPath <- sprintf "song_%s_preview.bnk" dlcKey
    attr.RelativeDifficulty <- Nullable(0) // Always zero
    attr.ShowlightsXML <- sprintf "urn:application:xml:%s_showlights" dlcKey
    attr.SongAsset <- sprintf "urn:application:musicgame-song:%s_%s" dlcKey name
    attr.SongBank <- sprintf "song_%s.bnk" dlcKey
    attr.SongEvent <- sprintf "Play_%s" project.DLCKey
    attr.SongXml <- sprintf "urn:application:xml:%s_%s" dlcKey name

    attr

let private initSongCommon xmlMetaData (project: DLCProject) (sng: SNG) (attr: Attributes) =
    let diffHard, diffMed, diffEasy = calculateDifficulties xmlMetaData sng
    let dnaChords, dnaRiffs, dnaSolo = calculateDNAs sng

    attr.AlbumName <- project.AlbumName
    attr.AlbumNameSort <- project.AlbumNameSort
    attr.ArtistName <- project.ArtistName
    attr.ArtistNameSort <- project.ArtistNameSort
    attr.CentOffset <- project.CentOffset |> Nullable
    attr.DNA_Chords <- dnaChords |> Nullable
    attr.DNA_Riffs <- dnaRiffs |> Nullable
    attr.DNA_Solo <- dnaSolo |> Nullable
    attr.EasyMastery <- Math.Round(float sng.NoteCounts.Easy / float sng.NoteCounts.Hard, 9) |> Nullable
    attr.MediumMastery <- Math.Round(float sng.NoteCounts.Medium / float sng.NoteCounts.Hard, 9) |> Nullable
    attr.NotesEasy <- float32 sng.NoteCounts.Easy |> Nullable
    attr.NotesHard <- float32 sng.NoteCounts.Hard |> Nullable
    attr.NotesMedium <- float32 sng.NoteCounts.Medium |> Nullable
    attr.SongDiffEasy <- diffEasy |> Nullable
    attr.SongDiffHard <- diffHard |> Nullable
    attr.SongDiffMed <- diffMed |> Nullable
    attr.SongDifficulty <- diffHard |> Nullable
    attr.SongLength <- sng.MetaData.SongLength |> Nullable
    attr.SongName <- project.Title
    attr.SongNameSort <- project.TitleSort
    attr.SongYear <- project.Year |> Nullable
    attr.Tuning <- Tuning.FromArray(xmlMetaData.Tuning.Strings) |> Some

    attr

let private initSongComplete partition
                             (xmlMetaData: XML.MetaData)
                             (xmlToneInfo: XML.ToneInfo)
                             (project: DLCProject)
                             (instrumental: Instrumental)
                             (sng: SNG)
                             (attr: Attributes) =
    let tones = 
        let toneNamesUsed =
            seq { xmlToneInfo.BaseToneName; yield! xmlToneInfo.Names |> Seq.filter (isNull >> not) }
            |> Set.ofSeq
        project.Tones
        |> List.filter (fun t -> toneNamesUsed.Contains t.Name)
        |> List.toArray

    attr.ArrangementProperties <- Some (convertArrangementProperties xmlMetaData.ArrangementProperties instrumental)
    attr.ArrangementType <- instrumental.ArrangementName |> LanguagePrimitives.EnumToValue |> Nullable
    attr.Chords <- createChordMap sng
    attr.ChordTemplates <- convertChordTemplates sng
    attr.LastConversionDateTime <- sng.MetaData.LastConversionDateTime
    attr.MaxPhraseDifficulty <- (sng.Levels.Length - 1) |> Nullable
    attr.PhraseIterations <- convertPhraseIterations sng
    attr.Phrases <- convertPhrases sng
    attr.Score_MaxNotes <- sng.NoteCounts.Hard |> float32 |> Nullable
    attr.Score_PNV <- (100000.f / float32 sng.NoteCounts.Hard) |> Nullable
    attr.Sections <- convertSections sng
    attr.SongAverageTempo <- xmlMetaData.AverageTempo |> Nullable
    attr.SongOffset <- -sng.MetaData.StartTime |> Nullable
    attr.SongPartition <- partition |> Nullable
    attr.TargetScore <- 100000 |> Nullable
    attr.Techniques <- createTechniqueMap sng
    attr.Tone_A <- if isNull xmlToneInfo.Names.[0] then String.Empty else xmlToneInfo.Names.[0]
    attr.Tone_B <- if isNull xmlToneInfo.Names.[1] then String.Empty else xmlToneInfo.Names.[1]
    attr.Tone_Base <- if isNull xmlToneInfo.BaseToneName then String.Empty else xmlToneInfo.BaseToneName
    attr.Tone_C <- if isNull xmlToneInfo.Names.[2] then String.Empty else xmlToneInfo.Names.[2]
    attr.Tone_D <- if isNull xmlToneInfo.Names.[3] then String.Empty else xmlToneInfo.Names.[3]
    attr.Tone_Multiplayer <- String.Empty
    attr.Tones <- tones

    attr

type AttributesConversion =
| FromVocals of Vocals
| FromInstrumental of inst: Instrumental * sng: SNG

let private create isHeader (project: DLCProject) (conversion: AttributesConversion) =
    let attributes = Attributes()
    let dlcKey = project.DLCKey.ToLowerInvariant()
    let partition = Partitioner.create project

    match conversion with
    | FromVocals v ->
        let arr = Vocals v
        let name = partition arr |> snd
        let attr = initBase name dlcKey project arr attributes

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
            initBase name dlcKey project arr attributes
            |> initSongCommon xmlMetaData project sng

        if isHeader then
            // Attributes unique to header
            attr.BassPick <- if xmlMetaData.ArrangementProperties.BassPick then Nullable(1) else Nullable()
            attr.Representative <- xmlMetaData.ArrangementProperties.Represent |> bti |> Nullable
            attr.RouteMask <- inst.RouteMask |> LanguagePrimitives.EnumToValue |> Nullable
            attr
        else
            let toneInfo = XML.InstrumentalArrangement.ReadToneNames(inst.XML)
            attr
            |> initAttributesCommon name dlcKey sng.Levels.Length project arr
            |> initSongComplete part xmlMetaData toneInfo project inst sng

let createAttributes = create false
let createAttributesHeader = create true
