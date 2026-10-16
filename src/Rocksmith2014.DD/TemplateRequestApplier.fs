module internal Rocksmith2014.DD.TemplateRequestApplier

open System.Collections.Generic
open Rocksmith2014.XML

[<Struct>]
type private NewData = { Fingers: sbyte array; Frets: sbyte array }

let private globalTemplatesLock = obj ()

/// Returns new finger and fret arrays for the chord template with the given number of notes removed.
/// Uses musical order because string 6 is below the stock strings.
let private removeNotes (notesToRemove: int) (fromHighest: bool) (template: ChordTemplate) : NewData =
    let fingers = Array.copy template.Fingers
    let frets = Array.copy template.Frets
    let lowToHigh = StringLayout.LowToHigh(template.StringCount)
    let removalOrder = if fromHighest then lowToHigh else Array.rev lowToHigh
    let mutable leftToRemove = notesToRemove

    for stringIndex in removalOrder do
        if leftToRemove > 0 && frets[stringIndex] <> -1y then
            fingers[stringIndex] <- -1y
            frets[stringIndex] <- -1y
            leftToRemove <- leftToRemove - 1

    assert (leftToRemove = 0)

    { Fingers = fingers; Frets = frets }

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
