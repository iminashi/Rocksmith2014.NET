module Rocksmith2014.XML.Processing.VocalsChecker

open Rocksmith2014.XML
open System
open System.Text
open Utils

let [<Literal>] LyricsCharset =
    """ !"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\]^_abcdefghijklmnopqrstuvwxyz{|}~¡¢¥¦§¨ª«°²³´•¸¹º»¼½¾¿ÀÁÂÄÅÆÇÈÉÊËÌÎÏÑÒÓÔÖØÙÚÛÜÞßàáâäåæçèéêëìíîïñòóôöøùúûüŒœŠšž„…€™␀★➨"""

let [<Literal>] private MaxLengthExcludingNullTerminator = 47

let private findInvalidCharLyric (customCharSet: string option) (vocals: ResizeArray<Vocal>) =
    let charSet =
        customCharSet
        |> Option.defaultValue LyricsCharset

    vocals
    |> ResizeArray.tryPick (fun vocal ->
        let actualLyric =
            let l = vocal.Lyric
            if l.EndsWith('-') || l.EndsWith('+') then
                l.AsSpan(0, l.Length - 1)
            else
                l.AsSpan()

        let index = actualLyric.IndexOfAnyExcept(charSet.AsSpan())
        if index < 0 then
            None
        else
            Some (vocal, actualLyric[index]))
    |> Option.map (fun (invalidVocal, invalidChar) ->
        issue (LyricWithInvalidChar(invalidChar, customCharSet.IsSome)) invalidVocal.Time)

let private isTooLong (vocal: Vocal) =
    Encoding.UTF8.GetByteCount(vocal.Lyric) > MaxLengthExcludingNullTerminator

let private findLongLyrics (vocals: ResizeArray<Vocal>) =
    vocals
    |> Seq.filter isTooLong
    |> Seq.map (fun vocal -> issue (LyricTooLong vocal.Lyric) vocal.Time)

let private hasNoLineBreaks (vocals: ResizeArray<Vocal>) =
    vocals
    // EOF may include a line break for the last vocal
    |> Seq.take (vocals.Count - 1)
    |> Seq.exists (fun vocal -> vocal.Lyric.EndsWith('+'))
    |> not

/// Checks the vocals for issues.
let check (customFont: GlyphDefinitions option) (vocals: ResizeArray<Vocal>) =
    let customCharSet =
        customFont
        |> Option.map (fun defs ->
            defs.Glyphs
            |> Seq.map (fun g -> g.Symbol)
            |> String.Concat)

    [
        // Check for too long lyrics
        yield! findLongLyrics vocals

        if hasNoLineBreaks vocals then
            yield GeneralIssue LyricsHaveNoLineBreaks

        // Check for characters not included in the font (default or custom)
        yield! findInvalidCharLyric customCharSet vocals |> Option.toList
    ]
