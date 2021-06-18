module Rocksmith2014.DD.HandShapeChooser

open Rocksmith2014.XML

let private isInsideHandShape (hs: HandShape) time =
    time >= hs.StartTime && time < hs.EndTime

let private noNotesInHandShape (entities: XmlEntity array) (hs: HandShape) =
    entities
    |> Array.exists (fun x ->
        let time = getTimeCode x
        time |> (isInsideHandShape hs)
        || time + getSustain x |> (isInsideHandShape hs))
    |> not

let private isArpeggio (entities: XmlEntity array) (hs: HandShape) =
    let handShapeNotes =
        entities
        |> Array.choose (function
            | XmlNote n when isInsideHandShape hs n.Time ->
                Some n
            | _ ->
                None)

    match handShapeNotes with
    | [||] ->
        false
    | notes ->
        notes
        |> Array.forall (fun n -> n.String = notes.[0].String || n.Time = notes.[0].Time)
        |> not

let choose (diffPercent: float)
           (levelEntities: XmlEntity array)
           (allEntities: XmlEntity array)
           (maxChordNotes: int)
           (templates: ResizeArray<ChordTemplate>)
           (handShapes: HandShape list) =
    let allowedNotes = Utils.getAllowedChordNotes diffPercent maxChordNotes

    handShapes
    |> List.choose (fun hs ->
        let template = templates.[int hs.ChordId]
        let noteCount = getNoteCount template

        if noNotesInHandShape levelEntities hs then
            None
        elif isArpeggio allEntities hs then
            // Always use the full handshape for arpeggios
            Some (hs, None)
        elif allowedNotes <= 1 then
            None
        elif allowedNotes >= noteCount then
            Some (hs, None)
        else
            let copy = HandShape(hs)
            let request = { OriginalId = hs.ChordId
                            NoteCount = byte allowedNotes
                            Target = HandShapeTarget copy }
            Some (copy, Some request)
    )
