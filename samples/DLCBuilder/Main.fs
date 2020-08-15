module DLCBuilder.Main

open Rocksmith2014.Common
open Rocksmith2014.Common.Manifest
open Rocksmith2014.DLCProject
open Rocksmith2014
open Elmish
open System.Xml
open System
open Avalonia
open Avalonia.Media.Imaging
open Avalonia.Input
open Avalonia.Platform
open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.Layout

let init () =
    let assets = AvaloniaLocator.Current.GetService<IAssetLoader>()
    let coverArt = new Bitmap(assets.Open(new Uri("avares://DLCBuilder/placeholder.png")))

    { Project = DLCProject.Empty
      Config = Configuration.Default
      CoverArt = Some coverArt
      SelectedArrangement = None
      SelectedTone = None
      ShowSortFields = false
      ShowJapaneseFields = false
      Overlay = NoOverlay
      ImportTones = [] }, Cmd.none

let private loadArrangement (fileName: string) =
    let rootName =
        using (XmlReader.Create(fileName))
              (fun reader -> reader.MoveToContent() |> ignore; reader.LocalName)

    match rootName with
    | "song" ->
        let metadata = XML.MetaData.Read fileName
        let toneInfo = XML.InstrumentalArrangement.ReadToneNames fileName
        let baseTone =
            if isNull toneInfo.BaseToneName then
                metadata.Arrangement + "_Base"
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
              CentOffset = metadata.CentOffset
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
            let lyricFile = IO.Path.Combine(IO.Path.GetDirectoryName(fileName), "lyrics.dds")
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
        let arr =
            { XML = fileName }
            |> Arrangement.Showlights
        Ok (arr, None)

    | _ -> Error "Not a Rocksmith 2014 arrangement."

let private updateArrangement old updated state =
    let arrangements =
        state.Project.Arrangements
        |> List.update old updated
    { state with Project = { state.Project with Arrangements = arrangements }
                 SelectedArrangement = Some updated }, Cmd.none

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

    | CloseOverlay ->
        {state with Overlay = NoOverlay }, Cmd.none

    | SelectOpenArrangement ->
        let dialog = Dialogs.openMultiFileDialog "Select Arrangement" Dialogs.xmlFileFilter
        state, Cmd.OfAsync.perform dialog None AddArrangements

    | SelectCoverArt ->
        let dialog = Dialogs.openFileDialog "Select Cover Art" Dialogs.imgFileFilter
        state, Cmd.OfAsync.perform dialog None AddCoverArt

    | SelectAudioFile ->
        let dialog = Dialogs.openFileDialog "Select Audio File" Dialogs.audioFileFilters
        state, Cmd.OfAsync.perform dialog None AddAudioFile

    | SelectCustomFont ->
        let dialog = Dialogs.openFileDialog "Select Custom Font Texture" Dialogs.ddsFileFilter
        state, Cmd.OfAsync.perform dialog None AddCustomFontFile

    | AddCustomFontFile (Some fileName) ->
        match state.SelectedArrangement with
        | Some (Vocals arr as old) ->
            let updated = Vocals ({ arr with CustomFont = Some fileName})
            updateArrangement old updated state
        | _ -> state, Cmd.none

    | AddAudioFile (Some fileName) ->
        let audioFile = { state.Project.AudioFile with Path = fileName }
        { state with Project = { state.Project with AudioFile = audioFile } }, Cmd.none

    | AddCoverArt (Some fileName) ->
        state.CoverArt |> Option.iter dispose
        let bm = new Bitmap(fileName)
        { state with CoverArt = Some bm
                     Project = { state.Project with AlbumArtFile = fileName } }, Cmd.none

    | AddArrangements (Some files) ->
        let results =
            files
            |> Array.map loadArrangement

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
                | Ok (arr, _) when shouldInclude state arr -> arr::state
                | _ -> state)

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
        { state with Project = { state.Project with Tones = tones } }, Cmd.none

    | ImportProfileTones ->
        let result = Profile.importTones state.Config.ProfilePath
        match result with
        | Ok toneArray ->
            { state with Overlay = SelectImportTones toneArray; ImportTones = [] }, Cmd.none
        | Error msg ->
            { state with Overlay = ErrorMessage msg }, Cmd.none

    | CreatePreviewAudio ->
        // TODO: UI to select preview start time
        Audio.Tools.createPreview state.Project.AudioFile.Path (TimeSpan.FromSeconds 22.)
        state, Cmd.none

    | ShowSortFields shown ->
        { state with ShowSortFields = shown }, Cmd.none

    | ShowJapaneseFields shown ->
        { state with ShowJapaneseFields = shown }, Cmd.none

    | EditInstrumental edit ->
        match state.SelectedArrangement with
        | Some (Instrumental arr as old) ->
            let updated = Instrumental (edit arr)
            updateArrangement old updated state
        | _ -> state, Cmd.none

    | EditVocals edit ->
        match state.SelectedArrangement with
        | Some (Vocals arr as old) ->
            let updated = Vocals (edit arr)
            updateArrangement old updated state
        | _ -> state, Cmd.none

    | EditProject edit ->
        { state with Project = edit state.Project }, Cmd.none
        
    | AddArrangements None | AddCoverArt None | AddAudioFile None | AddCustomFontFile None ->
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
                                Button.content "Add Arrangement"
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
                                    | :? Arrangement as arr ->
                                        dispatch (ArrangementSelected (Some arr))
                                    | null when state.Project.Arrangements.Length = 0 -> dispatch (ArrangementSelected None)
                                    | _ -> ()), SubPatchOptions.OnChangeOf(state))
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
                                    TextBlock.text "Select an arrangement to edit its details"
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
                            Button.create [
                                DockPanel.dock Dock.Top
                                Button.margin 5.0
                                Button.padding (15.0, 5.0)
                                Button.horizontalAlignment HorizontalAlignment.Left
                                Button.content "From Profile"
                                Button.onClick (fun _ -> dispatch ImportProfileTones)
                                Button.isEnabled ((not <| String.IsNullOrWhiteSpace state.Config.ProfilePath) && IO.File.Exists state.Config.ProfilePath)
                            ]

                            ListBox.create [
                                ListBox.margin 2.
                                ListBox.dataItems state.Project.Tones
                                match state.SelectedTone with
                                | Some t -> ListBox.selectedItem t
                                | None -> ()
                                ListBox.onSelectedItemChanged (fun item ->
                                    match item with
                                    | :? Tone as t -> dispatch (ToneSelected (Some t))
                                    | _ ->  dispatch (ToneSelected None))
                                ListBox.onKeyDown (fun k -> if k.Key = Key.Delete then dispatch DeleteTone)
                            ]
                        ]
                    ]

                    StackPanel.create [
                        Grid.column 2
                        Grid.row 1
                        StackPanel.margin 8.
                        StackPanel.children [
                            TextBlock.create [
                                TextBlock.text "Select a tone to edit its details"
                                TextBlock.horizontalAlignment HorizontalAlignment.Center
                            ]
                        ]
                    ]
                ]
            ]

            match state.Overlay with
            | NoOverlay -> ()
            | _ ->
                DockPanel.create [
                    DockPanel.background "#77000000"
                    DockPanel.children [
                        Border.create [
                            Border.padding 20.0
                            Border.cornerRadius 5.0
                            Border.horizontalAlignment HorizontalAlignment.Center
                            Border.verticalAlignment VerticalAlignment.Center
                            Border.background "#444444"
                            Border.child (
                                match state.Overlay with
                                | ErrorMessage msg ->
                                    StackPanel.create [
                                        StackPanel.spacing 8.
                                        StackPanel.children [
                                            TextBlock.create [
                                                TextBlock.fontSize 16.
                                                TextBlock.text msg
                                            ]
                                            Button.create [
                                                Button.fontSize 18.
                                                Button.padding (80., 10.)
                                                Button.horizontalAlignment HorizontalAlignment.Center
                                                Button.content "OK"
                                                Button.onClick (fun _ -> CloseOverlay |> dispatch)
                                            ]
                                        ]
                                    ]
                                | SelectImportTones tones ->
                                    StackPanel.create [
                                        StackPanel.spacing 8.
                                        StackPanel.children [
                                            ListBox.create [
                                                ListBox.name "tonesListBox"
                                                ListBox.dataItems tones
                                                // Multiple selection mode is broken in Avalonia 0.9
                                                // https://github.com/AvaloniaUI/Avalonia/issues/3497
                                                ListBox.selectionMode SelectionMode.Single
                                                ListBox.maxHeight 300.
                                                ListBox.onSelectedItemChanged (ImportTonesChanged >> dispatch)
                                            ]
                                            StackPanel.create [
                                                StackPanel.orientation Orientation.Horizontal
                                                StackPanel.spacing 8.
                                                StackPanel.children [
                                                    Button.create [
                                                        Button.fontSize 16.
                                                        Button.padding (50., 10.)
                                                        Button.horizontalAlignment HorizontalAlignment.Center
                                                        Button.content "Import"
                                                        Button.onClick (fun _ -> ImportSelectedTones |> dispatch)
                                                    ]
                                                    Button.create [
                                                        Button.fontSize 16.
                                                        Button.padding (50., 10.)
                                                        Button.horizontalAlignment HorizontalAlignment.Center
                                                        Button.content "Cancel"
                                                        Button.onClick (fun _ -> CloseOverlay |> dispatch)
                                                    ]
                                                ]
                                            ]
                                        ]
                                    ]
                                | NoOverlay -> failwith "This can not happen."
                            )
                        ]
                    ]
                ]
        ]
    ]
