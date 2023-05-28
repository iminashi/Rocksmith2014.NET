module DLCBuilder.Main

open Elmish
open Rocksmith2014
open Rocksmith2014.Audio
open Rocksmith2014.Common
open Rocksmith2014.Common.Manifest
open Rocksmith2014.Common.Profile
open Rocksmith2014.DLCProject
open Rocksmith2014.PSARC
open Rocksmith2014.XML.Processing
open System
open System.IO
open EditFunctions
open StateUtils

let private exceptionToErrorMessage (ex: exn) =
    let message =
        match ex with
        | :? AggregateException as a ->
            a.InnerExceptions
            |> Seq.map (fun x -> x.Message)
            |> Seq.distinct
            |> String.concat ", "
        | _ ->
            ex.Message

    ErrorMessage(message, Some(Utils.createExceptionInfoString ex))

let private buildPackage build state =
    match BuildValidator.validate state.Project with
    | Error error ->
        let msg =
            match error with
            | InvalidDLCKey ->
                state.Localizer.TranslateFormat(string error, [| DLCKey.MinimumLength |])
            | MultipleTonesSameKey key ->
                state.Localizer.TranslateFormat("MultipleTonesSameKey", [| key |])
            | other ->
                state.Localizer.Translate(string other)

        { state with Overlay = ErrorMessage(msg, None) }, Cmd.none
    | Ok () ->
        let task = build state.Config

        addTask BuildPackage state,
        Cmd.OfAsync.either task state.Project BuildComplete (fun ex -> TaskFailed(ex, BuildPackage))

let update (msg: Msg) (state: State) =
    let { Project = project; Config = config } = state
    let translate = state.Localizer.Translate
    let translatef key args = state.Localizer.TranslateFormat(key, args)

    match msg with
    | ReadAudioLength ->
        let task () =
            async {
                match project.AudioFile.Path |> Option.ofString with
                | Some (HasExtension (".wav" | ".ogg") as path) ->
                    return Audio.Utils.getLength path |> Some
                | _ ->
                    return None
            }

        state, Cmd.OfAsync.either task () SetAudioLength ErrorOccurred

    | SetAudioLength length ->
        { state with AudioLength = length }, Cmd.none

    | SetEditedPsarcAppId appId ->
        let quickEditData =
            state.QuickEditData
            |> Option.map (fun x -> { x with AppId = AppId.ofString appId })

        { state with QuickEditData = quickEditData }, Cmd.none

    | OpenWithShell path ->
        try
            Utils.openWithShell path
            state, Cmd.none
        with e ->
            state, Cmd.ofMsg (ErrorOccurred(e))

    | EnableIssueForProject code ->
        let updatedProject =
            { project with IgnoredIssues = project.IgnoredIssues.Remove(code) }

        { state with Project = updatedProject }, Cmd.none

    | IgnoreIssueForProject code ->
        let updatedProject =
            { project with IgnoredIssues = project.IgnoredIssues.Add(code) }

        { state with Project = updatedProject }, Cmd.none

    | ShowJapaneseLyricsCreator ->
        let newState =
            project.Arrangements
            |> List.choose Arrangement.pickVocals
            |> List.tryFind (fun x -> not x.Japanese)
            |> Option.map (fun vocals ->
                let initialState =
                    vocals.XML
                    |> XML.Vocals.Load
                    |> JapaneseLyricsCreator.LyricsCreatorState.init

                { state with Overlay = JapaneseLyricsCreator initialState })
            |> Option.defaultValue state

        newState, Cmd.none

    | ConfirmIdRegeneration (ids, reply) ->
        let arrangements =
            project.Arrangements
            |> List.filter (function
                | Instrumental inst when ids |> List.contains inst.PersistentID ->
                    true
                | _ ->
                    false)

        { state with Overlay = IdRegenerationConfirmation(arrangements, reply) }, Cmd.none

    | SetNewArrangementIds replacementMap ->
        let arrangements =
            project.Arrangements
            |> List.map (function
                | Instrumental inst as arr ->
                    replacementMap
                    |> Map.tryFind inst.PersistentID
                    |> Option.map (fun replacement ->
                        // Only get the IDs in case the user has edited the arrangement in the project
                        { inst with MasterID = Arrangement.getMasterId replacement
                                    PersistentID = Arrangement.getPersistentId replacement }
                        |> Instrumental)
                    |> Option.defaultValue arr
                | other ->
                    other)

        let updatedProject = { project with Arrangements = arrangements }

        { state with Project = updatedProject }, Cmd.none

    | SetSelectedGearSlot gearSlot ->
        { state with SelectedGearSlot = gearSlot }, Cmd.none

    | SetManuallyEditingKnobKey key ->
        { state with ManuallyEditingKnobKey = key }, Cmd.none

    | ShowToneEditor ->
        match getSelectedTone state with
        | Some _ -> showOverlay state ToneEditor, Cmd.none
        | None -> state, Cmd.none

    | NewProject ->
        state.AlbumArtLoader.InvalidateCache()
        deleteTemporaryFilesForQuickEdit state

        { state with
            Project = DLCProject.Empty
            SavedProject = DLCProject.Empty
            OpenProjectFile = None
            AlbumArtLoadTime = None
            QuickEditData = None
            ImportedBuildToolVersion = None
            AudioLength = None
            SelectedArrangementIndex = -1
            SelectedToneIndex = -1 }, Cmd.none

    | SetSelectedImportTones tones ->
        { state with SelectedImportTones = tones }, Cmd.none

    | ImportSelectedTones ->
        addTones state state.SelectedImportTones, Cmd.none

    | ImportTones tones ->
        addTones state tones, Cmd.none

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

        state, Cmd.OfTask.attempt task tone ErrorOccurred

    | CloseOverlay method ->
        let cmd =
            match state.Overlay with
            | ConfigEditor _ ->
                Cmd.OfTask.attempt Configuration.save config ErrorOccurred
            | ToneCollection c ->
                ToneCollection.CollectionState.disposeCollection c.ActiveCollection
                Cmd.none
            | _ ->
                Cmd.none

        match state.Overlay with
        | IdRegenerationConfirmation _ when method <> OverlayCloseMethod.OverlayButton ->
            // The confirmation needs to be answered with the buttons in the overlay
            state, cmd
        | JapaneseLyricsCreator _ when method = OverlayCloseMethod.ClickedOutside ->
            // Disabled to prevent accidentally closing the overlay with a click outside
            state, cmd
        | _ ->
            { state with Overlay = NoOverlay }, cmd

    | ImportPsarcQuick psarcPath ->
        let task () =
            task {
                let targetFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())
                let! r = importPsarc config targetFolder psarcPath
                let data =
                    { PsarcPath = psarcPath
                      TempDirectory = targetFolder
                      AppId = r.AppId
                      BuildToolVersion = r.BuildToolVersion }

                return r.GeneratedProject, Quick data
            }

        addTask PsarcImport state,
        Cmd.OfTask.either task () PsarcImported (fun ex -> TaskFailed(ex, PsarcImport))

    | ImportPsarc (psarcPath, targetFolder) ->
        let task () =
            task {
                let targetFolder = Path.Combine(targetFolder, Path.GetFileNameWithoutExtension(psarcPath))
                let! r = importPsarc config targetFolder psarcPath
                let data =
                    { ProjectFilePath = r.ProjectPath
                      BuildToolVersion = r.BuildToolVersion }

                return r.GeneratedProject, Normal data
            }

        addTask PsarcImport state,
        Cmd.OfTask.either task () PsarcImported (fun ex -> TaskFailed(ex, PsarcImport))

    | PsarcImported (project, importType) ->
        let cmd =
            Cmd.batch [
                Cmd.ofMsg (AddStatusMessage(translate "PsarcImportComplete"))
                Cmd.ofMsg (ProjectLoaded(project, FromPsarcImport importType))
            ]

        removeTask PsarcImport state, cmd

    | ImportToolkitTemplate fileName ->
        let cmd =
            try
                let project = ToolkitImporter.import fileName

                Cmd.ofMsg (ProjectLoaded(project, FromToolkitTemplateImport))
            with e ->
                Cmd.ofMsg (ErrorOccurred e)

        state, cmd

    | ImportTonesFromFile fileName ->
        let task () =
            match fileName with
            | EndsWith "psarc" ->
                Utils.importTonesFromPSARC fileName
            | EndsWith "xml" ->
                async { return [| Tone.fromXmlFile fileName |] }
            | EndsWith "json" ->
                Tone.fromJsonFile fileName
                |> Async.AwaitTask
                |> Async.map Array.singleton
            | _ ->
                failwith "Unknown tone file format."

        state, Cmd.OfAsync.either task () ShowImportToneSelector ErrorOccurred

    | ImportProfileTones ->
        if String.IsNullOrWhiteSpace(config.ProfilePath) then
            showOverlay state (ConfigEditor (Some FocusedSetting.ProfilePath)), Cmd.none
        else
            match Profile.importTones config.ProfilePath with
            | Ok toneArray ->
                state, Cmd.ofMsg (ShowImportToneSelector toneArray)
            | Error Profile.ToneImportError.NoTonesInProfile ->
                { state with Overlay = ErrorMessage(translate "NoTonesInProfile", None) }, Cmd.none
            | Error (Profile.ToneImportError.Exception ex) ->
                match ex with
                | :? ProfileMagicCheckFailure ->
                    { state with Overlay = ErrorMessage(translate "NotARocksmithProfileFile", None) }, Cmd.none
                | ex ->
                    { state with Overlay = ErrorMessage(ex.Message, Option.ofString ex.StackTrace) }, Cmd.none

    | ShowImportToneSelector tones ->
        match tones with
        | [||] ->
            { state with Overlay = ErrorMessage(translate "CouldNotFindTonesError", None) }, Cmd.none
        | [| one |] ->
            state, Cmd.ofMsg (ImportTones [ one ])
        | _ ->
            { state with SelectedImportTones = []; Overlay = ImportToneSelector tones }, Cmd.none

    | SetAudioFile audioPath ->
        let previewPath =
            if String.IsNullOrEmpty project.AudioPreviewFile.Path then
                Utils.determinePreviewPath audioPath
            else
                project.AudioPreviewFile.Path

        let cmds =
            [
                if config.AutoVolume && not <| String.endsWith ".wem" audioPath then
                    let previewChanged = project.AudioPreviewFile.Path <> previewPath
                    if previewChanged then
                        Cmd.ofMsg CalculateVolumes
                    else
                        Cmd.ofMsg (CalculateVolume MainAudio)
                Cmd.ofMsg ReadAudioLength
            ]

        let updatedProject =
            { project with
                AudioFile = { project.AudioFile with Path = audioPath }
                AudioPreviewFile = { project.AudioPreviewFile with Path = previewPath } }

        { state with Project = updatedProject }, Cmd.batch cmds

    | SetPreviewAudioFile previewPath ->
        let cmd =
            if config.AutoVolume && not <| String.endsWith ".wem" previewPath then
                Cmd.ofMsg (CalculateVolume PreviewAudio)
            else
                Cmd.none

        let updatedProject =
            { project with AudioPreviewFile = { project.AudioPreviewFile with Path = previewPath } }

        { state with Project = updatedProject }, cmd

    | ConvertToWem ->
        if DLCProject.audioFilesExist project then
            addTask WemConversion state,
            Cmd.OfAsync.either
                (Utils.convertAudio config.WwiseConsolePath) project
                WemConversionComplete
                (fun ex -> TaskFailed(ex, WemConversion))
        else
            state, Cmd.none

    | ConvertToWemCustom ->
        match getSelectedArrangement state with
        | Some (Instrumental { CustomAudio = Some audio }) ->
            addTask WemConversion state,
            Cmd.OfAsync.either
                (Wwise.convertToWem config.WwiseConsolePath) audio.Path
                WemConversionComplete
                (fun ex -> TaskFailed(ex, WemConversion))
        | _ ->
            state, Cmd.none

    | CalculateVolumes ->
        let doPreview = File.Exists(project.AudioPreviewFile.Path)

        let cmds =
            Cmd.batch [
                Cmd.ofMsg (CalculateVolume MainAudio)
                if doPreview then Cmd.ofMsg (CalculateVolume PreviewAudio)
            ]

        state, cmds

    | CalculateVolume target ->
        let task () =
            async {
                let path =
                    match target with
                    | MainAudio -> project.AudioFile.Path
                    | PreviewAudio -> project.AudioPreviewFile.Path
                    | CustomAudio (path, _) -> path

                return
                    match path with
                    | HasExtension ".wem" ->
                        match tryGetNonWemAudioFile path with
                        | Some altPath ->
                            Volume.calculate altPath
                        | None ->
                            Conversion.withTempOggFile Volume.calculate path
                    | _ ->
                        Volume.calculate path
            }

        addTask (VolumeCalculation target) state,
        Cmd.OfAsync.either task () (fun v -> VolumeCalculated(v, target))
                                   (fun ex -> TaskFailed(ex, (VolumeCalculation target)))

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
                            let audio =
                                inst.CustomAudio
                                |> Option.map (fun a -> { a with Volume = volume })

                            Instrumental { inst with CustomAudio = audio }
                        | other ->
                            other)

                { project with Arrangements = arrangements }

        removeTask (VolumeCalculation target) { state with Project = project }, Cmd.none

    | AddArrangements files ->
        let newState = addArrangements files state

        // Prompt to save a project file if auto-save is enabled
        // Only when first adding arrangements into a new project
        let cmd =
            if state.Config.AutoSave &&
               state.OpenProjectFile.IsNone &&
               state.Project.Arrangements.IsEmpty &&
               not newState.Project.Arrangements.IsEmpty
            then
                Cmd.ofMsg SaveProjectAs
            else
                Cmd.none

        newState, cmd

    | SetSelectedArrangementIndex index ->
        if index < project.Arrangements.Length then
            { state with SelectedArrangementIndex = index }, Cmd.none
        else
            state, Cmd.none

    | SetSelectedToneIndex index ->
        if index < project.Tones.Length then
            // Change the selected gear slot if it is not available in the newly selected tone
            // Prevents creating gaps in the tone gear slots
            let selectedGearSlot =
                let tone = project.Tones[index]
                match state.SelectedGearSlot with
                | ToneGear.PrePedal i when tone.GearList.PrePedals[i].IsNone ->
                    ToneGear.PrePedal 0
                | ToneGear.PostPedal i when tone.GearList.PostPedals[i].IsNone ->
                    ToneGear.PostPedal 0
                | ToneGear.Rack i when tone.GearList.Racks[i].IsNone ->
                    ToneGear.Rack 0
                | _ ->
                    state.SelectedGearSlot

            { state with SelectedToneIndex = index; SelectedGearSlot = selectedGearSlot }, Cmd.none
        else
            state, Cmd.none

    | DeleteSelectedArrangement ->
        let arrangements, index = Utils.removeSelected project.Arrangements state.SelectedArrangementIndex

        { state with Project = { project with Arrangements = arrangements }
                     SelectedArrangementIndex = index }, Cmd.none

    | DeleteSelectedTone ->
        let tones, index = Utils.removeSelected project.Tones state.SelectedToneIndex

        { state with Project = { project with Tones = tones }
                     SelectedToneIndex = index }, Cmd.none

    | AddNewTone ->
        match state.ToneGearRepository with
        | Some repository ->
            let newTone = ToneGear.emptyTone repository

            { state with Project = { project with Tones = newTone :: project.Tones } }, Cmd.none
        | None ->
            state, Cmd.none

    | AddToneToCollection ->
        getSelectedTone state
        |> Option.iter (ToneCollection.Database.addToneToUserCollection state.DatabaseConnector project)

        state, Cmd.ofMsg (AddStatusMessage(translate "ToneAddedToCollection"))

    | DuplicateTone ->
        let duplicate =
            getSelectedTone state
            |> Option.map (fun tone ->
                { tone with Name = tone.Name + "2"; Key = String.Empty })
            |> Option.toList

        { state with Project = { project with Tones = duplicate @ project.Tones } }, Cmd.none

    | MoveTone dir ->
        let tones, index = Utils.moveSelected dir state.SelectedToneIndex project.Tones

        { state with
            Project = { project with Tones = tones }
            SelectedToneIndex = index }, Cmd.none

    | ShowToneCollection ->
        let overlay =
            ToneCollection.CollectionState.init state.DatabaseConnector ToneCollection.ActiveTab.Official
            |> ToneCollection

        showOverlay state overlay, Cmd.none

    | MoveArrangement dir ->
        let arrangements, index = Utils.moveSelected dir state.SelectedArrangementIndex project.Arrangements

        { state with
            Project = { project with Arrangements = arrangements }
            SelectedArrangementIndex = index }, Cmd.none

    | CreatePreviewAudio InitialSetup ->
        let task () =
            async {
                let sourceFile = PreviewUtils.getOggOrWavAudio project
                let audioLength = Utils.getLength sourceFile

                return
                    { SourceFile = sourceFile
                      MaxPreviewStart = audioLength - Preview.DefaultPreviewLength }
            }

        state, Cmd.OfAsync.either task () (SetupStartTime >> CreatePreviewAudio) ErrorOccurred

    | CreatePreviewAudio (SetupStartTime data) ->
        showOverlay state (SelectPreviewStart data), Cmd.none

    | CreatePreviewAudio (CreateFile data) ->
        let task () =
            async {
                let startTime =
                    project.AudioPreviewStartTime
                    |> Option.defaultValue (TimeSpan())
                let targetPath = Utils.createPreviewAudioPath data.SourceFile
                Preview.create data.SourceFile targetPath startTime
                return targetPath
            }

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
                let msg = translatef "PreviewDeleteError" [| Path.GetFileName(wemPreview); ex.Message |]
                ErrorMessage(msg, ex.StackTrace |> Option.ofString)

        { state with Project = { project with AudioPreviewFile = previewFile }
                     Overlay = overlay }, cmd

    | ShowSortFields shown ->
        { state with ShowSortFields = shown }, Cmd.none

    | ShowJapaneseFields shown ->
        { state with ShowJapaneseFields = shown }, Cmd.none

    | ShowOverlay overlay ->
        showOverlay state overlay, Cmd.none

    | ShowLyricsViewer ->
        match getSelectedArrangement state with
        | Some (Vocals v) ->
            let lyrics =
                XML.Vocals.Load(v.XML)
                |> Utils.createLyricsString
            let overlay = LyricsViewer(lyrics, v.Japanese)
            showOverlay state overlay, Cmd.none
        | _ ->
            state, Cmd.none

    | ShowInstrumentalXmlDetailsViewer ->
        match getSelectedArrangement state with
        | Some (Instrumental inst) ->
            let xml = XML.InstrumentalArrangement.Load(inst.XML)
            let overlay = InstrumentalXmlDetailsViewer(xml, Path.GetFileName(inst.XML))
            showOverlay state overlay, Cmd.none
        | _ ->
            state, Cmd.none

    | SetConfiguration (newConfig, enableLoad, wasAbnormalExit) ->
        if config.Locale <> newConfig.Locale then
            state.Localizer.ChangeLocale(newConfig.Locale)
        let cmd =
            if enableLoad && File.Exists(newConfig.PreviousOpenedProject) then
                if newConfig.LoadPreviousOpenedProject then
                    Cmd.ofMsg (OpenProject newConfig.PreviousOpenedProject)
                elif wasAbnormalExit then
                    Cmd.ofMsg (ShowOverlay AbnormalExitMessage)
                else
                    Cmd.none
            else
                Cmd.none

        { state with Config = newConfig }, cmd

    | SetRecentFiles recent ->
        { state with RecentFiles = recent }, Cmd.none

    | ProgramClosing ->
        let task =
            backgroundTask {
                if config.AutoSave && project <> state.SavedProject then
                    match state.OpenProjectFile with
                    | Some path ->
                        do! DLCProject.save path project
                    | None ->
                        ()

                deleteTemporaryFilesForQuickEdit state

                do! RecentFilesList.save state.RecentFiles
            }

        task.Wait()
        state, Cmd.none

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

                UpdateMessage(update) :: statusMessages
            | _ ->
                state.StatusMessages

        { state with StatusMessages = messages; AvailableUpdate = update }, Cmd.none

    | SetToneRepository repository ->
        { state with ToneGearRepository = Some repository }, Cmd.none

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
        state, Cmd.OfTask.either OnlineUpdate.checkForUpdates () UpdateCheckCompleted ErrorOccurred

    | UpdateCheckCompleted (Error msg) ->
        let cmd = Cmd.ofMsg (ShowOverlay(ErrorMessage(msg, None)))
        { state with AvailableUpdate = None }, cmd

    | UpdateCheckCompleted (Ok update) ->
        let msg =
            match update with
            | Some _ -> ShowUpdateInformation
            | None -> AddStatusMessage(translate "NoUpdateAvailable")

        { state with AvailableUpdate = update }, Cmd.ofMsg msg

    | DownloadUpdate ->
        match state.AvailableUpdate with
        | Some update ->
            let id, download = createDownloadTask "DownloadingUpdate"
            let targetPath = Path.Combine(Configuration.appDataFolder, "update.exe")

            let cmd =
                Cmd.OfTask.either
                    (Downloader.downloadFile update.AssetUrl targetPath) id
                    (fun () -> UpdateDownloaded targetPath)
                    (fun ex -> TaskFailed(ex, download))

            addTask download { state with Overlay = NoOverlay }, cmd
        | None ->
            state, Cmd.none

    | UpdateDownloaded installerPath ->
        // Exits the program
        OnlineUpdate.applyUpdate installerPath
        state, Cmd.none

    | SaveProjectAs ->
        state, Cmd.ofMsg (Dialog.SaveProjectAs |> ShowDialog)

    | SaveProject targetPath ->
        let task () =
            task {
                do! DLCProject.save targetPath project
                return targetPath
            }

        state, Cmd.OfTask.either task () ProjectSaved ErrorOccurred

    | ProjectSaved target ->
        let recent, newConfig, cmd = updateRecentFilesAndConfig target state

        { state with
            OpenProjectFile = Some target
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
        state, Cmd.OfTask.either DLCProject.load fileName (fun p -> ProjectLoaded(p, FromFile fileName)) ErrorOccurred

    | ProjectLoaded (project, loadOrigin) ->
        let project =
            if loadOrigin.ShouldReloadTonesFromArrangementFiles then
                DLCProject.updateToneInfo project
            else
                project

        let projectPath = loadOrigin.ProjectPath
        let recent, newConfig, cmd =
            // Quick edit PSARC projects are not added to recent files
            match projectPath with
            | Some projectPath ->
                updateRecentFilesAndConfig projectPath state
            | None ->
                state.RecentFiles, state.Config, Cmd.none

        let albumArtLoadTime =
            if state.AlbumArtLoader.TryLoad(project.AlbumArtFile) then
                Some DateTime.Now
            else
                None

        deleteTemporaryFilesForQuickEdit state

        let cmds = Cmd.batch [ Cmd.ofMsg ReadAudioLength; cmd ]

        { state with
            Project = project
            SavedProject = project
            OpenProjectFile = projectPath
            RecentFiles = recent
            Config = newConfig
            ArrangementIssues = Map.empty
            AlbumArtLoadTime = albumArtLoadTime
            QuickEditData = loadOrigin.QuickEditData
            ImportedBuildToolVersion = loadOrigin.BuildToolVersion
            AudioLength = None
            SelectedArrangementIndex = -1
            SelectedToneIndex = -1 }, cmds

    | LoadMultipleFiles paths ->
        state, Cmd.batch (handleFilesDrop paths)

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

    | EditProject edit ->
        let newState =
            match edit with
            | SetAlbumArt path when state.AlbumArtLoader.TryLoad path ->
                { state with AlbumArtLoadTime = Some DateTime.Now }
            | _ ->
                state

        { newState with Project = editProject edit project }, Cmd.none

    | EditConfig edit ->
        { state with Config = editConfig edit config }, Cmd.none

    | DeleteTestBuilds ->
        match TestPackageBuilder.getTestBuildFiles config project with
        | [] ->
            state, Cmd.ofMsg <| AddStatusMessage(translate "NoTestBuildsFound")
        | [ _ ] as one ->
            state, Cmd.ofMsg <| DeleteConfirmed one
        | many ->
            { state with Overlay = DeleteConfirmation many }, Cmd.none

    | DeleteConfirmed files ->
        let cmd =
            try
                List.iter File.Delete files
                let f = translate (if files.Length = 1 then "file" else "files")
                let message = translatef "FilesDeleted" [| files.Length; f |]
                Cmd.ofMsg <| AddStatusMessage message
            with e ->
                Cmd.ofMsg <| ErrorOccurred e

        { state with Overlay = NoOverlay }, cmd

    | GenerateNewIds ->
        let arrangements =
            project.Arrangements
            |> List.mapi (fun i arr ->
                if i = state.SelectedArrangementIndex then
                    Arrangement.generateIds arr
                else
                    arr)

        let msg = Cmd.ofMsg (AddStatusMessage (translate "NewIdsGenerated"))
        { state with Project = { project with Arrangements = arrangements } }, msg

    | GenerateAllIds ->
        let arrangements = TestPackageBuilder.generateAllIds project.Arrangements
        let msg = Cmd.ofMsg (AddStatusMessage (translate "NewIdsGeneratedAll"))
        { state with Project = { project with Arrangements = arrangements } }, msg

    | ApplyLowTuningFix ->
        applyLowTuningFix state, Cmd.none

    | Build _ when not <| canBuild state ->
        state, Cmd.none

    | Build _ when not <| File.Exists(project.AudioPreviewFile.Path) ->
        addTask AutomaticPreviewCreation state,
        Cmd.OfAsync.either
            PreviewUtils.createAutoPreviewFile
            project
            (fun path -> AutoPreviewCreated(path, msg))
            ErrorOccurred

    | AutoPreviewCreated (previewPath, continuation) ->
        let cmds =
            Cmd.batch [
                if config.AutoVolume then Cmd.ofMsg (CalculateVolume PreviewAudio)
                Cmd.ofMsg continuation
            ]

        let preview = { project.AudioPreviewFile with Path = previewPath }
        let newState = { state with Project = { project with AudioPreviewFile = preview } }

        removeTask AutomaticPreviewCreation newState, cmds

    | Build PitchShifted ->
        buildPackage (ReleasePackageBuilder.buildPitchShifted state.OpenProjectFile) state

    | Build Test ->
        if String.notEmpty config.TestFolderPath then
            buildPackage (TestPackageBuilder.build state.CurrentPlatform) state
        else
            showOverlay state (ConfigEditor (Some FocusedSetting.TestFolder)), Cmd.none

    | Build Release ->
        buildPackage (ReleasePackageBuilder.build state.OpenProjectFile) state

    | Build (ReplacePsarc info) ->
        buildPackage (ReleasePackageBuilder.buildReplacePsarc info) state

    | BuildComplete completed ->
        let message =
            match completed with
            | BuildCompleteType.TestNewVersion version ->
                translatef "BuildNewTestVersionComplete" [| version |]
            | _ ->
                translate "BuildPackageComplete"

        let cmd =
            Cmd.batch [
                match completed with
                | BuildCompleteType.Release targetDirectory when config.OpenFolderAfterReleaseBuild ->
                    Cmd.ofMsg (OpenWithShell targetDirectory)
                | BuildCompleteType.ReplacePsarc ->
                    Cmd.ofMsg NewProject
                | _ ->
                    ()
                Cmd.ofMsg (AddStatusMessage message)
            ]

        removeTask BuildPackage state, cmd

    | WemConversionComplete _ ->
        removeTask WemConversion state,
        Cmd.ofMsg (AddStatusMessage(translate "WemConversionComplete"))

    | CheckArrangement arrangement ->
        let task () =
            async {
                let path = Arrangement.getFile arrangement
                return path, Utils.checkArrangement arrangement
            }

        addTask ArrangementCheckOne state,
        Cmd.OfAsync.either task () CheckOneCompleted (fun ex -> TaskFailed(ex, ArrangementCheckOne))

    | CheckOneCompleted (xmlFile, issues) ->
        let issueMap =
            state.ArrangementIssues
            |> Map.add xmlFile issues

        { removeTask ArrangementCheckOne state with ArrangementIssues = issueMap },
        Cmd.ofMsg (AddStatusMessage(translate "ValidationComplete"))

    | CheckArrangements ->
        if canRunValidation state then
            let task () =
                async {
                    return Utils.checkArrangements project ProgressReporters.ArrangementCheck
                }

            addTask ArrangementCheckAll state,
            Cmd.OfAsync.either task () CheckAllCompleted (fun ex -> TaskFailed(ex, ArrangementCheckAll))
        else
            state, Cmd.none

    | CheckAllCompleted issues ->
        { removeTask ArrangementCheckAll state with ArrangementIssues = issues },
        Cmd.ofMsg (AddStatusMessage(translate "ValidationComplete"))

    | PsarcUnpacked ->
        removeTask PsarcUnpack state,
        Cmd.ofMsg (AddStatusMessage (translate "PsarcUnpackComplete"))

    | WemToOggConversionCompleted ->
        removeTask WemToOggConversion state, Cmd.none

    | AddStatusMessage message ->
        let id = Guid.NewGuid()
        let messages = MessageString(id,  message) :: state.StatusMessages

        { state with StatusMessages = messages }, Cmd.OfAsync.result (removeStatusMessage id)

    | RemoveStatusMessage removeId ->
        let messages =
            state.StatusMessages
            |> List.filter (function
                | MessageString (id, _) when id = removeId -> false
                | TaskWithProgress(FileDownload({ Id = id }), _) when id = removeId -> false
                | _ -> true)

        { state with StatusMessages = messages }, Cmd.none

    | ShowIssueViewer ->
        match getSelectedArrangement state with
        | Some arr ->
            match state.ArrangementIssues.TryGetValue(Arrangement.getFile arr) with
            | true, issues when not issues.IsEmpty ->
                { state with Overlay = IssueViewer arr }, Cmd.none
            | _ ->
                state, Cmd.none
        | None ->
            state, Cmd.none

    | OpenProjectFolder ->
        let cmd =
            state.OpenProjectFile
            |> Option.map (Path.GetDirectoryName >> OpenWithShell >> Cmd.ofMsg)
            |> Option.defaultValue Cmd.none

        state, cmd

    | ErrorOccurred e ->
        showOverlay state (exceptionToErrorMessage e), Cmd.none

    | TaskProgressChanged (progressedTask, progress) ->
        let messages =
            state.StatusMessages
            |> List.map (function
                | TaskWithProgress (task, _) when task = progressedTask ->
                    TaskWithProgress(task, progress)
                | other ->
                    other)

        { state with StatusMessages = messages }, Cmd.none

    | TaskFailed (e, failedTask) ->
        { removeTask failedTask state with Overlay = exceptionToErrorMessage e }, Cmd.none

    | ChangeLocale newLocale ->
        if config.Locale <> newLocale then
            state.Localizer.ChangeLocale(newLocale)

        { state with Config = { config with Locale = newLocale } }, Cmd.none

    | ToolsMsg msg ->
        Tools.update msg state

    | ShowDialog _ ->
        // Handled elsewhere as a side effect
        state, Cmd.none

    | OfficialTonesDatabaseDownloaded downloadTask ->
        let newState =
            match state with
            | { Overlay = ToneCollection ({ ActiveCollection = ToneCollection.ActiveCollection.Official(None) } as s) } ->
                let newCollectionState =
                    ToneCollection.CollectionState.init s.Connector ToneCollection.ActiveTab.Official

                { state with Overlay = ToneCollection newCollectionState }
            | _ ->
                state

        removeTask downloadTask newState, Cmd.none

    | ToneCollectionMsg msg ->
        match state.Overlay with
        | ToneCollection collectionState ->
            let newCollectionState, effect = ToneCollection.MessageHandler.update collectionState msg
            let newState = { state with Overlay = ToneCollection newCollectionState }

            match effect with
            | ToneCollection.Nothing ->
                newState, Cmd.none
            | ToneCollection.AddToneToProject tone ->
                { newState with Project = { project with Tones = tone :: project.Tones } },
                Cmd.ofMsg (AddStatusMessage(translate "ToneAddedToProject"))
            | ToneCollection.ShowToneAddedToCollectionMessage ->
                newState, Cmd.ofMsg (AddStatusMessage(translate "ToneAddedToCollection"))
            | ToneCollection.BeginDownloadingTonesDatabase ->
                let id, download = createDownloadTask "DownloadingToneDatabase"
                let targetPath = Path.Combine(Configuration.appDataFolder, "tones", "official.db")

                let cmd =
                    Cmd.OfTask.either
                        (Downloader.downloadFile Downloader.ToneDBUrl targetPath) id
                        (fun () -> OfficialTonesDatabaseDownloaded download)
                        (fun ex -> TaskFailed(ex, download))

                addTask download newState, cmd
        | _ ->
            state, Cmd.none

    | LyricsCreatorMsg msg ->
        match state.Overlay with
        | JapaneseLyricsCreator lyricsState ->
            let newEditorState, effect =
                JapaneseLyricsCreator.MessageHandler.update lyricsState msg

            let newState = { state with Overlay = JapaneseLyricsCreator newEditorState }

            match effect with
            | JapaneseLyricsCreator.Nothing ->
                newState, Cmd.none
            | JapaneseLyricsCreator.AddVocalsToProject xmlPath ->
                addJapaneseVocals xmlPath newState
        | _ ->
            state, Cmd.none

    | HotKeyMsg msg ->
        match state.Overlay, msg with
        | NoOverlay, _ | _, CloseOverlay _ ->
            state, Cmd.ofMsg msg
        | _ ->
            // Ignore the message when an overlay is open
            state, Cmd.none

    | StartFontGenerator ->
        match getSelectedArrangement state, state.Config.FontGeneratorPath with
        | Some (Vocals { XML = xml }), Some generatorPath ->
            Utils.startProcess generatorPath $"\"{xml}\""
        | _ ->
            ()

        state, Cmd.none
