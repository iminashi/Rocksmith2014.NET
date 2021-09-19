module Rocksmith2014.DD.AnchorChooser

open Rocksmith2014.XML

// Assume that notes up to 3ms before the anchor were meant to be on the anchor
let [<Literal>] private ErrorMargin = 3

let private shouldInclude (entities: XmlEntity array) startTime endTime =
    entities
    |> Array.exists (fun e ->
        let time = getTimeCode e
        let sustain = getSustain e
        time + ErrorMargin >= startTime && time < endTime
        ||
        time + sustain + ErrorMargin >= startTime && time + sustain < endTime)

/// Chooses the necessary anchors for a difficulty level.
let choose (entities: XmlEntity array) (anchors: Anchor list) phraseStartTime phraseEndTime =
    let rec add result (anchors: Anchor list) =
        match anchors with
        | [] ->
            result
        | a :: tail ->
            let endTime =
                match tail with
                | a2 :: _ -> a2.Time
                | [] -> phraseEndTime

            let result =
                if a.Time = phraseStartTime || shouldInclude entities a.Time endTime then
                    a :: result
                else
                    result

            add result tail

    add [] anchors
    |> List.rev
