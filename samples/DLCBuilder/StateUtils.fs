module DLCBuilder.StateUtils

open Rocksmith2014.Audio
open Rocksmith2014.Common
open Rocksmith2014.Common.Manifest
open Rocksmith2014.DD
open Rocksmith2014.DLCProject
open Rocksmith2014.DLCProject.PackageBuilder
open System
open System.IO
open Elmish

let [<Literal>] private CherubRock = "248750"

let packageBuildProgress = Progress<float>()

let getSelectedArrangement state =
    List.tryItem state.SelectedArrangementIndex state.Project.Arrangements

let getSelectedTone state =
    List.tryItem state.SelectedToneIndex state.Project.Tones

/// Adds the given tones into the project.
let addTones (state: State) (tones: Tone list) =
    let tones = List.map Utils.addDescriptors tones
    { state with Project = { state.Project with Tones = tones @ state.Project.Tones }
                 Overlay = NoOverlay }

/// Creates a build configuration data structure.
let createBuildConfig buildType config project platforms =
    let convTask =
        DLCProject.getFilesThatNeedConverting project
        |> Seq.map (Wwise.convertToWem config.WwiseConsolePath)
        |> Async.Parallel
        |> Async.Ignore

    let phraseSearch =
        match config.DDPhraseSearchEnabled with
        | true -> WithThreshold config.DDPhraseSearchThreshold
        | false -> SearchDisabled

    let appId =
        match buildType, config.CustomAppId with
        | Test, Some customId -> customId
        | _ -> CherubRock

    { Platforms = platforms
      BuilderVersion = $"DLC Builder {AppVersion.versionString}"
      Author = config.CharterName
      AppId = appId
      GenerateDD = config.GenerateDD || buildType = Release
      DDConfig = { PhraseSearch = phraseSearch; LevelCountGeneration = config.DDLevelCountGeneration }
      ApplyImprovements = config.ApplyImprovements
      SaveDebugFiles = config.SaveDebugFiles && buildType <> Release
      AudioConversionTask = convTask
      ProgressReporter = Some (packageBuildProgress :> IProgress<float>) }

/// Returns true if a build or a wem conversion is not in progress.
let notBuilding state =
    state.RunningTasks
    |> Set.intersect (Set([ BuildPackage; WemConversion ]))
    |> Set.isEmpty

/// Returns true if the project can be built.
let canBuild state =
    notBuilding state
    && (not <| state.RunningTasks.Contains PsarcImport)
    && state.Project.Arrangements.Length > 0
    && String.notEmpty state.Project.AudioFile.Path

/// Returns true if the arrangement validation may be executed.
let canRunValidation state =
    state.Project.Arrangements.Length > 0
    &&
    not (state.RunningTasks |> Set.contains ArrangementCheck)

/// Returns true for tasks that report progress.
let taskHasProgress = function
    | BuildPackage | PsarcImport | PsarcUnpack | ArrangementCheck ->
        true
    | _ ->
        false

/// Adds a new long running task to the state.
let addTask newTask state =
    let message =
        match taskHasProgress newTask with
        | true -> TaskWithProgress(newTask, 0.)
        | false -> TaskWithoutProgress(newTask)

    { state with RunningTasks = state.RunningTasks |> Set.add newTask
                 StatusMessages = message::state.StatusMessages }

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

    { state with RunningTasks = state.RunningTasks |> Set.remove completedTask
                 StatusMessages = messages }

/// Updates the configuration and the recent files with the project filename.
let updateRecentFilesAndConfig projectFile state =
    let recent = RecentFilesList.update projectFile state.RecentFiles
    let newConfig = { state.Config with PreviousOpenedProject = projectFile }
    let cmd =
        if state.Config.PreviousOpenedProject <> projectFile then
            Cmd.OfAsync.attempt Configuration.save newConfig ErrorOccurred
        else
            Cmd.none
    recent, newConfig, cmd

/// Returns a throttled auto-save message.
let autoSave =
    let mutable id = 0L

    fun () ->
        id <- id + 1L
        let thisId = id
        async {
            do! Async.Sleep 1000
            if thisId = id then
                return Some AutoSaveProject
            else
                return None }

/// Returns a delayed message to remove the status message with the given ID.
let removeStatusMessage (id: Guid) = async {
    do! Async.Sleep 4000
    return RemoveStatusMessage id }

/// Adds the arrangements from the given filenames into the project in the state.
let addArrangements fileNames state =
    let results = Array.map Arrangement.fromFile fileNames

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

    let createErrorMsg (path: string) error = $"%s{Path.GetFileName path}:\n%s{error}"

    let arrangements, errors =
        ((state.Project.Arrangements, []), results)
        ||> Array.fold (fun (arrs, errors) result ->
            match result with
            | Ok (arr, _) ->
                match shouldInclude arrs arr with
                | Ok (Instrumental inst) when mainArrangementExists inst arrs ->
                    // Prevent multiple main arrangements of the same type
                    Instrumental { inst with Priority = ArrangementPriority.Alternative }::arrs, errors
                | Ok arr ->
                    arr::arrs, errors
                | Error error ->
                    let errorMsg = createErrorMsg (Arrangement.getFile arr) (translate <| string error)
                    arrs, errorMsg::errors
            | Error (UnknownArrangement path) ->
                let message = translate "UnknownArrangementError"
                let error = createErrorMsg path message
                arrs, error::errors
            | Error (FailedWithException (path, ex)) ->
                let error = createErrorMsg path ex.Message
                arrs, error::errors)

    let metadata =
        if state.Project.ArtistName = SortableString.Empty then
            results
            |> Array.tryPick (function Ok (_, md) -> md | Error _ -> None)
        else
            None

    let newState =
        let project = Utils.addMetadata metadata state.Config.CharterName state.Project
        { state with Project = { project with Arrangements = List.sortBy Arrangement.sorter arrangements } }

    match errors with
    | [] ->
        newState
    | _ ->
        let errorMessage = errors |> String.concat "\n\n"

        { newState with Overlay = ErrorMessage(errorMessage, None) }
