module Rocksmith2014.DLCProject.Utils

open System
open FSharp.Extensions

/// Converts a tuning pitch into cents.
let tuningPitchToCents (pitch: float) =
    Math.Round(1200. * log (pitch / 440.) / log 2.)

/// Converts a cent value into a tuning pitch value in hertz.
let centsToTuningPitch (cents: float) =
    Math.Round(440. * Math.Pow(Math.Pow(2., 1. / 1200.), cents), 2)

let private roots = [| "E"; "F"; "F#"; "G"; "Ab"; "A"; "Bb"; "B"; "C"; "C#"; "D"; "Eb" |]

/// Returns the type of the given tuning and its root note(s).
let getTuningName (tuning: int16 array) : string * obj array =
    let first = tuning[0]

    // Standard tunings
    if first > -11s && first < 3s && tuning.AsSpan().AllSame(first) then
        let i = int (first + 12s) % 12
        "Standard", [| roots[i] |]
    // Drop tunings
    elif first > -12s && first = tuning[1] - 2s && tuning.AsSpan(1).AllSame(tuning[1]) then
        let i = int (first + 12s) % 12
        let j = int (tuning[1] + 12s) % 12
        let root = if first < -2s then roots[j] + " " else String.Empty
        let drop = if roots[i] = "C#" then "Db" else roots[i]
        "Drop", [| root; drop |]
    else
        // Other tunings
        match tuning with
        | [| -2s;  0s; 0s; -1s; -2s; -2s |] -> "Open", [| "D" |]
        | [|  0s;  0s; 2s;  2s;  2s;  0s |] -> "Open", [| "A" |]
        | [| -2s; -2s; 0s;  0s;  0s; -2s |] -> "Open", [| "G" |]
        | [|  0s;  2s; 2s;  1s;  0s;  0s |] -> "Open", [| "E" |]
        | [| -2s;  0s; 0s;  0s; -2s; -2s |] -> "DADGAD", Array.empty
        | _ -> "Custom Tuning", Array.empty

/// If the exception is an aggregate exception, returns the distinct inner exception messages concatenated.
let distinctExceptionMessages (e: exn) =
    match e with
    | :? AggregateException as a ->
        a.InnerExceptions
        |> Seq.map (fun x -> x.Message)
        |> Seq.distinct
        |> String.concat ", "
    | _ ->
        e.Message
