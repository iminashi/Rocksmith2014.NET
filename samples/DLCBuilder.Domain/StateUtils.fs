module DLCBuilder.StateUtils

open Rocksmith2014.Audio
open Rocksmith2014.Common
open Rocksmith2014.Common.Manifest
open Rocksmith2014.DLCProject
open System
open System.IO
open Elmish

let getSelectedArrangement state =
    List.tryItem state.SelectedArrangementIndex state.Project.Arrangements

let getSelectedTone state =
    List.tryItem state.SelectedToneIndex state.Project.Tones

/// Adds the given tones into the project.
let addTones (state: State) (tones: Tone list) =
    let tones = List.map Utils.addDescriptors tones
    { state with Project = { state.Project with Tones = tones @ state.Project.Tones }
                 Overlay = NoOverlay }

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
    not (state.RunningTasks |> Set.contains ArrangementCheckAll)

/// Returns true for tasks that report progress.
let taskHasProgress = function
    | BuildPackage | PsarcImport | PsarcUnpack | ArrangementCheckAll ->
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

/// Returns a delayed message to remove the status message with the given ID.
let removeStatusMessage (id: Guid) = async {
    do! Async.Sleep 4000
    return RemoveStatusMessage id }

/// Adds the arrangements from the given filenames into the project in the state.
let addArrangements fileNames state =
    let results = Array.map Arrangement.fromFile fileNames
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
                    let errorMsg = createErrorMsg (Arrangement.getFile arr) (t.Translate <| string error)
                    arrs, errorMsg::errors
            | Error (UnknownArrangement path) ->
                let message = t.Translate "UnknownArrangementError"
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

/// Adds the Japanese vocals to the project if it does not have them already.
let addJapaneseVocals (xmlPath: string) state =
    // Only add the Japanese vocals if they were saved to the project directory
    // And the project does not already include Japanese vocals
    let shouldInclude =
        let currentVocals =
            state.Project.Arrangements
            |> List.choose Arrangement.pickVocals

        state.OpenProjectFile
        |> Option.exists (fun x -> Path.GetDirectoryName x = Path.GetDirectoryName xmlPath)
        && currentVocals.Length < 2
        && not <| List.exists (fun x -> x.Japanese) currentVocals

    if not shouldInclude then
        state, Cmd.none
    else
        let japaneseVocals =
            Vocals { XML = xmlPath
                     Japanese = true
                     CustomFont = None
                     PersistentID = Guid.NewGuid()
                     MasterID = RandomGenerator.next() }

        let updatedProject =
            let arrangements =
                japaneseVocals::state.Project.Arrangements
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
        reply.Reply false
    | _ ->
        ()

    { state with Overlay = overlay }, Cmd.none

let handleFilesDrop paths =
    let arrangements, other =
        paths
        |> Seq.toArray
        |> Array.partition (fun x -> String.endsWith "xml" x && not (String.endsWith "tone2014.xml" x || String.endsWith "dlc.xml" x))

    let otherCommands =
        other
        |> Array.choose (fun path ->
            match path with
            | EndsWith ".rs2dlc" ->
                Some (OpenProject path)
            | EndsWith ".dlc.xml" ->
                Some (ImportToolkitTemplate path)
            | EndsWith ".tone2014.xml"
            | EndsWith ".tone2014.json" ->
                Some (ImportTonesFromFile path)
            | EndsWith ".psarc" ->
                Some (path |> Dialog.PsarcImportTargetFolder |> ShowDialog)
            | EndsWith ".png"
            | EndsWith ".jpg"
            | EndsWith ".dds" ->
                Some (path |> SetAlbumArt |> EditProject)
            | EndsWith ".wav"
            | EndsWith ".ogg"
            | EndsWith ".wem" ->
                Some (SetAudioFile path)
            | _ ->
                None)
        |> Array.map Cmd.ofMsg

    seq {
        if arrangements.Length > 0 then
            AddArrangements arrangements |> Cmd.ofMsg
        yield! otherCommands }
