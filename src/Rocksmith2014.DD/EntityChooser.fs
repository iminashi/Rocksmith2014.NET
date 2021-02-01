﻿module Rocksmith2014.DD.EntityChooser

open Rocksmith2014.XML
open System.Collections.Generic

let [<Literal>] private AlwaysEnabledTechs = NoteMask.Ignore ||| NoteMask.FretHandMute ||| NoteMask.PalmMute ||| NoteMask.Harmonic

let private pruneTechniques diffPercent (removedLinkNexts: HashSet<sbyte>) (note: Note) =
    if diffPercent <= 20uy then
        if not note.IsBend then
            note.Sustain <- 0

        if note.IsLinkNext then
            removedLinkNexts.Add note.String |> ignore
            note.IsLinkNext <- false

        note.Mask <- note.Mask &&& AlwaysEnabledTechs
        note.Vibrato <- 0uy
        note.SlideTo <- -1y
        note.SlideUnpitchTo <- -1y
    elif diffPercent <= 35uy && note.IsTremolo && not note.IsTap then
        note.IsTremolo <- false
    elif diffPercent <= 45uy && note.IsVibrato && not note.IsTap then
        note.Vibrato <- 0uy

    if diffPercent <= 60uy && note.IsPinchHarmonic then
        note.IsPinchHarmonic <- false

let private pruneChordNotes diffPercent
                            noteCount
                            (removedLinkNexts: HashSet<sbyte>)
                            (pendingLinkNexts: Dictionary<sbyte, Note>)
                            (chord: Chord) =
    if chord.HasChordNotes then
        let cn = chord.ChordNotes
        let removeNotes = cn.Count - noteCount

        for i = cn.Count - removeNotes to cn.Count - 1 do
            if cn.[i].IsLinkNext then
                removedLinkNexts.Add cn.[i].String |> ignore
        cn.RemoveRange(cn.Count - removeNotes, removeNotes)

        for n in cn do
            pruneTechniques diffPercent removedLinkNexts n
            if n.IsLinkNext then pendingLinkNexts.Add(n.String, n)

        if chord.IsLinkNext && cn.TrueForAll(fun x -> not x.IsLinkNext) then
            chord.IsLinkNext <- false

let private shouldExclude diffPercent
                          (division: BeatDivision)
                          (notesInDivision: Map<BeatDivision, int>)
                          (currentNotes: Dictionary<BeatDivision, int>)
                          (range: DifficultyRange) =
    if diffPercent < range.Low then
        // The entity is outside of the difficulty range -> Exclude
        true
    elif diffPercent >= range.Low && diffPercent < range.High then
        // The entity is within the difficulty range -> Check the number of allowed notes
        let notes = notesInDivision.[division]
        let currentCount =
            match currentNotes.TryGetValue(division) with
            | true, v -> v
            | false, _ -> 0
        let allowedPercent = 100 * int (diffPercent - range.Low) / int (range.High - range.Low)
        let allowedCount = max 1 (notes * allowedPercent / 100)

        (currentCount + 1) > allowedCount
    else
        // The current difficulty is greater than the upper limit of the range -> Include
        false
    
let private removePreviousLinkNext (pendingLinkNexts: Dictionary<sbyte, Note>) = function
    | XmlChord _ ->
        ()
    | XmlNote note ->
        let mutable lnNote = null
        if pendingLinkNexts.Remove(note.String, &lnNote) then
            if lnNote.IsSlide then
                lnNote.SlideTo <- -1y
                lnNote.Sustain <- 0
            lnNote.IsLinkNext <- false

let private findEntityWithString (entities: XmlEntity seq) string =
    entities
    |> Seq.tryFind (function
        | XmlNote n ->
            n.String = string
        | XmlChord c ->
            c.HasChordNotes
            &&
            c.ChordNotes.Exists(fun cn -> cn.String = string))

let private findPrevEntityAll (allEntities: XmlEntity array) string time =
    allEntities
    |> Array.tryFindBack (function
        | XmlNote n ->
            n.String = string && n.Time < time
        | XmlChord c ->
            c.Time < time
            && c.HasChordNotes
            && c.ChordNotes.Exists(fun cn -> cn.String = string))

let private isFirstChordInHs (entities: XmlEntity list) (handShapes: HandShape list) (chord: Chord) =
    let hs =
        handShapes
        |> List.tryFind (fun x -> chord.Time >= x.StartTime && chord.Time < x.EndTime)
    
    // The hand shape might not be found if the start of the phrase is placed poorly
    match hs with
    | None ->
        true
    | Some hs ->
        let prevChord =
            entities
            |> List.tryFind (function
                | XmlNote _ -> false
                | XmlChord c ->
                    c.HasChordNotes
                    && c.ChordId = chord.ChordId
                    && c.Time >= hs.StartTime && c.Time < hs.EndTime)

        prevChord.IsNone

let private chordNotesFromTemplate (template: ChordTemplate) (chord: Chord) =
    let cn = ResizeArray<Note>()
    for i = 0 to 5 do
        if template.Frets.[i] <> -1y then
            cn.Add(Note(Time = chord.Time,
                        String = sbyte i,
                        Fret = template.Frets.[i],
                        LeftHand = template.Fingers.[i],
                        IsFretHandMute = chord.IsFretHandMute,
                        IsPalmMute = chord.IsPalmMute,
                        IsAccent = chord.IsAccent))
    cn

let private createNote diffPercent
                       (removedLinkNexts: HashSet<sbyte>)
                       (template: ChordTemplate)
                       (chord: Chord) =
    if chord.HasChordNotes then
        // Create the note from a chord note
        for i = 1 to chord.ChordNotes.Count - 1 do
            if chord.ChordNotes.[i].IsLinkNext then
                removedLinkNexts.Add(chord.ChordNotes.[i].String) |> ignore

        let n = Note(chord.ChordNotes.[0], LeftHand = -1y)
        pruneTechniques diffPercent removedLinkNexts n
        n
    else
        // Create the note from the chord template
        let string = template.Frets |> Array.findIndex (fun x -> x <> -1y)
        Note(Time = chord.Time,
             String = sbyte string,
             Fret = template.Frets.[string],
             IsFretHandMute = chord.IsFretHandMute,
             IsPalmMute = chord.IsPalmMute,
             IsAccent = chord.IsAccent,
             IsIgnore = chord.IsIgnore)

let choose diffPercent
           (divisionMap: Map<BeatDivision, DifficultyRange>)
           (noteTimeToDivision: Map<int, int>)
           (notesInDivision: Map<BeatDivision, int>)
           (templates: ResizeArray<ChordTemplate>)
           (handShapes: HandShape list)
           (maxChordNotes: int)
           (entities: XmlEntity array) =
    let removedLinkNexts = HashSet<sbyte>()
    let pendingLinkNexts = Dictionary<sbyte, Note>() 
    let currentNotesInDivision = Dictionary<BeatDivision, int>()
    
    let incrementCount division =
        match currentNotesInDivision.TryGetValue division with
        | true, v ->
            currentNotesInDivision.[division] <- v + 1
        | false, _ ->
            currentNotesInDivision.[division] <- 1
    
    let allowedChordNotes = Utils.getAllowedChordNotes diffPercent maxChordNotes
    
    ([], entities)
    ||> Array.fold (fun acc e ->
        let division = noteTimeToDivision.[getTimeCode e]
        let range = divisionMap.[division]

        /// Always include notes without techniques that are linked into
        let includeAlways =
            // TODO: Always include notes at the same time code when link next is used?
            match e with
            | XmlChord _ ->
                false
            | XmlNote n ->
                n.Mask &&& (~~~ (NoteMask.Ignore ||| NoteMask.LinkNext)) = NoteMask.None
                && pendingLinkNexts.ContainsKey n.String
                && not (n.IsSlide || n.IsUnpitchedSlide || n.IsBend || n.IsVibrato)

        if not includeAlways && shouldExclude diffPercent division notesInDivision currentNotesInDivision range then
            removePreviousLinkNext pendingLinkNexts e

            // Update removedLinkNexts (when not a slide)
            match e with
            | XmlNote xn when xn.IsLinkNext && not xn.IsSlide ->
                removedLinkNexts.Add xn.String |> ignore
            | XmlNote xn ->
                removedLinkNexts.Remove xn.String |> ignore
            | XmlChord xc when xc.IsLinkNext ->
                for cn in xc.ChordNotes do
                    if cn.IsLinkNext && not cn.IsSlide then
                        removedLinkNexts.Add cn.String |> ignore
            | _ -> ()

            acc
        // The entity is within the difficulty range
        else
            match e with
            | XmlNote oNote ->
                if removedLinkNexts.Contains oNote.String then
                    if not oNote.IsLinkNext then
                        removedLinkNexts.Remove oNote.String |> ignore
    
                    acc
                else
                    incrementCount division
                    let note = Note(oNote)

                    pruneTechniques diffPercent removedLinkNexts note

                    pendingLinkNexts.Remove(note.String) |> ignore
                    if note.IsLinkNext then pendingLinkNexts.Add(note.String, note)
    
                    if note.IsHopo then
                        let prevLevelEntity = findEntityWithString (acc |> Seq.map fst) note.String
                        let prevAllEntity = findPrevEntityAll entities note.String note.Time

                        match prevLevelEntity, prevAllEntity with
                        // Leave the HOPO if this is a tapping phrase
                        | Some (XmlNote n), _ when n.IsTap -> ()
                        // Leave the HOPO if the previous note on the same string is an appropriate one and comes right before this one
                        // TODO: Handle case when first note is "hammer-on from nowhere"
                        | Some (XmlNote n as xn), _ when List.head acc |> fst = xn
                                                         && not (n.IsFretHandMute || n.IsHarmonic)
                                                         && ((note.IsPullOff && n.Fret > note.Fret) || (note.IsHammerOn && n.Fret < note.Fret)) -> ()
                        // Leave the HOPO if the previous note/chord is the actual one before this
                        | Some (XmlNote n), Some (XmlNote nn) when n.Time = nn.Time -> ()
                        | Some (XmlChord c), Some (XmlChord cc) when c.Time = cc.Time -> ()
                        // Otherwise remove the HOPO
                        | _ ->
                            note.IsHammerOn <- false
                            note.IsPullOff <- false
    
                    (XmlNote note, None)::acc
    
            | XmlChord chord ->
                incrementCount division
       
                let template = templates.[int chord.ChordId]
                let noteCount = getNoteCount template
 
                if allowedChordNotes <= 1 then
                    // Convert the chord into a note
                    let note = createNote diffPercent removedLinkNexts template chord
                    if note.IsLinkNext then pendingLinkNexts.Add(note.String, note)

                    (XmlNote note, None)::acc
                else
                    let copy = Chord(chord)

                    // Create chord notes if this is the first chord in the hand shape
                    if isNull copy.ChordNotes && isFirstChordInHs (List.map fst acc) handShapes copy then
                        copy.ChordNotes <- chordNotesFromTemplate template copy

                    if allowedChordNotes >= noteCount then
                        if copy.HasChordNotes then
                            for cn in copy.ChordNotes do
                                pruneTechniques diffPercent removedLinkNexts cn
                                if cn.IsLinkNext then pendingLinkNexts.Add(cn.String, cn)
    
                        (XmlChord copy, None)::acc
                    else
                        pruneChordNotes diffPercent allowedChordNotes removedLinkNexts pendingLinkNexts copy
    
                        (XmlChord copy, Some { OriginalId = chord.ChordId; NoteCount = byte allowedChordNotes; Target = ChordTarget copy })::acc
    )
    |> List.rev
    |> List.toArray