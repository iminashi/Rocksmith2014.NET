module Rocksmith2014.Conversion.ConvertInstrumental

open Rocksmith2014.SNG.Types
open Rocksmith2014.XML
open Rocksmith2014.SNG
open Rocksmith2014.Conversion
open Rocksmith2014.Conversion.Utils
open System
open System.IO
open Nessos.Streams

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

let convertSngFileToXml fileName platform =
    let xml = SNGFile.readPacked fileName platform |> sngToXml
    let targetFile = Path.ChangeExtension(fileName, "xml")
    xml.Save targetFile

let xmlToSng (arr:InstrumentalArrangement) =
    let convertBeat = XmlToSng.convertBeat() arr
    let beats =
        arr.Ebeats
        |> Stream.ofResizeArray
        |> Stream.map convertBeat
        |> Stream.toArray

    let phrases =
        arr.Phrases
        |> Stream.ofResizeArray
        |> Stream.mapi (XmlToSng.convertPhrase arr)
        |> Stream.toArray

    let chords =
        arr.ChordTemplates
        |> Stream.ofResizeArray
        |> Stream.map (XmlToSng.convertChord arr)
        |> Stream.toArray

    let phraseIterations =
        arr.PhraseIterations
        |> Stream.ofResizeArray
        |> Stream.mapi (XmlToSng.convertPhraseIteration arr)
        |> Stream.toArray

    let NLDs =
        arr.NewLinkedDiffs
        |> Stream.ofResizeArray
        |> Stream.map XmlToSng.convertNLD
        |> Stream.toArray

    let events =
        arr.Events
        |> Stream.ofResizeArray
        |> Stream.map XmlToSng.convertEvent
        |> Stream.toArray

    let tones =
        arr.Tones.Changes
        |> Stream.ofResizeArray
        |> Stream.map XmlToSng.convertTone
        |> Stream.toArray

    let DNAs = XmlToSng.createDNAs arr

    let sections =
        arr.Sections
        |> Stream.ofResizeArray
        |> Stream.mapi (XmlToSng.convertSection arr)
        |> Stream.toArray

    let accuData = XmlToSng.AccuData.Init(arr)
    let convertLevel = XmlToSng.convertLevel accuData arr

    let levels =

        arr.Levels
        |> Stream.ofResizeArray
        |> Stream.map convertLevel
        |> Stream.toArray

    let metadata = XmlToSng.convertMetaData accuData arr

    { Beats = beats
      Phrases = phrases
      Chords = chords
      ChordNotes = [||]
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