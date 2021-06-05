[<AutoOpen>]
module Rocksmith2014.DD.Types

open Rocksmith2014.XML
open System.Collections.Generic

type XmlEntity =
    | XmlNote of xmlNote: Note
    | XmlChord of xmlChord: Chord

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

type GeneratorConfig =
    { PhraseSearch: PhraseSearch
      LevelCountGeneration: LevelCountGeneration }

type internal DifficultyRange = { Low: float; High: float }

type internal BeatDivision = int

type internal DivisionMap = IReadOnlyDictionary<BeatDivision, DifficultyRange>
