module HandShapes

open Rocksmith2014.EOF.EOFTypes
open Rocksmith2014.XML

let private handShapeNotNeeded isArpeggio (notesInHs: EOFNote array) =
    let b = notesInHs |> Array.tryHead |> Option.map (fun x -> x.BitFlag)
    match b with
    | None ->
        // Should never get here
        // There should be always at least a full ghost chord at the start of the handshape
        false
    | Some b ->
        // Exclude if not an arpeggio and all the chords are the same in the handshape
        not isArpeggio
        && notesInHs |> Array.forall (fun n -> n.BitFlag = b && n.Flags &&& EOFNoteFlag.SPLIT = EOFNoteFlag.ZERO)

let convertHandShapes (inst: InstrumentalArrangement) (notes: EOFNote array) =
    inst.Levels
    |> Seq.mapi (fun diff level ->
        let diff = byte diff

        level.HandShapes.ToArray()
        |> Array.map (fun hs ->
            let notesInHs =
                notes
                |> Array.filter (fun n ->
                    n.Difficulty = diff && (int n.Position >= hs.StartTime && int n.Position < hs.EndTime))

            let isArpeggio = inst.ChordTemplates[int hs.ChordId].IsArpeggio

            if handShapeNotNeeded isArpeggio notesInHs then
                let updates =
                    notesInHs
                    |> Array.mapi (fun i n ->
                        match notesInHs |> Array.tryItem (i + 1) with
                        | Some next ->
                            { Difficulty = diff; Time = n.Position; NewSustain = next.Position - n.Position - 5u }
                        | None ->
                            { Difficulty = diff; Time = n.Position; NewSustain = uint hs.EndTime - n.Position })

                AdjustSustains updates
            else
                EOFSection.Create(diff, uint hs.StartTime, uint hs.EndTime, if isArpeggio then 0u else 2u)
                |> SectionCreated
        )
    )
    |> Array.concat
