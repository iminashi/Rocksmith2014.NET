namespace JapaneseLyricsCreator

open Rocksmith2014.XML

type MatchedSyllable =
    { Vocal: Vocal
      Japanese: string option }

type TargetLocation =
    { LineNumber: int
      Index: int }

type FusionOrSplit =
    | Fusion of TargetLocation
    | Split of TargetLocation

    member this.LineNumber =
        match this with
        | Fusion c -> c.LineNumber
        | Split s -> s.LineNumber

type Msg =
    | SetJapaneseLyrics of lyrics: string
    | CombineSyllableWithNext of TargetLocation
    | CombineJapaneseWithNext of TargetLocation
    | SplitJapanese of TargetLocation
    | UndoLyricsChange
    | SaveLyricsToFile of targetPath: string

type Effect =
    | Nothing
    | AddVocalsToProject of xmlPath: string
