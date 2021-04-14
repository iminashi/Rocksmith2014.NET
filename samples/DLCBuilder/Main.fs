module DLCBuilder.Main

open Rocksmith2014
open Rocksmith2014.Audio
open Rocksmith2014.Common
open Rocksmith2014.Common.Manifest
open Rocksmith2014.DD
open Rocksmith2014.DLCProject
open Rocksmith2014.PSARC
open Rocksmith2014.XML.Processing
open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Controls.Selection
open Elmish
open System
open System.Diagnostics
open System.IO
open EditFunctions

let checkProgress = Progress<float>()

type private ArrangementAddingResult =
    | ShouldInclude
    | MaxInstrumentals
    | MaxShowlights
    | MaxVocals

let private addArrangements files state =
    let results = Array.map (Arrangement.fromFile translate) files
    
    let shouldInclude arrangements arr =
        let count f = (List.choose f arrangements).Length
        match arr with
        | Showlights _ when count Arrangement.pickShowlights = 1 -> MaxShowlights
        | Instrumental _ when count Arrangement.pickInstrumental = 5 -> MaxInstrumentals
        | Vocals _ when count Arrangement.pickVocals = 2 -> MaxVocals
        | _ -> ShouldInclude
       
    let mainArrangementExists inst arrangements =
        arrangements
        |> List.exists (function
            | Instrumental x ->
                inst.RouteMask = x.RouteMask
                && inst.Priority = x.Priority
                && x.Priority = ArrangementPriority.Main
            | _ -> false)

    let createErrorMsg (path: string) error = $"%s{Path.GetFileName path}:\n%s{error}"
    
    let arrangements, errors =
        ((state.Project.Arrangements, []), results)
        ||> Array.fold (fun (arrs, errors) result ->
            match result with
            | Ok (arr, _) ->
                match shouldInclude arrs arr with
                | ShouldInclude ->
                    let arr =
                        // Prevent multiple main arrangements of the same type
                        match arr with
                        | Instrumental inst when mainArrangementExists inst arrs ->
                            Instrumental { inst with Priority = ArrangementPriority.Alternative }
                        | _ ->
                            arr
                    arr::arrs, errors
                | error ->
                    let error = createErrorMsg (Arrangement.getFile arr) (translate (string error))
                    arrs, error::errors
            | Error (file, error) ->
                let error = createErrorMsg file error
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

let init arg =
    let commands =
        let loadProject =
            arg
            |> Option.bind (function
                | EndsWith ".rs2dlc" as path ->
                    Some <| Cmd.OfAsync.either DLCProject.load path (fun p -> ProjectLoaded(p, path)) ErrorOccurred
                | _ ->
                    None)

        Cmd.batch [
            Cmd.OfAsync.perform Configuration.load () (fun config -> SetConfiguration(config, loadProject.IsNone)) 
            Cmd.OfAsync.perform RecentFilesList.load () SetRecentFiles
            yield! loadProject |> Option.toList
        ]

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
      ArrangementIssues = Map.empty }, commands

let private getSelectedArrangement state =
    match state.SelectedArrangementIndex with
    | -1 -> None
    | index -> Some state.Project.Arrangements.[index]

let private removeSelected initialList index =
    let list = List.removeAt index initialList
    let newSelectedIndex =
        if list.IsEmpty then
            -1
        else
            min index (list.Length - 1)
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

        Utils.addTask BuildPackage state false,
        Cmd.OfAsync.either task state.Project (fun () -> BuildComplete buildType) (fun ex -> TaskFailed(ex, BuildPackage))

let private removeStatusMessage (id: Guid) = async {
    do! Async.Sleep 4000
    return RemoveStatusMessage id }

let update (msg: Msg) (state: State) =
    let { Project=project; Config=config } = state

    match msg with
    | SetSelectedGear gear ->
        let cmd =
            match gear with
            | Some gear ->
                let currentGear =
                    match state.SelectedToneIndex with
                    | -1 -> None
                    | index -> ToneGear.getGearDataForCurrentPedal project.Tones.[index].GearList state.SelectedGearSlot
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
        if state.SelectedToneIndex <> -1 then
            { state with Overlay = ToneEditor }, Cmd.none
        else
            state, Cmd.none

    | NewProject ->
        state.CoverArt |> Option.iter(fun x -> x.Dispose())
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
        match state.SelectedToneIndex with
        | -1 ->
            state, Cmd.none
        | index ->
            let tone = project.Tones.[index]
            state, Cmd.ofMsg (Dialog.ExportTone tone |> ShowDialog)

    | ExportTone (tone, path) ->
        let task =
            match path with
            | EndsWith "xml" -> Tone.exportXml path
            | _ -> Tone.exportJson path
        state, Cmd.OfAsync.attempt task tone ErrorOccurred

    | CloseOverlay ->
        let cmd =
            match state.Overlay with
            | ConfigEditor -> Cmd.OfAsync.attempt Configuration.save config ErrorOccurred
            | _ -> Cmd.none
        { state with Overlay = NoOverlay }, cmd

    | ImportPsarc (psarcFile, targetFolder) ->
        let task() = async {
            let! project, fileName = PsarcImporter.import psarcFile targetFolder

            match config.ConvertAudio with
            | NoConversion -> ()
            | ToOgg | ToWav as conv ->
                [ yield project.AudioFile
                  yield project.AudioPreviewFile
                  yield! project.Arrangements
                         |> List.choose (function Instrumental i -> i.CustomAudio | _ -> None) ]
                |> List.iter (fun { Path=path } ->
                    if conv = ToOgg
                    then Conversion.wemToOgg path
                    else Conversion.wemToWav path)

            if config.RemoveDDOnImport then
                do! project.Arrangements
                    |> List.map (fun a -> async {
                        match a with
                        | Instrumental i ->
                            let arr = XML.InstrumentalArrangement.Load i.XML
                            do! arr.RemoveDD false
                            arr.Save i.XML
                        | _ -> () })
                    |> Async.Sequential
                    |> Async.Ignore

            return project, fileName }

        let newState, onError =
            match config.ConvertAudio with
            | ToOgg | ToWav ->
                Utils.addTask PsarcImport state false, fun ex -> TaskFailed(ex, PsarcImport)
            | NoConversion ->
                state, ErrorOccurred

        newState, Cmd.OfAsync.either task () ProjectLoaded onError

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
            | Error msg ->
                { state with Overlay = ErrorMessage(msg, None) }, Cmd.none

    | ShowImportToneSelector tones ->
        match tones with
        | [||] ->
            { state with Overlay = ErrorMessage(translate "couldNotFindTonesError", None) }, Cmd.none
        | [| _ |] ->
            state, Cmd.ofMsg (ImportTones (List.ofArray tones))
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
            Utils.addTask WemConversion state false,
            Cmd.OfAsync.either (Utils.convertAudio config.WwiseConsolePath) project WemConversionComplete (fun ex -> TaskFailed(ex, WemConversion))
        else
            state, Cmd.none

    | ConvertToWemCustom ->
        match getSelectedArrangement state with
        | Some (Instrumental { CustomAudio = Some audio }) ->
            Utils.addTask WemConversion state false,
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
        Utils.addTask (VolumeCalculation target) state false,
        Cmd.OfAsync.either task () (fun v -> VolumeCalculated(v, target)) (fun ex -> TaskFailed(ex, (VolumeCalculation target)))

    | VolumeCalculated (volume, target) ->
        let state =
            match target with
            | MainAudio ->
                { state with Project = { project with AudioFile = { project.AudioFile with Volume = volume } } }
            | PreviewAudio ->
                { state with Project = { project with AudioPreviewFile = { project.AudioPreviewFile with Volume = volume } } }
            | CustomAudio (_, arrId) ->
                project.Arrangements
                |> List.tryPick (fun arr ->
                    match arr with
                    | Instrumental inst when inst.PersistentID = arrId ->
                        Some arr
                    | _ ->
                        None)
                |> function
                | Some (Instrumental inst as old) ->
                    let updated =
                        Instrumental { inst with CustomAudio = inst.CustomAudio |> Option.map (fun a -> { a with Volume = volume }) }
                        
                    let arrangements =
                        project.Arrangements
                        |> List.update old updated

                    { state with Project = { project with Arrangements = arrangements } }
                | _ ->
                    state
                    
        Utils.removeTask (VolumeCalculation target) state, Cmd.none

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
        match state.SelectedToneIndex with
        | -1 ->
            state, Cmd.none
        | index ->
            let tone = project.Tones.[index]
            let duplicate = { tone with Name = tone.Name + "2"; Key = String.Empty }
            { state with Project = { project with Tones = duplicate::project.Tones } }, Cmd.none

    | MoveTone dir ->
        let tones, index = moveSelected dir state.SelectedToneIndex project.Tones
        { state with Project = { project with Tones = tones }; SelectedToneIndex = index }, Cmd.none

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
            if config.AutoVolume then
                Cmd.ofMsg (CalculateVolume PreviewAudio)
            else
                Cmd.none

        // Delete the old converted file if one exists
        let overlay =
            let wemPreview = Path.ChangeExtension(previewPath, "wem")
            if File.Exists wemPreview then
                try
                    File.Delete wemPreview
                    NoOverlay
                with ex ->
                    let msg = translatef "previewDeleteError" [| Path.GetFileName(wemPreview); ex.Message |]
                    ErrorMessage(msg, ex.StackTrace |> Option.ofString)
            else
                NoOverlay

        { state with Project = { project with AudioPreviewFile = previewFile }
                     Overlay = overlay }, cmd

    | ShowSortFields shown -> { state with ShowSortFields = shown }, Cmd.none
    
    | ShowJapaneseFields shown -> { state with ShowJapaneseFields = shown }, Cmd.none

    | ShowConfigEditor -> { state with Overlay = ConfigEditor }, Cmd.none
    
    | SetConfiguration (newConfig, enableLoad) ->
        if config.Locale <> newConfig.Locale then
            changeLocale newConfig.Locale
        let cmd =
            if enableLoad && newConfig.LoadPreviousOpenedProject && File.Exists newConfig.PreviousOpenedProject then
                Cmd.ofMsg (OpenProject newConfig.PreviousOpenedProject)
            else
                Cmd.none
            
        { state with Config = newConfig }, cmd

    | SetRecentFiles recent -> { state with RecentFiles = recent }, Cmd.none

    | SaveProjectAs ->
        state, Cmd.ofMsg (Dialog.SaveProjectAs |> ShowDialog)

    | SaveProject targetPath ->
        let task() = async {
            do! DLCProject.save targetPath project
            return targetPath }
        state, Cmd.OfAsync.either task () ProjectSaved ErrorOccurred

    | ProjectSaved target ->
        let recent = RecentFilesList.update target state.RecentFiles

        let newConfig = { config with PreviousOpenedProject = target }
        let cmd =
            if config.PreviousOpenedProject <> target then
                Cmd.OfAsync.attempt Configuration.save newConfig ErrorOccurred
            else
                Cmd.none

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

    | OpenProject fileName ->
        state, Cmd.OfAsync.either DLCProject.load fileName (fun p -> ProjectLoaded(p, fileName)) ErrorOccurred

    | ProjectLoaded (project, projectFile) ->
        let coverArt = Utils.changeCoverArt state.CoverArt project.AlbumArtFile
        let project = DLCProject.updateToneInfo project
        let recent = RecentFilesList.update projectFile state.RecentFiles

        let newConfig = { config with PreviousOpenedProject = projectFile }
        let cmd =
            if config.PreviousOpenedProject <> projectFile then
                Cmd.OfAsync.attempt Configuration.save newConfig ErrorOccurred
            else
                Cmd.none

        { state with CoverArt = coverArt
                     Project = project
                     SavedProject = project
                     OpenProjectFile = Some projectFile
                     RecentFiles = recent
                     RunningTasks = state.RunningTasks |> Set.remove PsarcImport
                     Config = newConfig
                     SelectedArrangementIndex = -1
                     SelectedToneIndex = -1 }, cmd

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

    | ShowPitchShifter -> { state with Overlay = PitchShifter }, Cmd.none

    | BuildPitchShifted ->
        buildPackage Release (ReleasePackageBuilder.buildPitchShifted state.OpenProjectFile) state

    | Build Test ->
        buildPackage Test (TestPackageBuilder.build state.CurrentPlatform) state

    | Build Release ->
        buildPackage Release (ReleasePackageBuilder.build state.OpenProjectFile) state

    | BuildComplete buildType ->
        if buildType = Release && config.OpenFolderAfterReleaseBuild then
            let projectPath =
                state.OpenProjectFile
                |> Option.defaultValue project.AudioFile.Path
                |> Path.GetDirectoryName
            Process.Start(ProcessStartInfo(projectPath, UseShellExecute = true)) |> ignore

        { state with RunningTasks = state.RunningTasks.Remove BuildPackage },
        Cmd.ofMsg (AddStatusMessage (translate "BuildPackageComplete"))

    | WemConversionComplete _ ->
        { state with RunningTasks = state.RunningTasks.Remove WemConversion },
        Cmd.ofMsg (AddStatusMessage (translate "WemConversionComplete"))

    | CheckArrangements ->
        let task() = async { return Utils.checkArrangements project checkProgress }

        Utils.addTask ArrangementCheck state true,
        Cmd.OfAsync.either task () CheckCompleted (fun ex -> TaskFailed(ex, ArrangementCheck))

    | CheckCompleted issues ->
        { Utils.removeTask ArrangementCheck state with ArrangementIssues = issues },
        Cmd.ofMsg (AddStatusMessage (translate "ValidationComplete"))

    | TaskProgressChanged (progressedTask, progress) ->
        let messages =
            state.StatusMessages
            |> List.map (function
                | TaskProgress (id, task, _) when task = progressedTask -> TaskProgress(id, task, progress)
                | other -> other)
        { state with StatusMessages = messages }, Cmd.none

    | PsarcUnpacked ->
        Utils.removeTask PsarcUnpack state,
        Cmd.ofMsg (AddStatusMessage (translate "PsarcUnpackComplete"))
        
    | AddStatusMessage message ->
        let id = Guid.NewGuid()
        let messages = MessageString(id,  message)::state.StatusMessages
        { state with StatusMessages = messages }, Cmd.OfAsync.result (removeStatusMessage id)

    | RemoveStatusMessage removeId ->
        let messages =
            state.StatusMessages
            |> List.filter (fun message -> StatusMessage.getId message <> removeId)
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

    | CloseApplication ->
        (Application.Current.ApplicationLifetime :?> IClassicDesktopStyleApplicationLifetime).Shutdown(0)
        state, Cmd.none
    
    // When the user canceled any of the dialogs
    | Ignore ->
        state, Cmd.none
