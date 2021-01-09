module Rocksmith2014.DD.EntityChooser

open Rocksmith2014.XML
open System.Collections.Generic

let private pruneTechniques diffPercent (removedLinkNexts: HashSet<sbyte>) (note: Note) =
    if diffPercent <= 20uy then
        note.Sustain <- 0
        if note.IsLinkNext then
            removedLinkNexts.Add note.String |> ignore
            note.IsLinkNext <- false
        note.Mask <- note.Mask &&& (NoteMask.Ignore ||| NoteMask.FretHandMute)
        note.SlideTo <- -1y
        note.SlideUnpitchTo <- -1y

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
            if n.IsLinkNext then
                pendingLinkNexts.Add(n.String, n)

let private shouldExclude diffPercent
                          (division: BeatDivision)
                          (notesInDivision: Map<BeatDivision, int>)
                          (currentNotes: Dictionary<BeatDivision, int>)
                          (range: DifficultyRange) =
    let notes = notesInDivision.[division]
    let currentCount =
        match currentNotes.TryGetValue(division) with
        | true, v -> v
        | false, _ -> 0
    let allowedPercent = 100 * int diffPercent / int range.High
    let allowedCount = notes * allowedPercent / 100

    (currentCount + 1) > allowedCount
    
let private removePreviousLinkNext (pendingLinkNexts: Dictionary<sbyte, Note>) (entity: XmlEntity) =
    match entity with
    | XmlChord _ ->
        ()
    | XmlNote note ->
        let mutable lnNote = null
        if pendingLinkNexts.Remove(note.String, &lnNote) then
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
        |> List.find (fun x -> chord.Time >= x.StartTime && chord.Time < x.EndTime)

    let prevChord =
        entities
        |> List.tryFind (function
            | XmlNote _ -> false
            | XmlChord c ->
                c.HasChordNotes
                && c.ChordId = chord.ChordId
                && c.Time >= hs.StartTime && c.Time < hs.EndTime)

    prevChord.IsNone

let private chordNotesFromTemplate (template: ChordTemplate) time =
    let cn = ResizeArray<Note>()
    for i = 0 to 5 do
        if template.Frets.[i] <> -1y then
            cn.Add(Note(Time = time, String = sbyte i, Fret = template.Frets.[i], LeftHand = template.Fingers.[i]))
    cn

let choose diffPercent
           (divisions: (int * BeatDivision) array)
           (notesInDivision: Map<BeatDivision, int>)
           (templates: ResizeArray<ChordTemplate>)
           (handShapes: HandShape list)
           (entities: XmlEntity array) =
    let removedLinkNexts = HashSet<sbyte>()
    
    let noteTimeToDivision = Map.ofArray divisions
    let divisionMap = BeatDivider.createDivisionMap divisions entities.Length
    let currentNotesInDivision = Dictionary<BeatDivision, int>()
    
    let incrementCount division =
        match currentNotesInDivision.TryGetValue division with
        | true, v ->
            currentNotesInDivision.[division] <- v + 1
        | false, _ ->
            currentNotesInDivision.[division] <- 1
    
    let pendingLinkNexts = Dictionary<sbyte, Note>()
    
    entities
    |> Array.fold (fun acc e ->
        let division = noteTimeToDivision.[getTimeCode e]
        let range = divisionMap.[division]

        // The entity is outside of the difficulty range
        if diffPercent < range.Low then
            removePreviousLinkNext pendingLinkNexts e
    
            acc
        // The entity is within the difficulty range but there are already enough notes chosen
        elif (diffPercent >= range.Low && diffPercent < range.High)
             && shouldExclude diffPercent division notesInDivision currentNotesInDivision range then
            removePreviousLinkNext pendingLinkNexts e
    
            acc
        // The entity is within the difficulty range
        else
            match e with
            | XmlNote note ->
                if removedLinkNexts.Contains note.String then
                    if not note.IsLinkNext then
                        removedLinkNexts.Remove note.String |> ignore
    
                    acc
                else
                    incrementCount division
                    let copy = Note(note)

                    pendingLinkNexts.Remove(copy.String) |> ignore
                    if copy.IsLinkNext then
                        pendingLinkNexts.Add(copy.String, copy)                        
    
                    // TODO: Techniques
    
                    if copy.IsHopo then
                        let prevLevelEntity = findEntityWithString (acc |> Seq.map fst) copy.String
                        let prevAllEntity = findPrevEntityAll entities copy.String copy.Time

                        match prevLevelEntity, prevAllEntity with
                        // Leave the HOPO if the previous note on the same string is on an appropriate fret
                        | Some (XmlNote n), _ when (copy.IsPullOff && n.Fret > copy.Fret) || (copy.IsHammerOn && n.Fret < copy.Fret) -> ()
                        // Leave the HOPO if the previous note/chord is the actual one before this
                        | Some (XmlNote n), Some (XmlNote nn) when n.Time = nn.Time -> ()
                        | Some (XmlChord c), Some (XmlChord cc) when c.Time = cc.Time -> ()
                        // Otherwise remove the HOPO
                        | _ ->
                            copy.IsHammerOn <- false
                            copy.IsPullOff <- false
    
                    (XmlNote copy, None)::acc
    
            | XmlChord chord ->
                incrementCount division
       
                let template = templates.[int chord.ChordId]
                let noteCount = getNoteCount template
  
                if diffPercent <= 17uy && noteCount > 1 then
                    // Create a note from the chord
                    let note =
                        if chord.HasChordNotes then
                            for i = 1 to chord.ChordNotes.Count - 1 do
                                if chord.ChordNotes.[i].IsLinkNext then
                                    removedLinkNexts.Add(chord.ChordNotes.[i].String) |> ignore
                            let n = Note(chord.ChordNotes.[0], LeftHand = -1y)
                            pruneTechniques diffPercent removedLinkNexts n
                            n
                        else
                            let string = template.Frets |> Array.findIndex (fun x -> x <> -1y)
                            Note(Time = chord.Time, String = sbyte string, Fret = template.Frets.[string], IsFretHandMute = chord.IsFretHandMute)
                
                    (XmlNote note, None)::acc
                else
                    let copy = Chord(chord)

                    // Create chord notes if this is the first chord in the hand shape
                    if isNull copy.ChordNotes && isFirstChordInHs (List.map fst acc) handShapes copy then
                        copy.ChordNotes <- chordNotesFromTemplate template copy.Time

                    if diffPercent <= 34uy && noteCount > 2 then
                        pruneChordNotes diffPercent 2 removedLinkNexts pendingLinkNexts copy
    
                        (XmlChord copy, Some { OriginalId = chord.ChordId; NoteCount = 2uy; Target = ChordTarget copy })::acc
                    elif diffPercent <= 51uy && noteCount > 3 then
                        pruneChordNotes diffPercent 3 removedLinkNexts pendingLinkNexts copy
    
                        (XmlChord copy, Some { OriginalId = chord.ChordId; NoteCount = 3uy; Target = ChordTarget copy })::acc
                    elif diffPercent <= 68uy && noteCount > 4 then
                        pruneChordNotes diffPercent 4 removedLinkNexts pendingLinkNexts copy
    
                        (XmlChord copy, Some { OriginalId = chord.ChordId; NoteCount = 4uy; Target = ChordTarget copy })::acc
                    elif diffPercent <= 85uy && noteCount > 5 then
                        pruneChordNotes diffPercent 5 removedLinkNexts pendingLinkNexts copy
    
                        (XmlChord copy, Some { OriginalId = chord.ChordId; NoteCount = 5uy; Target = ChordTarget copy })::acc
                    else
                        if copy.HasChordNotes then
                            for n in copy.ChordNotes do
                                if n.IsLinkNext then
                                    pendingLinkNexts.TryAdd(n.String, n) |> ignore
    
                        (XmlChord copy, None)::acc
    ) []
    |> List.rev
    |> List.toArray
