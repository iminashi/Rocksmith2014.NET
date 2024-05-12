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

[<Struct>]
type ArrangementId =
    | ArrangementId of Guid

    static member Value (ArrangementId id) = id
    static member New = ArrangementId(Guid.NewGuid())

type Instrumental =
    {
        Id: ArrangementId
        XmlPath: string
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
        MasterId: int
        PersistentId: Guid
    }

    member this.AllTones =
        this.BaseTone :: this.Tones

    static member Empty =
        let defaultId = Guid.NewGuid()
        {
            Id = ArrangementId defaultId
            XmlPath = String.Empty
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
            MasterId = 0
            PersistentId = defaultId
        }

type Vocals =
    {
        Id: ArrangementId
        XmlPath: string
        Japanese: bool
        CustomFont: string option
        MasterId: int
        PersistentId: Guid
    }

type Showlights =
    {
        Id: ArrangementId
        XmlPath: string
    }

type Arrangement =
    | Instrumental of Instrumental
    | Vocals of Vocals
    | Showlights of Showlights

type ArrangementLoadError =
    | UnknownArrangement of failedFile: string
    | FailedWithException of failedFile: string * exn
    | EofExtVocalsFile of failedFile: string

module Arrangement =
    /// Returns the master ID of an arrangement.
    let getMasterId = function
        | Vocals v -> v.MasterId
        | Instrumental i -> i.MasterId
        | Showlights _ -> failwith "No"

    /// Returns the persistent ID of an arrangement.
    let getPersistentId = function
        | Vocals v -> v.PersistentId
        | Instrumental i -> i.PersistentId
        | Showlights _ -> failwith "No"

    /// Returns the unique indentifier of an arrangement.
    let getId = function
        | Vocals v -> v.Id
        | Instrumental i -> i.Id
        | Showlights s -> s.Id

    /// Returns the name of an arrangement.
    let getName (arr: Arrangement) (isGeneric: bool) =
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

    /// Returns the path of the XML file for an arrangement.
    let getFile = function
        | Vocals v -> v.XmlPath
        | Instrumental i -> i.XmlPath
        | Showlights s -> s.XmlPath

    let getTones = function
        | Instrumental i -> i.AllTones
        | Vocals _
        | Showlights _ -> List.empty

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
    /// Function `createBaseToneName` is used for an instrumental arrangement if the file does not define a base tone.
    let fromFile (createBaseToneName: MetaData -> RouteMask -> string) (path: string) =
        try
            let rootName =
                using (XmlReader.Create(path))
                      (fun reader -> reader.MoveToContent() |> ignore; reader.LocalName)

            match rootName with
            | "song" ->
                let metadata = MetaData.Read(path)
                let arrProp = metadata.ArrangementProperties
                let toneInfo = InstrumentalArrangement.ReadToneNames(path)

                let routeMask =
                    if arrProp.PathBass then
                        RouteMask.Bass
                    elif arrProp.PathRhythm then
                        RouteMask.Rhythm
                    else
                        RouteMask.Lead

                let baseTone =
                     toneInfo.BaseToneName
                     |> Option.ofString
                     |> Option.defaultWith (fun () -> createBaseToneName metadata routeMask)

                let tones =
                    toneInfo.Names
                    |> Array.choose Option.ofString
                    |> Array.toList

                let name =
                    match ArrangementName.TryParse(metadata.Arrangement) with
                    | true, name ->
                        name
                    | false, _ ->
                        match routeMask with
                        | RouteMask.Bass -> ArrangementName.Bass
                        | RouteMask.Rhythm -> ArrangementName.Rhythm
                        | _ -> ArrangementName.Lead

                let defaultId = Guid.NewGuid()

                let tuning =
                    if routeMask = RouteMask.Bass then
                        // Update bass tuning for the nonexistent strings (EOF does not set the tuning correctly for drop tunings)
                        let fourthString = metadata.Tuning.Strings[3]
                        metadata.Tuning.Strings
                        |> Array.mapi (fun i t -> if i > 3 then fourthString else t)
                    else
                        metadata.Tuning.Strings

                let arr =
                    { Id = ArrangementId defaultId
                      XmlPath = path
                      Name = name
                      Priority =
                        if arrProp.Represent then
                            ArrangementPriority.Main
                        elif arrProp.BonusArrangement then
                            ArrangementPriority.Bonus
                        else
                            ArrangementPriority.Alternative
                      Tuning = tuning
                      TuningPitch = Utils.centsToTuningPitch (float metadata.CentOffset)
                      RouteMask = routeMask
                      ScrollSpeed = 1.3
                      BaseTone = baseTone
                      Tones = tones
                      BassPicked = arrProp.BassPick
                      MasterId = RandomGenerator.next ()
                      PersistentId = defaultId
                      CustomAudio = None
                      ArrangementProperties = None }
                    |> Arrangement.Instrumental

                Ok(arr, Some metadata)

            | "vocals" ->
                // Attempt to infer whether the lyrics are Japanese from the filename
                let isJapanese =
                    Regex.IsMatch(Path.GetFileName(path), "j.?(vocal|lyric)", RegexOptions.IgnoreCase)

                // Try to find custom font for Japanese vocals
                let customFont =
                    let fontFile =
                        Path.Combine(Path.GetDirectoryName(path), "lyrics.dds")

                    if isJapanese && File.Exists(fontFile) then
                        Some fontFile
                    else
                        None

                let defaultId = Guid.NewGuid()

                let arr =
                    { Id = ArrangementId defaultId
                      XmlPath = path
                      Japanese = isJapanese
                      CustomFont = customFont
                      MasterId = RandomGenerator.next ()
                      PersistentId = defaultId }
                    |> Arrangement.Vocals

                Ok(arr, None)

            | "showlights" ->
                let arr = Arrangement.Showlights { Id = ArrangementId.New; XmlPath = path }
                Ok(arr, None)

            | _ ->
                Error(UnknownArrangement path)
        with
        | :? XmlException when Path.GetFileNameWithoutExtension(path) |> String.endsWith "_EXT" ->
            Error(EofExtVocalsFile(path))
        | ex ->
            Error(FailedWithException(path, ex))

    /// Reads the tone info from the arrangement's XML file.
    let updateToneInfo (updateBaseTone: bool) (inst: Instrumental) =
        let toneInfo = InstrumentalArrangement.ReadToneNames(inst.XmlPath)

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
                MasterId = RandomGenerator.next ()
                PersistentId = Guid.NewGuid() }
            |> Instrumental
        | Vocals vocals ->
            { vocals with
                MasterId = RandomGenerator.next ()
                PersistentId = Guid.NewGuid() }
            |> Vocals
        | Showlights _ as sl ->
            sl
