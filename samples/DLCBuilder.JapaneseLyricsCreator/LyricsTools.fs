module JapaneseLyricsCreator.LyricsTools

open Rocksmith2014.XML
open System

type String with
    member this.TryFirstChar() =
        if this.Length = 0 then
            ValueNone
        else
            ValueSome this[0]

let private backCombiningChars = Set.ofList [ 'ゃ'; 'ゅ'; 'ょ'; 'ャ'; 'ュ'; 'ョ'; 'ァ'; 'ィ'; 'ゥ'; 'ェ'; 'ォ'; 'ぁ'; 'ぃ'; 'ぅ'; 'ぇ'; 'ぉ'; 'っ'; 'ー' ]
let private forwardCombiningChars = Set.ofList [ '“'; '「'; '｢'; '『'; '['; '('; '〔'; '【'; '〈'; '《'; '«' ]

let isPunctuation (c: char) = Char.IsPunctuation(c)
let isSpace (c: char) = Char.IsWhiteSpace(c)
let isBackwardsCombining (c: char) = backCombiningChars.Contains(c)
let isForwardsCombining (c: char) = forwardCombiningChars.Contains(c)
let isCommonLatin (c: char) = c < char 0x0100

let withoutTrailingDash (str: String) =
    if str.EndsWith "-" then
        str.AsSpan(0, str.Length - 1)
    else
        str.AsSpan()

let private revCharListToString = List.rev >> Array.ofList >> String

/// Hyphenates a string of Japanese into a list of kanji/kana.
let hyphenate (str: string) =
    let rec getSyllables (results: string list) (current: char list) (characters: char list) =
        match characters with
        | a :: rest when isSpace a ->
            let res = current |> revCharListToString

            getSyllables (res :: results) [] rest

        | a :: rest when not <| isForwardsCombining a && (isPunctuation a || isBackwardsCombining a || isCommonLatin a) ->
            let current = a :: current

            getSyllables results current rest

        | a :: rest ->
            let results, current =
                match current with
                | [] ->
                    results, [ a ]
                | current when List.forall isPunctuation current ->
                    results, a :: current
                | current ->
                    let result = '-' :: current |> revCharListToString
                    result :: results, [ a ]

            getSyllables results current rest

        | [] ->
            let result = current |> revCharListToString
            result :: results

    str
    |> List.ofSeq
    |> getSyllables [] []
    |> List.rev

/// Converts a string of Japanese into an array of hyphenated lines.
let hyphenateToSyllableLines (str: string) =
    str.Split('\n')
    |> Array.map (hyphenate >> List.filter String.notEmpty >> List.toArray)
    |> Array.filter (Array.isEmpty >> not)

/// Splits a matched syllable sequence into lines with the character '+' as a line break.
let toLines (vocals: MatchedSyllable seq) =
    (([], []), vocals)
    ||> Seq.fold (fun (lines, currentLine) elem ->
        if elem.Vocal.Lyric.EndsWith('+') then
            let result = (elem :: currentLine) |> List.rev
            result :: lines, []
        else
            lines, elem :: currentLine)
    |> fst
    |> List.rev
    |> List.map List.toArray
    |> List.toArray

/// Tries to find the word in the syllable array and returns the hyphenated word if successful.
let matchHyphenation (word: string) (syllables: string array) =
    let startIndex =
        syllables
        |> Array.tryFindIndex (fun x ->
            x.EndsWith('-') &&
            word.AsSpan().StartsWith(x.AsSpan(0, x.Length - 1), StringComparison.OrdinalIgnoreCase))

    match startIndex with
    | Some startIndex ->
        let syllables = syllables[startIndex..]

        let wordEnd =
            syllables
            |> Array.findIndex (fun x -> not <| x.EndsWith('-'))

        let hyphenated =
            syllables
            |> Array.take (wordEnd + 1)

        let completeWord =
            hyphenated
            |> Array.map (fun x -> if x.EndsWith('-') || x.EndsWith('+') then x.Substring(0, x.Length - 1) else x)
            |> String.Concat

        if String.equalsIgnoreCase completeWord word then
            hyphenated
        else
            Array.singleton word
    | None ->
        Array.singleton word

/// Tries to match non-Japanese words included in the Japanese lines to the hyphenation found in the matched lines.
let matchNonJapaneseHyphenation (matchedLines: MatchedSyllable array array) (japaneseLines: String array array) =
    japaneseLines
    |> Array.mapi (fun lineNumber line ->
        line
        |> Array.collect (fun word ->
            match word.TryFirstChar() with
            | ValueSome firstChar when isCommonLatin firstChar ->
                let words =
                    matchedLines
                    |> Array.tryItem lineNumber
                    |> Option.map (Array.map (fun x -> x.Vocal.Lyric))

                match words with
                | Some words ->
                    matchHyphenation word words
                | None ->
                    Array.singleton word
            | _ ->
               Array.singleton word))

/// Applies the given modifications to the lines of Japanese kanji/kana.
let applyModifications (modifications: FusionOrSplit list) (japaneseLines: string array array) =
    japaneseLines
    |> Array.mapi (fun lineNumber line ->
        match modifications |> List.filter (fun m -> m.LineNumber = lineNumber) with
        | [] ->
            line
        | modificationsForLine ->
            // Apply the modifications in reverse order (i.e. oldest to newest)
            let mods = List.rev modificationsForLine

            (line, mods)
            ||> List.fold (fun acc modification ->
                acc
                |> Array.collecti (fun i word ->
                    match modification with
                    | Fusion { Index = modIndex } ->
                        if i = modIndex && i + 1 < acc.Length then
                            [| String.Concat(withoutTrailingDash word, acc[i + 1].AsSpan()) |]
                        elif modIndex + 1 = i then
                            // Remove the word that was combined to the previous
                            Array.empty
                        else
                            [| word |]
                    | Split { Index = modIndex } ->
                        if i = modIndex && (withoutTrailingDash word).Length > 1 then
                            [| String.Concat(word.Substring(0, 1), "-"); word.Substring(1) |]
                        else
                            [| word |]
                )
            )
    )

let createJapaneseLines matchedLines modifications japaneseText =
    japaneseText
    |> hyphenateToSyllableLines
    |> matchNonJapaneseHyphenation matchedLines
    |> applyModifications modifications

let combineVocals (v1: Vocal) (v2: Vocal) =
    let lyric =
        String.Concat(withoutTrailingDash v1.Lyric, v2.Lyric.AsSpan())

    Vocal(v1.Time, (v2.Time + v2.Length) - v1.Time, lyric, v1.Note)
