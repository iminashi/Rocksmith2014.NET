module Rocksmith2014.DLCProject.Utils

open System

/// Converts a tuning pitch into cents.
let tuningPitchToCents (pitch: float) =
    Math.Round(1200. * Math.Log(pitch / 440.) / Math.Log(2.))

/// Converts a cent value into a tuning pitch value in hertz.
let centsToTuningPitch (cents: float) =
    Math.Round(440. * Math.Pow(Math.Pow(2., 1. / 1200.), cents), 2)
