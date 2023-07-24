module DLCBuilder.StateUtils

open Elmish
open Rocksmith2014.Audio
open Rocksmith2014.Common
open Rocksmith2014.Common.Manifest
open Rocksmith2014.DLCProject
open Rocksmith2014.DLCProject.PsarcImportTypes
open Rocksmith2014.EOF
open System
open System.IO

let getSelectedArrangement state =
    List.tryItem state.SelectedArrangementIndex state.Project.Arrangements

let getSelectedTone state =
    List.tryItem state.SelectedToneIndex state.Project.Tones

let updateToneKey (config: Configuration) (newKey: string) (tone: Tone) =
    // When the name field is hidden, keep the name in sync with the key
    if not config.ShowAdvanced then
        { tone with Key = newKey; Name = newKey }
    else
        { tone with Key = newKey }

/// Adds the given tones into the project.
let addTones (state: State) (tones: Tone list) =
    let tones = List.map Utils.addDescriptors tones
    let keysOfAddedTones = tones |> List.map (fun t -> t.Key) |> Set.ofList
    // Prevent duplicate tone keys
    let updatedProjectTones =
        state.Project.Tones
        |> List.map (fun tone ->
            if keysOfAddedTones.Contains(tone.Key) then
                updateToneKey state.Config String.Empty tone
            else
                tone)

    { state with
        Project = { state.Project with Tones = tones @ updatedProjectTones }
        Overlay = NoOverlay }

/// Returns true if a build or a wem conversion is not in progress.
let notBuilding state =
    state.RunningTasks
    |> Set.exists (function
        | BuildPackage
        | WemConversion _ ->
            true
        | _ ->
            false)
    |> not

/// Returns true if the project can be built.
let canBuild state =
    notBuilding state
    && not (state.RunningTasks.Contains(PsarcImport) || state.RunningTasks.Contains(AutomaticPreviewCreation))
    && state.Project.Arrangements.Length > 0
    && String.notEmpty state.Project.AudioFile.Path

/// Returns true if the arrangement validation may be executed.
let canRunValidation state =
    state.Project.Arrangements.Length > 0
    && not (state.RunningTasks.Contains(ArrangementCheckAll))

/// Returns true for tasks that report progress.
let taskHasProgress = function
    | ArrangementCheckAll
    | BuildPackage
    | FileDownload _
    | PsarcImport
    | PsarcUnpack ->
        true
    | ArrangementCheckOne
    | AutomaticPreviewCreation
    | VolumeCalculation _
    | WemConversion _
    | WemToOggConversion ->
        false

/// Adds a new long running task to the state.
let addTask newTask state =
    let message =
        match taskHasProgress newTask with
        | true -> TaskWithProgress(newTask, 0.)
        | false -> TaskWithoutProgress(newTask)

    { state with
        RunningTasks = state.RunningTasks |> Set.add newTask
        StatusMessages = message :: state.StatusMessages }

/// Removes the completed task from the state.
let removeTask completedTask state =
    let messages =
        state.StatusMessages
        |> List.filter (function
            | TaskWithProgress (task, _)
            | TaskWithoutProgress task when task = completedTask ->
                false
            | _ ->
                true)

    { state with
        RunningTasks = state.RunningTasks |> Set.remove completedTask
        StatusMessages = messages }

/// Updates the configuration and the recent files with the project filename.
let updateRecentFilesAndConfig projectFile state =
    let recent = RecentFilesList.update projectFile state.RecentFiles
    let newConfig = { state.Config with PreviousOpenedProject = projectFile }

    let cmd =
        if state.Config.PreviousOpenedProject <> projectFile then
            Cmd.OfTask.attempt Configuration.save newConfig ErrorOccurred
        else
            Cmd.none

    recent, newConfig, cmd

/// Returns a delayed message to remove the status message with the given ID.
let removeStatusMessage (id: Guid) =
    async {
        do! Async.Sleep 4000
        return RemoveStatusMessage id
    }

let createToneKeyFromTitleAndArrangmentName title arrangementName =
    title
    |> StringValidator.fileName
    |> fun t ->
        let titleLower = t.Replace("-", "").ToLowerInvariant()
        $"{titleLower}_{arrangementName}"

/// Adds the arrangements from the given filenames into the project in the state.
let addArrangements fileNames state =
    let getBaseToneName (metadata: Rocksmith2014.XML.MetaData) =
        let arrName = metadata.Arrangement.ToLowerInvariant()
        let getDefault () = $"{arrName}_base"

        match state.Config.BaseToneNamingScheme with
        | BaseToneNamingScheme.Default ->
            getDefault ()
        | BaseToneNamingScheme.TitleAndArrangement ->
            Option.ofString state.Project.Title.Value
            |> Option.orElseWith (fun () -> Option.ofString metadata.Title)
            |> Option.map (fun title -> createToneKeyFromTitleAndArrangmentName title arrName)
            |> Option.defaultWith getDefault

    let results = Array.map (Arrangement.fromFile getBaseToneName) fileNames
    let t = state.Localizer

    let shouldInclude arrangements arr =
        let count f = List.choose f arrangements |> List.length

        match arr with
        | Showlights _ when count Arrangement.pickShowlights = 1 ->
            Error MaxShowlights
        | Instrumental _ when count Arrangement.pickInstrumental = 5 ->
            Error MaxInstrumentals
        | Vocals _ when count Arrangement.pickVocals = 2 ->
            Error MaxVocals
        | _ ->
            Ok arr

    let mainArrangementExists newInst arrangements =
        arrangements
        |> List.choose Arrangement.pickInstrumental
        |> List.exists (fun oldInst ->
            newInst.RouteMask = oldInst.RouteMask
            && newInst.Priority = oldInst.Priority
            && oldInst.Priority = ArrangementPriority.Main)

    let createErrorMsg (path: string) error =
        $"%s{Path.GetFileName path}:\n%s{error}"

    let arrangements, errors =
        ((state.Project.Arrangements, []), results)
        ||> Array.fold (fun (arrs, errors) result ->
            match result with
            | Ok (arr, _) ->
                match shouldInclude arrs arr with
                | Ok (Instrumental inst) when mainArrangementExists inst arrs ->
                    // Prevent multiple main arrangements of the same type
                    Instrumental { inst with Priority = ArrangementPriority.Alternative } :: arrs, errors
                | Ok arr ->
                    arr :: arrs, errors
                | Error error ->
                    let errorMsg = createErrorMsg (Arrangement.getFile arr) (t.Translate(string error))
                    arrs, errorMsg :: errors
            | Error (UnknownArrangement path) ->
                let message = t.Translate "UnknownArrangementError"
                let error = createErrorMsg path message
                arrs, error :: errors
            | Error (EofExtVocalsFile path) ->
                let message =  t.Translate "EofExtVocalsFileError"
                let error = createErrorMsg path message
                arrs, error :: errors
            | Error (FailedWithException (path, ex)) ->
                let error = createErrorMsg path ex.Message
                arrs, error :: errors)

    let metadata =
        results
        |> Array.tryPick (function Ok (_, md) -> md | Error _ -> None)

    let newState =
        let project =
            Utils.addMetadata metadata state.Config.CharterName state.Project

        { state with Project = { project with Arrangements = List.sortBy Arrangement.sorter arrangements } }

    if errors.IsEmpty then
        newState
    else
        let errorMessage = errors |> String.concat "\n\n"

        { newState with Overlay = ErrorMessage(errorMessage, None) }

/// Adds the Japanese vocals to the project if it does not have them already.
let addJapaneseVocals (xmlPath: string) state =
    // Only add the Japanese vocals if they were saved to the project directory
    // And the project does not already include Japanese vocals
    let shouldInclude =
        let currentVocals =
            state.Project.Arrangements
            |> List.choose Arrangement.pickVocals

        state.OpenProjectFile
        |> Option.exists (fun x -> Path.GetDirectoryName(x) = Path.GetDirectoryName(xmlPath))
        && currentVocals.Length < 2
        && not <| List.exists (fun x -> x.Japanese) currentVocals

    if not shouldInclude then
        state, Cmd.none
    else
        let japaneseVocals =
            { XML = xmlPath
              Japanese = true
              CustomFont = None
              PersistentID = Guid.NewGuid()
              MasterID = RandomGenerator.next () }
            |> Vocals

        let updatedProject =
            let arrangements =
                japaneseVocals :: state.Project.Arrangements
                |> List.sortBy Arrangement.sorter

            { state.Project with Arrangements = arrangements }

        { state with Project = updatedProject },
        Cmd.ofMsg (AddStatusMessage (state.Localizer.Translate "ArrangementWasAddedToProject"))

/// Applies the low tuning fix to the selected arrangement.
let applyLowTuningFix state =
    let arrangements =
        match getSelectedArrangement state with
        | Some (Instrumental inst) ->
            let updated =
                { inst with TuningPitch = inst.TuningPitch / 2.
                            Tuning = Array.map ((+) 12s) inst.Tuning }
                |> Instrumental

            state.Project.Arrangements
            |> List.updateAt state.SelectedArrangementIndex updated
        | _ ->
            state.Project.Arrangements

    { state with Project = { state.Project with Arrangements = arrangements } }

let showOverlay state overlay =
    match state.Overlay with
    | IdRegenerationConfirmation (_, reply) ->
        // Might end up here in rare cases
        reply.Reply(false)
    | ToneCollection cs ->
        match overlay with
        | ToneCollection _ ->
            ()
        | _ ->
            ToneCollection.CollectionState.disposeCollection cs.ActiveCollection
    | _ ->
        ()

    { state with Overlay = overlay }

let handleFilesDrop config paths =
    let arrangements, other =
        paths
        |> Seq.toArray
        |> Array.partition (fun x ->
            String.endsWith "xml" x
            && not (String.endsWith "tone2014.xml" x || String.endsWith "dlc.xml" x))

    let otherCommands =
        other
        |> Array.choose (fun path ->
            match path with
            | HasExtension ".rs2dlc" ->
                Some(OpenProject path)
            | HasExtension ".psarc" ->
                if config.QuickEditOnPsarcDragAndDrop then
                    Some(ImportPsarcQuick path)
                else
                    Some(path |> Dialog.PsarcImportTargetFolder |> ShowDialog)
            | HasExtension (".png" | ".jpg" | ".dds") ->
                Some(path |> SetAlbumArt |> EditProject)
            | HasExtension (".wav" | ".ogg" | ".wem") ->
                Some(SetAudioFile path)
            | EndsWith ".tone2014.xml"
            | EndsWith ".tone2014.json" ->
                Some(ImportTonesFromFile path)
            | EndsWith ".dlc.xml" ->
                Some(ImportToolkitTemplate path)
            | _ ->
                None)
        |> Array.map Cmd.ofMsg

    seq {
        if arrangements.Length > 0 then
            AddArrangements arrangements |> Cmd.ofMsg
        yield! otherCommands
    }

let private createPsarcImportProgressReporter config =
    let maxProgress =
        3.
        + match config.ConvertAudio with None -> 0. | _ -> 1.
        + Convert.ToDouble(config.RemoveDDOnImport)
        + Convert.ToDouble(config.CreateEOFProjectOnImport)

    let mutable currentProgress = 0.

    fun () ->
        currentProgress <- currentProgress + 1.
        currentProgress / maxProgress * 100.
        |> (ProgressReporters.PsarcImport :> IProgress<float>).Report

let private importTrackSorter (arr: Arrangement, _) =
    match arr with
    | Instrumental i ->
        // Sorts by lead, rhythm, bass
        // Main arrangements first, then alternative, last bonus arrangements
        LanguagePrimitives.EnumToValue(i.RouteMask) + 10 * LanguagePrimitives.EnumToValue(i.Priority)
    | _ ->
        // Order of vocals and showlights does not matter
        99

let private createEofTrackList (arrangements: (Arrangement * ImportedData) list) =
    let arrangements = arrangements |> List.sortBy importTrackSorter
    let getCustomName (xmlPath: string) =
        let fn = Path.GetFileNameWithoutExtension(xmlPath)
        if fn.EndsWith("_RS2") then
            fn.Remove(fn.Length - 4)
        else
            fn

    let vocals =
        arrangements
        |> List.tryPick (function
            | Vocals { XML = xml; Japanese = false }, ImportedData.Vocals v ->
                Some { Vocals = v :> seq<_>; CustomName = getCustomName xml }
            | _ ->
                None)

    let getInstrumental filter input =
        match input with
        | Instrumental { XML = xml; Name = name }, ImportedData.Instrumental data ->
            if filter name then
                Some { Data = data ; CustomName = getCustomName xml }
            else
                None
        | _ ->
            None

    let mainAndRest arr =
        if Array.length arr <= 2 then
            arr, Array.empty
        else
            arr |> Array.splitAt 2

    let guitar, extraGuitar =
        arrangements
        |> List.choose (getInstrumental (fun name -> name <> ArrangementName.Bass))
        |> List.toArray
        |> mainAndRest

    let bass, extraBass =
        arrangements
        |> List.choose (getInstrumental (fun name -> name = ArrangementName.Bass))
        |> List.toArray
        |> mainAndRest

    let guitar, bass, bonus =
        if extraGuitar.Length = 1 then
            guitar, bass, Some extraGuitar[0]
        elif extraGuitar.Length > 1 then
            // Append extra guitar arrangements to the bass arrangements
            let bonus, extra = extraGuitar |> Array.splitAt 1
            guitar, bass |> Array.append extra, Some bonus[0]
        elif extraBass.Length = 1 then
            guitar, bass, Some extraBass[0]
        elif extraBass.Length > 1 then
            // Append extra bass arrangements to the guitar arrangements
            let bonus, extra = extraBass |> Array.splitAt 1
            guitar |> Array.append extra, bass, Some bonus[0]
        else
            guitar, bass, None

    { PartGuitar = guitar |> Array.truncate 2
      PartBass = bass |> Array.truncate 2
      PartBonus = bonus
      PartVocals = vocals }

let importPsarc (config: Configuration) (targetFolder: string) (psarcPath: string)  =
    backgroundTask {
        let progress = createPsarcImportProgressReporter config

        Directory.CreateDirectory(targetFolder) |> ignore
        let! r = PsarcImporter.import progress psarcPath targetFolder
        let project = r.GeneratedProject

        match config.ConvertAudio with
        | None ->
            ()
        | Some conv ->
            Utils.convertProjectAudioFromWem conv project
            progress ()

        // Remove DD levels
        if config.RemoveDDOnImport then
            let instrumentalData =
                r.ArrangementData
                |> List.choose (fun (arr, data) ->
                    match data with
                    | ImportedData.Instrumental data ->
                        Some (Arrangement.getFile arr, data)
                    | _ ->
                        None)

            do! Utils.removeDD instrumentalData
            progress ()

        let oggFileName =
            match config.ConvertAudio with
            | Some ToOgg ->
                Path.ChangeExtension(Path.GetFileName(project.AudioFile.Path), "ogg")
            | _ ->
                String.Empty

        // Create EOF project
        if config.CreateEOFProjectOnImport then
            let eofProjectPath = Path.Combine(Path.GetDirectoryName(r.ProjectPath), "notes.eof")
            let eofTracks = createEofTrackList r.ArrangementData
            EOFProjectWriter.writeEofProject oggFileName eofProjectPath eofTracks
            progress ()

        return { r with GeneratedProject = project }
    }

let createDownloadTask locString =
    let id = { Id = Guid.NewGuid(); LocString = locString }
    id, FileDownload id

/// Gets the sort value from the sortable string if it is defined or can be created from the value.
let private sortValueFromSortableString f sStr =
    sStr.SortValue
    |> Option.ofString
    |> Option.orElse (Option.ofString sStr.Value |> Option.map (f >> StringValidator.convertToSortField))

/// Returns a filename for the project: "artist_title.rs2dlc"
/// Or "new_project.rs2dlc" if artist and title are not set.
let createProjectFilename project =
    let artist = sortValueFromSortableString StringValidator.FieldType.ArtistName project.ArtistName
    let title = sortValueFromSortableString StringValidator.FieldType.Title project.Title

    (artist, title)
    ||> Option.map2 (fun artist title -> StringValidator.fileName $"{artist}_{title}")
    |> Option.defaultValue "new_project"
    |> sprintf "%s.rs2dlc"

let private deleteTempFiles { TempDirectory = dir } =
    try
        Directory.Delete(dir, recursive = true)
    with _ -> ()

let deleteTemporaryFilesForQuickEdit state =
    state.QuickEditData |> Option.iter deleteTempFiles

/// Returns path to wav or ogg audio file if one exists.
let tryGetNonWemAudioFile wemPath =
    match Path.ChangeExtension(wemPath, "wav") with
    | wavPath when File.Exists(wavPath) ->
        Some wavPath
    | _ ->
        match Path.ChangeExtension(wemPath, "ogg") with
        | oggPath when File.Exists(oggPath) ->
            Some oggPath
        | _ ->
            None

let getOptionalWemConversionCmd state audioPath =
    if state.Config.AutoAudioConversion then
        let wemFileExists = Path.ChangeExtension(audioPath, ".wem") |> File.Exists
        let conversionAlreadyInProgress =
            state.RunningTasks
            |> Set.exists (function
                | WemConversion files ->
                    files |> Array.contains audioPath
                | _ ->
                    false)

        if not wemFileExists && not conversionAlreadyInProgress then
            Cmd.ofMsg (ConvertAudioToWem [| audioPath |] |> ToolsMsg)
        else
            Cmd.none
    else
        Cmd.none

let checkAllArrangements state continuation =
    let task () =
        async {
            return Utils.checkArrangements state.Project ProgressReporters.ArrangementCheck
        }

    addTask ArrangementCheckAll state,
    Cmd.OfAsync.either task () continuation (fun ex -> TaskFailed(ex, ArrangementCheckAll))
