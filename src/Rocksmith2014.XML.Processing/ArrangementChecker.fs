module Rocksmith2014.XML.Processing.ArrangementChecker

/// Runs all the checks on the given arrangement.
let checkInstrumental arrangement = InstrumentalChecker.runAllChecks arrangement

/// Checks the vocals for issues.
let checkVocals hasCustomFont vocals = VocalsChecker.check hasCustomFont vocals

/// Checks that the show lights have at least one beam and one fog note.
let checkShowlights showLights = ShowLightsChecker.check showLights
