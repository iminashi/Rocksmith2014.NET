module Rocksmith2014.DD.HandShapeChooser

open Rocksmith2014.XML

let private isInsideHandShape (hs: HandShape) time =
    time >= hs.StartTime && time < hs.EndTime

let private noNotesInHandShape (entities: XmlEntity array) (hs: HandShape) =
    entities
    |> Array.tryFind (fun entity ->
        let time = getTimeCode entity
        isInsideHandShape hs time)
    |> Option.isNone

let private isArpeggio (entities: XmlEntity array) (hs: HandShape) =
    let handShapeNotes =
        entities
        |> Array.choose (fun entity ->
            match entity with
            | XmlNote n when isInsideHandShape hs n.Time -> Some n
            | _ -> None)
    match handShapeNotes with
    | [||] ->
        false
    | notes ->
        notes
        |> Array.forall (fun n -> n.String = notes.[0].String)
        |> not

let choose diffPercent
           (entities: XmlEntity array)
           (templates: ResizeArray<ChordTemplate>)
           (handShapes: HandShape list) =
    // TODO: Special handling for empty handshapes

    handShapes
    |> List.choose (fun hs ->
        let template = templates.[int hs.ChordId]
        let noteCount = getNoteCount template

        if noNotesInHandShape entities hs then
            None
        elif isArpeggio entities hs then
            // Always use the full handshape for arpeggios
            Some (hs, None)
        elif diffPercent <= 17uy && noteCount > 1 then
            None
        elif diffPercent <= 34uy && noteCount > 2 then
            let copy = HandShape(hs)
            Some (copy, Some { OriginalId = hs.ChordId; NoteCount = 2uy; Target = HandShapeTarget copy })
        elif diffPercent <= 51uy && noteCount > 3 then
            let copy = HandShape(hs)
            Some (copy, Some { OriginalId = hs.ChordId; NoteCount = 3uy; Target = HandShapeTarget copy })
        elif diffPercent <= 68uy && noteCount > 4 then
            let copy = HandShape(hs)
            Some (copy, Some { OriginalId = hs.ChordId; NoteCount = 4uy; Target = HandShapeTarget copy })
        elif diffPercent <= 85uy && noteCount > 5 then
            let copy = HandShape(hs)
            Some (copy, Some { OriginalId = hs.ChordId; NoteCount = 5uy; Target = HandShapeTarget copy })
        else
            Some (hs, None)
    )
