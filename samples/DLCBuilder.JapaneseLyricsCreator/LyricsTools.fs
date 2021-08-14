module JapaneseLyricsCreator.LyricsTools

open Rocksmith2014.Common
open Rocksmith2014.XML
open System
open System.Text.RegularExpressions

type String with
    member this.TryFirstChar() =
        if this.Length = 0 then
            ValueNone
        else
            ValueSome this.[0]

let private combiningRegex = Regex("[ゃゅょャュョァィゥェォぁぃぅぇぉっー]", RegexOptions.Compiled)

let isPunctuation (c: char) = Char.IsPunctuation(c)
let isSpace (c: char) = Char.IsWhiteSpace(c)
let isBackwardsCombining (c: char) = combiningRegex.IsMatch(string c)
let isCombining (c: char) = isPunctuation c
let isCommonLatin (c: char) = c < char 0x0100

let withoutTrailingDash (str: String) =
    if str.EndsWith "-" then str.Substring(0, str.Length - 1) else str

let private revCharListToString = List.rev >> Array.ofList >> String

let hyphenate (str: string) =
    let rec getSyllables (results: string list) current list =
        match list with
        | a::rest when isSpace a ->
            let results =
                let res = current |> revCharListToString
                (string a)::res::results

            getSyllables results [] rest

        | a::rest when isCombining a || isBackwardsCombining a || isCommonLatin a ->
            let current = a::current

            getSyllables results current rest

        | a::rest ->
            let results, current =
                match current with
                | [] ->
                    results, [ a ]
                | [ x ] when isSpace x ->
                    results, [ a ]
                | [ x ] when isCombining x ->
                    results, [ a; x ]
                | current ->
                    let result = '-'::current |> revCharListToString
                    result::results, [ a ]

            getSyllables results current rest

        | [] ->
            let result = current |> revCharListToString
            result::results

    str
    |> List.ofSeq
    |> getSyllables [] []
    |> List.rev

let hyphenateToSyllableLines (str: string) =
    str.Split('\n')
    |> Array.map (hyphenate >> List.filter String.notEmpty >> List.toArray)
    |> Array.filter (Array.isEmpty >> not)

let toLines (vocals: MatchedSyllable seq) =
    vocals
    |> Seq.fold (fun (lines, currentLine) elem ->
        if elem.Vocal.Lyric.EndsWith "+" then
            let result = (elem::currentLine) |> List.rev
            result::lines, []
        else
            lines, elem::currentLine) ([], [])
    |> fst
    |> List.rev
    |> List.map List.toArray
    |> List.toArray

let matchHyp (oneWord: string) (manyWords: string array) =
    let startIndex =
        manyWords
        |> Array.tryFindIndex (fun x ->
            x.EndsWith "-" &&
            oneWord.StartsWith(x.Substring(0, x.Length - 1), StringComparison.OrdinalIgnoreCase))

    match startIndex with
    | Some startIndex ->
        let manyWords = manyWords.[startIndex..]

        let wordEnd =
            manyWords
            |> Array.findIndex (fun x -> not <| x.EndsWith "-")

        let hyphenated =
            manyWords
            |> Array.take (wordEnd + 1)

        let completeWord =
            hyphenated
            |> Array.map (fun x -> if x.EndsWith "-" || x.EndsWith "+" then x.Substring(0, x.Length - 1) else x)
            |> String.concat ""

        if completeWord.Equals(oneWord, StringComparison.OrdinalIgnoreCase) then
            hyphenated
        else
            Array.singleton oneWord
    | None ->
        Array.singleton oneWord

let matchNonJapaneseHyphenation (matchedLines: MatchedSyllable array array) (japaneseLines: String array array) =
    japaneseLines
    |> Array.mapi (fun lineNumber line ->
        line
        |> Array.map (fun word ->
            match word.TryFirstChar() with
            | ValueSome firstChar when isCommonLatin firstChar ->
                let words =
                    matchedLines
                    |> Array.tryItem lineNumber
                    |> Option.map (Array.map (fun x -> x.Vocal.Lyric))

                match words with
                | Some words ->
                    matchHyp word words
                | None ->
                    Array.singleton word
            | _ ->
               Array.singleton word)
        |> Array.collect id)

let applyCombinations (replacements: CombinationLocation list) (japaneseLines: string array array) =
    japaneseLines
    |> Array.mapi (fun lineNumber line ->
        let rep =
            replacements
            |> List.filter (fun x -> x.LineNumber = lineNumber)

        if rep.IsEmpty then
            line
        else
            let repIndexes =
                rep
                |> List.map (fun x -> x.Index)
                // Apply the replacements in reverse order (i.e. oldest to newest)
                |> List.rev

            (line, repIndexes)
            ||> List.fold (fun acc replacementIndex ->
                acc
                |> Array.mapi (fun i word ->
                    if i = replacementIndex && i + 1 < acc.Length then
                        Some $"{withoutTrailingDash word}{acc.[i + 1]}"
                    elif replacementIndex = i - 1 then
                        None
                    else
                        Some word)
                |> Array.choose id))

let createJapaneseLines matchedLines combinedJapanese japaneseText =
    japaneseText
    |> hyphenateToSyllableLines
    |> matchNonJapaneseHyphenation matchedLines
    |> applyCombinations combinedJapanese

let combineVocals (v1: Vocal) (v2: Vocal) =
    let lyric = $"{withoutTrailingDash v1.Lyric}{v2.Lyric}"
    Vocal(v1.Time, (v2.Time + v2.Length) - v1.Time, lyric, v1.Note)
