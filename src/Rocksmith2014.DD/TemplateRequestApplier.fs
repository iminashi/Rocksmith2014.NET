module internal Rocksmith2014.DD.TemplateRequestApplier

open Rocksmith2014.XML
open System.Collections.Generic

[<Struct>]
type private NewData = { Fingers: sbyte array; Frets: sbyte array }

let private globalTemplatesLock = obj ()

/// Returns new finger and fret arrays for the chord template with the given number of notes removed.
let private removeNotes notesToRemove fromHighest (template: ChordTemplate) =
    let fingers = Array.copy template.Fingers
    let frets = Array.copy template.Frets
    let startIndex, change = if fromHighest then 0, 1 else frets.Length - 1, -1

    let rec loop i leftToRemove =
        if leftToRemove = 0 || i < 0 || i > 5 then
            assert (leftToRemove = 0)
            { Fingers = fingers; Frets = frets }
        elif frets[i] <> -1y then
            fingers[i] <- -1y
            frets[i] <- -1y
            loop (i + change) (leftToRemove - 1)
        else
            loop (i + change) leftToRemove

    loop startIndex notesToRemove

/// Returns the ID of an existing chord template if an identical one already exists, otherwise adds a new template to the global template list.
let private getChordId (templates: ResizeArray<ChordTemplate>) (template: ChordTemplate) (newData: NewData) =
    lock globalTemplatesLock (fun () ->
        let existing = templates.FindIndex(fun x ->
            x.DisplayName = template.Name
            && x.Name = template.DisplayName
            && x.Frets = newData.Frets
            && x.Fingers = newData.Fingers)

        match existing with
        | -1 ->
            ChordTemplate(template.Name, template.DisplayName, newData.Fingers, newData.Frets)
            |> templates.Add

            int16 (templates.Count - 1)
        | index ->
            int16 index)

/// Returns a function for applying the correct chord ID to the request target.
let applyChordId (templates: ResizeArray<ChordTemplate>) =
    let templateMap = Dictionary<int16 * byte, int16>()

    fun (request: TemplateRequest) ->
        let chordId =
            match templateMap.TryGetValue((request.OriginalId, request.NoteCount)) with
            | true, chordId ->
                chordId
            | false, _ ->
                let template = templates[int request.OriginalId]
                let noteCount = getNoteCount template
                let notesToRemove = noteCount - int request.NoteCount
                let chordId =
                    removeNotes notesToRemove request.FromHighestNote template
                    |> getChordId templates template

                templateMap.Add((request.OriginalId, request.NoteCount), chordId)
                chordId

        match request.Target with
        | ChordTarget chord -> chord.ChordId <- chordId
        | HandShapeTarget hs -> hs.ChordId <- chordId
