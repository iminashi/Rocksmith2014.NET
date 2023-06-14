module Rocksmith2014.XML.Processing.VocalsChecker

open Rocksmith2014.XML
open System.Text
open Utils

let [<Literal>] LyricsCharset =
    """ !"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\]^_abcdefghijklmnopqrstuvwxyz{|}~¡¢¥¦§¨ª«°²³´•¸¹º»¼½¾¿ÀÁÂÄÅÆÇÈÉÊËÌÎÏÑÒÓÔÖØÙÚÛÜÞßàáâäåæçèéêëìíîïñòóôöøùúûüŒœŠšž„…€™␀★➨"""

let [<Literal>] private MaxLengthExcludingNullTerminator = 47

let private findInvalidCharLyric (vocals: ResizeArray<Vocal>) =
    vocals
    |> ResizeArray.tryPick (fun vocal ->
        vocal.Lyric
        |> Seq.tryFindIndex (LyricsCharset.Contains >> not)
        |> Option.map (fun i -> vocal, vocal.Lyric[i]))
    |> Option.map (fun (invalidVocal, invalidChar) ->
        issue (LyricWithInvalidChar invalidChar) invalidVocal.Time)

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
let check hasCustomFont (vocals: ResizeArray<Vocal>) =
    [ // Check for too long lyrics
      yield! findLongLyrics vocals

      if hasNoLineBreaks vocals then
        yield issue LyricsHaveNoLineBreaks 0

      // Check for characters not included in the default font
      if not hasCustomFont then
         yield! findInvalidCharLyric vocals |> Option.toList ]
