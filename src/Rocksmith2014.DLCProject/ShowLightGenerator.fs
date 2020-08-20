module Rocksmith2014.DLCProject.ShowLightGenerator

open Rocksmith2014.PSARC
open Rocksmith2014.XML

let generate (targetFile: string) =
    // TODO: Actually generate show lights
    let sl = ResizeArray<ShowLight>()
    sl.Add(ShowLight(10_000, 25uy))
    sl.Add(ShowLight(10_000, 42uy))
    ShowLights.Save(targetFile, sl)
    Utils.getFileStreamForRead targetFile
