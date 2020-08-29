module Rocksmith2014.DLCProject.ShowLightGenerator

open Rocksmith2014.XML
open Rocksmith2014.SNG
open Rocksmith2014.Common
open Rocksmith2014.Conversion
open System.Collections.Generic
open System

type private MidiNote = { Time: float32; Note: int }

let private toMidiNote (sng: SNG) (note: Note) =
    let mn =
        if note.ChordId = -1 then
            Midi.toMidiNote (int note.StringIndex) note.FretId sng.MetaData.Tuning sng.MetaData.CapoFretId false
        else
            sng.Chords.[note.ChordId].Notes
            |> Array.find ((>) 0)
    { Time = note.Time; Note = mn }

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

let rec private getRandomFogNote (excludeNote: byte) =
    let fog = byte <| RandomGenerator.nextInRange (int ShowLight.FogMin) (int ShowLight.FogMax + 1)
    if fog = excludeNote then
        getRandomFogNote excludeNote
    else
        fog

let private getBeamNote (midiNote: int) = byte <| ShowLight.BeamMin + (byte midiNote % 12uy)

let private generateFogNotes (sng: SNG) =
    let sections = sng.Sections
    let sectionFogNotes = Dictionary<string, byte>()
    let mutable prevSectionName = String.Empty
    let mutable prevNote = 0uy
    seq { for section in sections do
            let name = section.Name
            if name <> prevSectionName then
                let startTime = section.StartTime
                let fogNote =
                    if sectionFogNotes.ContainsKey(name) then
                        sectionFogNotes.[name]
                    else
                        let fog = getRandomFogNote prevNote
                        sectionFogNotes.[name] <- fog
                        fog
                yield ShowLight(int (startTime  * 1000.f), fogNote)
                prevNote <- fogNote
            prevSectionName <- name }

let private generateBeamNotes (sng: SNG) =
    let minTime = 0.35f
    let midiNotes = getMidiNotes sng
    let mutable prevNote = 0uy
    let mutable prevTime = 0.f
    
    seq { for midiNote in midiNotes do
            if midiNote.Time - prevTime >= minTime then
                let beamNote = getBeamNote midiNote.Note
                if beamNote <> prevNote then
                    yield ShowLight(int (midiNote.Time * 1000.f), beamNote)
                prevNote <- beamNote
                prevTime <- midiNote.Time }

let private generateLaserNotes (sng: SNG) =
    let soloSection =
        sng.Sections
        |> Array.tryFind (fun x -> x.Name = "solo")
    let lasersOn =
        match soloSection with
        | Some s -> ShowLight(int (s.StartTime * 1000.f), ShowLight.LasersOn)
        // No solo sections, set lasers on at 60% into the song
        | None -> ShowLight(int (sng.MetaData.SongLength * 0.6f * 1000.f), ShowLight.LasersOn)
    let lasersOff =
        ShowLight(int ((sng.MetaData.SongLength - 5.0f) * 1000.f), ShowLight.LasersOff)

    seq { lasersOn; lasersOff }

let private validateShowLights (slList: ResizeArray<ShowLight>) =
    if slList.FindIndex(fun x -> (x.Note >= ShowLight.BeamMin && x.Note <= ShowLight.BeamMax) || x.Note = ShowLight.BeamOff) = -1 then
        slList.Insert(0, ShowLight(0, ShowLight.BeamMin))

    if slList.FindIndex(fun x -> x.Note >= ShowLight.FogMin && x.Note <= ShowLight.FogMax) = -1 then
        slList.Insert(0, ShowLight(0, ShowLight.FogMin))

    slList

let generate (targetFile: string) (sngs: (Arrangement * SNG) list) =
    // Select an instrumental arrangement to generate the show lights from
    let sng =
        let leadFile =
            sngs
            |> List.tryFind (fun x ->
                match fst x with
                | Instrumental i -> i.RouteMask = RouteMask.Lead
                | _ -> false)
            |> Option.map snd
        leadFile |> Option.defaultWith (fun _ ->
            sngs
            |> List.find (fun x -> match fst x with | Instrumental _ -> true | _ -> false)
            |> snd)

    let showlights =
        generateFogNotes sng
        |> Seq.append (generateBeamNotes sng)
        |> Seq.append (generateLaserNotes sng)
        |> Seq.sortBy (fun x -> x.Time)

    let list = validateShowLights (ResizeArray(showlights))
    ShowLights.Save(targetFile, list)
