module Rocksmith2014.DLCProject.Utils

open System

let tuningPitchToCents (pitch: float) =
    Math.Round(1200. * Math.Log(pitch / 440.) / Math.Log(2.))

let centsToTuningPitch (cents: float) =
    Math.Round(440. * Math.Pow(Math.Pow(2., 1. / 1200.), cents), 2)
