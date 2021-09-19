module Rocksmith2014.XML.Processing.ShowLightsChecker

open Rocksmith2014.XML

/// Checks that the showlights have at least one beam and one fog note.
let check (showLights: ResizeArray<ShowLight>) =
    let isValid =
        showLights.Exists(fun sl -> sl.IsBeam())
        && showLights.Exists(fun sl -> sl.IsFog())

    if isValid then
        None
    else
        Some(Utils.issue InvalidShowlights 0)
