namespace Rocksmith2014.DLCProject

open System
open System.IO
open System.Text.RegularExpressions
open System.Xml
open Rocksmith2014.Common
open Rocksmith2014.XML
open Rocksmith2014.DLCProject.ArrangementPropertiesOverride

type ArrangementName =
    | Lead = 0
    | Combo = 1
    | Rhythm = 2
    | Bass = 3

type RouteMask =
    | None = 0
    | Lead = 1
    | Rhythm = 2
    | Any = 3
    | Bass = 4

type ArrangementPriority =
    | Main = 0
    | Alternative = 1
    | Bonus = 2

type Instrumental =
    { XML: string
      Name: ArrangementName
      RouteMask: RouteMask
      Priority: ArrangementPriority
      ScrollSpeed: float
      BassPicked: bool
      Tuning: int16 array
      TuningPitch: float
      BaseTone: string
      Tones: string list
      CustomAudio: AudioFile option
      ArrangementProperties: ArrPropFlags option
      MasterID: int
      PersistentID: Guid }

    member this.AllTones =
        this.BaseTone :: this.Tones

    static member Empty =
        { XML = String.Empty
          Name = ArrangementName.Lead
          RouteMask = RouteMask.Lead
          Priority = ArrangementPriority.Main
          ScrollSpeed = 1.3
          BassPicked = false
          Tuning = Array.replicate 6 0s
          TuningPitch = 440.
          BaseTone = String.Empty
          Tones = List.empty
          CustomAudio = None
          ArrangementProperties = None
          MasterID = 0
          PersistentID = Guid.NewGuid() }

type Vocals =
    { XML: string
      Japanese: bool
      CustomFont: string option
      MasterID: int
      PersistentID: Guid }

type Showlights = { XML: string }

type Arrangement =
    | Instrumental of Instrumental
    | Vocals of Vocals
    | Showlights of Showlights

type ArrangementLoadError =
    | UnknownArrangement of failedFile: string
    | FailedWithException of failedFile: string * exn

module Arrangement =
    /// Returns the master ID of an arrangement.
    let getMasterId = function
        | Vocals v -> v.MasterID
        | Instrumental i -> i.MasterID
        | Showlights _ -> failwith "No"

    /// Returns the persistent ID of an arrangement.
    let getPersistentId = function
        | Vocals v -> v.PersistentID
        | Instrumental i -> i.PersistentID
        | Showlights _ -> failwith "No"

    /// Returns the name of an arrangement.
    let getName (arr: Arrangement) isGeneric =
        match arr with
        | Vocals { Japanese = true } when not isGeneric ->
            "JVocals"
        | Vocals _ ->
            "Vocals"
        | Showlights _ ->
            "Showlights"
        | Instrumental i ->
            i.Name.ToString()

    /// Returns the localization string for the name and prefix of an arrangement.
    let getNameAndPrefix arr =
        let prefix =
            match arr with
            | Instrumental { Priority = ArrangementPriority.Alternative } ->
                "Alt"
            | Instrumental { Priority = ArrangementPriority.Bonus } ->
                "Bonus"
            | _ ->
                String.Empty

        let name =
            match arr with
            | Instrumental inst ->
                $"{inst.RouteMask}Arr"
            | Vocals { Japanese = true } ->
                "Japanese Vocals"
            | Vocals _ ->
                "Vocals"
            | Showlights _ ->
                "Showlights"

        name, prefix

    /// Returns the XML file of an arrangement.
    let getFile = function
        | Vocals v -> v.XML
        | Instrumental i -> i.XML
        | Showlights s -> s.XML

    let pickInstrumental = function Instrumental i -> Some i | _ -> None
    let pickVocals = function Vocals v -> Some v | _ -> None
    let pickShowlights = function Showlights s -> Some s | _ -> None

    /// Returns the comparable values for sorting arrangements.
    let sorter = function
        | Instrumental i ->
            LanguagePrimitives.EnumToValue(i.RouteMask), LanguagePrimitives.EnumToValue(i.Priority)
        | Vocals v when not v.Japanese ->
            5, 0
        | Vocals _ ->
            6, 0
        | Showlights _ ->
            7, 0

    /// Loads an arrangement from a file.
    let fromFile (fileName: string) =
        try
            let rootName =
                using (XmlReader.Create(fileName))
                      (fun reader -> reader.MoveToContent() |> ignore; reader.LocalName)

            match rootName with
            | "song" ->
                let metadata = MetaData.Read(fileName)
                let arrProp = metadata.ArrangementProperties
                let toneInfo = InstrumentalArrangement.ReadToneNames(fileName)

                let baseTone =
                    match toneInfo.BaseToneName with
                    | null ->
                        $"{metadata.Arrangement.ToLowerInvariant()}_base"
                    | toneKey ->
                        toneKey

                let tones =
                    toneInfo.Names
                    |> Array.choose Option.ofString
                    |> Array.toList

                let routeMask =
                    if arrProp.PathBass then
                        RouteMask.Bass
                    elif arrProp.PathRhythm then
                        RouteMask.Rhythm
                    else
                        RouteMask.Lead

                let name =
                    match ArrangementName.TryParse(metadata.Arrangement) with
                    | true, name ->
                        name
                    | false, _ ->
                        match routeMask with
                        | RouteMask.Bass -> ArrangementName.Bass
                        | RouteMask.Rhythm -> ArrangementName.Rhythm
                        | _ -> ArrangementName.Lead

                let arr =
                    { XML = fileName
                      Name = name
                      Priority =
                        if arrProp.Represent then
                            ArrangementPriority.Main
                        elif arrProp.BonusArrangement then
                            ArrangementPriority.Bonus
                        else
                            ArrangementPriority.Alternative
                      Tuning = metadata.Tuning.Strings
                      TuningPitch = Utils.centsToTuningPitch (float metadata.CentOffset)
                      RouteMask = routeMask
                      ScrollSpeed = 1.3
                      BaseTone = baseTone
                      Tones = tones
                      BassPicked = arrProp.BassPick
                      MasterID = RandomGenerator.next ()
                      PersistentID = Guid.NewGuid()
                      CustomAudio = None
                      ArrangementProperties = None }
                    |> Arrangement.Instrumental

                Ok(arr, Some metadata)

            | "vocals" ->
                // Attempt to infer whether the lyrics are Japanese from the filename
                let isJapanese =
                    Regex.IsMatch(fileName, "j.?(vocal|lyric)", RegexOptions.IgnoreCase)

                // Try to find custom font for Japanese vocals
                let customFont =
                    let fontFile =
                        Path.Combine(Path.GetDirectoryName(fileName), "lyrics.dds")

                    if isJapanese && File.Exists(fontFile) then
                        Some fontFile
                    else
                        None

                let arr =
                    { XML = fileName
                      Japanese = isJapanese
                      CustomFont = customFont
                      MasterID = RandomGenerator.next ()
                      PersistentID = Guid.NewGuid() }
                    |> Arrangement.Vocals

                Ok(arr, None)

            | "showlights" ->
                let arr = Arrangement.Showlights { XML = fileName }
                Ok(arr, None)

            | _ ->
                Error(UnknownArrangement fileName)
        with ex ->
            Error(FailedWithException(fileName, ex))

    /// Reads the tone info from the arrangement's XML file.
    let updateToneInfo (inst: Instrumental) updateBaseTone =
        let toneInfo = InstrumentalArrangement.ReadToneNames(inst.XML)

        let tones =
            toneInfo.Names
            |> Array.choose Option.ofString
            |> Array.toList

        if updateBaseTone && notNull toneInfo.BaseToneName then
            { inst with
                Tones = tones
                BaseTone = toneInfo.BaseToneName }
        else
            { inst with Tones = tones }

    /// Generates new IDs for the given arrangement.
    let generateIds = function
        | Instrumental inst ->
            { inst with
                MasterID = RandomGenerator.next ()
                PersistentID = Guid.NewGuid() }
            |> Instrumental
        | Vocals vocals ->
            { vocals with
                MasterID = RandomGenerator.next ()
                PersistentID = Guid.NewGuid() }
            |> Vocals
        | other ->
            other
