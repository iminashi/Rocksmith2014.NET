[<AutoOpen>]
module Rocksmith2014.XML.Processing.Types

type IssueType =
    | ApplauseEventWithoutEnd
    | EventBetweenIntroApplause of eventCode: string
    | NoteLinkedToChord
    | LinkNextMissingTargetNote
    | LinkNextSlideMismatch
    | LinkNextFretMismatch
    | LinkNextBendMismatch
    | IncorrectLinkNext
    | UnpitchedSlideWithLinkNext
    | PhraseChangeOnLinkNextNote
    | DoubleHarmonic
    | SeventhFretHarmonicWithSustain
    | MissingBendValue
    | OverlappingBendValues
    | ToneChangeOnNote
    | NoteInsideNoguitarSection
    | MissingLinkNextChordNotes
    | ChordAtEndOfHandShape
    | FingeringAnchorMismatch
    | PossiblyWrongChordFingering
    | BarreOverOpenStrings
    | MutedStringInNonMutedChord
    | AnchorInsideHandShape
    | AnchorInsideHandShapeAtPhraseBoundary
    | AnchorCloseToUnpitchedSlide
    | AnchorNotOnNote of distance: int
    | FirstPhraseNotEmpty
    | NoEndPhrase
    | MoreThan100Phrases
    | HopoIntoSameNote
    | FingerChangeDuringSlide
    | PositionShiftIntoPullOff
    | LyricWithInvalidChar of invalidChar: char
    | LyricTooLong of lyric: string
    | LyricsHaveNoLineBreaks
    | InvalidShowlights

type Issue =
    { Type: IssueType
      TimeCode: int }

let issueCode = function
    | ApplauseEventWithoutEnd -> "I01"
    | EventBetweenIntroApplause _ -> "I02"
    | NoteLinkedToChord -> "I03"
    | LinkNextMissingTargetNote -> "I04"
    | LinkNextSlideMismatch -> "I05"
    | LinkNextFretMismatch -> "I06"
    | LinkNextBendMismatch -> "I07"
    | IncorrectLinkNext -> "I08"
    | UnpitchedSlideWithLinkNext -> "I09"
    | PhraseChangeOnLinkNextNote -> "I10"
    | DoubleHarmonic -> "I11"
    //| MissingIgnore -> "I12"
    | SeventhFretHarmonicWithSustain -> "I13"
    | MissingBendValue -> "I14"
    | ToneChangeOnNote -> "I15"
    | NoteInsideNoguitarSection -> "I16"
    //| VaryingChordNoteSustains -> "I17"
    | MissingLinkNextChordNotes -> "I18"
    | ChordAtEndOfHandShape -> "I19"
    | FingeringAnchorMismatch -> "I20"
    | AnchorInsideHandShape -> "I21"
    | AnchorInsideHandShapeAtPhraseBoundary -> "I22"
    | AnchorCloseToUnpitchedSlide -> "I23"
    | AnchorNotOnNote _ -> "I24"
    | FirstPhraseNotEmpty -> "I25"
    | NoEndPhrase -> "I26"
    | PossiblyWrongChordFingering -> "I27"
    | BarreOverOpenStrings -> "I28"
    | MutedStringInNonMutedChord -> "I29"
    | MoreThan100Phrases -> "I30"
    | HopoIntoSameNote -> "I31"
    | FingerChangeDuringSlide -> "I32"
    | PositionShiftIntoPullOff -> "I33"
    | OverlappingBendValues -> "I34"
    | LyricWithInvalidChar _ -> "V01"
    | LyricTooLong _ -> "V02"
    | LyricsHaveNoLineBreaks -> "V03"
    | InvalidShowlights -> "S01"
