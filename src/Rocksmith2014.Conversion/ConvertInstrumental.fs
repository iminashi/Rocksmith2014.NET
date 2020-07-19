module Rocksmith2014.Conversion.ConvertInstrumental

open Rocksmith2014.SNG.Types
open Rocksmith2014.XML
open Rocksmith2014.SNG
open Rocksmith2014.Conversion
open Rocksmith2014.Conversion.Utils
open System
open System.IO

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
