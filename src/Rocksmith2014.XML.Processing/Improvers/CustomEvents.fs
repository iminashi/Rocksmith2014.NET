module Rocksmith2014.XML.Processing.CustomEvents

open Rocksmith2014.XML
open Rocksmith2014.XML.Extensions
open System
open System.Globalization

/// Processes custom events.
let improve (arrangement: InstrumentalArrangement) =
    let events = arrangement.Events

    // Anchor width 3 events
    events
    |> Seq.filter (fun e -> e.Code.Equals("w3", StringComparison.OrdinalIgnoreCase))
    |> Seq.toArray
    |> Array.iter (fun event ->
        arrangement.Levels
        |> Seq.collect (fun l -> l.Anchors)
        |> Seq.filter (fun a -> a.Time = event.Time)
        |> Seq.iter (fun a -> a.Width <- 3y)

        events.Remove event |> ignore)

    // Remove beats event
    match events.Find(fun e -> e.Code.Equals("removebeats", StringComparison.OrdinalIgnoreCase)) with
    | null -> ()
    | removeBeats ->
        arrangement.Ebeats.RemoveAll(fun b -> b.Time >= removeBeats.Time) |> ignore
        events.Remove removeBeats |> ignore

    // Slide-out events
    events
    |> Seq.filter (fun e -> e.Code.StartsWith("so", StringComparison.OrdinalIgnoreCase))
    |> Seq.toArray
    |> Array.iter (fun slideEvent ->
        // Find the max level for the phrase the slide is in
        let phraseIter =
            arrangement.PhraseIterations.FindLast(fun pi -> pi.Time <= slideEvent.Time)
        let diff = arrangement.Phrases.[phraseIter.PhraseId].MaxDifficulty
        let level = arrangement.Levels.[int diff]

        let slideTime =
            // If a number was given after the event code, get the time of the chord or note that is right of the event by that number
            if slideEvent.Code.Length > 2 then
                match Int32.TryParse(slideEvent.Code.AsSpan(2), NumberStyles.Integer, NumberFormatInfo.InvariantInfo) with
                | true, rightBy ->
                    Utils.findTimeOfNthNoteFrom level slideEvent.Time rightBy
                | _ ->
                    failwith $"Invalid slide-out event at {Utils.timeToString slideEvent.Time}"
            else
                slideEvent.Time

        let noteIndex = level.Notes.FindIndexByTime slideTime
        let chordIndex = level.Chords.FindIndexByTime slideTime

        if noteIndex = -1 && chordIndex = -1 then
            failwith $"Could not find the notes or chord for slide-out event at {Utils.timeToString slideEvent.Time}"

        let notes, originalChordTemplate =
            if chordIndex = -1 then
                // These are notes that follow a LinkNext chord
                let linkNextChord = level.Chords.FindLast(fun c -> c.Time < slideTime)
                let chordHs = level.HandShapes.Find(fun hs -> hs.StartTime = linkNextChord.Time)

                // Shorten hand shapes that include the slide out notes
                // If chord notes is null here, there is an error in the XML file
                if not <| isNull chordHs && chordHs.EndTime > linkNextChord.Time + linkNextChord.ChordNotes.[0].Sustain then
                    chordHs.EndTime <- linkNextChord.Time + linkNextChord.ChordNotes.[0].Sustain

                level.Notes.FindAll(fun n -> n.Time = slideTime && n.IsUnpitchedSlide).ToArray(),
                arrangement.ChordTemplates.[int linkNextChord.ChordId]
            else
                // It is a normal chord with unpitched slide out
                let chord = level.Chords.[chordIndex]
                if isNull chord.ChordNotes then
                    failwith $"Chord missing chord notes for SlideOut event at {Utils.timeToString slideEvent.Time}"

                // The length of the handshape likely needs to be shortened
                let chordHs = level.HandShapes.Find(fun hs -> hs.StartTime = chord.Time)
                if not <| isNull chordHs then
                    chordHs.EndTime <- chordHs.StartTime + ((chordHs.EndTime - chordHs.StartTime) / 3)

                chord.ChordNotes.FindAll(fun cn -> cn.IsUnpitchedSlide).ToArray(),
                arrangement.ChordTemplates.[int chord.ChordId]

        if notes.Length = 0 then
            failwith $"Invalid SlideOut event at {Utils.timeToString slideEvent.Time}"

        // Create a new handshape at the slide end (add 1ms to include the sustain of the notes inside the handshape)
        let endTime = notes.[0].Time + notes.[0].Sustain + 1
        let startTime = endTime - (notes.[0].Sustain / 3)
        let chordId = int16 arrangement.ChordTemplates.Count
        level.HandShapes.InsertByTime(HandShape(chordId, startTime, endTime))

        // Create a new chord template for the handshape
        let soChordTemplate = ChordTemplate()
        for note in notes do
            let s = int note.String
            soChordTemplate.Frets.[s] <- note.SlideUnpitchTo
            soChordTemplate.Fingers.[s] <- originalChordTemplate.Fingers.[s]

        arrangement.ChordTemplates.Add soChordTemplate

        events.Remove slideEvent |> ignore
    )
