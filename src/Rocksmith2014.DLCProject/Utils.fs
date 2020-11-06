module Rocksmith2014.DLCProject.Utils

open System

/// Converts a tuning pitch into cents.
let tuningPitchToCents (pitch: float) =
    Math.Round(1200. * Math.Log(pitch / 440.) / Math.Log(2.))

/// Converts a cent value into a tuning pitch value in hertz.
let centsToTuningPitch (cents: float) =
    Math.Round(440. * Math.Pow(Math.Pow(2., 1. / 1200.), cents), 2)

let private roots = [| "E"; "F"; "F#"; "G"; "Ab"; "A"; "Bb"; "B"; "C"; "C#"; "D"; "Eb" |]

let getTuningString (tuning : int16 array) =
    let first = tuning.[0]

    // Standard tunings
    if first > -11s && first < 3s && Array.forall ((=) first) tuning then
        let i = int (first + 12s) % 12
        roots.[i] + " Standard"
    // Drop tunings
    elif Array.forall ((=) tuning.[1]) tuning.[2..] && first = tuning.[1] - 2s then
        let i = int (first + 12s) % 12
        let j = int (tuning.[1] + 12s) % 12
        let root = if first < -2s then roots.[j] + " " else String.Empty
        let drop = if roots.[i] = "C#" then "Db" else roots.[i]
        root + "Drop " + drop
    else
        // Other tunings
        match tuning with
        | [| -2s;  0s; 0s; -1s; -2s; -2s |] -> "Open D"
        | [|  0s;  0s; 2s;  2s;  2s;  0s |] -> "Open A"
        | [| -2s; -2s; 0s;  0s;  0s; -2s |] -> "Open G"
        | [|  0s;  2s; 2s;  1s;  0s;  0s |] -> "Open E"
        | [| -2s;  0s; 0s;  0s; -2s; -2s |] -> "DADGAD"
        | _ -> String.Empty
