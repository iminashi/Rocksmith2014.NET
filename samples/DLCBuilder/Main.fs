module DLCBuilder.Main

open Rocksmith2014.Common
open Rocksmith2014.Common.Manifest
open Rocksmith2014.DLCProject
open Rocksmith2014
open Elmish
open System.Runtime.InteropServices
open System.Xml
open System
open Avalonia
open Avalonia.Media.Imaging
open Avalonia.Input
open Avalonia.Platform
open Avalonia.Controls
open Avalonia.Controls.Shapes
open Avalonia.FuncUI.DSL
open Avalonia.Layout

let private loadPlaceHolderAlbumArt () =
    let assets = AvaloniaLocator.Current.GetService<IAssetLoader>()
    new Bitmap(assets.Open(new Uri("avares://DLCBuilder/coverart_placeholder.png")))

let init () =
    { Project = DLCProject.Empty
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
      CurrentPlatform = if RuntimeInformation.IsOSPlatform OSPlatform.OSX then Mac else PC
      OpenProjectFile = None
      Localization = Localization(Locales.English) }, Cmd.OfAsync.perform Configuration.load () SetConfiguration

let private loadArrangement state (fileName: string) =
    let rootName =
        using (XmlReader.Create(fileName))
              (fun reader -> reader.MoveToContent() |> ignore; reader.LocalName)

    match rootName with
    | "song" ->
        let metadata = XML.MetaData.Read fileName
        let toneInfo = XML.InstrumentalArrangement.ReadToneNames fileName
        let baseTone =
            if isNull toneInfo.BaseToneName then
                metadata.Arrangement.ToLowerInvariant() + "_base"
            else
                toneInfo.BaseToneName
        let tones =
            toneInfo.Names
            |> Array.filter (isNull >> not)
            |> Array.toList
        let arr =
            { XML = fileName
              Name = ArrangementName.Parse metadata.Arrangement
              Priority =
                if metadata.ArrangementProperties.Represent then ArrangementPriority.Main
                elif metadata.ArrangementProperties.BonusArrangement then ArrangementPriority.Bonus
                else ArrangementPriority.Alternative
              Tuning = metadata.Tuning.Strings
              TuningPitch = Utils.centsToTuningPitch(float metadata.CentOffset)
              RouteMask =
                if metadata.ArrangementProperties.PathBass then RouteMask.Bass
                elif metadata.ArrangementProperties.PathLead then RouteMask.Lead
                else RouteMask.Rhythm
              ScrollSpeed = 1.3
              BaseTone = baseTone
              Tones = tones
              BassPicked = metadata.ArrangementProperties.BassPick
              MasterID = RandomGenerator.next()
              PersistentID = Guid.NewGuid() }
            |> Arrangement.Instrumental
        Ok (arr, Some metadata)

    | "vocals" ->
        // Attempt to infer whether the lyrics are Japanese from the filename
        let isJapanese =
            fileName.Contains("jvocal", StringComparison.OrdinalIgnoreCase) ||
            fileName.Contains("jlyric", StringComparison.OrdinalIgnoreCase)

        // Try to find custom font for Japanese vocals
        let customFont =
            let lyricFile = IO.Path.Combine(IO.Path.GetDirectoryName fileName, "lyrics.dds")
            if isJapanese && IO.File.Exists lyricFile then Some lyricFile else None

        let arr =
            { XML = fileName
              Japanese = isJapanese
              CustomFont = customFont
              MasterID = RandomGenerator.next()
              PersistentID = Guid.NewGuid() }
            |> Arrangement.Vocals
        Ok (arr, None)

    | "showlights" ->
        let arr = Arrangement.Showlights { XML = fileName }
        Ok (arr, None)

    | _ -> Error (state.Localization.GetString "unknownArrangementError")

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
    match msg with
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
                if isNull x.ToneDescriptors || x.ToneDescriptors.Length = 0 then
                    let descs =
                        ToneDescriptor.getDescriptionsOrDefault x.Name
                        |> Array.map (fun x -> x.UIName)
                    { x with ToneDescriptors = descs; SortOrder = Nullable(); NameSeparator = " - " }
                else
                    { x with SortOrder = Nullable(); NameSeparator = " - " })
        let tones =
            state.Project.Tones
            |> List.append importedTones
        { state with Project = { state.Project with Tones = tones }
                     Overlay = NoOverlay }, Cmd.none

    | CloseOverlay -> {state with Overlay = NoOverlay }, Cmd.none

    | ConditionalCmdDispatch (Some str, msg) -> state, Cmd.ofMsg (msg str)
    | ConditionalCmdDispatch (None, _) -> state, Cmd.none

    | OpenFileDialog (locString, filter, msg) ->
        let dialog = Dialogs.openFileDialog (state.Localization.GetString locString) (filter state.Localization)
        state, Cmd.OfAsync.perform dialog None (fun file -> ConditionalCmdDispatch(file, msg))

    | OpenFolderDialog (locString, msg) ->
        let dialog = Dialogs.openFolderDialog (state.Localization.GetString locString)
        state, Cmd.OfAsync.perform dialog None (fun folder -> ConditionalCmdDispatch(folder, msg))

    | SelectOpenArrangement ->
        let dialog = Dialogs.openMultiFileDialog (state.Localization.GetString "selectArrangement") (Dialogs.xmlFileFilter state.Localization)
        state, Cmd.OfAsync.perform dialog None AddArrangements

    | SelectImportPsarcFolder psarcFile ->
        let dialog = Dialogs.openFolderDialog (state.Localization.GetString "selectPsarcExtractFolder")
        state, Cmd.OfAsync.perform dialog None (fun folder -> ImportPsarc(psarcFile, folder))

    | ImportPsarc (psarcFile, Some targetFolder) ->
        let task() = PsarcImporter.import psarcFile targetFolder
        state, Cmd.OfAsync.either task () (fun (project, projectFile) -> ProjectLoaded(project, Some projectFile)) ErrorOccurred

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
            if fileName.EndsWith("psarc", StringComparison.OrdinalIgnoreCase) then
                Utils.importTonesFromPSARC fileName
            else
                async { return [| Tone.fromXmlFile fileName |] }
        state, Cmd.OfAsync.either task () ShowImportToneSelector ErrorOccurred

    | ImportProfileTones ->
        if String.IsNullOrWhiteSpace state.Config.ProfilePath then
            state, Cmd.none
        else
            let result = Profile.importTones state.Config.ProfilePath
            match result with
            | Ok toneArray ->
                state, Cmd.ofMsg (ShowImportToneSelector toneArray)
            | Error msg ->
                { state with Overlay = ErrorMessage msg }, Cmd.none

    | ShowImportToneSelector tones ->
        match tones.Length with
        | 0 -> { state with Overlay = ErrorMessage (state.Localization.GetString "couldNotFindTonesError") }, Cmd.none
        | 1 -> { state with Project = { state.Project with Tones = tones.[0]::state.Project.Tones } }, Cmd.none
        | _ -> { state with Overlay = ImportToneSelector tones; ImportTones = [] }, Cmd.none

    | ProjectSaveAs ->
        let intialFileName =
            state.OpenProjectFile
            |> Option.map IO.Path.GetFileName
            |> Option.orElse
                (let fn =
                    sprintf "%s_%s" state.Project.ArtistName.Value state.Project.Title.Value
                    |> StringValidator.fileName
                sprintf "%s.rs2dlc" fn
                |> Some)
        let initialDir =
            state.OpenProjectFile
            |> Option.map IO.Path.GetDirectoryName
            |> Option.orElse (Option.ofString state.Config.ProjectsFolderPath)
        let dialog = Dialogs.saveFileDialog (state.Localization.GetString "saveProjectAs") (Dialogs.projectFilter state.Localization) intialFileName
        state, Cmd.OfAsync.perform dialog initialDir SaveProject

    | AddProjectsFolderPath path ->
        let config = { state.Config with ProjectsFolderPath = path }
        { state with Config = config }, Cmd.none

    | AddTestFolderPath path ->
        let config = { state.Config with TestFolderPath = path }
        { state with Config = config }, Cmd.none

    | AddProfilePath path ->
        if not <| path.EndsWith("_PRFLDB", StringComparison.OrdinalIgnoreCase) then
            state, Cmd.none
        else
            let config = { state.Config with ProfilePath = path }
            { state with Config = config }, Cmd.none

    | AddCustomFontFile fileName ->
        match state.SelectedArrangement with
        | Some (Vocals arr as old) ->
            let updated = Vocals ({ arr with CustomFont = Some fileName})
            updateArrangement old updated state
        | _ -> state, Cmd.none

    | AddAudioFile fileName ->
        let audioFile = { state.Project.AudioFile with Path = fileName }
        let previewPath =
            let previewPath =
                let dir = IO.Path.GetDirectoryName fileName
                let fn = IO.Path.GetFileNameWithoutExtension fileName
                let ext = IO.Path.GetExtension fileName
                IO.Path.Combine(dir, sprintf "%s_preview%s" fn ext)
            if IO.File.Exists previewPath then
                previewPath
            else
                String.Empty
        let previewFile = { state.Project.AudioPreviewFile with Path = previewPath }
        { state with Project = { state.Project with AudioFile = audioFile; AudioPreviewFile = previewFile } }, Cmd.none

    | ConvertToWem ->
        let audioFile = state.Project.AudioFile.Path
        if IO.File.Exists audioFile && IO.File.Exists state.Project.AudioPreviewFile.Path then
            let target =
                IO.Path.Combine (IO.Path.GetDirectoryName audioFile, 
                                 IO.Path.GetFileNameWithoutExtension audioFile)
            let task () = async { Wwise.convertToWem audioFile target }
            { state with BuildInProgress = true }, Cmd.OfAsync.either task () BuildComplete ErrorOccurred
        else
            state, Cmd.none

    | AddCoverArt fileName ->
        state.CoverArt.Dispose()
        
        { state with CoverArt = Utils.loadBitmap fileName
                     Project = { state.Project with AlbumArtFile = fileName } }, Cmd.none

    | AddArrangements (Some files) ->
        let results = Array.map (loadArrangement state) files

        let shouldInclude arrangements arr =
            match arr with
            // Allow only one show lights arrangement
            | Showlights _ when arrangements |> List.exists (function Showlights _ -> true | _ -> false) -> false

            // Allow max five instrumental arrangements
            | Instrumental _ when (arrangements |> List.choose Arrangement.pickInstrumental).Length = 5 -> false

            // Allow max two instrumental arrangements
            | Vocals _ when (arrangements |> List.choose (function Vocals _ -> Some 1 | _ -> None)).Length = 2 -> false
            | _ -> true

        let arrangements =
            (state.Project.Arrangements, results)
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
            if state.Project.ArtistName = SortableString.Empty then
                results
                |> Array.tryPick (function Ok (_, md) -> md | Error _ -> None)
            else
                None

        match metadata with
        | Some md ->
            { state with
                Project = { state.Project with
                                DLCKey = DLCKey.create state.Config.CharterName md.ArtistName md.Title
                                ArtistName = SortableString.Create md.ArtistName // Ignore the sort value from the XML
                                Title = SortableString.Create (md.Title, md.TitleSort)
                                AlbumName = SortableString.Create (md.AlbumName, md.AlbumNameSort)
                                Year = md.AlbumYear
                                Arrangements = arrangements } }, Cmd.none
        | None ->
            { state with Project = { state.Project with Arrangements = arrangements } }, Cmd.none

    | ArrangementSelected selected -> { state with SelectedArrangement = selected }, Cmd.none

    | ToneSelected selected -> { state with SelectedTone = selected }, Cmd.none

    | DeleteArrangement ->
        let arrangements =
            match state.SelectedArrangement with
            | None -> state.Project.Arrangements
            | Some selected -> List.remove selected state.Project.Arrangements
        { state with Project = { state.Project with Arrangements = arrangements }
                     SelectedArrangement = None }, Cmd.none

    | DeleteTone ->
        let tones =
            match state.SelectedTone with
            | None -> state.Project.Tones
            | Some selected -> List.remove selected state.Project.Tones
        { state with Project = { state.Project with Tones = tones }
                     SelectedTone = None }, Cmd.none

    | PreviewAudioStartChanged time ->
        { state with PreviewStartTime = TimeSpan.FromSeconds time }, Cmd.none

    | CreatePreviewAudio (SetupStartTime) ->
        let totalLength = Audio.Tools.getLength state.Project.AudioFile.Path
        // Remove the length of the preview from the total length
        let length = totalLength - TimeSpan.FromSeconds 28.
        { state with Overlay = SelectPreviewStart length }, Cmd.none

    | CreatePreviewAudio (CreateFile) ->
        let task () = async { return Audio.Tools.createPreview state.Project.AudioFile.Path state.PreviewStartTime }
        { state with Overlay = NoOverlay }, Cmd.OfAsync.either task () (FileCreated >> CreatePreviewAudio) ErrorOccurred

    | CreatePreviewAudio (FileCreated previewPath) ->
        let previewFile = { state.Project.AudioPreviewFile with Path = previewPath }
        { state with Project = { state.Project with AudioPreviewFile = previewFile } }, Cmd.none

    | ShowSortFields shown -> { state with ShowSortFields = shown }, Cmd.none
    
    | ShowJapaneseFields shown -> { state with ShowJapaneseFields = shown }, Cmd.none

    | ShowConfigEditor -> { state with Overlay = ConfigEditor }, Cmd.none
    
    | SaveConfiguration ->
        { state with Overlay = NoOverlay }, Cmd.OfAsync.attempt Configuration.save state.Config ErrorOccurred

    | SetConfiguration config -> { state with Config = config
                                              Localization = Localization(config.Locale) }, Cmd.none

    | SaveProject (Some target) ->
        let task() = DLCProject.save target state.Project
        { state with OpenProjectFile = Some target }, Cmd.OfAsync.attempt task () ErrorOccurred

    | ProjectSaveOrSaveAs ->
        let msg =
            match state.OpenProjectFile with
            | Some _ as fn -> SaveProject fn
            | None -> ProjectSaveAs
        state, Cmd.ofMsg msg

    | OpenProject fileName ->
        let task() = DLCProject.load fileName
        state, Cmd.OfAsync.either task () (fun p -> ProjectLoaded(p, Some fileName)) ErrorOccurred

    | ProjectLoaded (project, projectFile) ->
        state.CoverArt.Dispose()
        let bm =
            if IO.File.Exists project.AlbumArtFile then
                Utils.loadBitmap(project.AlbumArtFile)
            else
                loadPlaceHolderAlbumArt()

        { state with CoverArt = bm
                     Project = project
                     OpenProjectFile = projectFile
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

    | EditProject edit -> { state with Project = edit state.Project }, Cmd.none
    | EditConfig edit -> { state with Config = edit state.Config }, Cmd.none

    | BuildTest ->
        match BuildValidator.validate state.Localization state.Project with
        | Error error ->
            { state with Overlay = ErrorMessage error }, Cmd.none
        | Ok _ ->
            let testDir = state.Config.TestFolderPath
            let path = IO.Path.Combine(testDir, state.Project.DLCKey.ToLowerInvariant())
            let task () = PackageBuilder.buildPackages path [ state.CurrentPlatform ] state.Config.CharterName state.Project
            { state with BuildInProgress = true }, Cmd.OfAsync.either task () BuildComplete ErrorOccurred

    | BuildRelease ->
        match BuildValidator.validate state.Localization state.Project with
        | Error error ->
            { state with Overlay = ErrorMessage error }, Cmd.none
        | Ok _ ->
            let releaseDir =
                state.OpenProjectFile
                |> Option.map IO.Path.GetDirectoryName
                |> Option.defaultWith (fun _ -> IO.Path.GetDirectoryName state.Project.AudioFile.Path)
            let fn =
                sprintf "%s_%s_v%s" state.Project.ArtistName.Value state.Project.Title.Value (state.Project.Version.Replace('.', '_'))
                |> StringValidator.fileName
            let path = IO.Path.Combine(releaseDir, fn)
            let task () = PackageBuilder.buildPackages path state.Config.ReleasePlatforms state.Config.CharterName state.Project
            { state with BuildInProgress = true }, Cmd.OfAsync.either task () BuildComplete ErrorOccurred

    | BuildComplete _ -> { state with BuildInProgress = false }, Cmd.none
   
    | ErrorOccurred e -> { state with Overlay = ErrorMessage e.Message
                                      BuildInProgress = false }, Cmd.none

    | ChangeLocale newLocale ->
        { state with Config = { state.Config with Locale = newLocale }
                     Localization = Localization(newLocale) }, Cmd.none
    
    // When the user canceled any of the dialogs
    | AddArrangements None | SaveProject None | ImportPsarc (_, None) ->
        state, Cmd.none

let view (state: State) dispatch =
    Grid.create [
        Grid.children [
            Grid.create [
                Grid.columnDefinitions "2*,*,2*"
                Grid.rowDefinitions "3*,2*"
                //Grid.showGridLines true
                Grid.children [
                    ProjectDetails.view state dispatch

                    DockPanel.create [
                        Grid.column 1
                        DockPanel.children [
                            Button.create [
                                DockPanel.dock Dock.Top
                                Button.margin 5.0
                                Button.padding (15.0, 5.0)
                                Button.horizontalAlignment HorizontalAlignment.Left
                                Button.content (state.Localization.GetString "addArrangement")
                                Button.onClick (fun _ -> dispatch SelectOpenArrangement)
                            ]

                            ListBox.create [
                                ListBox.virtualizationMode ItemVirtualizationMode.None
                                ListBox.margin 2.
                                ListBox.dataItems state.Project.Arrangements
                                match state.SelectedArrangement with
                                | Some a -> ListBox.selectedItem a
                                | None -> ()
                                ListBox.onSelectedItemChanged ((fun item ->
                                    match item with
                                    | :? Arrangement as arr -> dispatch (ArrangementSelected (Some arr))
                                    | null when state.Project.Arrangements.Length = 0 -> dispatch (ArrangementSelected None)
                                    | _ -> ()), SubPatchOptions.OnChangeOf state)
                                ListBox.onKeyDown (fun k ->
                                    if k.Key = Key.Delete then
                                        k.Handled <- true
                                        dispatch DeleteArrangement)
                            ]
                        ]
                    ]

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
                                    TextBlock.text (Arrangement.getName arr false)
                                    TextBlock.horizontalAlignment HorizontalAlignment.Center
                                ]

                                // Arrangement filename
                                TextBlock.create [
                                    TextBlock.text (IO.Path.GetFileName (Arrangement.getFile arr))
                                    TextBlock.horizontalAlignment HorizontalAlignment.Center
                                ]

                                match arr with
                                | Showlights _ -> ()
                                | Instrumental i -> InstrumentalDetails.view state dispatch i
                                | Vocals v -> VocalsDetails.view state dispatch v
                        ]
                    ]

                    DockPanel.create [
                        Grid.column 1
                        Grid.row 1
                        DockPanel.children [
                            StackPanel.create [
                                DockPanel.dock Dock.Top
                                StackPanel.orientation Orientation.Horizontal
                                StackPanel.spacing 4.
                                StackPanel.margin 5.
                                StackPanel.children [
                                    Button.create [
                                        Button.padding (15.0, 5.0)
                                        Button.horizontalAlignment HorizontalAlignment.Left
                                        Button.content (state.Localization.GetString "fromProfile")
                                        Button.onClick (fun _ -> dispatch ImportProfileTones)
                                        Button.isEnabled (IO.File.Exists state.Config.ProfilePath)
                                        ToolTip.tip (state.Localization.GetString "profileImportToolTip")
                                    ]
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
                                    | _ -> ()), SubPatchOptions.OnChangeOf state)
                                ListBox.onKeyDown (fun k -> if k.Key = Key.Delete then dispatch DeleteTone)
                            ]
                        ]
                    ]

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
                            )
                        ]
                    ]
                ]
        ]
    ]
