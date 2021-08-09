module DLCBuilder.ProgressReporters

open System

let PackageBuild = Progress<float>()

let PsarcUnpack = Progress<float>()

let PsarcImport = Progress<float>()

let ArrangementCheck = Progress<float>()
