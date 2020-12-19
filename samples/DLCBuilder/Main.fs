module DLCBuilder.Main

open Rocksmith2014
open Rocksmith2014.Common
open Rocksmith2014.Common.Manifest
open Rocksmith2014.DLCProject
open Rocksmith2014.DLCProject.PackageBuilder
open Rocksmith2014.XML.Processing
open Elmish
open System.Runtime.InteropServices
open System
open Avalonia
open Avalonia.Media.Imaging
open Avalonia.Input
open Avalonia.Layout
open Avalonia.Platform
open Avalonia.Controls
open Avalonia.Controls.Shapes
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Components.Hosts
open Media

let private loadPlaceHolderAlbumArt () =
    let assets = AvaloniaLocator.Current.GetService<IAssetLoader>()
    new Bitmap(assets.Open(Uri("avares://DLCBuilder/Assets/coverart_placeholder.png")))

let init arg =
    let commands =
        let loadProject =
            arg
            |> Option.bind (fun a ->
                if String.endsWith ".rs2dlc" a then
                    Some <| Cmd.OfAsync.either DLCProject.load a (fun p -> ProjectLoaded(p, a)) ErrorOccurred
                else None)

        Cmd.batch [
            Cmd.OfAsync.perform Configuration.load () SetConfiguration
            Cmd.OfAsync.perform RecentFilesList.load () SetRecentFiles
            yield! loadProject |> Option.toList
        ]

    { Project = DLCProject.Empty
      SavedProject = DLCProject.Empty
      RecentFiles = []
      Config = Configuration.Default
      CoverArt = loadPlaceHolderAlbumArt()
      SelectedArrangement = None
      SelectedTone = None
      ShowSortFields = false
      ShowJapaneseFields = false
      Overlay = NoOverlay
      ImportTones = []
      PreviewStartTime = TimeSpan()
      BuildInProgress = false
      CheckInProgress = false
      CurrentPlatform = if RuntimeInformation.IsOSPlatform OSPlatform.OSX then Mac else PC
      OpenProjectFile = None
      ArrangementIssues = Map.empty
      Localization = Localization(Locales.English) }, commands

let private convertAudioIfNeeded cliPath project = async {
    if not <| DLCProject.wemFilesExist project then
        do! Wwise.convertToWem cliPath project.AudioFile }

let private createBuildConfig state appId platforms =
    let convTask = convertAudioIfNeeded state.Config.WwiseConsolePath state.Project
    { Platforms = platforms
      Author = state.Config.CharterName
      AppId = appId
      // TODO: Add UI option
      ApplyImprovements = false
      AudioConversionTask = convTask }

let private updateArrangement old updated state =
    let arrangements =
        state.Project.Arrangements
        |> List.update old updated
    { state with Project = { state.Project with Arrangements = arrangements }
                 SelectedArrangement = Some updated }, Cmd.none

let private updateTone old updated state =
    let tones =
        state.Project.Tones
        |> List.update old updated
    { state with Project = { state.Project with Tones = tones } 
                 SelectedTone = Some updated }, Cmd.none

let update (msg: Msg) (state: State) =
    let { Project=project; Config=config } = state
    let localize = state.Localization.GetString

    match msg with
    | NewProject ->
        state.CoverArt.Dispose()
        { state with Project = DLCProject.Empty
                     SavedProject = DLCProject.Empty
                     OpenProjectFile = None
                     SelectedArrangement = None
                     SelectedTone = None
                     CoverArt = loadPlaceHolderAlbumArt () }, Cmd.none

    | ImportTonesChanged item ->
        if isNull item then state, Cmd.none
        else
            let tones = [ item :?> Tone ]
                //items
                //|> Seq.cast<Tone>
                //|> Seq.toList
            { state with ImportTones = tones }, Cmd.none

    | ImportSelectedTones ->
        let importedTones =
            state.ImportTones
            |> List.map (fun x ->
                let descs =
                    match x.ToneDescriptors with
                    | null | [||] ->
                        ToneDescriptor.getDescriptionsOrDefault x.Name
                        |> Array.map (fun x -> x.UIName)
                    | descriptors -> descriptors
                { x with ToneDescriptors = descs; SortOrder = Nullable(); NameSeparator = " - " })

        let tones =
            project.Tones
            |> List.append importedTones
        { state with Project = { project with Tones = tones }
                     Overlay = NoOverlay }, Cmd.none

    | CloseOverlay ->
        let cmd =
            match state.Overlay with
            | ConfigEditor -> Cmd.ofMsg SaveConfiguration
            | _ -> Cmd.none
        { state with Overlay = NoOverlay }, cmd

    | ConditionalCmdDispatch (Some str, msg) -> state, Cmd.ofMsg (msg str)
    | ConditionalCmdDispatch (None, _) -> state, Cmd.none

    | OpenFileDialog (locString, filter, msg) ->
        let dialog = Dialogs.openFileDialog (localize locString) (filter state.Localization)
        state, Cmd.OfAsync.perform dialog None (fun file -> ConditionalCmdDispatch(file, msg))

    | OpenFolderDialog (locString, msg) ->
        let dialog = Dialogs.openFolderDialog (localize locString)
        state, Cmd.OfAsync.perform dialog None (fun folder -> ConditionalCmdDispatch(folder, msg))

    | SelectOpenArrangement ->
        let dialog = Dialogs.openMultiFileDialog (localize "selectArrangement") (Dialogs.xmlFileFilter state.Localization)
        state, Cmd.OfAsync.perform dialog None AddArrangements

    | SelectImportPsarcFolder psarcFile ->
        let dialog = Dialogs.openFolderDialog (localize "selectPsarcExtractFolder")
        state, Cmd.OfAsync.perform dialog None (fun folder -> ImportPsarc(psarcFile, folder))

    | ImportPsarc (psarcFile, Some targetFolder) ->
        let task() = PsarcImporter.import psarcFile targetFolder
        state, Cmd.OfAsync.either task () ProjectLoaded ErrorOccurred

    | ImportToolkitTemplate fileName ->
        try
            let project = ToolkitImporter.import fileName
            let coverArt =
                if IO.File.Exists project.AlbumArtFile then
                    state.CoverArt.Dispose()
                    Utils.loadBitmap project.AlbumArtFile
                else
                    state.CoverArt
            { state with Project = project; OpenProjectFile = None; CoverArt = coverArt
                         SelectedArrangement = None; SelectedTone = None }, Cmd.none
        with e -> state, Cmd.ofMsg (ErrorOccurred e)

    | ImportTonesFromFile fileName ->
        let task () =
            if String.endsWith "psarc" fileName then
                Utils.importTonesFromPSARC fileName
            else
                async { return [| Tone.fromXmlFile fileName |] }
        state, Cmd.OfAsync.either task () ShowImportToneSelector ErrorOccurred

    | ImportProfileTones ->
        if String.IsNullOrWhiteSpace config.ProfilePath then
            state, Cmd.none
        else
            let result = Profile.importTones config.ProfilePath
            match result with
            | Ok toneArray ->
                state, Cmd.ofMsg (ShowImportToneSelector toneArray)
            | Error msg ->
                { state with Overlay = ErrorMessage msg }, Cmd.none

    | ShowImportToneSelector tones ->
        let newState =
            match tones with
            | [||] -> { state with Overlay = ErrorMessage (localize "couldNotFindTonesError") }
            | [| tone |] -> { state with Project = { project with Tones = tone::project.Tones } }
            | _ -> { state with Overlay = ImportToneSelector tones; ImportTones = [] }
        newState, Cmd.none

    | ProjectSaveAs ->
        let intialFileName =
            state.OpenProjectFile
            |> Option.map IO.Path.GetFileName
            |> Option.orElseWith (fun () ->
                sprintf "%s_%s" project.ArtistName.Value project.Title.Value
                |> StringValidator.fileName
                |> sprintf "%s.rs2dlc"
                |> Some)

        let initialDir =
            state.OpenProjectFile
            |> Option.map IO.Path.GetDirectoryName
            |> Option.orElse (Option.ofString config.ProjectsFolderPath)

        let dialog = Dialogs.saveFileDialog (localize "saveProjectAs") (Dialogs.projectFilter state.Localization) intialFileName
        state, Cmd.OfAsync.perform dialog initialDir SaveProject

    | SetProjectsFolderPath path ->
        { state with Config = { config with ProjectsFolderPath = path } }, Cmd.none

    | SetTestFolderPath path ->
        { state with Config = { config with TestFolderPath = path } }, Cmd.none

    | SetProfilePath path ->
        if not <| String.endsWith "_PRFLDB" path then
            state, Cmd.none
        else
            { state with Config = { config with ProfilePath = path } }, Cmd.none

    | SetWwiseConsolePath path ->
        { state with Config = { config with WwiseConsolePath = Option.ofString path } }, Cmd.none

    | SetCustomAppId appId ->
        { state with Config = { config with CustomAppId = appId } }, Cmd.none

    | SetCustomFontFile fileName ->
        match state.SelectedArrangement with
        | Some (Vocals arr as old) ->
            let updated = Vocals { arr with CustomFont = Some fileName }
            updateArrangement old updated state
        | _ -> state, Cmd.none

    | SetAudioFile fileName ->
        let audioFile = { project.AudioFile with Path = fileName }
        let previewPath =
            let previewPath = Utils.previewPathFromMainAudio fileName
            if IO.File.Exists previewPath then
                previewPath
            else
                String.Empty
        let previewFile = { project.AudioPreviewFile with Path = previewPath }
        { state with Project = { project with AudioFile = audioFile; AudioPreviewFile = previewFile } }, Cmd.none

    | ConvertToWem ->
        if DLCProject.audioFilesExist project then
            { state with BuildInProgress = true },
            Cmd.OfAsync.either (Wwise.convertToWem state.Config.WwiseConsolePath) project.AudioFile BuildComplete ErrorOccurred
        else
            state, Cmd.none

    | SetCoverArt fileName ->
        state.CoverArt.Dispose()
        
        { state with CoverArt = Utils.loadBitmap fileName
                     Project = { project with AlbumArtFile = fileName } }, Cmd.none

    | AddArrangements (Some files) ->
        let results = Array.map (Arrangement.fromFile localize) files

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
                | Error msg -> Some(sprintf "%s:\n%s\n" file msg))
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
                                    Title = SortableString.Create (md.Title, md.TitleSort)
                                    AlbumName = SortableString.Create (md.AlbumName, md.AlbumNameSort)
                                    Year = md.AlbumYear
                                    Arrangements = arrangements } }
            | None ->
                { state with Project = { project with Arrangements = arrangements } }

        match errors with
        | [||] -> newState, Cmd.none
        | _ -> { newState with Overlay = ErrorMessage(String.Join('\n', errors)) }, Cmd.none

    | ArrangementSelected selected -> { state with SelectedArrangement = selected }, Cmd.none

    | ToneSelected selected -> { state with SelectedTone = selected }, Cmd.none

    | DeleteArrangement ->
        let arrangements =
            match state.SelectedArrangement with
            | None -> project.Arrangements
            | Some selected -> List.remove selected project.Arrangements
        { state with Project = { project with Arrangements = arrangements }
                     SelectedArrangement = None }, Cmd.none

    | DeleteTone ->
        let tones =
            match state.SelectedTone with
            | None -> project.Tones
            | Some selected -> List.remove selected project.Tones
        { state with Project = { project with Tones = tones }
                     SelectedTone = None }, Cmd.none

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

    | PreviewAudioStartChanged time ->
        { state with PreviewStartTime = TimeSpan.FromSeconds time }, Cmd.none

    | CreatePreviewAudio (SetupStartTime) ->
        let totalLength = Audio.Tools.getLength project.AudioFile.Path
        // Remove the length of the preview from the total length
        let length = totalLength - TimeSpan.FromSeconds 28.
        { state with Overlay = SelectPreviewStart length }, Cmd.none

    | CreatePreviewAudio (CreateFile) ->
        let task () = async { return Audio.Tools.createPreview project.AudioFile.Path state.PreviewStartTime }
        { state with Overlay = NoOverlay }, Cmd.OfAsync.either task () (FileCreated >> CreatePreviewAudio) ErrorOccurred

    | CreatePreviewAudio (FileCreated previewPath) ->
        let previewFile = { project.AudioPreviewFile with Path = previewPath }
        { state with Project = { project with AudioPreviewFile = previewFile } }, Cmd.none

    | ShowSortFields shown -> { state with ShowSortFields = shown }, Cmd.none
    
    | ShowJapaneseFields shown -> { state with ShowJapaneseFields = shown }, Cmd.none

    | ShowConfigEditor -> { state with Overlay = ConfigEditor }, Cmd.none
    
    | SaveConfiguration ->
        { state with Overlay = NoOverlay },
        Cmd.OfAsync.attempt Configuration.save config ErrorOccurred

    | SetConfiguration config -> { state with Config = config
                                              Localization = Localization(config.Locale) }, Cmd.none

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
        state.CoverArt.Dispose()
        let bm =
            if IO.File.Exists project.AlbumArtFile then
                Utils.loadBitmap project.AlbumArtFile
            else
                loadPlaceHolderAlbumArt()

        let project = DLCProject.updateToneInfo project
        let recent = RecentFilesList.update projectFile state.RecentFiles

        { state with CoverArt = bm
                     Project = project
                     SavedProject = project
                     OpenProjectFile = Some projectFile
                     RecentFiles = recent
                     SelectedArrangement = None
                     SelectedTone = None }, Cmd.none

    | EditInstrumental edit ->
        match state.SelectedArrangement with
        | Some (Instrumental arr as old) ->
            let updated = Instrumental (edit state arr)
            updateArrangement old updated state
        | _ -> state, Cmd.none

    | EditVocals edit ->
        match state.SelectedArrangement with
        | Some (Vocals arr as old) ->
            let updated = Vocals (edit arr)
            updateArrangement old updated state
        | _ -> state, Cmd.none

    | EditTone edit ->
        state.SelectedTone
        |> Option.map (fun old ->  updateTone old (edit old) state)
        |> Option.defaultValue (state, Cmd.none)

    | EditProject edit -> { state with Project = edit project }, Cmd.none
    | EditConfig edit -> { state with Config = edit config }, Cmd.none

    | BuildTest ->
        match BuildValidator.validate project with
        | Error error ->
            { state with Overlay = localize error |> ErrorMessage }, Cmd.none
        | Ok _ ->
            let path = IO.Path.Combine(config.TestFolderPath, project.DLCKey.ToLowerInvariant())
            let appId = config.CustomAppId |> Option.defaultValue "248750"
            let buildConfig = createBuildConfig state appId [ state.CurrentPlatform ]
            let task () = buildPackages path buildConfig project

            { state with BuildInProgress = true }, Cmd.OfAsync.either task () BuildComplete ErrorOccurred

    | BuildRelease ->
        match BuildValidator.validate project with
        | Error error ->
            { state with Overlay = localize error |> ErrorMessage }, Cmd.none
        | Ok _ ->
            let releaseDir =
                state.OpenProjectFile
                |> Option.map IO.Path.GetDirectoryName
                |> Option.defaultWith (fun _ -> IO.Path.GetDirectoryName project.AudioFile.Path)

            let fn =
                sprintf "%s_%s_v%s" project.ArtistName.Value project.Title.Value (project.Version.Replace('.', '_'))
                |> StringValidator.fileName

            let path = IO.Path.Combine(releaseDir, fn)
            let buildConfig = createBuildConfig state "248750" config.ReleasePlatforms
            let task () = buildPackages path buildConfig project

            { state with BuildInProgress = true }, Cmd.OfAsync.either task () BuildComplete ErrorOccurred

    | BuildComplete _ -> { state with BuildInProgress = false }, Cmd.none

    | CheckArrangements ->
        // TODO: Showlights validation
        let task() = async {
            return state.Project.Arrangements
                   |> List.map (function
                       | Instrumental inst as arr ->
                           let issues =
                               XML.InstrumentalArrangement.Load inst.XML
                               |> ArrangementChecker.runAllChecks
                           arr, issues
                       | Vocals v as arr when Option.isNone v.CustomFont ->
                           let issues =
                               XML.Vocals.Load v.XML
                               |> ArrangementChecker.checkVocals
                               |> Option.toList
                           arr, issues
                       | arr -> arr, [])
                   |> Map.ofList }

        { state with CheckInProgress = true }, Cmd.OfAsync.either task () CheckCompleted ErrorOccurred

    | CheckCompleted issues ->
        { state with ArrangementIssues = issues
                     CheckInProgress = false }, Cmd.none

    | ShowIssueViewer ->
        match state.SelectedArrangement with
        | Some arr -> { state with Overlay = IssueViewer (state.ArrangementIssues.[arr]) }, Cmd.none
        | None -> state, Cmd.none
   
    | ErrorOccurred e -> { state with Overlay = ErrorMessage e.Message
                                      BuildInProgress = false
                                      CheckInProgress = false }, Cmd.none

    | ChangeLocale newLocale ->
        { state with Config = { config with Locale = newLocale }
                     Localization = Localization(newLocale) }, Cmd.none
    
    // When the user canceled any of the dialogs
    | AddArrangements None | SaveProject None | ImportPsarc (_, None) ->
        state, Cmd.none

let view (window: HostWindow) (state: State) dispatch =
    if state.BuildInProgress then
        window.Cursor <- Cursors.appStarting
    else
        window.Cursor <- Cursors.arrow
        
    window.Title <-
        match state.OpenProjectFile with
        | Some project ->
            let dot = if state.SavedProject <> state.Project then "*" else String.Empty
            $"{dot}Rocksmith 2014 DLC Builder - {project}"
        | None -> "Rocksmith 2014 DLC Builder"

    Grid.create [
        Grid.children [
            Grid.create [
                Grid.columnDefinitions "2*,*,2*"
                Grid.rowDefinitions "3*,2*"
                //Grid.showGridLines true
                Grid.children [
                    ProjectDetails.view state dispatch

                    // Arrangements
                    DockPanel.create [
                        Grid.column 1
                        DockPanel.children [
                            DockPanel.create [
                                DockPanel.dock Dock.Top
                                DockPanel.margin 5.0

                                DockPanel.children [
                                    Button.create [
                                        DockPanel.dock Dock.Right
                                        Button.padding (10.0, 5.0)
                                        Button.content (state.Localization.GetString "validate")
                                        Button.onClick (fun _ -> dispatch CheckArrangements)
                                        Button.isEnabled (state.Project.Arrangements.Length > 0 && not state.CheckInProgress)
                                    ]

                                    // Add arrangement
                                    Button.create [
                                        DockPanel.dock Dock.Right
                                        Button.padding (15.0, 5.0)
                                        Button.content (state.Localization.GetString "addArrangement")
                                        Button.onClick (fun _ -> dispatch SelectOpenArrangement)
                                        // 5 instrumentals, 2 vocals, 1 showlights
                                        Button.isEnabled (state.Project.Arrangements.Length < 8)
                                    ]

                                    // Title
                                    TextBlock.create [
                                        TextBlock.text (state.Localization.GetString "arrangements")
                                        TextBlock.verticalAlignment VerticalAlignment.Bottom
                                    ]
                                ]
                            ]

                            ListBox.create [
                                ListBox.virtualizationMode ItemVirtualizationMode.None
                                ListBox.margin 2.
                                ListBox.dataItems state.Project.Arrangements
                                ListBox.itemTemplate Templates.arrangement
                                match state.SelectedArrangement with
                                | Some a -> ListBox.selectedItem a
                                | None -> ()
                                ListBox.onSelectedItemChanged ((fun item ->
                                    match item with
                                    | :? Arrangement as arr -> dispatch (ArrangementSelected (Some arr))
                                    | null when state.Project.Arrangements.Length = 0 -> dispatch (ArrangementSelected None)
                                    | _ -> ()), SubPatchOptions.OnChangeOf state.Project.Arrangements)
                                ListBox.onKeyDown (fun k ->
                                    if k.Key = Key.Delete then
                                        k.Handled <- true
                                        dispatch DeleteArrangement)
                            ]
                        ]
                    ]

                    // Arrangement details
                    StackPanel.create [
                        Grid.column 2
                        StackPanel.margin 8.
                        StackPanel.children [
                            match state.SelectedArrangement with
                            | None ->
                                TextBlock.create [
                                    TextBlock.text (state.Localization.GetString "selectArrangementPrompt")
                                    TextBlock.horizontalAlignment HorizontalAlignment.Center
                                ]

                            | Some arr ->
                                // Arrangement name
                                TextBlock.create [
                                    TextBlock.fontSize 17.
                                    TextBlock.text (Arrangement.getHumanizedName arr)
                                    TextBlock.horizontalAlignment HorizontalAlignment.Center
                                ]

                                // Arrangement filename
                                TextBlock.create [
                                    TextBlock.text (IO.Path.GetFileName (Arrangement.getFile arr))
                                    TextBlock.horizontalAlignment HorizontalAlignment.Center
                                ]

                                // Validation Icon
                                if state.ArrangementIssues.ContainsKey arr then
                                    let noIssues = state.ArrangementIssues.[arr].IsEmpty
                                    StackPanel.create [
                                        StackPanel.orientation Orientation.Horizontal
                                        StackPanel.background Brushes.Transparent
                                        if not noIssues then
                                            StackPanel.onTapped (fun _ -> dispatch ShowIssueViewer)
                                            StackPanel.cursor Cursors.hand
                                        StackPanel.children [
                                            Path.create [
                                                Path.fill (if noIssues then Brushes.Green else Brushes.Red)
                                                Path.data (if noIssues then Icons.check else Icons.x)
                                                Path.verticalAlignment VerticalAlignment.Center
                                                Path.margin (0., 0., 6., 0.)
                                            ]

                                            TextBlock.create[
                                                TextBlock.text (if noIssues then "OK" else state.Localization.GetString "issues")
                                                TextBlock.verticalAlignment VerticalAlignment.Center
                                            ]
                                        ]
                                    ]

                                match arr with
                                | Showlights _ -> ()
                                | Instrumental i -> InstrumentalDetails.view state dispatch i
                                | Vocals v -> VocalsDetails.view state dispatch v
                        ]
                    ]

                    // Tones
                    DockPanel.create [
                        Grid.column 1
                        Grid.row 1
                        DockPanel.children [
                            // Title
                            TextBlock.create [
                                DockPanel.dock Dock.Top
                                TextBlock.text (state.Localization.GetString "tones")
                                TextBlock.margin 5.0
                            ]

                            StackPanel.create [
                                DockPanel.dock Dock.Top
                                StackPanel.orientation Orientation.Horizontal
                                StackPanel.spacing 4.
                                StackPanel.margin 5.
                                StackPanel.children [
                                    // Import from profile
                                    Button.create [
                                        Button.padding (15.0, 5.0)
                                        Button.horizontalAlignment HorizontalAlignment.Left
                                        Button.content (state.Localization.GetString "fromProfile")
                                        Button.onClick (fun _ -> dispatch ImportProfileTones)
                                        Button.isEnabled (IO.File.Exists state.Config.ProfilePath)
                                        ToolTip.tip (state.Localization.GetString "profileImportToolTip")
                                    ]
                                    // Import from a file
                                    Button.create [
                                        Button.padding (15.0, 5.0)
                                        Button.horizontalAlignment HorizontalAlignment.Left
                                        Button.content (state.Localization.GetString "import")
                                        Button.onClick (fun _ -> dispatch (Msg.OpenFileDialog("selectImportToneFile", Dialogs.toneImportFilter, ImportTonesFromFile)))
                                    ]
                                ]
                            ]

                            ListBox.create [
                                ListBox.margin 2.
                                ListBox.dataItems state.Project.Tones
                                match state.SelectedTone with
                                | Some t -> ListBox.selectedItem t
                                | None -> ()
                                ListBox.onSelectedItemChanged ((fun item ->
                                    match item with
                                    | :? Tone as t -> dispatch (ToneSelected (Some t))
                                    | null when state.Project.Tones.Length = 0 -> dispatch (ToneSelected None)
                                    | _ -> ()), SubPatchOptions.OnChangeOf state.Project.Tones)
                                ListBox.onKeyDown (fun k ->
                                    match k.KeyModifiers, k.Key with
                                    | KeyModifiers.None, Key.Delete -> dispatch DeleteTone
                                    | KeyModifiers.Alt, Key.Up -> dispatch (MoveTone Up)
                                    | KeyModifiers.Alt, Key.Down -> dispatch (MoveTone Down)
                                    | _ -> ())
                            ]
                        ]
                    ]

                    // Tone details
                    StackPanel.create [
                        Grid.column 2
                        Grid.row 1
                        StackPanel.margin 8.
                        StackPanel.children [
                            match state.SelectedTone with
                            | None ->
                                TextBlock.create [
                                    TextBlock.text(state.Localization.GetString "selectTonePrompt")
                                    TextBlock.horizontalAlignment HorizontalAlignment.Center
                                ]
                            | Some tone -> ToneDetails.view state dispatch tone
                        ]
                    ]
                ]
            ]

            match state.Overlay with
            | NoOverlay -> ()
            | _ ->
                Grid.create [
                    Grid.children [
                        Rectangle.create [
                            Rectangle.fill "#77000000"
                            Rectangle.onTapped (fun _ -> CloseOverlay |> dispatch)
                        ]
                        Border.create [
                            Border.padding (20., 10.)
                            Border.cornerRadius 5.0
                            Border.horizontalAlignment HorizontalAlignment.Center
                            Border.verticalAlignment VerticalAlignment.Center
                            Border.background "#444444"
                            Border.child (
                                match state.Overlay with
                                | NoOverlay -> failwith "This can not happen."
                                | ErrorMessage msg -> ErrorMessage.view state dispatch msg
                                | SelectPreviewStart audioLength -> SelectPreviewStart.view state dispatch audioLength
                                | ImportToneSelector tones -> SelectImportTones.view state dispatch tones
                                | ConfigEditor -> ConfigEditor.view state dispatch
                                | IssueViewer issues -> IssueViewer.view state dispatch issues
                            )
                        ]
                    ]
                ]
        ]
    ]
