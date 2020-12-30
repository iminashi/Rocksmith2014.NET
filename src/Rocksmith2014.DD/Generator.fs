module Rocksmith2014.DD.Generator

open Rocksmith2014.XML
open System
open LevelCounter
open System.Collections.Generic

type XmlEntity =
    | XmlNote of Note
    | XmlChord of Chord

let private getTimeCode = function
    | XmlNote xn -> xn.Time
    | XmlChord xc -> xc.Time

type RequestTarget = ChordTarget of Chord | HandShapeTarget of HandShape

type TemplateRequest = { OriginalId: int16; NoteCount: byte; Target: RequestTarget }

let private getNoteCount (template: ChordTemplate) =
    template.Frets
    |> Array.fold (fun acc elem -> if elem >= 0y then acc + 1 else acc) 0

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

let private chooseEntities diffPercent (templates: ResizeArray<ChordTemplate>) (entities: XmlEntity array) =
    entities
    |> Array.choose (fun e ->
        match e with
        | XmlNote _ ->
            // TODO: Actual implementation
            Some (e, None)
        | XmlChord chord ->
            let template = templates.[int chord.ChordId]
            let noteCount = getNoteCount template

            // TODO: Techniques, link next

            // TODO: Chord notes thinning

            if diffPercent <= 17 && noteCount > 1 then
                let template = templates.[int chord.ChordId]
                let string = template.Frets |> Array.findIndex (fun x -> x <> -1y)
                let note = Note(Time = chord.Time, String = sbyte string, Fret = template.Frets.[string])
                Some (XmlNote note, None)
            elif diffPercent <= 34 && noteCount > 2 then
                let copy = Chord(chord)
                Some (XmlChord copy, Some { OriginalId = chord.ChordId; NoteCount = 2uy; Target = ChordTarget copy })
            elif diffPercent <= 51 && noteCount > 3 then
                let copy = Chord(chord)
                Some (XmlChord copy, Some { OriginalId = chord.ChordId; NoteCount = 3uy; Target = ChordTarget copy })
            elif diffPercent <= 68 && noteCount > 4 then
                let copy = Chord(chord)
                Some (XmlChord copy, Some { OriginalId = chord.ChordId; NoteCount = 4uy; Target = ChordTarget copy })
            elif diffPercent <= 85 && noteCount > 5 then
                let copy = Chord(chord)
                Some (XmlChord copy, Some { OriginalId = chord.ChordId; NoteCount = 5uy; Target = ChordTarget copy })
            else
                Some (e, None)
    )

let private chooseHandShapes diffPercent (templates: ResizeArray<ChordTemplate>) (handShapes: HandShape list) =
    handShapes
    |> List.choose (fun hs ->
        let template = templates.[int hs.ChordId]
        let noteCount = getNoteCount template

        if diffPercent <= 17 && noteCount > 1 then
            None
        elif diffPercent <= 34 && noteCount > 2 then
            let copy = HandShape(hs)
            Some (copy, Some { OriginalId = hs.ChordId; NoteCount = 2uy; Target = HandShapeTarget copy })
        elif diffPercent <= 51 && noteCount > 3 then
            let copy = HandShape(hs)
            Some (copy, Some { OriginalId = hs.ChordId; NoteCount = 3uy; Target = HandShapeTarget copy })
        elif diffPercent <= 68 && noteCount > 4 then
            let copy = HandShape(hs)
            Some (copy, Some { OriginalId = hs.ChordId; NoteCount = 4uy; Target = HandShapeTarget copy })
        elif diffPercent <= 85 && noteCount > 5 then
            let copy = HandShape(hs)
            Some (copy, Some { OriginalId = hs.ChordId; NoteCount = 5uy; Target = HandShapeTarget copy })
        else
            Some (hs, None)
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

                let newTemplate = ChordTemplate(template.Name, template.DisplayName, newFingers, newFrets)

                let id =
                    lock lockObj (fun _ ->
                        let existing = templates.FindIndex(fun x ->
                            x.DisplayName = newTemplate.DisplayName
                            && x.Name = newTemplate.Name
                            && x.Frets = newTemplate.Frets
                            && x.Fingers = newTemplate.Fingers)

                        match existing with
                        | -1 ->
                            let id = int16 templates.Count
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
        level.Anchors.AddRange(phraseData.Anchors)
        [| level |]
    else
        Array.init levelCount (fun diff ->
            // Copy everything for the hardest level
            if diff = levelCount - 1 then
                Level(sbyte diff,
                      ResizeArray(phraseData.Notes),
                      ResizeArray(phraseData.Chords),
                      ResizeArray(phraseData.Anchors),
                      ResizeArray(phraseData.HandShapes))
            else
                let diffPercent = 100 * diff / levelCount
                let entities = createXmlEntityArray phraseData.Notes phraseData.Chords
                let levelEntities, templateRequests1 =
                    chooseEntities diffPercent arr.ChordTemplates entities
                    |> Array.unzip
                let notes = levelEntities |> Array.choose (function XmlNote n -> Some n | _ -> None)
                let chords = levelEntities |> Array.choose (function XmlChord n -> Some n | _ -> None)
                let handShapes, templateRequests2 =
                    chooseHandShapes diffPercent arr.ChordTemplates phraseData.HandShapes
                    |> List.unzip

                templateRequests1
                |> Seq.append templateRequests2
                |> Seq.choose id
                |> Seq.iter (applyChordId arr.ChordTemplates)

                Level(sbyte diff,
                      ResizeArray(notes),
                      ResizeArray(chords),
                      ResizeArray(chooseAnchors levelEntities phraseData.Anchors phraseData.EndTime),
                      ResizeArray(handShapes))     
        )

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

    // TODO: Search for similar phrases
    // Use same name or create linked difficulty

    // TODO: Combine noguitar phrases

    arr.Levels <- ResizeArray(combinedLevels)
    arr.Phrases <- ResizeArray(phrases)
    arr.PhraseIterations <- ResizeArray(newPhraseIterations)

    arr

let generateForFile fileName targetFile =
    let arr =
        InstrumentalArrangement.Load fileName
        |> generateForArrangement

    arr.Save targetFile
