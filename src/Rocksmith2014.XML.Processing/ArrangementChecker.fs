module Rocksmith2014.XML.Processing.ArrangementChecker

open Rocksmith2014.XML

/// Runs all the checks on the given arrangement.
let checkInstrumental (arrangement: InstrumentalArrangement) = InstrumentalChecker.runAllChecks arrangement

/// Checks the vocals for issues.
let checkVocals (customFont: GlyphDefinitions option) (vocals: ResizeArray<Vocal>) = VocalsChecker.check customFont vocals

/// Checks that the show lights have at least one beam and one fog note.
let checkShowlights (showLights: ResizeArray<ShowLight>) = ShowLightsChecker.check showLights
