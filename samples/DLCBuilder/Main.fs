module DLCBuilder.Main

open Rocksmith2014
open Rocksmith2014.Audio
open Rocksmith2014.Common
open Rocksmith2014.Common.Manifest
open Rocksmith2014.DLCProject
open Rocksmith2014.PSARC
open Rocksmith2014.XML.Processing
open Avalonia.Controls.Selection
open Elmish
open System
open System.Diagnostics
open System.IO
open EditFunctions

let arrangementCheckProgress = Progress<float>()
let psarcImportProgress = Progress<float>()

type private ArrangementAddingError =
    | MaxInstrumentals
    | MaxShowlights
    | MaxVocals

let private addArrangements files state =
    let results = Array.map Arrangement.fromFile files

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
                    let errorMsg = createErrorMsg (Arrangement.getFile arr) (translate (string error))
                    arrs, errorMsg::errors
            | Error (UnknownArrangement file) ->
                let message = translate "unknownArrangementError"
                let error = createErrorMsg file message
                arrs, error::errors
            | Error (FailedWithException (file, ex)) ->
                let error = createErrorMsg file ex.Message
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
        { newState with Overlay = ErrorMessage(String.Join(String.replicate 2 Environment.NewLine, errors), None) }

let private createExitCheckFile () =
    using (File.Create Configuration.exitCheckFilePath) ignore

let init args =
    let commands =
        let wasAbnormalExit = File.Exists Configuration.exitCheckFilePath
        createExitCheckFile()

        let loadProject =
            args
            |> Array.tryFind (String.endsWith ".rs2dlc")
            |> Option.map (fun path ->
                Cmd.OfAsync.either DLCProject.load path (fun p -> ProjectLoaded(p, path)) ErrorOccurred)
            |> Option.toList

        Cmd.batch [
            Cmd.OfAsync.perform Configuration.load () (fun config -> SetConfiguration(config, loadProject.IsEmpty, wasAbnormalExit))
            Cmd.OfAsync.perform RecentFilesList.load () SetRecentFiles
            Cmd.OfAsync.perform OnlineUpdate.checkForUpdates () SetAvailableUpdate
            yield! loadProject ]

    { Project = DLCProject.Empty
      SavedProject = DLCProject.Empty
      RecentFiles = []
      Config = Configuration.Default
      CoverArt = None
      SelectedArrangementIndex = -1
      SelectedToneIndex = -1
      SelectedGear = None
      SelectedGearSlot = ToneGear.Amp
      ManuallyEditingKnobKey = None
      ShowSortFields = false
      ShowJapaneseFields = false
      Overlay = NoOverlay
      // TODO: Refactor to remove dependency on Avalonia class in the model?
      SelectedImportTones = SelectionModel(SingleSelect = false)
      RunningTasks = Set.empty
      StatusMessages = []
      CurrentPlatform = if OperatingSystem.IsMacOS() then Mac else PC
      OpenProjectFile = None
      ArrangementIssues = Map.empty
      AvailableUpdate = None }, commands

let private getSelectedArrangement state =
    List.tryItem state.SelectedArrangementIndex state.Project.Arrangements

let private getSelectedTone state =
    List.tryItem state.SelectedToneIndex state.Project.Tones

let private removeSelected initialList index =
    let list = List.removeAt index initialList
    let newSelectedIndex = min index (list.Length - 1)
    list, newSelectedIndex

let private exceptionToErrorMessage (e: exn) =
    ErrorMessage (e.Message, Some $"{e.GetType().Name}: {e.Message}\n{e.StackTrace}")

let private moveSelected dir selectedIndex (list: List<_>) =
    match selectedIndex with
    | -1 ->
        list, selectedIndex
    | index ->
        let selected = list.[index]
        let change = match dir with Up -> -1 | Down -> 1 
        let insertPos = index + change
        if insertPos >= 0 && insertPos < list.Length then
            List.removeAt index list
            |> List.insertAt insertPos selected, insertPos
        else
            list, selectedIndex

let private buildPackage buildType build state =
    match BuildValidator.validate state.Project with
    | Error error ->
        let msg =
            match error with
            | InvalidDLCKey ->
                translatef (string error) [| DLCKey.MinimumLength |]
            | other ->
                other |> string |> translate
        { state with Overlay = ErrorMessage(msg, None) }, Cmd.none
    | Ok _ ->
        let task = build state.Config

        Utils.addTask BuildPackage state,
        Cmd.OfAsync.either task state.Project (fun () -> BuildComplete buildType) (fun ex -> TaskFailed(ex, BuildPackage))

let private removeStatusMessage (id: Guid) = async {
    do! Async.Sleep 4000
    return RemoveStatusMessage id }

let private updateRecentFilesAndConfig projectFile state =
    let recent = RecentFilesList.update projectFile state.RecentFiles
    let newConfig = { state.Config with PreviousOpenedProject = projectFile }
    let cmd =
        if state.Config.PreviousOpenedProject <> projectFile then
            Cmd.OfAsync.attempt Configuration.save newConfig ErrorOccurred
        else
            Cmd.none
    recent, newConfig, cmd

let update (msg: Msg) (state: State) =
    let { Project=project; Config=config } = state

    match msg with
    | SetSelectedGear gear ->
        let cmd =
            match gear with
            | Some gear ->
                let currentGear =
                    getSelectedTone state
                    |> Option.bind (fun tone -> ToneGear.getGearDataForCurrentPedal tone.GearList state.SelectedGearSlot)
                match currentGear with
                // Don't change the cabinet if its name is the same as the current one
                | Some data when gear.Type = "Cabinets" && data.Name = gear.Name ->
                    Cmd.none
                | _ ->
                    Cmd.ofMsg (gear |> SetPedal |> EditTone)
            | None ->
                Cmd.none

        { state with SelectedGear = gear }, cmd

    | SetSelectedGearSlot gearSlot -> { state with SelectedGearSlot = gearSlot }, Cmd.none

    | SetManuallyEditingKnobKey key -> { state with ManuallyEditingKnobKey = key}, Cmd.none

    | ShowToneEditor ->
        match getSelectedTone state with
        | Some _ -> { state with Overlay = ToneEditor }, Cmd.none
        | None -> state, Cmd.none

    | NewProject ->
        state.CoverArt |> Option.iter (fun x -> x.Dispose())
        { state with Project = DLCProject.Empty
                     SavedProject = DLCProject.Empty
                     OpenProjectFile = None
                     SelectedArrangementIndex = -1
                     SelectedToneIndex = -1
                     CoverArt = None }, Cmd.none

    | ImportSelectedTones ->
        let tones = state.SelectedImportTones.SelectedItems |> List.ofSeq
        Utils.addTones state tones, Cmd.none

    | ImportTones tones -> Utils.addTones state tones, Cmd.none

    | ExportSelectedTone ->
        let cmd =
            getSelectedTone state
            |> Option.map (Dialog.ExportTone >> ShowDialog >> Cmd.ofMsg)
            |> Option.defaultValue Cmd.none
        state, cmd

    | ExportTone (tone, path) ->
        let task =
            match path with
            | EndsWith "xml" -> Tone.exportXml path
            | _ -> Tone.exportJson path
        state, Cmd.OfAsync.attempt task tone ErrorOccurred

    | CloseOverlay ->
        let cmd =
            match state.Overlay with
            | ConfigEditor ->
                Cmd.OfAsync.attempt Configuration.save config ErrorOccurred
            | ToneCollection (api, _, _) ->
                api.Dispose()
                Cmd.none
            | _ ->
                Cmd.none
        { state with Overlay = NoOverlay }, cmd

    | ImportPsarc (psarcFile, targetFolder) ->
        let task() = async {
            let progress =
                let maxProgress =
                    3.
                    + match config.ConvertAudio with NoConversion -> 0. | _ -> 1.
                    + match config.RemoveDDOnImport with false -> 0. | true -> 1.
                let mutable currentProgress = 0.

                fun () ->
                    currentProgress <- currentProgress + 1.
                    currentProgress / maxProgress * 100.
                >> (psarcImportProgress :> IProgress<float>).Report

            let! project, fileName = PsarcImporter.import progress psarcFile targetFolder

            match config.ConvertAudio with
            | NoConversion ->
                ()
            | ToOgg | ToWav as conv ->
                DLCProject.getAudioFiles project
                |> Seq.iter (fun { Path=path } ->
                    if conv = ToOgg
                    then Conversion.wemToOgg path
                    else Conversion.wemToWav path)
                progress()

            if config.RemoveDDOnImport then
                do! project.Arrangements
                    |> List.choose Arrangement.pickInstrumental
                    |> List.map (fun inst -> async {
                        let arr = XML.InstrumentalArrangement.Load inst.XML
                        do! arr.RemoveDD false
                        arr.Save inst.XML })
                    |> Async.Sequential
                    |> Async.Ignore
                progress()

            return project, fileName }

        Utils.addTask PsarcImport state,
        Cmd.OfAsync.either task () PsarcImported (fun ex -> TaskFailed(ex, PsarcImport))

    | PsarcImported (project, projectFile) ->
        let cmd = Cmd.batch [
            Cmd.ofMsg (AddStatusMessage "PsarcImportComplete")
            Cmd.ofMsg (ProjectLoaded(project, projectFile)) ]
        Utils.removeTask PsarcImport state, cmd

    | ImportToolkitTemplate fileName ->
        try
            let project = ToolkitImporter.import fileName
            let coverArt = Utils.changeCoverArt state.CoverArt project.AlbumArtFile

            { state with Project = project; OpenProjectFile = None; CoverArt = coverArt
                         SelectedArrangementIndex = -1; SelectedToneIndex = -1 }, Cmd.none
        with e -> state, Cmd.ofMsg (ErrorOccurred e)

    | ImportTonesFromFile fileName ->
        let task () =
            match fileName with
            | EndsWith "psarc" ->
                Utils.importTonesFromPSARC fileName
            | EndsWith "xml" ->
                async { return [| Tone.fromXmlFile fileName |] }
            | EndsWith "json" ->
                Tone.fromJsonFile fileName
                |> Async.map Array.singleton
            | _ ->
                failwith "Unknown tone file format."

        state, Cmd.OfAsync.either task () ShowImportToneSelector ErrorOccurred

    | ImportProfileTones ->
        if String.IsNullOrWhiteSpace config.ProfilePath then
            state, Cmd.none
        else
            match Profile.importTones config.ProfilePath with
            | Ok toneArray ->
                state, Cmd.ofMsg (ShowImportToneSelector toneArray)
            | Error Profile.ToneImportError.NoTonesInProfile ->
                { state with Overlay = ErrorMessage(translate "NoTonesInProfile", None) }, Cmd.none
            | Error (Profile.ToneImportError.Exception ex) ->
                { state with Overlay = ErrorMessage(ex.Message, Option.ofString ex.StackTrace) }, Cmd.none

    | ShowImportToneSelector tones ->
        match tones with
        | [||] ->
            { state with Overlay = ErrorMessage(translate "couldNotFindTonesError", None) }, Cmd.none
        | [| one |] ->
            state, Cmd.ofMsg (ImportTones (List.singleton one))
        | _ ->
            state.SelectedImportTones.Clear()
            state.SelectedImportTones.Source <- null
            { state with Overlay = ImportToneSelector tones }, Cmd.none

    | SetAudioFile fileName ->
        let audioFile = { project.AudioFile with Path = fileName }
        let previewPath =
            let previewPath = Utils.previewPathFromMainAudio fileName
            let wavPreview = Path.ChangeExtension(previewPath, "wav")
            if File.Exists previewPath then
                previewPath
            elif File.Exists wavPreview then
                wavPreview
            else
                String.Empty

        let cmd =
            if config.AutoVolume && not <| String.endsWith ".wem" fileName then
                Cmd.ofMsg CalculateVolumes
            else
                Cmd.none

        let previewFile = { project.AudioPreviewFile with Path = previewPath }
        { state with Project = { project with AudioFile = audioFile; AudioPreviewFile = previewFile } }, cmd

    | ConvertToWem ->
        if DLCProject.audioFilesExist project then
            Utils.addTask WemConversion state,
            Cmd.OfAsync.either (Utils.convertAudio config.WwiseConsolePath) project WemConversionComplete (fun ex -> TaskFailed(ex, WemConversion))
        else
            state, Cmd.none

    | ConvertToWemCustom ->
        match getSelectedArrangement state with
        | Some (Instrumental { CustomAudio = Some audio }) ->
            Utils.addTask WemConversion state,
            Cmd.OfAsync.either (Wwise.convertToWem config.WwiseConsolePath) audio.Path WemConversionComplete (fun ex -> TaskFailed(ex, WemConversion))
        | _ ->
            state, Cmd.none

    | CalculateVolumes ->
        let previewPath = project.AudioPreviewFile.Path
        let doPreview = File.Exists previewPath && not <| String.endsWith "wem" previewPath
        let cmds =
            Cmd.batch [
                Cmd.ofMsg (CalculateVolume MainAudio)
                if doPreview then Cmd.ofMsg (CalculateVolume PreviewAudio) ]
        state, cmds

    | CalculateVolume target ->
        let path =
            match target with
            | MainAudio -> project.AudioFile.Path
            | PreviewAudio -> project.AudioPreviewFile.Path
            | CustomAudio (path, _) -> path
        let task () = async { return Volume.calculate path }
        Utils.addTask (VolumeCalculation target) state,
        Cmd.OfAsync.either task () (fun v -> VolumeCalculated(v, target)) (fun ex -> TaskFailed(ex, (VolumeCalculation target)))

    | VolumeCalculated (volume, target) ->
        let project =
            match target with
            | MainAudio ->
                { project with AudioFile = { project.AudioFile with Volume = volume } }
            | PreviewAudio ->
                { project with AudioPreviewFile = { project.AudioPreviewFile with Volume = volume } }
            | CustomAudio (_, arrId) ->
                let arrangements =
                    project.Arrangements
                    |> List.map (function
                        | Instrumental inst when inst.PersistentID = arrId ->
                            let audio = inst.CustomAudio |> Option.map (fun a -> { a with Volume = volume })
                            Instrumental { inst with CustomAudio = audio }
                        | other ->
                            other)
                { project with Arrangements = arrangements }

        Utils.removeTask (VolumeCalculation target) { state with Project = project }, Cmd.none

    | SetCoverArt fileName ->
        { state with CoverArt = Utils.changeCoverArt state.CoverArt fileName
                     Project = { project with AlbumArtFile = fileName } }, Cmd.none

    | AddArrangements files ->
        addArrangements files state, Cmd.none

    | SetSelectedArrangementIndex index ->
        { state with SelectedArrangementIndex = index }, Cmd.none

    | SetSelectedToneIndex index ->
        // Change the selected gear slot if it is not available in the newly selected tone
        // Prevents creating gaps in the tone gear slots
        let selectedGearSlot =
            let tone = project.Tones.[index]
            match state.SelectedGearSlot with
            | ToneGear.PrePedal i when tone.GearList.PrePedals.[i].IsNone ->
                ToneGear.PrePedal 0
            | ToneGear.PostPedal i when tone.GearList.PostPedals.[i].IsNone ->
                ToneGear.PostPedal 0
            | ToneGear.Rack i when tone.GearList.Racks.[i].IsNone ->
                ToneGear.Rack 0
            | _ ->
                state.SelectedGearSlot

        { state with SelectedToneIndex = index; SelectedGearSlot = selectedGearSlot }, Cmd.none

    | DeleteArrangement ->
        let arrangements, index = removeSelected project.Arrangements state.SelectedArrangementIndex

        { state with Project = { project with Arrangements = arrangements }
                     SelectedArrangementIndex = index }, Cmd.none

    | DeleteTone ->
        let tones, index = removeSelected project.Tones state.SelectedToneIndex

        { state with Project = { project with Tones = tones }
                     SelectedToneIndex = index }, Cmd.none

    | DuplicateTone ->
        let duplicate =
            getSelectedTone state
            |> Option.map (fun tone ->
                { tone with Name = tone.Name + "2"; Key = String.Empty })
            |> Option.toList
        { state with Project = { project with Tones = duplicate @ project.Tones } }, Cmd.none

    | MoveTone dir ->
        let tones, index = moveSelected dir state.SelectedToneIndex project.Tones
        { state with Project = { project with Tones = tones }; SelectedToneIndex = index }, Cmd.none

    | AddDbTone (api, id) ->
        match api.GetToneById id with
        | Some tone ->
            { state with Project = { project with Tones = tone::project.Tones} }, Cmd.none
        | None ->
            state, Cmd.none

    | MoveArrangement dir ->
        let arrangements, index = moveSelected dir state.SelectedArrangementIndex project.Arrangements
        { state with Project = { project with Arrangements = arrangements }; SelectedArrangementIndex = index }, Cmd.none

    | CreatePreviewAudio (SetupStartTime) ->
        let totalLength = Utils.getLength project.AudioFile.Path
        // Remove the length of the preview from the total length
        let length = totalLength - TimeSpan.FromSeconds 28.
        { state with Overlay = SelectPreviewStart length }, Cmd.none

    | CreatePreviewAudio (CreateFile) ->
        match project.AudioPreviewStartTime with
        | None ->
            state, Cmd.none
        | Some startTime ->
            let task () = async {
                let targetPath = Utils.createPreviewAudioPath project.AudioFile.Path
                Preview.create project.AudioFile.Path targetPath (TimeSpan.FromSeconds startTime)
                return targetPath }

            { state with Overlay = NoOverlay },
            Cmd.OfAsync.either task () (FileCreated >> CreatePreviewAudio) ErrorOccurred

    | CreatePreviewAudio (FileCreated previewPath) ->
        let previewFile = { project.AudioPreviewFile with Path = previewPath }
        let cmd =
            match config.AutoVolume with
            | true -> Cmd.ofMsg (CalculateVolume PreviewAudio)
            | false -> Cmd.none

        // Delete the old converted file if one exists
        let overlay =
            let wemPreview = Path.ChangeExtension(previewPath, "wem")
            try
                File.tryMap File.Delete wemPreview |> ignore
                NoOverlay
            with ex ->
                let msg = translatef "previewDeleteError" [| Path.GetFileName(wemPreview); ex.Message |]
                ErrorMessage(msg, ex.StackTrace |> Option.ofString)

        { state with Project = { project with AudioPreviewFile = previewFile }
                     Overlay = overlay }, cmd

    | ShowSortFields shown -> { state with ShowSortFields = shown }, Cmd.none
    
    | ShowJapaneseFields shown -> { state with ShowJapaneseFields = shown }, Cmd.none

    | ShowOverlay overlay -> { state with Overlay = overlay }, Cmd.none
    
    | SetConfiguration (newConfig, enableLoad, wasAbnormalExit) ->
        if config.Locale <> newConfig.Locale then
            changeLocale newConfig.Locale
        let cmd =
            if enableLoad && File.Exists newConfig.PreviousOpenedProject then
                if newConfig.LoadPreviousOpenedProject then
                    Cmd.ofMsg (OpenProject newConfig.PreviousOpenedProject)
                elif wasAbnormalExit then
                    Cmd.ofMsg (ShowOverlay AbnormalExitMessage)
                else
                    Cmd.none
            else
                Cmd.none

        { state with Config = newConfig }, cmd

    | SetRecentFiles recent -> { state with RecentFiles = recent }, Cmd.none

    | SetAvailableUpdate (Error _) ->
        // Don't show an error message if the update check fails when starting the program
        state, Cmd.none

    | SetAvailableUpdate (Ok update) ->
        let messages =
            match update with
            | Some update ->
                let statusMessages =
                    state.StatusMessages
                    |> List.filter (function UpdateMessage _ -> false | _ -> true)

                UpdateMessage(update)::statusMessages
            | _ -> 
                state.StatusMessages

        { state with StatusMessages = messages; AvailableUpdate = update }, Cmd.none

    | DismissUpdateMessage ->
        let statusMessages =
            state.StatusMessages
            |> List.filter (function UpdateMessage _ -> false | _ -> true)

        { state with StatusMessages = statusMessages }, Cmd.none

    | ShowUpdateInformation ->
        let newState =
            match state.AvailableUpdate with
            | Some update ->
                { state with Overlay = UpdateInformationDialog update }
            | None ->
                state
        newState, Cmd.none

    | CheckForUpdates ->
        state, Cmd.OfAsync.either OnlineUpdate.checkForUpdates () UpdateCheckCompleted ErrorOccurred

    | UpdateCheckCompleted (Error msg) ->
        { state with AvailableUpdate = None; Overlay = ErrorMessage(msg, None) }, Cmd.none

    | UpdateCheckCompleted (Ok update) ->
        let msg =
            match update with
            | Some _ -> ShowUpdateInformation
            | None -> AddStatusMessage "noUpdate"
        { state with AvailableUpdate = update }, Cmd.ofMsg msg

    | UpdateAndRestart ->
        match state.AvailableUpdate with
        | Some update ->
            let messageId = Guid.NewGuid()
            let statusMessages =
                MessageString(messageId, translate "downloadingUpdate")::state.StatusMessages

            { state with StatusMessages = statusMessages; Overlay = NoOverlay },
            Cmd.OfAsync.attempt OnlineUpdate.downloadAndApplyUpdate update (fun e -> UpdateFailed(messageId, e))
        | None ->
            state, Cmd.none

    | UpdateFailed (messageId, error) ->
        { state with Overlay = exceptionToErrorMessage error }, Cmd.ofMsg (RemoveStatusMessage messageId)

    | SaveProjectAs ->
        state, Cmd.ofMsg (Dialog.SaveProjectAs |> ShowDialog)

    | SaveProject targetPath ->
        let task() = async {
            do! DLCProject.save targetPath project
            return targetPath }
        state, Cmd.OfAsync.either task () ProjectSaved ErrorOccurred

    | ProjectSaved target ->
        let recent, newConfig, cmd = updateRecentFilesAndConfig target state

        { state with OpenProjectFile = Some target
                     SavedProject = project
                     RecentFiles = recent
                     Config = newConfig }, cmd

    | ProjectSaveOrSaveAs ->
        let msg =
            state.OpenProjectFile
            |> Option.map SaveProject
            |> Option.defaultValue SaveProjectAs
        state, Cmd.ofMsg msg

    | AutoSaveProject ->
        match state.OpenProjectFile with
        | Some projectPath -> state, Cmd.ofMsg (SaveProject projectPath)
        | None -> state, Cmd.none

    | OpenPreviousProjectConfirmed ->
        { state with Overlay = NoOverlay }, Cmd.ofMsg (OpenProject config.PreviousOpenedProject)

    | OpenProject fileName ->
        state, Cmd.OfAsync.either DLCProject.load fileName (fun p -> ProjectLoaded(p, fileName)) ErrorOccurred

    | ProjectLoaded (project, projectFile) ->
        let coverArt = Utils.changeCoverArt state.CoverArt project.AlbumArtFile
        let project = DLCProject.updateToneInfo project
        let recent, newConfig, cmd = updateRecentFilesAndConfig projectFile state

        { state with CoverArt = coverArt
                     Project = project
                     SavedProject = project
                     OpenProjectFile = Some projectFile
                     RecentFiles = recent
                     Config = newConfig
                     ArrangementIssues = Map.empty
                     SelectedArrangementIndex = -1
                     SelectedToneIndex = -1 },
        cmd

    | EditInstrumental edit ->
        match getSelectedArrangement state with
        | Some (Instrumental inst) ->
            editInstrumental state edit state.SelectedArrangementIndex inst
        | _ ->
            state, Cmd.none

    | EditVocals edit ->
        match getSelectedArrangement state with
        | Some (Vocals vocals) ->
            editVocals state edit state.SelectedArrangementIndex vocals
        | _ ->
            state, Cmd.none

    | EditTone edit ->
        match state.SelectedToneIndex with
        | -1 -> state, Cmd.none
        | index -> editTone state edit index

    | EditProject edit -> { state with Project = editProject edit project }, Cmd.none

    | EditConfig edit -> { state with Config = editConfig edit config }, Cmd.none

    | DeleteTestBuilds ->
        let packageName = TestPackageBuilder.createPackageName project
        let filesToDelete =
            if packageName.Length >= DLCKey.MinimumLength && Directory.Exists config.TestFolderPath then
                Directory.EnumerateFiles config.TestFolderPath
                |> Seq.filter (Path.GetFileName >> (String.startsWith packageName))
                |> List.ofSeq
            else
                List.empty

        match filesToDelete with
        | [] ->
            state, Cmd.none
        | [ _ ] as one ->
            state, Cmd.ofMsg (DeleteConfirmed one)
        | many ->
            { state with Overlay = DeleteConfirmation(many) }, Cmd.none

    | DeleteConfirmed files ->
        let cmd =
            try
                List.iter File.Delete files
                Cmd.none
            with e ->
                Cmd.ofMsg <| ErrorOccurred e
        { state with Overlay = NoOverlay }, cmd

    | GenerateNewIds ->
        let arrangements =
            project.Arrangements
            |> List.mapi (fun i arr ->
                if i = state.SelectedArrangementIndex then
                    TestPackageBuilder.generateIds arr
                else
                    arr)
        { state with Project = { project with Arrangements = arrangements } }, Cmd.none

    | GenerateAllIds ->
        let arrangements = TestPackageBuilder.generateAllIds project.Arrangements
        { state with Project = { project with Arrangements = arrangements } }, Cmd.none

    | ApplyLowTuningFix ->
        let arrangements =
            match getSelectedArrangement state with
            | Some (Instrumental inst) ->
                let updated =
                    { inst with TuningPitch = inst.TuningPitch / 2.
                                Tuning = Array.map ((+) 12s) inst.Tuning }
                    |> Instrumental
                project.Arrangements |> List.updateAt state.SelectedArrangementIndex updated
            | _ ->
                project.Arrangements

        { state with Project = { project with Arrangements = arrangements } }, Cmd.none

    | Build _ when not <| Utils.canBuild state ->
        state, Cmd.none

    | Build PitchShifted ->
        buildPackage Release (ReleasePackageBuilder.buildPitchShifted state.OpenProjectFile) state

    | Build Test ->
        if String.notEmpty config.TestFolderPath then
            buildPackage Test (TestPackageBuilder.build state.CurrentPlatform) state
        else
            state, Cmd.none

    | Build Release ->
        buildPackage Release (ReleasePackageBuilder.build state.OpenProjectFile) state

    | BuildComplete buildType ->
        let task() = async {
            if buildType = Release && config.OpenFolderAfterReleaseBuild then
                let projectPath =
                    ReleasePackageBuilder.getTargetDirectory state.OpenProjectFile project
                Process.Start(ProcessStartInfo(projectPath, UseShellExecute = true)) |> ignore }

        let cmd = Cmd.batch [
            Cmd.OfAsync.attempt task () ErrorOccurred
            Cmd.ofMsg (AddStatusMessage "BuildPackageComplete") ]

        Utils.removeTask BuildPackage state, cmd

    | WemConversionComplete _ ->
        Utils.removeTask WemConversion state,
        Cmd.ofMsg (AddStatusMessage "WemConversionComplete")

    | CheckArrangements ->
        let task() = async { return Utils.checkArrangements project arrangementCheckProgress }

        Utils.addTask ArrangementCheck state,
        Cmd.OfAsync.either task () CheckCompleted (fun ex -> TaskFailed(ex, ArrangementCheck))

    | CheckCompleted issues ->
        { Utils.removeTask ArrangementCheck state with ArrangementIssues = issues },
        Cmd.ofMsg (AddStatusMessage "ValidationComplete")

    | TaskProgressChanged (progressedTask, progress) ->
        let messages =
            state.StatusMessages
            |> List.map (function
                | TaskWithProgress (task, _) when task = progressedTask -> TaskWithProgress(task, progress)
                | other -> other)
        { state with StatusMessages = messages }, Cmd.none

    | PsarcUnpacked ->
        Utils.removeTask PsarcUnpack state,
        Cmd.ofMsg (AddStatusMessage "PsarcUnpackComplete")

    | WemToOggConversionCompleted ->
        Utils.removeTask WemToOggConversion state, Cmd.none

    | AddStatusMessage locString ->
        let id = Guid.NewGuid()
        let message = translate locString
        let messages = MessageString(id,  message)::state.StatusMessages
        { state with StatusMessages = messages }, Cmd.OfAsync.result (removeStatusMessage id)

    | RemoveStatusMessage removeId ->
        let messages =
            state.StatusMessages
            |> List.filter (function
                | MessageString (id, _) when id = removeId -> false
                | _ -> true)
        { state with StatusMessages = messages }, Cmd.none

    | ShowIssueViewer ->
        match getSelectedArrangement state with
        | Some arr ->
            let xmlFile = Arrangement.getFile arr
            { state with Overlay = IssueViewer (state.ArrangementIssues.[xmlFile]) }, Cmd.none
        | None ->
            state, Cmd.none

    | ErrorOccurred e ->
        { state with Overlay = exceptionToErrorMessage e }, Cmd.none

    | TaskFailed (e, failedTask) ->
        { Utils.removeTask failedTask state with Overlay = exceptionToErrorMessage e }, Cmd.none

    | ChangeLocale newLocale ->
        if config.Locale <> newLocale then changeLocale newLocale
        { state with Config = { config with Locale = newLocale } }, Cmd.none

    | ToolsMsg msg ->
        Tools.update msg state

    | ShowDialog dialog ->
        Dialogs.showDialog dialog state

    | HotKeyMsg msg ->
        match state.Overlay, msg with
        | NoOverlay, _ | _, CloseOverlay ->
            state, Cmd.ofMsg msg
        | _ ->
            // Ignore the message when an overlay is open
            state, Cmd.none
