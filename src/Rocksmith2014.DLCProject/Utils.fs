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
let private standardTuningOffsets = [| 0; 5; 10; 3; 7; 0 |]

let private isDoubleDropTuning (first: int16) (strings: int16 array) =
    first = strings[1] - 2s && first = strings[5]
    && strings.AsSpan(1, 4).AllSame(strings[1])

let private (|Standard|Drop|DoubleDrop|Other|) (strings: int16 array) =
    let first = strings[0]

    // Standard tunings
    if first > -11s && first < 3s && strings.AsSpan().AllSame(first) then
        Standard
    // Drop tunings
    elif first = strings[1] - 2s && strings.AsSpan(1).AllSame(strings[1]) then
        Drop
    // Double drop tunings
    elif isDoubleDropTuning first strings then
        DoubleDrop
    // Other tunings
    else
        Other

type TuningName =
    | TranslatableTuning of string * obj array
    | CustomTuning of string

let private getStringNoteName (useFlats: bool) (stringIndex: int) (stringTuning: int16) =
    let m = (int stringTuning + standardTuningOffsets[stringIndex]) % 12
    let i = if m < 0 then roots.Length + m else m
    let n = roots[i]
    if useFlats then
         match n with
         | "F#" -> "Gb"
         | "C#" -> "Db"
         | _ -> n
    else
        n

/// Returns a name for the given tuning.
let getTuningName (tuning: int16 array) : TuningName =
    let first = tuning[0]

    match tuning with
    | Standard ->
        let n = getStringNoteName false 0 first
        TranslatableTuning("Standard", [| n |])
    | Drop ->
        let n1 = getStringNoteName true 0 first
        // Low E string index (0) is used to get the note name for the low string in standard tuning
        let n2 = getStringNoteName true 0 tuning[1]
        let root = if first < -2s then n2 + " " else String.Empty
        TranslatableTuning("Drop", [| root; n1 |])
    | DoubleDrop ->
        let n = getStringNoteName true 0 first
        TranslatableTuning("Double Drop", [| n |])
    | Other ->
        match tuning with
        | [| -2s;  0s; 0s; -1s; -2s; -2s |] -> TranslatableTuning("OpenTuning", [| "D" |])
        | [|  0s;  0s; 2s;  2s;  2s;  0s |] -> TranslatableTuning("OpenTuning", [| "A" |])
        | [| -2s; -2s; 0s;  0s;  0s; -2s |] -> TranslatableTuning("OpenTuning", [| "G" |])
        | [|  0s;  2s; 2s;  1s;  0s;  0s |] -> TranslatableTuning("OpenTuning", [| "E" |])
        | _ ->
            tuning
            |> Array.mapi (getStringNoteName true)
            |> String.Concat
            |> CustomTuning

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
let getCustomFontName (isJapanese: bool) (dlcName: string) =
    let separator = if isJapanese then "" else "v_"
    $"lyrics_{separator}{dlcName}"
