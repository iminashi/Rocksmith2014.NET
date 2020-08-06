module Rocksmith2014.Conversion.ConvertInstrumental

open Rocksmith2014.XML
open Rocksmith2014.SNG
open Rocksmith2014.Common
open Rocksmith2014.Conversion
open Rocksmith2014.Conversion.Utils
open System

let private convertArrProps (arrProps: Manifest.ArrangementProperties) =
    let btb b = b = 1uy

    ArrangementProperties(
        Represent = btb arrProps.represent,
        BonusArrangement = btb arrProps.bonusArr,
        StandardTuning = btb arrProps.standardTuning,
        NonStandardChords = btb arrProps.nonStandardChords,
        BarreChords = btb arrProps.barreChords,
        PowerChords = btb arrProps.powerChords,
        DropDPower = btb arrProps.dropDPower,
        OpenChords = btb arrProps.openChords,
        FingerPicking = btb arrProps.fingerPicking,
        PickDirection = btb arrProps.pickDirection,
        DoubleStops = btb arrProps.doubleStops,
        PalmMutes = btb arrProps.palmMutes,
        Harmonics = btb arrProps.harmonics,
        PinchHarmonics = btb arrProps.pinchHarmonics,
        Hopo = btb arrProps.hopo,
        Tremolo = btb arrProps.tremolo,
        Slides = btb arrProps.slides,
        UnpitchedSlides = btb arrProps.unpitchedSlides,
        Bends = btb arrProps.bends,
        Tapping = btb arrProps.tapping,
        Vibrato = btb arrProps.vibrato,
        FretHandMutes = btb arrProps.fretHandMutes,
        SlapPop = btb arrProps.slapPop,
        TwoFingerPicking = btb arrProps.twoFingerPicking,
        FifthsAndOctaves = btb arrProps.fifthsAndOctaves,
        Syncopation = btb arrProps.syncopation,
        BassPick = btb arrProps.bassPick,
        Sustain = btb arrProps.sustain,
        PathLead = btb arrProps.pathLead,
        PathRhythm = btb arrProps.pathRhythm,
        PathBass = btb arrProps.pathBass)

/// Converts an SNG arrangement into an InstrumentalArrangement.
let sngToXml (attr: Manifest.Attributes option) (sng: SNG) =
    let phrases =
        mapToResizeArray SngToXml.convertPhrase sng.Phrases
    let phraseIterations =
        mapToResizeArray SngToXml.convertPhraseIteration sng.PhraseIterations
    let nld =
        mapToResizeArray SngToXml.convertNLD sng.NewLinkedDifficulties
    let chordTemplates =
        mapToResizeArray SngToXml.convertChordTemplate sng.Chords
    let beats =
        mapToResizeArray SngToXml.convertBeat sng.Beats
    let tones =
        mapToResizeArray (SngToXml.convertTone attr) sng.Tones
    let sections =
        mapToResizeArray SngToXml.convertSection sng.Sections
    let events =
        mapToResizeArray SngToXml.convertEvent sng.Events
    let levels =
        mapToResizeArray (SngToXml.convertLevel sng) sng.Levels
    let phraseProperties =
        mapToResizeArray SngToXml.convertPhraseExtraInfo sng.PhraseExtraInfo

    let metaData =
        match attr with
        | Some attr ->
            let m = MetaData(Arrangement = attr.ArrangementName,
                             CentOffset = int (attr.CentOffset.GetValueOrDefault()),
                             AverageTempo = attr.SongAverageTempo.GetValueOrDefault(),
                             Title = attr.SongName,
                             TitleSort = attr.SongNameSort,
                             ArtistName = attr.ArtistName,
                             ArtistNameSort = attr.ArtistNameSort,
                             AlbumName = attr.AlbumName,
                             AlbumNameSort = attr.AlbumNameSort,
                             AlbumYear = attr.SongYear.GetValueOrDefault())
            match attr.ArrangementProperties with
            | Some arrProps ->
                m.ArrangementProperties <- convertArrProps arrProps
            | None -> ()
                
            m
        | None -> MetaData()

    metaData.Part <- sng.MetaData.Part
    metaData.Capo <- Math.Max(sng.MetaData.CapoFretId, 0y)
    metaData.LastConversionDateTime <- sng.MetaData.LastConversionDateTime
    metaData.SongLength <- Utils.secToMs sng.MetaData.SongLength

    let arr = InstrumentalArrangement(
                MetaData = metaData,
                Ebeats = beats,
                Phrases = phrases,
                PhraseIterations = phraseIterations,
                PhraseProperties = phraseProperties,
                Sections = sections,
                NewLinkedDiffs = nld,
                Events = events,
                ChordTemplates = chordTemplates,
                Levels = levels,
                TranscriptionTrack = Level())
    Array.Copy (sng.MetaData.Tuning, arr.MetaData.Tuning.Strings, 6)
    arr.Tones.Changes <- tones

    attr |> Option.iter (fun attr ->
        if not <| String.IsNullOrWhiteSpace attr.Tone_Base then arr.Tones.BaseToneName <- attr.Tone_Base
        if not <| String.IsNullOrWhiteSpace attr.Tone_A then arr.Tones.Names.[0] <- attr.Tone_A
        if not <| String.IsNullOrWhiteSpace attr.Tone_B then arr.Tones.Names.[1] <- attr.Tone_B
        if not <| String.IsNullOrWhiteSpace attr.Tone_C then arr.Tones.Names.[2] <- attr.Tone_C
        if not <| String.IsNullOrWhiteSpace attr.Tone_D then arr.Tones.Names.[3] <- attr.Tone_D)

    arr

/// Adds the string masks from a section to the one before it.
let processStringMasks (stringMasks: int8[][]) (maxDiff: int) =
    for s = 0 to stringMasks.Length - 2 do
        for d = 0 to maxDiff - 1 do
            let mask = stringMasks.[s].[d]
            stringMasks.[s].[d] <- mask ||| stringMasks.[s + 1].[d]

let createPhraseIterationTimesArray (xml: InstrumentalArrangement) =
    Array.init (xml.PhraseIterations.Count + 1) (fun i ->
        if i = xml.PhraseIterations.Count then
            // Use song length as a sentinel
            xml.MetaData.SongLength
        else
            xml.PhraseIterations.[i].Time)

/// Converts an InstrumentalArrangement into SNG.
let xmlToSng (arr: InstrumentalArrangement) =
    let accuData = AccuData.Init(arr)
    let piTimes = createPhraseIterationTimesArray arr
    let convertBeat = XmlToSng.convertBeat() arr
    let convertLevel = XmlToSngLevel.convertLevel accuData piTimes arr

    let beats =
        arr.Ebeats |> mapToArray convertBeat
    let phrases =
        arr.Phrases |> mapiToArray (XmlToSng.convertPhrase arr)
    let phraseExtraInfo =
        if arr.PhraseProperties.Count = 0 then [||]
        else arr.PhraseProperties |> mapToArray XmlToSng.convertPhraseExtraInfo
    let chords =
        arr.ChordTemplates |> mapToArray (XmlToSng.convertChord arr)
    let phraseIterations =
        arr.PhraseIterations |> mapiToArray (XmlToSng.convertPhraseIteration piTimes)
    let NLDs =
        arr.NewLinkedDiffs |> mapToArray XmlToSng.convertNLD
    let events =
        arr.Events |> mapToArray XmlToSng.convertEvent
    let tones =
        arr.Tones.Changes |> mapToArray XmlToSng.convertTone
    let DNAs = XmlToSng.createDNAs arr
    let levels =
        //arr.Levels |> mapToArray convertLevel
        arr.Levels.ToArray()
        |> Array.Parallel.map convertLevel

    // For whatever reason, the string masks from a section need to be included in the section before it
    processStringMasks accuData.StringMasks arr.Levels.Count

    let firstNoteTime = 
        let mutable time = Single.MaxValue
        for level in levels do
            if level.Notes.Length > 0 && level.Notes.[0].Time < time then
                time <- level.Notes.[0].Time
        time

    let sections =
        arr.Sections |> mapiToArray (XmlToSng.convertSection accuData.StringMasks arr)
    let metadata = XmlToSng.createMetaData accuData firstNoteTime arr

    { Beats = beats
      Phrases = phrases
      Chords = chords
      ChordNotes = accuData.ChordNotes.ToArray()
      Vocals = [||]
      SymbolsHeaders = [||]
      SymbolsTextures = [||]
      SymbolDefinitions = [||]
      PhraseIterations = phraseIterations
      PhraseExtraInfo = phraseExtraInfo
      NewLinkedDifficulties = NLDs
      Actions = [||]
      Events = events
      Tones = tones
      DNAs = DNAs
      Sections = sections
      Levels = levels
      MetaData = metadata
      NoteCounts = accuData.NoteCounts.AsImmutable() }

/// Converts an SNG instrumental arrangement into an XML file.
let sngFileToXml sngFile targetFile platform =
    let xml = SNGFile.readPacked sngFile platform |> sngToXml None
    xml.Save targetFile

/// Converts an XML instrumental arrangement into an SNG file.
let xmlFileToSng xmlFile targetFile platform =
    let sng = InstrumentalArrangement.Load(xmlFile) |> xmlToSng
    SNGFile.savePacked targetFile platform sng
