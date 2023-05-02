module internal Rocksmith2014.DD.EntityChooser

open Rocksmith2014.XML
open Rocksmith2014.XML.Extension
open System
open System.Collections
open System.Collections.Generic

// Array index = string number
type private PendingLinkNexts = Note array

let [<Literal>] private AlwaysEnabledTechs =
    NoteMask.Ignore ||| NoteMask.FretHandMute ||| NoteMask.PalmMute ||| NoteMask.Harmonic

let private pruneTechniques (diffPercent: float) (removedLinkNexts: BitArray) (note: Note) =
    if diffPercent <= 0.2 then
        if not note.IsBend then
            note.Sustain <- 0

        if note.IsLinkNext then
            removedLinkNexts.Set(int note.String, true)
            note.IsLinkNext <- false

        note.Mask <- note.Mask &&& AlwaysEnabledTechs
        note.Vibrato <- 0uy
        note.SlideTo <- -1y
        note.SlideUnpitchTo <- -1y
    elif diffPercent <= 0.35 && note.IsTremolo && not note.IsTap then
        note.IsTremolo <- false
    elif diffPercent <= 0.45 && note.IsVibrato && not note.IsTap then
        note.Vibrato <- 0uy

    if diffPercent <= 0.60 && note.IsPinchHarmonic then
        note.IsPinchHarmonic <- false

let private pruneChordNotes
        (diffPercent: float)
        (noteCount: int)
        (removedLinkNexts: BitArray)
        (pendingLinkNexts: PendingLinkNexts)
        (chord: Chord) =
    let cn = chord.ChordNotes
    let notesToRemove = cn.Count - noteCount

    if notesToRemove < 0 then
        failwith $"Chord at time {float cn[0].Time / 1000.} has less chord notes than its chord template."

    for i = cn.Count - notesToRemove to cn.Count - 1 do
        if cn[i].IsLinkNext then
            removedLinkNexts.Set(int cn[i].String, true)

    cn.RemoveRange(cn.Count - notesToRemove, notesToRemove)

    for n in cn do
        pruneTechniques diffPercent removedLinkNexts n

        if n.IsLinkNext then
            pendingLinkNexts[int n.String] <- n

let private shouldExclude
        (diffPercent: float)
        (score: NoteScore)
        (notesWithScore: IReadOnlyDictionary<NoteScore, int>)
        (currentNotes: Dictionary<NoteScore, int>)
        (range: DifficultyRange) =
    if diffPercent < range.Low then
        // The entity is outside of the difficulty range -> Exclude
        true
    elif diffPercent >= range.Low && diffPercent < range.High then
        // The entity is within the difficulty range -> Check the number of allowed notes
        let notes = notesWithScore[score]
        let currentCount =
            match currentNotes.TryGetValue(score) with
            | true, v -> v
            | false, _ -> 0
        let allowedPercent = (diffPercent - range.Low) / (range.High - range.Low)
        let allowedNotes = Math.Round(float notes * allowedPercent, MidpointRounding.AwayFromZero)
        let minNotes = if range.Low = 0. then 1 else 0
        let allowedCount = max minNotes (int allowedNotes)

        (currentCount + 1) > allowedCount
    else
        // The current difficulty is greater than the upper limit of the range -> Include
        false

let private removePreviousLinkNext (pendingLinkNexts: PendingLinkNexts) = function
    | XmlChord _ ->
        ()
    | XmlNote note ->
        let strIndex = int note.String
        let lnNote = pendingLinkNexts[strIndex]
        pendingLinkNexts[strIndex] <- null
        if notNull lnNote then
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
            c.HasChordNotes && c.ChordNotes.Exists(fun cn -> cn.String = string))

let private findPrevEntityAll (allEntities: XmlEntity array) (string: sbyte) (time: int) =
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
                | XmlNote _ ->
                    false
                | XmlChord c ->
                    c.HasChordNotes
                    && c.ChordId = chord.ChordId
                    && c.Time >= hs.StartTime && c.Time < hs.EndTime)

        prevChord.IsNone

/// Creates a list of chord notes from a chord template.
let private chordNotesFromTemplate (template: ChordTemplate) (chord: Chord) =
    let cn = ResizeArray<Note>()

    for i = 0 to 5 do
        if template.Frets[i] <> -1y then
            cn.Add(
                Note(
                    Time = chord.Time,
                    String = sbyte i,
                    Fret = template.Frets[i],
                    LeftHand = template.Fingers[i],
                    IsFretHandMute = chord.IsFretHandMute,
                    IsPalmMute = chord.IsPalmMute,
                    IsAccent = chord.IsAccent
               )
           )

    cn

/// Creates a note from a chord.
let private noteFromChord
        (diffPercent: float)
        (removedLinkNexts: BitArray)
        (chordTotalNotes: int)
        (template: ChordTemplate)
        (chord: Chord) =
    let fromHighest = shouldStartFromHighestNote chordTotalNotes template

    if chord.HasChordNotes then
        let cns = chord.ChordNotes
        let noteIndex = if fromHighest then cns.Count - 1 else 0
        // Create the note from a chord note
        for i = 0 to cns.Count - 1 do
            if i <> noteIndex && cns[i].IsLinkNext then
                removedLinkNexts.Set(int cns[i].String, true)

        let note = Note(cns[noteIndex], LeftHand = -1y)
        pruneTechniques diffPercent removedLinkNexts note
        note
    else
        // Create the note from the chord template
        let find = if fromHighest then Array.findIndexBack else Array.findIndex
        let string = template.Frets |> find ((<>) -1y)

        Note(
            Time = chord.Time,
            String = sbyte string,
            Fret = template.Frets[string],
            IsFretHandMute = chord.IsFretHandMute,
            IsPalmMute = chord.IsPalmMute,
            IsAccent = chord.IsAccent,
            IsIgnore = chord.IsIgnore
        )

let choose (diffPercent: float)
           (divisionMap: ScoreMap)
           (noteTimeToScore: IReadOnlyDictionary<NoteTime, NoteScore>)
           (notesWithScore: IReadOnlyDictionary<NoteScore, int>)
           (templates: ResizeArray<ChordTemplate>)
           (handShapes: HandShape list)
           (maxChordNotes: int)
           (entities: XmlEntity array) =
    // Strings from which a linknext note has been removed
    let removedLinkNexts = BitArray(6)
    let pendingLinkNexts: PendingLinkNexts = Array.zeroCreate 6
    let currentNotesWithScore = Dictionary<NoteScore, int>()

    let incrementCount division =
        match currentNotesWithScore.TryGetValue(division) with
        | true, v ->
            currentNotesWithScore[division] <- v + 1
        | false, _ ->
            currentNotesWithScore[division] <- 1

    let allowedChordNotes = getAllowedChordNotes diffPercent maxChordNotes

    ([], entities)
    ||> Array.fold (fun acc e ->
        let score = noteTimeToScore[getTimeCode e]
        let range = divisionMap[score]

        let includeAlways =
            match e with
            | XmlChord _ ->
                false
            | XmlNote n ->
                // Always include notes without techniques that are linked into
                n.Mask &&& (~~~ (NoteMask.Ignore ||| NoteMask.LinkNext)) = NoteMask.None
                && notNull pendingLinkNexts[int n.String]
                && not (n.IsSlide || n.IsUnpitchedSlide || n.IsBend || n.IsVibrato)

        if not includeAlways && shouldExclude diffPercent score notesWithScore currentNotesWithScore range then
            removePreviousLinkNext pendingLinkNexts e

            // Update removedLinkNexts (when not a slide)
            match e with
            | XmlNote xn when xn.IsLinkNext && not xn.IsSlide ->
                removedLinkNexts.Set(int xn.String, true)
            | XmlNote xn ->
                removedLinkNexts.Set(int xn.String, false)
            | XmlChord xc when xc.IsLinkNext && notNull xc.ChordNotes ->
                for cn in xc.ChordNotes do
                    if cn.IsLinkNext && not cn.IsSlide then
                        removedLinkNexts.Set(int cn.String, true)
            | _ ->
                ()

            acc
        // The entity is within the difficulty range
        else
            match e with
            | XmlNote oNote ->
                if removedLinkNexts[int oNote.String] then
                    // Previous note on the same string had linknext and was removed, remove this note also
                    // Reset removed linknext bit for this string unless this note also has linknext
                    if not oNote.IsLinkNext then
                        removedLinkNexts.Set(int oNote.String, false)

                    acc
                else
                    incrementCount score
                    let note = Note(oNote)

                    pruneTechniques diffPercent removedLinkNexts note

                    pendingLinkNexts[int note.String] <- null
                    if note.IsLinkNext then pendingLinkNexts[int note.String] <- note

                    if note.IsHopo then
                        let prevLevelEntity = findEntityWithString (acc |> Seq.map fst) note.String
                        let prevAllEntity = findPrevEntityAll entities note.String note.Time

                        match prevLevelEntity, prevAllEntity with
                        | _, None ->
                            // Likely a "hammer-on from nowhere"
                            ()
                        | Some (XmlNote n), _ when n.IsTap ->
                            // Leave the HOPO if this is a tapping phrase
                            ()
                        | Some (XmlNote n), Some (XmlNote nn) when n.Time = nn.Time ->
                            // Leave the HOPO if the previous note is the actual one before this
                            ()
                        | Some (XmlChord c), Some (XmlChord cc) when c.Time = cc.Time ->
                            // Leave the HOPO if the previous chord is the actual one before this
                            ()
                        | Some (XmlNote n as xn), _ when List.head acc |> fst = xn
                                                         && not (n.IsFretHandMute || n.IsHarmonic)
                                                         && ((note.IsPullOff && n.Fret > note.Fret) || (note.IsHammerOn && n.Fret < note.Fret)) ->
                            // Leave the HOPO if the previous note on the same string is an appropriate one and comes right before this one
                            ()
                        | _ ->
                            // Otherwise remove the HOPO
                            note.IsHammerOn <- false
                            note.IsPullOff <- false

                    (XmlNote note, None) :: acc

            | XmlChord chord ->
                incrementCount score

                let template = templates[int chord.ChordId]
                let noteCount = getNoteCount template

                if allowedChordNotes <= 1 then
                    // Convert the chord into a note
                    let note = noteFromChord diffPercent removedLinkNexts noteCount template chord
                    if note.IsLinkNext then pendingLinkNexts[int note.String] <- note

                    (XmlNote note, None) :: acc
                else
                    let copy = Chord(chord)

                    // Create chord notes if this is the first chord in the handshape
                    if isNull copy.ChordNotes && isFirstChordInHs (List.map fst acc) handShapes copy then
                        copy.ChordNotes <- chordNotesFromTemplate template copy

                    if allowedChordNotes >= noteCount then
                        if copy.HasChordNotes then
                            for cn in copy.ChordNotes do
                                pruneTechniques diffPercent removedLinkNexts cn

                                if cn.IsLinkNext then
                                    pendingLinkNexts[int cn.String] <- cn

                        (XmlChord copy, None) :: acc
                    else
                        if copy.HasChordNotes then
                            pruneChordNotes diffPercent allowedChordNotes removedLinkNexts pendingLinkNexts copy

                        let templateRequest =
                            ChordTarget copy
                            |> createTemplateRequest chord.ChordId allowedChordNotes noteCount template

                        (XmlChord copy, Some templateRequest) :: acc)
    |> List.rev
    |> List.toArray
