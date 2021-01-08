module Rocksmith2014.DD.Generator

open Rocksmith2014.XML
open System
open LevelCounter
open System.Collections.Generic

let private lockObj = obj()

/// Creates an XML entity array from the notes and chords.
let private createXmlEntityArray (xmlNotes: Note list) (xmlChords: Chord list) =
    if xmlChords.Length = 0 then
        xmlNotes
        |> List.map XmlNote
        |> Array.ofList
    elif xmlNotes.Length = 0 then
        xmlChords
        |> List.map XmlChord
        |> Array.ofList
    else
        let entityArray =
            [| yield! List.map XmlNote xmlNotes
               yield! List.map XmlChord xmlChords |]

        Array.sortInPlaceBy getTimeCode entityArray
        entityArray

let private pruneTechniques diffPercent (removedLinkNexts: HashSet<sbyte>) (note: Note) =
    if diffPercent <= 20uy then
        note.Sustain <- 0
        if note.IsLinkNext then
            removedLinkNexts.Add note.String |> ignore
            note.IsLinkNext <- false
        note.Mask <- note.Mask &&& (NoteMask.Ignore ||| NoteMask.FretHandMute)
        note.SlideTo <- -1y
        note.SlideUnpitchTo <- -1y

let private pruneChordNotes diffPercent noteCount (removedLinkNexts: HashSet<sbyte>) (chord: Chord) =
    let cn = chord.ChordNotes
    if not <| isNull cn && cn.Count > 0 then
        let removeNotes = cn.Count - noteCount
        for i = cn.Count - removeNotes to cn.Count - 1 do
            if cn.[i].IsLinkNext then
                removedLinkNexts.Add cn.[i].String |> ignore
        cn.RemoveRange(cn.Count - removeNotes, removeNotes)

        for n in cn do pruneTechniques diffPercent removedLinkNexts n

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
    
let private chooseEntities diffPercent
                           (divisions: (int * BeatDivision) array)
                           notesInDivision
                           (templates: ResizeArray<ChordTemplate>)
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

    entities
    |> Array.choose (fun e ->
        let division = noteTimeToDivision.[getTimeCode e]
        let range = divisionMap.[division]

        if diffPercent < range.Low then
            None
        elif (diffPercent >= range.Low && diffPercent < range.High) && shouldExclude diffPercent division notesInDivision currentNotesInDivision range then
            None
        else
            match e with
            | XmlNote note ->
                if removedLinkNexts.Contains note.String then
                    if not note.IsLinkNext then
                        removedLinkNexts.Remove note.String |> ignore
                    None
                else
                    incrementCount division
                    // TODO: Techniques
                    Some (e, None)

            | XmlChord chord ->
                incrementCount division

                let template = templates.[int chord.ChordId]
                let noteCount = getNoteCount template

                if diffPercent <= 17uy && noteCount > 1 then
                    let template = templates.[int chord.ChordId]

                    let note =
                        if not <| isNull chord.ChordNotes then
                            for i = 1 to chord.ChordNotes.Count - 1 do
                                if chord.ChordNotes.[i].IsLinkNext then
                                    removedLinkNexts.Add(chord.ChordNotes.[i].String) |> ignore
                            let n = Note(chord.ChordNotes.[0], LeftHand = -1y)
                            pruneTechniques diffPercent removedLinkNexts n
                            n
                        else
                            let string = template.Frets |> Array.findIndex (fun x -> x <> -1y)
                            Note(Time = chord.Time, String = sbyte string, Fret = template.Frets.[string], IsFretHandMute = chord.IsFretHandMute)
                
                    Some (XmlNote note, None)
                elif diffPercent <= 34uy && noteCount > 2 then
                    let copy = Chord(chord)
                    pruneChordNotes diffPercent 2 removedLinkNexts copy
                    Some (XmlChord copy, Some { OriginalId = chord.ChordId; NoteCount = 2uy; Target = ChordTarget copy })
                elif diffPercent <= 51uy && noteCount > 3 then
                    let copy = Chord(chord)
                    pruneChordNotes diffPercent 3 removedLinkNexts copy
                    Some (XmlChord copy, Some { OriginalId = chord.ChordId; NoteCount = 3uy; Target = ChordTarget copy })
                elif diffPercent <= 68uy && noteCount > 4 then
                    let copy = Chord(chord)
                    pruneChordNotes diffPercent 4 removedLinkNexts copy
                    Some (XmlChord copy, Some { OriginalId = chord.ChordId; NoteCount = 4uy; Target = ChordTarget copy })
                elif diffPercent <= 85uy && noteCount > 5 then
                    let copy = Chord(chord)
                    pruneChordNotes diffPercent 5 removedLinkNexts copy
                    Some (XmlChord copy, Some { OriginalId = chord.ChordId; NoteCount = 5uy; Target = ChordTarget copy })
                else
                    Some (e, None)
    )

/// Copies the necessary anchors into the difficulty level.
let private chooseAnchors (entities: XmlEntity array) (anchors: Anchor list) phraseEndTime =
    // Assume that anchors up to 3ms after a note were meant to be on the note
    let errorMargin = 3

    // TODO: Anchors that contain no notes even at the hardest level

    let rec add result (anchors: Anchor list) =
        match anchors with
        | [] -> result
        | a::tail ->
            let endTime =
                match tail with
                | a2::_ -> a2.Time
                | [] -> phraseEndTime
            let result =
                if entities |> Array.exists (fun e ->
                    let time = getTimeCode e
                    time + errorMargin >= a.Time && time < endTime)
                then
                    a::result
                else
                    result
            add result tail

    add [] anchors
    |> List.rev

let private applyChordId (templates: ResizeArray<ChordTemplate>) =
    let templateMap = Dictionary<int16 * byte, int16>()

    fun (request: TemplateRequest) ->
        let id = 
            match templateMap.TryGetValue((request.OriginalId, request.NoteCount)) with
            | true, id ->
                id
            | false, _ ->
                let template = templates.[int request.OriginalId]
                let noteCount = getNoteCount template

                let mutable removeNotes = noteCount - int request.NoteCount
                let newFingers, newFrets =
                    let fingers = Array.copy template.Fingers
                    let frets = Array.copy template.Frets
                    for i = frets.Length - 1 downto 0 do
                        if frets.[i] <> -1y && removeNotes > 0 then
                            removeNotes <- removeNotes - 1
                            fingers.[i] <- -1y
                            frets.[i] <- -1y
                    fingers, frets

                let id =
                    lock lockObj (fun _ ->
                        let existing = templates.FindIndex(fun x ->
                            x.DisplayName = template.Name
                            && x.Name = template.DisplayName
                            && x.Frets = newFrets
                            && x.Fingers = newFingers)

                        match existing with
                        | -1 ->
                            let id = int16 templates.Count
                            let newTemplate = ChordTemplate(template.Name, template.DisplayName, newFingers, newFrets)
                            templates.Add newTemplate
                            id
                        | index ->
                            int16 index)
                templateMap.Add((request.OriginalId, request.NoteCount), id)
                id

        match request.Target with
        | ChordTarget chord -> chord.ChordId <- id
        | HandShapeTarget hs -> hs.ChordId <- id

let private generateLevels (arr: InstrumentalArrangement) (phraseData: DataExtractor.PhraseData) =
    // Determine the number of levels to generate for this phrase
    let levelCount = predictLevelCount (DataExtractor.getPath arr) phraseData

    if phraseData.NoteCount + phraseData.ChordCount = 0 then
        // Copy anchors only
        let level = Level 0y
        level.Anchors.AddRange phraseData.Anchors
        [| level |]
    else
        let entities = createXmlEntityArray phraseData.Notes phraseData.Chords
        let divisions =
            entities
            |> Array.map (fun e ->
                let time = getTimeCode e
                time, BeatDivider.getDivision phraseData.Beats time)

        let notesInDivision =
            divisions
            |> Array.groupBy snd
            |> Array.map (fun (group, elems) -> group, elems.Length)
            |> Map.ofArray

        let applyChordId' = applyChordId arr.ChordTemplates

        Array.init levelCount (fun diff ->
            // Copy everything for the hardest level
            if diff = levelCount - 1 then
                Level(sbyte diff,
                      ResizeArray(phraseData.Notes),
                      ResizeArray(phraseData.Chords),
                      ResizeArray(phraseData.Anchors),
                      ResizeArray(phraseData.HandShapes))
            else
                let diffPercent = byte <| 100 * (diff + 1) / levelCount

                let levelEntities, templateRequests1 =
                    chooseEntities diffPercent divisions notesInDivision arr.ChordTemplates entities
                    |> Array.unzip
                let notes = levelEntities |> Array.choose (function XmlNote n -> Some n | _ -> None)
                let chords = levelEntities |> Array.choose (function XmlChord n -> Some n | _ -> None)

                let handShapes, templateRequests2 =
                    HandShapeChooser.choose diffPercent levelEntities arr.ChordTemplates phraseData.HandShapes
                    |> List.unzip

                let anchors = chooseAnchors levelEntities phraseData.Anchors phraseData.EndTime

                templateRequests1
                |> Seq.append templateRequests2
                |> Seq.choose id
                |> Seq.iter applyChordId'

                Level(sbyte diff,
                      ResizeArray(notes),
                      ResizeArray(chords),
                      ResizeArray(anchors),
                      ResizeArray(handShapes))     
        )

let private combineNGPhrases (iterations: PhraseIteration array) (phrases: Phrase array) =
    let isNGPhrase = Predicate<Phrase>(fun p -> p.Name = "NG")

    let phrases = ResizeArray(phrases)
    let firstIndex = phrases.FindIndex isNGPhrase
    let mutable lastIndex = phrases.FindLastIndex isNGPhrase

    while firstIndex <> -1 && lastIndex <> firstIndex do
        phrases.RemoveAt lastIndex

        // Update the phrase ids of the phrase iterations
        for pi in iterations do
            if pi.PhraseId = lastIndex then pi.PhraseId <- firstIndex
            elif pi.PhraseId > lastIndex then pi.PhraseId <- pi.PhraseId - 1

        lastIndex <- phrases.FindLastIndex isNGPhrase

    phrases

let generateForArrangement (arr: InstrumentalArrangement) =
    let phraseIterations = arr.PhraseIterations.ToArray()

    // Create the difficulty levels
    let levels =
        phraseIterations
        |> Array.Parallel.map (DataExtractor.getPhraseIterationData arr >> generateLevels arr)

    let maxDiff =
        levels
        |> Array.maxBy (fun l -> l.Length)
        |> fun l -> l.Length

    // Create phrases
    let phrases =
        levels
        |> Array.mapi (fun i lvls ->
            let name =
                if i = 0 then
                    // Usually "COUNT"
                    arr.Phrases.[0].Name
                elif i = levels.Length - 1 then
                    "END"
                elif lvls.Length = 1 then
                    "NG"
                else
                    $"p{i}"

            Phrase(name, byte (lvls.Length - 1), PhraseMask.None)
        )

    // Create phrase iterations
    let newPhraseIterations =
        phraseIterations
        |> Array.mapi (fun i pi ->
            let phrase = phrases.[i]
            let pi = PhraseIteration(pi.Time, i)
            if phrase.MaxDifficulty > 0uy then
                // Create hero levels
                let heroLevels = 
                    HeroLevels(
                        phrase.MaxDifficulty / 3uy,
                        phrase.MaxDifficulty / 2uy,
                        phrase.MaxDifficulty)
                pi.HeroLevels <- heroLevels
            pi
        )

    // Combine the data in the levels
    let combinedLevels =
        Seq.init maxDiff (fun diff -> 
            let level = Level(sbyte diff)
            levels
            |> Array.iter
                (fun lvl ->
                    if diff < lvl.Length then
                        level.Anchors.AddRange(lvl.[diff].Anchors)
                        level.Notes.AddRange(lvl.[diff].Notes)
                        level.HandShapes.AddRange(lvl.[diff].HandShapes)
                        level.Chords.AddRange(lvl.[diff].Chords)
                )
            level
        )

    let phrases = combineNGPhrases newPhraseIterations phrases

    // TODO: Search for similar phrases
    // Use same name or create linked difficulty

    arr.Levels <- ResizeArray(combinedLevels)
    arr.Phrases <- phrases
    arr.PhraseIterations <- ResizeArray(newPhraseIterations)

    arr

let generateForFile fileName targetFile =
    let arr =
        InstrumentalArrangement.Load fileName
        |> generateForArrangement

    arr.Save targetFile
