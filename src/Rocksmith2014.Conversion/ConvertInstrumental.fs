module Rocksmith2014.Conversion.ConvertInstrumental

open Rocksmith2014.SNG.Types
open Rocksmith2014.XML
open Rocksmith2014.SNG
open Rocksmith2014.Conversion
open Rocksmith2014.Conversion.Utils
open System

let sngToXml (sng:SNG) =
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
                Capo = sng.MetaData.CapoFretId,
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

let convertSngFileToXml sngFile targetFile platform =
    let xml = SNGFile.readPacked sngFile platform |> sngToXml
    xml.Save targetFile

let xmlToSng (arr:InstrumentalArrangement) =
    let accuData = XmlToSng.AccuData.Init(arr)
    let convertBeat = XmlToSng.convertBeat() arr
    let convertLevel = XmlToSng.convertLevel accuData arr

    let beats =
        arr.Ebeats |> mapToArray convertBeat
    let phrases =
        arr.Phrases |> mapiToArray (XmlToSng.convertPhrase arr)
    let chords =
        arr.ChordTemplates |> mapToArray (XmlToSng.convertChord arr)
    let phraseIterations =
        arr.PhraseIterations |> mapiToArray (XmlToSng.convertPhraseIteration arr)
    let NLDs =
        arr.NewLinkedDiffs |> mapToArray XmlToSng.convertNLD
    let events =
        arr.Events |> mapToArray XmlToSng.convertEvent
    let tones =
        arr.Tones.Changes |> mapToArray XmlToSng.convertTone
    let DNAs = XmlToSng.createDNAs arr
    let sections =
        arr.Sections |> mapiToArray (XmlToSng.convertSection accuData.StringMasks arr)
    let levels =
        arr.Levels |> mapToArray convertLevel
    let metadata = XmlToSng.convertMetaData accuData arr

    { Beats = beats
      Phrases = phrases
      Chords = chords
      ChordNotes = accuData.ChordNotes.ToArray()
      Vocals = [||]
      SymbolsHeaders = [||]
      SymbolsTextures = [||]
      SymbolDefinitions = [||]
      PhraseIterations = phraseIterations
      PhraseExtraInfo = [||] // TODO: implement
      NewLinkedDifficulties = NLDs
      Actions = [||]
      Events = events
      Tones = tones
      DNAs = DNAs
      Sections = sections
      Levels = levels
      MetaData = metadata }

let convertXmlFileToSng xmlFile targetFile platform =
    let sng = InstrumentalArrangement.Load(xmlFile) |> xmlToSng
    SNGFile.savePacked targetFile platform sng
