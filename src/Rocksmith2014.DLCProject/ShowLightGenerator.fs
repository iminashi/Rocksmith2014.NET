module Rocksmith2014.DLCProject.ShowLightGenerator

open Rocksmith2014.XML
open Rocksmith2014.SNG
open Rocksmith2014.Common
open Rocksmith2014.Conversion
open System.Collections.Generic
open System

type private MidiNote = { Time: float32; Note: int }

let private toMs secs = int (secs * 1000.f)

let private tryFindSoloSection =
    Array.tryFind (fun (x: Section) -> x.Name.StartsWith("solo", StringComparison.OrdinalIgnoreCase))

/// Creates a MIDI note from an SNG note.
let private toMidiNote (sng: SNG) (note: Note) =
    let mn =
        if note.ChordId = -1 then
            Midi.toMidiNote (int note.StringIndex) note.FretId sng.MetaData.Tuning sng.MetaData.CapoFretId false
        else
            match sng.Chords.[note.ChordId].Notes |> Array.tryFind (fun x -> x > 0) with
            | Some n -> n
            | None -> 0
    { Time = note.Time; Note = mn }

/// Creates MIDI notes of all the notes in the highest difficulty level.
let private getMidiNotes (sng: SNG) =
    let midiNotes = ResizeArray<MidiNote>()
    for i = 1 to sng.PhraseIterations.Length - 2 do
        let phraseIteration = sng.PhraseIterations.[i]
        let phraseId = phraseIteration.PhraseId
        let maxDifficulty = sng.Phrases.[phraseId].MaxDifficulty
        if maxDifficulty > 0 then
            let phraseStartTime = phraseIteration.StartTime
            let phraseEndTime = phraseIteration.EndTime
            let highestLevelForPhrase = sng.Levels.[maxDifficulty]

            let phraseIterationMidiNotes = 
                highestLevelForPhrase.Notes
                |> Array.filter (fun n -> n.Time >= phraseStartTime && n.Time < phraseEndTime)
                |> Array.map (toMidiNote sng)

            midiNotes.AddRange(phraseIterationMidiNotes)
    midiNotes

/// Returns a random fog note.
let rec private getRandomFogNote (excludeNote: byte) =
    let fog = byte <| RandomGenerator.nextInRange (int ShowLight.FogMin) (int ShowLight.FogMax + 1)
    if fog = excludeNote then
        getRandomFogNote excludeNote
    else
        fog

/// Returns a beam note matching the MIDI note.
let private getBeamNote (midiNote: int) = byte <| ShowLight.BeamMin + (byte midiNote % 12uy)

/// Generates fog notes from the sections in the SNG.
let private generateFogNotes (sng: SNG) =
    let sections = sng.Sections
    let sectionFogNotes = Dictionary<string, byte>()
    let mutable prevSectionName = String.Empty
    let mutable prevNote = 0uy
    [ for section in sections do
        let name = section.Name
        if name <> prevSectionName then
            let fogNote =
                if sectionFogNotes.ContainsKey name then
                    sectionFogNotes.[name]
                else
                    let fog = getRandomFogNote prevNote
                    sectionFogNotes.[name] <- fog
                    fog
            yield ShowLight(toMs section.StartTime, fogNote)
            prevNote <- fogNote
        prevSectionName <- name ]

/// Generates beam notes from the notes in the SNG.
let private generateBeamNotes (sng: SNG) =
    let minTime = 0.35f

    ([], getMidiNotes sng)
    ||> Seq.fold (fun acc midi ->
        let beamNote = getBeamNote midi.Note
        match acc with
        | (prevTime, prevNote)::_ when midi.Time - prevTime >= minTime && beamNote <> prevNote ->
            (midi.Time, beamNote)::acc
        | [] ->
            [ midi.Time, beamNote ]
        | list ->
            list)
    |> List.map (fun (time, beam) -> ShowLight(toMs time, beam))

/// Generates laser notes from the SNG.
let private generateLaserNotes (sng: SNG) =
    // The lasers will be enabled at the first solo section, if one is present.
    // If there is no solo sections, the lasers are set on at 60% into the song.
    let lasersOn =
        let time =
            match tryFindSoloSection sng.Sections with
            | Some soloSection -> soloSection.StartTime
            | None -> sng.MetaData.SongLength * 0.6f
        ShowLight(toMs time, ShowLight.LasersOn)

    let lasersOff =
        ShowLight(toMs (sng.MetaData.SongLength - 5.0f), ShowLight.LasersOff)

    [ lasersOn; lasersOff ]

let private isBeam (x: ShowLight) = x.IsBeam()
let private isFog (x: ShowLight) = x.IsFog()

/// Ensures that the show lights contain at least one beam and one fog note.
let private validateShowLights songLength (slList: ShowLight list) =
    slList @ [
        if not <| List.exists isBeam slList then ShowLight(0, ShowLight.BeamMin)
        if not <| List.exists isFog slList then ShowLight(0, ShowLight.FogMin)
        // Add an extra fog note at the end to prevent a glitch
        ShowLight(toMs songLength, ShowLight.FogMax) ]

/// Generates show lights and saves them into the target file.
let generate (targetFile: string) (sngs: (Arrangement * SNG) list) =
    // Select an instrumental arrangement to generate the show lights from
    let _, sng =
        sngs
        |> List.tryFind (function
            | Instrumental i, _ -> i.RouteMask = RouteMask.Lead
            | _ -> false)
        |> Option.defaultWith (fun () ->
            sngs
            |> List.find (function Instrumental _, _ -> true | _ -> false))

    let showlights = 
        [ yield! generateFogNotes sng
          yield! generateBeamNotes sng
          yield! generateLaserNotes sng ]
        |> validateShowLights sng.MetaData.SongLength
        |> List.sortBy (fun x -> x.Time)
        |> ResizeArray

    ShowLights.Save(targetFile, showlights)
