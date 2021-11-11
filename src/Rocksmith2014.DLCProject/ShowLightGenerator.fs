module Rocksmith2014.DLCProject.ShowLightGenerator

open Rocksmith2014.XML
open Rocksmith2014.SNG
open Rocksmith2014.Common
open Rocksmith2014.Conversion
open System.Collections.Generic
open System.Runtime.CompilerServices
open System

[<IsReadOnly; Struct>]
type private MidiNote = { Time: float32; Note: int }

let private toMs secs = int (secs * 1000.f)

let private tryFindSoloSection =
    Array.tryFind (fun (x: Section) -> String.startsWith "solo" x.Name)

let private isWithinPhraseIteration (pi: PhraseIteration) (note: Note) =
    note.Time >= pi.StartTime && note.Time < pi.EndTime

let private isBeam (x: ShowLight) = x.IsBeam()
let private isFog (x: ShowLight) = x.IsFog()

/// Creates a MIDI note from an SNG note.
let private toMidiNote (sng: SNG) (note: Note) =
    let midiValue =
        match note.ChordId with
        | -1 ->
            Midi.toMidiNote (int note.StringIndex) note.Fret sng.MetaData.Tuning sng.MetaData.CapoFretId false
        | chordId ->
            sng.Chords[chordId].Notes
            |> Array.tryFind (fun x -> x > 0)
            |> Option.defaultValue 0

    { Time = note.Time; Note = midiValue }

/// Creates MIDI notes of all the notes in the highest difficulty level.
let private getMidiNotes (sng: SNG) =
    sng.PhraseIterations
    |> Array.collect (fun pi ->
        let maxDifficulty = sng.Phrases[pi.PhraseId].MaxDifficulty

        sng.Levels[maxDifficulty].Notes
        |> Array.filter (isWithinPhraseIteration pi)
        |> Array.map (toMidiNote sng))

/// Returns a random fog note.
let rec private getRandomFogNote (excludeNote: byte) =
    let fog =
        RandomGenerator.nextInRange (int ShowLight.FogMin) (int ShowLight.FogMax + 1)
        |> byte

    if fog = excludeNote then
        getRandomFogNote excludeNote
    else
        fog

/// Returns a beam note matching the MIDI note.
let private getBeamNote (midiNote: int) =
    byte <| ShowLight.BeamMin + (byte midiNote % 12uy)

let private getFogNoteForSection () =
    let sectionFogNotes = Dictionary<string, byte>()

    fun (current: ShowLight list) time sectionName ->
        let note =
            sectionFogNotes
            |> Dictionary.tryGetValue sectionName
            |> Option.defaultWith (fun () ->
                let prevNote =
                    match List.tryHead current with
                    | Some x -> x.Note
                    | None -> 0uy

                let note = getRandomFogNote prevNote
                sectionFogNotes[sectionName] <- note
                note)

        ShowLight(toMs time, note)

/// Generates fog notes from the sections in the SNG.
let generateFogNotes (sng: SNG) =
    let getFog = getFogNoteForSection ()

    ((String.Empty, []), sng.Sections)
    ||> Array.fold (fun (prevName, acc) { Name=name; StartTime=startTime } ->
        name,
        if name = prevName then
            acc
        else
            (getFog acc startTime name) :: acc)
    |> snd

/// Generates beam notes from the notes in the SNG.
let generateBeamNotes (sng: SNG) =
    let minTime = 0.35f

    ([], getMidiNotes sng)
    ||> Array.fold (fun acc midi ->
        let beamNote = getBeamNote midi.Note

        match acc with
        | (prevTime, prevNote) :: _ when midi.Time - prevTime >= minTime && beamNote <> prevNote ->
            (midi.Time, beamNote) :: acc
        | [] ->
            [ midi.Time, beamNote ]
        | list ->
            list)
    |> List.map (fun (time, beam) -> ShowLight(toMs time, beam))

/// Generates laser notes from the SNG.
let generateLaserNotes (sng: SNG) =
    (* The lasers will be enabled at the first solo section, if one is present.
       If there are no solo sections, the lasers are set on at 60% into the song. *)
    let lasersOn =
        let time =
            match tryFindSoloSection sng.Sections with
            | Some soloSection ->
                soloSection.StartTime
            | None ->
                sng.MetaData.SongLength * 0.6f

        ShowLight(toMs time, ShowLight.LasersOn)

    let lasersOff =
        ShowLight(toMs (sng.MetaData.SongLength - 5.0f), ShowLight.LasersOff)

    [ lasersOn; lasersOff ]

/// Ensures that the show lights contain at least one beam and one fog note.
let private validateShowLights songLength (slList: ShowLight list) =
    slList
    @ [ if not <| List.exists isBeam slList then
            ShowLight(0, ShowLight.BeamMin)
        if not <| List.exists isFog slList then
            ShowLight(0, ShowLight.FogMin)
        // Add an extra fog note at the end to prevent a glitch
        ShowLight(toMs songLength, ShowLight.FogMax) ]

/// Generates show lights.
let generate (sngs: (Arrangement * SNG) list) =
    // Select an instrumental arrangement to generate the show lights from
    let _, sng =
        sngs
        |> List.tryFind (function
            | Instrumental i, _ ->
                i.RouteMask = RouteMask.Lead
            | _ ->
                false)
        |> Option.defaultWith (fun () ->
            sngs
            |> List.tryFind (function Instrumental _, _ -> true | _ -> false)
            |> Option.defaultWith (fun () ->
                failwith "An instrumental arrangement is required for generating showlights."))

    [ yield! generateFogNotes sng
      yield! generateBeamNotes sng
      yield! generateLaserNotes sng ]
    |> validateShowLights sng.MetaData.SongLength
    |> List.sortBy (fun x -> x.Time)

/// Generates show lights and saves them into the target file.
let generateFile (targetFile: string) (sngs: (Arrangement * SNG) list) =
    ShowLights.Save(targetFile, ResizeArray(generate sngs))
