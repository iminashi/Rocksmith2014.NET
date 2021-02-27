module DLCBuilder.Main

open Rocksmith2014
open Rocksmith2014.Audio
open Rocksmith2014.Common
open Rocksmith2014.Common.Manifest
open Rocksmith2014.DD
open Rocksmith2014.DLCProject
open Rocksmith2014.DLCProject.PackageBuilder
open Rocksmith2014.XML.Processing
open Elmish
open System
open System.IO
open EditFunctions

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
            Cmd.OfAsync.perform Configuration.load () SetConfiguration
            Cmd.OfAsync.perform RecentFilesList.load () SetRecentFiles
            yield! loadProject |> Option.toList
        ]

    { Project = DLCProject.Empty
      SavedProject = DLCProject.Empty
      RecentFiles = []
      Config = Configuration.Default
      CoverArt = None
      SelectedArrangement = None
      SelectedTone = None
      SelectedGear = None
      SelectedGearType = ToneGear.Amp
      ShowSortFields = false
      ShowJapaneseFields = false
      Overlay = NoOverlay
      ImportTones = []
      RunningTasks = Set.empty
      CurrentPlatform = if OperatingSystem.IsMacOS() then Mac else PC
      OpenProjectFile = None
      ArrangementIssues = Map.empty }, commands

let private addTask newTask state =
    { state with RunningTasks = state.RunningTasks |> Set.add newTask }

let private removeTask completedTask state =
    { state with RunningTasks = state.RunningTasks |> Set.remove completedTask }

let update (msg: Msg) (state: State) =
    let { Project=project; Config=config } = state

    match msg with
    | SetSelectedGear gear ->
        let cmd =
            match gear with
            | Some gear ->
                let currentGear =
                    state.SelectedTone
                    |> Option.bind (fun tone -> ToneGear.getGearDataForCurrentPedal tone.GearList state.SelectedGearType)
                match currentGear with
                // Don't change the cabinet if its name is the same as the current one
                | Some data when gear.Type = "Cabinets" && data.Name = gear.Name ->
                    Cmd.none
                | _ ->
                    Cmd.ofMsg (gear |> SetPedal |> EditTone)
            | _ -> Cmd.none

        { state with SelectedGear = gear }, cmd

    | SetSelectedGearType gearType -> { state with SelectedGearType = gearType }, Cmd.none

    | ShowToneEditor -> { state with Overlay = ToneEditor }, Cmd.none

    | NewProject ->
        state.CoverArt |> Option.iter(fun x -> x.Dispose())
        { state with Project = DLCProject.Empty
                     SavedProject = DLCProject.Empty
                     OpenProjectFile = None
                     SelectedArrangement = None
                     SelectedTone = None
                     CoverArt = None }, Cmd.none

    | ImportTonesChanged item ->
        if isNull item then
            state, Cmd.none
        else
            let tones = [ item :?> Tone ]
                //items
                //|> Seq.cast<Tone>
                //|> Seq.toList
            { state with ImportTones = tones }, Cmd.none

    | ImportSelectedTones -> Utils.addTones state state.ImportTones, Cmd.none

    | ImportTones tones -> Utils.addTones state tones, Cmd.none

    | ExportSelectedTone ->
        match state.SelectedTone with
        | Some tone ->
            let initialFileName = Some $"{tone.Name}.tone2014.xml"
            let dialog = Dialogs.saveFileDialog (translate "exportToneAs") (Dialogs.toneExportFilter()) initialFileName
            state, Cmd.OfAsync.perform dialog None (fun path -> ExportTone(tone, path))
        | None ->
            state, Cmd.none

    | ExportTone (tone, Some path) ->
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

    | ConditionalCmdDispatch (Some str, msg) -> state, Cmd.ofMsg (msg str)
    | ConditionalCmdDispatch (None, _) -> state, Cmd.none

    | OpenFileDialog (locString, filter, msg) ->
        let dialog = Dialogs.openFileDialog (translate locString) (filter())
        state, Cmd.OfAsync.perform dialog None (fun file -> ConditionalCmdDispatch(file, msg))

    | OpenFolderDialog (locString, msg) ->
        let dialog = Dialogs.openFolderDialog (translate locString)
        state, Cmd.OfAsync.perform dialog None (fun folder -> ConditionalCmdDispatch(folder, msg))

    | SelectOpenArrangement ->
        let dialog = Dialogs.openMultiFileDialog (translate "selectArrangement") (Dialogs.xmlFileFilter())
        state, Cmd.OfAsync.perform dialog None AddArrangements

    | SelectImportPsarcFolder psarcFile ->
        let dialog = Dialogs.openFolderDialog (translate "selectPsarcExtractFolder")
        state, Cmd.OfAsync.perform dialog None (fun folder -> ImportPsarc(psarcFile, folder))

    | ImportPsarc (psarcFile, Some targetFolder) ->
        let task() = async {
            let! project, fileName = PsarcImporter.import psarcFile targetFolder

            match config.ConvertAudio with
            | ToOgg | ToWav as conv ->
                [ yield project.AudioFile
                  yield project.AudioPreviewFile
                  yield! project.Arrangements
                         |> List.choose (function Instrumental i -> i.CustomAudio | _ -> None) ]
                |> List.iter (fun { Path=path } ->
                    if conv = ToOgg
                    then Conversion.wemToOgg path
                    else Conversion.wemToWav path)
            | NoConversion ->
                ()

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
                addTask PsarcImport state, fun ex -> TaskFailed(ex, PsarcImport)
            | NoConversion ->
                state, ErrorOccurred

        newState, Cmd.OfAsync.either task () ProjectLoaded onError

    | ImportToolkitTemplate fileName ->
        try
            let project = ToolkitImporter.import fileName
            let coverArt = Utils.changeCoverArt state.CoverArt project.AlbumArtFile

            { state with Project = project; OpenProjectFile = None; CoverArt = coverArt
                         SelectedArrangement = None; SelectedTone = None }, Cmd.none
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
        let newState =
            match tones with
            | [||] -> { state with Overlay = ErrorMessage(translate "couldNotFindTonesError", None) }
            | [| tone |] -> { state with Project = { project with Tones = (Utils.addDescriptors tone)::project.Tones } }
            | _ -> { state with Overlay = ImportToneSelector tones; ImportTones = [] }
        newState, Cmd.none

    | ProjectSaveAs ->
        let intialFileName =
            state.OpenProjectFile
            |> Option.map Path.GetFileName
            |> Option.orElseWith (fun () ->
                sprintf "%s_%s" project.ArtistName.SortValue project.Title.SortValue
                |> StringValidator.fileName
                |> sprintf "%s.rs2dlc"
                |> Some)

        let initialDir =
            state.OpenProjectFile
            |> Option.map Path.GetDirectoryName
            |> Option.orElse (Option.ofString config.ProjectsFolderPath)

        let dialog = Dialogs.saveFileDialog (translate "saveProjectAs") (Dialogs.projectFilter()) intialFileName
        state, Cmd.OfAsync.perform dialog initialDir SaveProject

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
            let task() = Utils.convertAudio config.WwiseConsolePath project

            addTask WemConversion state,
            Cmd.OfAsync.either task () BuildComplete (fun ex -> TaskFailed(ex, WemConversion))
        else
            state, Cmd.none

    | ConvertToWemCustom ->
        match state.SelectedArrangement with
        | Some (Instrumental { CustomAudio = Some audio }) ->
            addTask WemConversion state,
            Cmd.OfAsync.either (Wwise.convertToWem config.WwiseConsolePath) audio.Path BuildComplete (fun ex -> TaskFailed(ex, WemConversion))
        | _ ->
            state, Cmd.none

    | CalculateVolumes ->
        let previewPath = state.Project.AudioPreviewFile.Path
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
            | CustomAudio path -> path
        let task () = async { return Volume.calculate path }
        addTask (VolumeCalculation target) state,
        Cmd.OfAsync.either task () (fun v -> VolumeCalculated(v, target)) (fun ex -> TaskFailed(ex, (VolumeCalculation target)))

    | VolumeCalculated (volume, target) ->
        let state =
            match target with
            | MainAudio ->
                { state with Project = { project with AudioFile = { project.AudioFile with Volume = volume } } }
            | PreviewAudio ->
                { state with Project = { project with AudioPreviewFile = { project.AudioPreviewFile with Volume = volume } } }
            | CustomAudio audioPath ->
                project.Arrangements
                |> List.tryPick (fun arr ->
                    // TODO: This won't work correctly if there are multiple arrangements with the same custom audio file
                    match arr with
                    | Instrumental { CustomAudio = Some audio } when audio.Path = audioPath ->
                        Some arr
                    | _ ->
                        None)
                |> function
                | Some (Instrumental inst as old) ->
                    let updated =
                        Instrumental { inst with CustomAudio = inst.CustomAudio |> Option.map (fun a -> { a with Volume = volume }) }
                        
                    let arrangements =
                        state.Project.Arrangements
                        |> List.update old updated

                    let selected = 
                        // Update the selected arrangement unless it was changed
                        match state.SelectedArrangement with
                        | Some arr when arr = old ->
                            Some updated
                        | _ ->
                            state.SelectedArrangement
                    { state with Project = { state.Project with Arrangements = arrangements }
                                 SelectedArrangement = selected }
                | _ ->
                    state
                    
        removeTask (VolumeCalculation target) state, Cmd.none

    | SetCoverArt fileName ->
        { state with CoverArt = Utils.changeCoverArt state.CoverArt fileName
                     Project = { project with AlbumArtFile = fileName } }, Cmd.none

    | AddArrangements (Some files) ->
        let results = Array.map (Arrangement.fromFile translate) files

        let shouldInclude arrangements arr =
            match arr with
            // Allow only one show lights arrangement
            | Showlights _ when arrangements |> List.exists (function Showlights _ -> true | _ -> false) -> false

            // Allow max five instrumental arrangements
            | Instrumental _ when (arrangements |> List.choose Arrangement.pickInstrumental).Length = 5 -> false

            // Allow max two vocals arrangements
            | Vocals _ when (arrangements |> List.choose Arrangement.pickVocals).Length = 2 -> false
            | _ -> true

        let errors =
            (files, results)
            ||> Array.map2 (fun file result ->
                match result with
                | Ok _ -> None
                | Error msg -> Some $"{file}:\n{msg}\n")
            |> Array.choose id

        let arrangements =
            (project.Arrangements, results)
            ||> Array.fold (fun state elem ->
                match elem with
                | Ok (arr, _) when shouldInclude state arr ->
                    let arr =
                        // Prevent multiple main arrangements of the same type
                        match arr with
                        | Instrumental inst when state |> List.exists (function
                                | Instrumental x ->
                                    inst.RouteMask = x.RouteMask
                                    && x.Priority = ArrangementPriority.Main
                                    && inst.Priority = ArrangementPriority.Main
                                | _ -> false) ->
                            Instrumental { inst with Priority = ArrangementPriority.Alternative }
                        | _ -> arr
                    arr::state
                | _ -> state)
            |> List.sortBy Arrangement.sorter

        let metadata = 
            if project.ArtistName = SortableString.Empty then
                results
                |> Array.tryPick (function Ok (_, md) -> md | Error _ -> None)
            else
                None

        let newState = 
            match metadata with
            | Some md ->
                { state with
                    Project = { project with
                                    DLCKey = DLCKey.create config.CharterName md.ArtistName md.Title
                                    ArtistName = SortableString.Create md.ArtistName // Ignore the sort value from the XML
                                    Title = SortableString.Create(md.Title, md.TitleSort)
                                    AlbumName = SortableString.Create(md.AlbumName, md.AlbumNameSort)
                                    Year = md.AlbumYear
                                    Arrangements = arrangements } }
            | None ->
                { state with Project = { project with Arrangements = arrangements } }

        match errors with
        | [||] -> newState, Cmd.none
        | _ -> { newState with Overlay = ErrorMessage(String.Join('\n', errors), None) }, Cmd.none

    | SetSelectedArrangement selected -> { state with SelectedArrangement = selected }, Cmd.none

    | SetSelectedTone selected ->
        // Ignore the message if the tone editor is open
        // Fix for weird infinite update loop
        if state.Overlay <> ToneEditor then
            { state with SelectedTone = selected }, Cmd.none
        else
            state, Cmd.none

    | DeleteArrangement ->
        let arrangements =
            Utils.removeSelected project.Arrangements state.SelectedArrangement

        { state with Project = { project with Arrangements = arrangements }
                     SelectedArrangement = None }, Cmd.none

    | DeleteTone ->
        let tones =
             Utils.removeSelected project.Tones state.SelectedTone

        { state with Project = { project with Tones = tones }
                     SelectedTone = None }, Cmd.none

    | DuplicateTone ->
        match state.SelectedTone with
        | None ->
            state, Cmd.none
        | Some tone ->
            let duplicate = { tone with Name = tone.Name + "2"; Key = String.Empty }
            { state with Project = { project with Tones = duplicate::state.Project.Tones } }, Cmd.none

    | MoveTone dir ->
        let tones = 
            match state.SelectedTone with
            | None -> project.Tones
            | Some selected ->
                let change = match dir with Up -> -1 | Down -> 1 
                let insertPos = (List.findIndex ((=) selected) project.Tones) + change
                if insertPos >= 0 && insertPos < project.Tones.Length then
                    List.remove selected project.Tones
                    |> List.insertAt insertPos selected
                else
                    project.Tones
        { state with Project = { project with Tones = tones } }, Cmd.none

    | CreatePreviewAudio (SetupStartTime) ->
        let totalLength = Utils.getLength project.AudioFile.Path
        // Remove the length of the preview from the total length
        let length = totalLength - TimeSpan.FromSeconds 28.
        { state with Overlay = SelectPreviewStart length }, Cmd.none

    | CreatePreviewAudio (CreateFile) ->
        match state.Project.AudioPreviewStartTime with
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
        { state with Project = { project with AudioPreviewFile = previewFile } }, cmd

    | ShowSortFields shown -> { state with ShowSortFields = shown }, Cmd.none
    
    | ShowJapaneseFields shown -> { state with ShowJapaneseFields = shown }, Cmd.none

    | ShowConfigEditor -> { state with Overlay = ConfigEditor }, Cmd.none
    
    | SetConfiguration config ->
        if state.Config.Locale <> config.Locale then changeLocale config.Locale
        { state with Config = config }, Cmd.none

    | SetRecentFiles recent -> { state with RecentFiles = recent }, Cmd.none

    | SaveProject (Some target) ->
        let task() = async {
            do! DLCProject.save target project
            return target }
        state, Cmd.OfAsync.either task () ProjectSaved ErrorOccurred

    | ProjectSaved target ->
        let recent = RecentFilesList.update target state.RecentFiles

        { state with OpenProjectFile = Some target
                     SavedProject = project
                     RecentFiles = recent }, Cmd.none

    | ProjectSaveOrSaveAs ->
        let msg =
            match state.OpenProjectFile with
            | Some _ as fn -> SaveProject fn
            | None -> ProjectSaveAs
        state, Cmd.ofMsg msg

    | OpenProject fileName ->
        state, Cmd.OfAsync.either DLCProject.load fileName (fun p -> ProjectLoaded(p, fileName)) ErrorOccurred

    | ProjectLoaded (project, projectFile) ->
        let coverArt = Utils.changeCoverArt state.CoverArt project.AlbumArtFile
        let project = DLCProject.updateToneInfo project
        let recent = RecentFilesList.update projectFile state.RecentFiles

        { state with CoverArt = coverArt
                     Project = project
                     SavedProject = project
                     OpenProjectFile = Some projectFile
                     RecentFiles = recent
                     SelectedArrangement = None
                     RunningTasks = state.RunningTasks |> Set.remove PsarcImport
                     SelectedTone = None }, Cmd.none

    | EditInstrumental edit ->
        match state.SelectedArrangement with
        | Some (Instrumental inst as old) -> editInstrumental state edit old inst
        | _ -> state, Cmd.none

    | EditVocals edit ->
        match state.SelectedArrangement with
        | Some (Vocals vocals as old) -> editVocals state edit old vocals
        | _ -> state, Cmd.none

    | EditTone edit ->
         match state.SelectedTone with
         | Some old -> editTone state edit old
         | None -> state, Cmd.none

    | EditProject edit -> { state with Project = editProject edit project }, Cmd.none

    | EditConfig edit -> { state with Config = editConfig edit config }, Cmd.none

    | Build Test ->
        match BuildValidator.validate project with
        | Error error ->
            { state with Overlay = ErrorMessage(translate error, None) }, Cmd.none
        | Ok _ ->
            let path = Path.Combine(config.TestFolderPath, project.DLCKey.ToLowerInvariant())
            let buildConfig = Utils.createBuildConfig Test config project [ state.CurrentPlatform ]
            let task () = buildPackages path buildConfig project

            addTask BuildPackage state, Cmd.OfAsync.either task () BuildComplete (fun ex -> TaskFailed(ex, BuildPackage))

    | Build Release ->
        match BuildValidator.validate project with
        | Error error ->
            { state with Overlay = ErrorMessage(translate error, None) }, Cmd.none
        | Ok _ ->
            let releaseDir =
                state.OpenProjectFile
                |> Option.map Path.GetDirectoryName
                |> Option.defaultWith (fun _ -> Path.GetDirectoryName project.AudioFile.Path)

            let fn =
                sprintf "%s_%s_v%s" project.ArtistName.SortValue project.Title.SortValue (project.Version.Replace('.', '_'))
                |> StringValidator.fileName

            let path = Path.Combine(releaseDir, fn)
            let buildConfig = Utils.createBuildConfig Release config project (Set.toList config.ReleasePlatforms)
            let task () = buildPackages path buildConfig project

            addTask BuildPackage state, Cmd.OfAsync.either task () BuildComplete (fun ex -> TaskFailed(ex, BuildPackage))

    | BuildComplete _ ->
        let runningTasks =
            Set([ BuildPackage; WemConversion ])
            |> Set.difference state.RunningTasks
        { state with RunningTasks = runningTasks }, Cmd.none

    | CheckArrangements ->
        let task() = async { return Utils.checkArrangements project }

        addTask ArrangementCheck state, Cmd.OfAsync.either task () CheckCompleted (fun ex -> TaskFailed(ex, ArrangementCheck))

    | CheckCompleted issues ->
        { state with ArrangementIssues = issues
                     RunningTasks = state.RunningTasks |> Set.remove ArrangementCheck }, Cmd.none

    | ShowIssueViewer ->
        match state.SelectedArrangement with
        | Some arr ->
            let xmlFile = Arrangement.getFile arr
            { state with Overlay = IssueViewer (state.ArrangementIssues.[xmlFile]) }, Cmd.none
        | None ->
            state, Cmd.none
   
    | ErrorOccurred e ->
        { state with Overlay = ErrorMessage (e.Message, Option.ofString e.StackTrace) }, Cmd.none

    | TaskFailed (ex, failedTask) ->          
        { state with Overlay = ErrorMessage (ex.Message, Option.ofString ex.StackTrace)
                     RunningTasks = state.RunningTasks |> Set.remove failedTask }, Cmd.none

    | ChangeLocale newLocale ->
        if state.Config.Locale <> newLocale then changeLocale newLocale
        { state with Config = { config with Locale = newLocale } }, Cmd.none

    | HotKeyMsg msg ->
        match state.Overlay, msg with
        | NoOverlay, _ | _, CloseOverlay ->
            state, Cmd.ofMsg msg
        | _ ->
            // Ignore the message when an overlay is open
            state, Cmd.none
    
    // When the user canceled any of the dialogs
    | AddArrangements None | SaveProject None | ImportPsarc (_, None) | ExportTone (_, None) ->
        state, Cmd.none
