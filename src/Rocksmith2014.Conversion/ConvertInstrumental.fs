module Rocksmith2014.Conversion.ConvertInstrumental

open Rocksmith2014.XML
open Rocksmith2014.SNG
open Rocksmith2014.Conversion
open Rocksmith2014.Conversion.Utils
open System

/// Converts an SNG arrangement into an InstrumentalArrangement.
let sngToXml (sng: SNG) =
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
        mapToResizeArray SngToXml.convertTone sng.Tones
    let sections =
        mapToResizeArray SngToXml.convertSection sng.Sections
    let events =
        mapToResizeArray SngToXml.convertEvent sng.Events
    let levels =
        mapToResizeArray (SngToXml.convertLevel sng) sng.Levels
    let phraseProperties =
        mapToResizeArray SngToXml.convertPhraseExtraInfo sng.PhraseExtraInfo

    let arr = InstrumentalArrangement(
                Part = sng.MetaData.Part,
                Capo = Math.Max(sng.MetaData.CapoFretId, 0y),
                LastConversionDateTime = sng.MetaData.LastConversionDateTime,
                SongLength = Utils.secToMs sng.MetaData.SongLength,
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
    Array.Copy (sng.MetaData.Tuning, arr.Tuning.Strings, 6)
    arr.Tones.Changes <- tones

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
            xml.SongLength
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
        arr.Levels |> mapToArray convertLevel

    // For whatever reason, the string masks from a section need to be included in the section before it
    processStringMasks accuData.StringMasks arr.Levels.Count

    let sections =
        arr.Sections |> mapiToArray (XmlToSng.convertSection accuData.StringMasks arr)
    let metadata = XmlToSng.createMetaData accuData arr

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
      MetaData = metadata }

/// Converts an SNG instrumental arrangement into an XML file.
let sngFileToXml sngFile targetFile platform =
    let xml = SNGFile.readPacked sngFile platform |> sngToXml
    xml.Save targetFile

/// Converts an XML instrumental arrangement into an SNG file.
let xmlFileToSng xmlFile targetFile platform =
    let sng = InstrumentalArrangement.Load(xmlFile) |> xmlToSng
    SNGFile.savePacked targetFile platform sng
