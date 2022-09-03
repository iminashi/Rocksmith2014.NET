module Rocksmith2014.DLCProject.PsarcImportTypes

open Rocksmith2014.Common
open Rocksmith2014.XML

[<RequireQualifiedAccess>]
type ImportedData =
    | Vocals of ResizeArray<Vocal>
    | Instrumental of InstrumentalArrangement
    | ShowLights

type PsarcImportResult =
    {
        /// The generated project.
        GeneratedProject: DLCProject

        /// Path to the project file that was saved when importing the PSARC.
        ProjectPath: string

        /// App ID of the PSARC if it was other than Cherub Rock.
        AppId: AppId option

        /// The version of the Toolkit or DLC Builder that was used to build the package.
        BuildToolVersion: string option

        /// Data converted from SNG for possible DD removal / EOF conversion.
        ArrangementData: (Arrangement * ImportedData) list
    }
