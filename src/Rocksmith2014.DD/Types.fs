[<AutoOpen>]
module Rocksmith2014.DD.Types

open Rocksmith2014.XML
open System.Collections.Generic

type RequestTarget =
    | ChordTarget of chordTarget: Chord
    | HandShapeTarget of handshapeTarget: HandShape

type TemplateRequest =
    { OriginalId: int16
      NoteCount: byte
      Target: RequestTarget }

type PhraseSearch =
    | SearchDisabled
    | WithThreshold of threshold: int

[<RequireQualifiedAccess>]
type LevelCountGeneration =
    | Simple
    | MLModel
    /// Generates the same number of levels for all phrases. For testing purposes.
    | Constant of levelCount: int

type GeneratorConfig =
    { PhraseSearch: PhraseSearch
      LevelCountGeneration: LevelCountGeneration }

type internal DifficultyRange = { Low: float; High: float }

type internal BeatDivision = int

type internal DivisionMap = IReadOnlyDictionary<BeatDivision, DifficultyRange>
