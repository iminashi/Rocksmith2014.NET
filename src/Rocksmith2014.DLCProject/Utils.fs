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

let private isDoubleDropTuning first (strings: int16 array) =
    first > -12s && first < -1s
    && first = strings[1] - 2s && first = strings[5]
    && strings.AsSpan(1, 4).AllSame(strings[1])

let private (|Standard|Drop|DoubleDrop|Other|) (strings: int16 array) =
    let first = strings[0]

    // Standard tunings
    if first > -11s && first < 3s && strings.AsSpan().AllSame(first) then
        Standard
    // Drop tunings
    elif first > -12s && first = strings[1] - 2s && strings.AsSpan(1).AllSame(strings[1]) then
        Drop
    // Double drop tunings
    elif isDoubleDropTuning first strings then
        DoubleDrop
    // Other tunings
    else
        Other

/// Returns the type of the given tuning and its root note(s).
let getTuningName (tuning: int16 array) : string * obj array =
    let first = tuning[0]

    match tuning with
    | Standard ->
        let i = int (first + 12s) % 12
        "Standard", [| roots[i] |]
    | Drop ->
        let i = int (first + 12s) % 12
        let j = int (tuning[1] + 12s) % 12
        let root = if first < -2s then roots[j] + " " else String.Empty
        let drop = if roots[i] = "C#" then "Db" else roots[i]
        "Drop", [| root; drop |]
    | DoubleDrop ->
        let i = int (first + 12s) % 12
        let drop = if roots[i] = "C#" then "Db" else roots[i]
        "Double Drop", [| drop |]
    | Other ->
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

/// Returns the filename for a custom font.
let getCustomFontName isJapanese dlcName =
    let separator = if isJapanese then "" else "v_"
    $"lyrics_{separator}{dlcName}"
