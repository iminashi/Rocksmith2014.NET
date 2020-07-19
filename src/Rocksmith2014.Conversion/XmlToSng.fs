module Rocksmith2014.Conversion.XmlToSng

open Rocksmith2014
open Rocksmith2014.SNG.Types

let msToSec (time:int) = float32 time / 1000.0f

let convertVocal (xmlVocal:XML.Vocal) =
    { Time = msToSec xmlVocal.Time
      Length = msToSec xmlVocal.Length
      Lyric = xmlVocal.Lyric
      Note = int xmlVocal.Note }

let convertPhrase phraseId (xml:XML.InstrumentalArrangement) (xmlPhrase:XML.Phrase) =
    let piLinks =
        xml.PhraseIterations
        |> Seq.filter (fun pi -> pi.PhraseId = phraseId)
        |> Seq.length

    { Solo = if (xmlPhrase.Mask &&& XML.PhraseMask.Solo) <> XML.PhraseMask.None then 1y else 0y
      Disparity = if (xmlPhrase.Mask &&& XML.PhraseMask.Disparity) <> XML.PhraseMask.None then 1y else 0y
      Ignore = if (xmlPhrase.Mask &&& XML.PhraseMask.Ignore) <> XML.PhraseMask.None then 1y else 0y
      MaxDifficulty = int xmlPhrase.MaxDifficulty
      PhraseIterationLinks = piLinks
      Name = xmlPhrase.Name }

let convertChord (xmlChord:XML.ChordTemplate) =
    let mask =
        if xmlChord.DisplayName.EndsWith("-arp") then
            ChordMask.Arpeggio
        elif xmlChord.DisplayName.EndsWith("-nop") then
            ChordMask.Nop
        else
            ChordMask.None

    { Mask = mask
      Frets = Array.copy xmlChord.Frets
      Fingers = Array.copy xmlChord.Fingers
      Notes = Array.zeroCreate 6 // TODO: Implement
      Name = xmlChord.Name }

let convertBendValue (xmlBv:XML.BendValue) =
    { Time = msToSec xmlBv.Time
      Step = xmlBv.Step
      Unk3 = 0s
      Unk4 = 0y
      Unk5 = 0y }

let convertPhraseIteration index (xml:XML.InstrumentalArrangement) (xmlPi:XML.PhraseIteration) =
    let endTime =
        if index = xml.PhraseIterations.Count - 1 then
            msToSec xml.SongLength
        else
            msToSec xml.PhraseIterations.[index + 1].Time

    { PhraseId = xmlPi.PhraseId
      StartTime = msToSec xmlPi.Time
      NextPhraseTime = endTime
      Difficulty = [| int xmlPi.HeroLevels.Easy; int xmlPi.HeroLevels.Medium; int xmlPi.HeroLevels.Hard |] }

let convertNLD (xmlNLD:XML.NewLinkedDiff) =
    { LevelBreak = int xmlNLD.LevelBreak
      NLDPhrases = Array.ofSeq xmlNLD.PhraseIds }

let convertEvent (xmlEvent:XML.Event) =
    { Time = msToSec xmlEvent.Time
      Name = xmlEvent.Code }