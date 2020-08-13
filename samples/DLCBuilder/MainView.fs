module DLCBuilder.MainView

open Rocksmith2014.Common
open Rocksmith2014.Common.Manifest
open Rocksmith2014.DLCProject
open Rocksmith2014.XML
open Elmish
open System.Xml
open System
open Avalonia
open Avalonia.Media.Imaging
open Avalonia.Media
open Avalonia.Input
open Avalonia.Platform
open Avalonia.Controls
open Avalonia.Controls.Shapes
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open Media

type State =
    { Project : DLCProject
      Config : Configuration
      CoverArt : Bitmap option
      SelectedArrangement : Arrangement option
      ShowSortFields : bool
      ShowJapaneseFields : bool }

let init () =
    let assets = AvaloniaLocator.Current.GetService<IAssetLoader>()
    let coverArt = new Bitmap(assets.Open(new Uri("avares://DLCBuilder/placeholder.png")))
    { Project = DLCProject.Empty
      Config = Configuration.Empty
      CoverArt = Some coverArt
      SelectedArrangement = None
      ShowSortFields = false
      ShowJapaneseFields = false }, Cmd.none

type Msg =
    | SelectOpenArrangement
    | SelectCoverArt
    | SelectAudioFile
    | AddArrangements of files : string[] option
    | AddCoverArt of fileName : string option
    | AddAudioFile of fileName : string option
    | ArrangementSelected of selected : Arrangement option
    | DeleteArrangement
    | ImportTones
    | CreatePreviewAudio
    | ShowSortFields of shown : bool
    | ShowJapaneseFields of shown : bool

let private loadArrangement (fileName: string) =
    use reader = XmlReader.Create(fileName)
    reader.MoveToContent() |> ignore

    match reader.LocalName with
    | "song" ->
        let metadata = MetaData.Read fileName
        let arr =
            { XML = fileName
              Name = ArrangementName.Parse(metadata.Arrangement)
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
            let lyricFile = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(fileName), "lyrics.dds")
            if isJapanese && System.IO.File.Exists lyricFile then Some lyricFile else None
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

let update (msg: Msg) (state: State) =
    match msg with
    | SelectOpenArrangement ->
        let dialog = Dialogs.openMultiFileDialog "Select Arrangement" Dialogs.xmlFileFilter
        state, Cmd.OfAsync.perform dialog None AddArrangements

    | SelectCoverArt ->
        let dialog = Dialogs.openFileDialog "Select Cover Art" Dialogs.imgFileFilter
        state, Cmd.OfAsync.perform dialog None AddCoverArt

    | SelectAudioFile ->
        let dialog = Dialogs.openFileDialog "Select Audio File" Dialogs.audioFileFilters
        state, Cmd.OfAsync.perform dialog None AddAudioFile

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

        let shouldInclude state arr =
            match arr with
            | Showlights _ when state |> List.exists (function Showlights _ -> true | _ -> false) -> false
            | Instrumental _ when (state |> List.choose (function Instrumental _ -> Some 1 | _ -> None)).Length = 5 -> false
            | Vocals _ when (state |> List.choose (function Vocals _ -> Some 1 | _ -> None)).Length = 2 -> false
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
                                ArtistName = SortableString.Create (md.ArtistName, md.ArtistNameSort)
                                Title = SortableString.Create (md.Title, md.TitleSort)
                                AlbumName = SortableString.Create (md.AlbumName, md.AlbumNameSort)
                                Year = md.AlbumYear
                                Arrangements = arrangements } }, Cmd.none
        | None ->
            { state with Project = { state.Project with Arrangements = arrangements } }, Cmd.none

    | ArrangementSelected selected -> { state with SelectedArrangement = selected }, Cmd.none

    | DeleteArrangement ->
        let arrangements =
            match state.SelectedArrangement with
            | None -> state.Project.Arrangements
            | Some selected -> List.remove selected state.Project.Arrangements
        { state with Project = { state.Project with Arrangements = arrangements } }, Cmd.none

    | ImportTones ->
        let result = Profile.importTones state.Config.ProfilePath
        match result with
        | Ok toneArray ->
            let tones =
                // TODO: Display UI to select tones
                toneArray.[0..3]
                |> Array.toList
                |> List.map (fun x ->
                    if isNull x.ToneDescriptors || x.ToneDescriptors.Length = 0 then
                        let descs =
                            ToneDescriptor.getDescriptionsOrDefault x.Name
                            |> Array.map (fun x -> x.UIName)
                        { x with ToneDescriptors = descs; SortOrder = Nullable(); NameSeparator = " - " }
                    else
                        { x with SortOrder = Nullable(); NameSeparator = " - " })
            { state with Project = { state.Project with Tones = tones } }, Cmd.none
        | Error _ ->
            state, Cmd.none

    | CreatePreviewAudio ->
        // TODO: UI to select preview start time
        Audio.Tools.createPreview state.Project.AudioFile.Path (TimeSpan.FromSeconds 22.)
        state, Cmd.none

    | ShowSortFields shown ->
        { state with ShowSortFields = shown }, Cmd.none

    | ShowJapaneseFields shown ->
        { state with ShowJapaneseFields = shown }, Cmd.none
        
    | AddArrangements None | AddCoverArt None | AddAudioFile None ->
        state, Cmd.none

let view (state: State) (dispatch) =
    Grid.create [
        Grid.columnDefinitions "2*,*,2*"
        Grid.rowDefinitions "*,*"
        //Grid.showGridLines true
        Grid.children [
            DockPanel.create [
                Grid.rowSpan 2
                DockPanel.margin (8.0, 8.0, 0.0, 8.0)
                DockPanel.children [
                    Image.create [
                        DockPanel.dock Dock.Top
                        Image.source (Option.toObj state.CoverArt)
                        Image.width 200.
                        Image.height 200.
                        Image.onTapped (fun _ -> dispatch SelectCoverArt)
                        Image.cursor (Cursor(StandardCursorType.Hand))
                        ToolTip.tip "Click to select a cover art file.\nThe image should be a square, preferably 512x512."
                    ]

                    Grid.create [
                        Grid.columnDefinitions "3*,*"
                        Grid.rowDefinitions "35,35,35,35,35,40,40,*"
                        //Grid.showGridLines true
                        Grid.children [
                            TextBox.create [
                                Grid.column 0
                                Grid.row 0
                                TextBox.watermark "DLC Key"
                                TextBox.text state.Project.DLCKey
                                ToolTip.tip "DLC Key"
                            ]

                            TextBox.create [
                                Grid.column 1
                                Grid.row 0
                                TextBox.horizontalAlignment HorizontalAlignment.Left
                                TextBox.width 60.
                                TextBox.watermark "Version"
                                TextBox.text state.Project.Version
                                ToolTip.tip "Version"
                            ]

                            TextBox.create [
                                Grid.column 0
                                Grid.row 1
                                TextBox.watermark "Artist Name"
                                TextBox.text state.Project.ArtistName.Value
                                ToolTip.tip "Artist Name"
                            ]

                            TextBox.create [
                                Grid.column 0
                                Grid.row 1
                                TextBox.watermark "Artist Name Sort"
                                TextBox.text state.Project.ArtistName.SortValue
                                TextBox.isVisible state.ShowSortFields
                                ToolTip.tip "Artist Name Sort"
                            ]

                            TextBox.create [
                                Grid.column 0
                                Grid.row 1
                                TextBox.watermark "Japanese Artist Name"
                                TextBox.text (defaultArg state.Project.JapaneseArtistName String.Empty)
                                TextBox.isVisible (state.ShowJapaneseFields)
                                ToolTip.tip "Japanese Artist Name"
                            ]

                            TextBox.create [
                                Grid.column 0
                                Grid.row 2
                                TextBox.watermark "Title"
                                TextBox.text state.Project.Title.Value
                                ToolTip.tip "Title"
                            ]

                            TextBox.create [
                                Grid.column 0
                                Grid.row 2
                                TextBox.watermark "Title Sort"
                                TextBox.text state.Project.Title.SortValue
                                TextBox.isVisible state.ShowSortFields
                                ToolTip.tip "Title Sort"
                            ]

                            TextBox.create [
                                Grid.column 0
                                Grid.row 2
                                TextBox.watermark "Japanese Title"
                                TextBox.text (defaultArg state.Project.JapaneseTitle String.Empty)
                                TextBox.isVisible (state.ShowJapaneseFields)
                                ToolTip.tip "Japanese Title"
                            ]

                            TextBox.create [
                                Grid.column 0
                                Grid.row 3
                                TextBox.watermark "Album Name"
                                TextBox.text state.Project.AlbumName.Value
                                ToolTip.tip "Album Name"
                            ]

                            TextBox.create [
                                Grid.column 0
                                Grid.row 3
                                TextBox.watermark "Album Name Sort"
                                TextBox.text state.Project.AlbumName.SortValue
                                TextBox.isVisible state.ShowSortFields
                                ToolTip.tip "Album Name Sort"
                            ]

                            TextBox.create [
                                Grid.column 1
                                Grid.row 3
                                TextBox.horizontalAlignment HorizontalAlignment.Left
                                TextBox.width 60.
                                TextBox.watermark "Year"
                                TextBox.text (string state.Project.Year)
                                ToolTip.tip "Year"
                            ]

                            StackPanel.create [
                                Grid.columnSpan 2
                                Grid.row 4
                                StackPanel.orientation Orientation.Horizontal
                                StackPanel.horizontalAlignment HorizontalAlignment.Center
                                StackPanel.children [
                                    CheckBox.create [
                                        CheckBox.content "Show Sort Fields"
                                        CheckBox.isChecked (state.ShowSortFields)
                                        CheckBox.onChecked (fun _ -> dispatch (ShowSortFields true))
                                        CheckBox.onUnchecked (fun _ -> dispatch (ShowSortFields false))
                                    ]
                                    CheckBox.create [
                                          CheckBox.content "Show Japanese Fields"
                                          CheckBox.isChecked (state.ShowJapaneseFields)
                                          CheckBox.onChecked (fun _ -> dispatch (ShowJapaneseFields true))
                                          CheckBox.onUnchecked (fun _ -> dispatch (ShowJapaneseFields false))
                                      ]
                                ]
                            ]

                            DockPanel.create [
                                Grid.column 0
                                Grid.row 5
                                DockPanel.children [
                                    Button.create [
                                        DockPanel.dock Dock.Right
                                        Button.margin (0.0, 4.0, 4.0, 4.0)
                                        Button.padding (10.0, 0.0)
                                        Button.content "..."
                                        Button.onClick (fun _ -> dispatch SelectAudioFile)
                                        ToolTip.tip "Select Audio File"
                                    ]
                                    TextBox.create [
                                        TextBox.margin (4.0, 4.0, 0.0, 4.0)
                                        TextBox.watermark "Audio File"
                                        TextBox.text (System.IO.Path.GetFileName state.Project.AudioFile.Path)
                                        ToolTip.tip "Audio File"
                                    ]
                                ]
                            ]

                            NumericUpDown.create [
                                Grid.column 1
                                Grid.row 5
                                NumericUpDown.margin (2.0, 2.0, 2.0, 2.0)
                                NumericUpDown.horizontalAlignment HorizontalAlignment.Left
                                NumericUpDown.value state.Project.AudioFile.Volume
                                NumericUpDown.formatString "F1"
                                ToolTip.tip "Audio Volume (dB)"
                            ]
                
                            Button.create [
                                let previewExists =
                                    if String.IsNullOrWhiteSpace state.Project.AudioFile.Path then false
                                    else IO.File.Exists(IO.Path.Combine(IO.Path.GetDirectoryName(state.Project.AudioFile.Path), IO.Path.GetFileNameWithoutExtension(state.Project.AudioFile.Path) + "_preview.wav"))
                                Grid.column 0
                                Grid.row 6
                                Button.horizontalAlignment HorizontalAlignment.Center
                                Button.content (
                                    StackPanel.create [
                                        StackPanel.orientation Orientation.Horizontal
                                        StackPanel.children [
                                            Path.create [
                                                Path.fill Brushes.Gray
                                                Path.data (if previewExists then Icons.check else Icons.x)
                                            ]
                                            TextBlock.create [
                                                TextBlock.verticalAlignment VerticalAlignment.Center
                                                TextBlock.text " Create Preview Audio"
                                            ]
                                        ]
                                    ]
                                )
                                Button.isEnabled (state.Project.AudioFile.Path.EndsWith(".wav"))
                                Button.onClick (fun _ -> dispatch CreatePreviewAudio)
                                ToolTip.tip (if previewExists then "Preview audio file exists, click to create a new one." else "Preview audio file does not exist, click to create one.")
                            ]

                            NumericUpDown.create [
                                Grid.column 1
                                Grid.row 6
                                NumericUpDown.margin (2.0, 2.0, 2.0, 2.0)
                                NumericUpDown.horizontalAlignment HorizontalAlignment.Left
                                NumericUpDown.value state.Project.AudioPreviewFile.Volume
                                NumericUpDown.formatString "F1"
                                ToolTip.tip "Preview Audio Volume (dB)"
                            ]

                            StackPanel.create [
                                StackPanel.verticalAlignment VerticalAlignment.Center
                                StackPanel.horizontalAlignment HorizontalAlignment.Center
                                Grid.columnSpan 2
                                Grid.row 7
                                StackPanel.children [
                                    Button.create [
                                        Button.padding (15., 8.)
                                        Button.margin 4.
                                        Button.fontSize 16.
                                        Button.content "Build Test"
                                    ]
                                    Button.create [
                                        Button.padding (15., 8.)
                                        Button.margin 4.
                                        Button.fontSize 16.
                                        Button.content "Build Release"
                                    ]
                                    StackPanel.create [
                                        StackPanel.orientation Orientation.Horizontal
                                        StackPanel.children [
                                            TextBlock.create [
                                                TextBlock.verticalAlignment VerticalAlignment.Center
                                                TextBlock.text "Platforms: "
                                            ]
                                            CheckBox.create [
                                                CheckBox.margin 2.
                                                CheckBox.content "PC"
                                                CheckBox.isChecked true
                                            ]
                                            CheckBox.create [
                                                CheckBox.margin 2.
                                                CheckBox.content "Mac"
                                            ]
                                        ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]

            DockPanel.create [
                Grid.column 1
                DockPanel.children [
                    Button.create [
                        DockPanel.dock Dock.Top
                        Button.margin (5.0)
                        Button.padding (15.0, 5.0)
                        Button.horizontalAlignment HorizontalAlignment.Left
                        Button.content "Add Arrangement"
                        Button.onClick (fun _ -> dispatch SelectOpenArrangement)
                    ]

                    ListBox.create [
                        ListBox.dataItems state.Project.Arrangements
                        match state.SelectedArrangement with
                        | Some a -> ListBox.selectedItem a
                        | None -> ()
                        ListBox.onSelectedItemChanged (fun item ->
                            match item with
                            | :? Arrangement as arr -> dispatch (ArrangementSelected (Some arr))
                            | _ ->  dispatch (ArrangementSelected None)
                        )
                        ListBox.onKeyDown (fun k -> if k.Key = Key.Delete then dispatch DeleteArrangement)
                    ]
                ]
            ]

            StackPanel.create [
                Grid.column 2
                StackPanel.margin 8.
                StackPanel.children [
                    match state.SelectedArrangement with
                    | Some arr ->
                        TextBlock.create [
                            TextBlock.fontSize 17.
                            TextBlock.text (Arrangement.getName arr false)
                            TextBlock.horizontalAlignment HorizontalAlignment.Center
                        ]
                        TextBlock.create [
                            TextBlock.text (System.IO.Path.GetFileName (Arrangement.getFile arr))
                            TextBlock.horizontalAlignment HorizontalAlignment.Center
                        ]
                        match arr with
                        | Instrumental i ->
                            StackPanel.create [
                                StackPanel.orientation Orientation.Horizontal
                                StackPanel.children [
                                    TextBlock.create [
                                        TextBlock.verticalAlignment VerticalAlignment.Center
                                        TextBlock.text "Name: "
                                        TextBlock.horizontalAlignment HorizontalAlignment.Center
                                    ]
                                    ComboBox.create [
                                        ComboBox.width 100.
                                        ComboBox.dataItems (Enum.GetNames(typeof<ArrangementName>))
                                        ComboBox.selectedItem (string i.Name)
                                    ]
                                ]
                            ]
                            StackPanel.create [
                                StackPanel.orientation Orientation.Horizontal
                                StackPanel.children [
                                    TextBlock.create [
                                        TextBlock.verticalAlignment VerticalAlignment.Center
                                        TextBlock.text "Ordering: "
                                        TextBlock.horizontalAlignment HorizontalAlignment.Center
                                    ]
                                    RadioButton.create [
                                        RadioButton.groupName "Ordering"
                                        RadioButton.content "Main"
                                        RadioButton.isChecked (i.Priority = ArrangementPriority.Main)
                                    ]
                                    RadioButton.create [
                                        RadioButton.groupName "Ordering"
                                        RadioButton.content "Alternative"
                                        RadioButton.isChecked (i.Priority = ArrangementPriority.Alternative)
                                    ]
                                    RadioButton.create [
                                        RadioButton.groupName "Ordering"
                                        RadioButton.content "Bonus"
                                        RadioButton.isChecked (i.Priority = ArrangementPriority.Bonus)
                                    ]
                                ]
                            ]
                            StackPanel.create [
                                StackPanel.orientation Orientation.Horizontal
                                StackPanel.isVisible (i.Name = ArrangementName.Combo)
                                StackPanel.children [
                                    TextBlock.create [
                                        TextBlock.verticalAlignment VerticalAlignment.Center
                                        TextBlock.text "Route: "
                                        TextBlock.horizontalAlignment HorizontalAlignment.Center
                                    ]
                                    RadioButton.create [
                                        RadioButton.groupName "RouteMask"
                                        RadioButton.content "Lead"
                                        RadioButton.isChecked (i.RouteMask = RouteMask.Lead)
                                    ]
                                    RadioButton.create [
                                        RadioButton.groupName "RouteMask"
                                        RadioButton.content "Rhythm"
                                        RadioButton.isChecked (i.RouteMask = RouteMask.Rhythm)
                                    ]
                                ]
                            ]
                            CheckBox.create [
                                CheckBox.content "Picked"
                                CheckBox.isVisible (i.Name = ArrangementName.Bass)
                                CheckBox.isChecked (i.BassPicked)
                            ]
                            StackPanel.create [
                                StackPanel.orientation Orientation.Horizontal
                                StackPanel.children [
                                    TextBlock.create [
                                        TextBlock.verticalAlignment VerticalAlignment.Center
                                        TextBlock.text "Tuning: "
                                        TextBlock.horizontalAlignment HorizontalAlignment.Center
                                    ]
                                    TextBox.create [
                                        TextBox.width 30.
                                        TextBox.text (string i.Tuning.[0])
                                    ]
                                    TextBox.create [
                                        TextBox.width 30.
                                        TextBox.text (string i.Tuning.[1])
                                    ]
                                    TextBox.create [
                                        TextBox.width 30.
                                        TextBox.text (string i.Tuning.[2])
                                    ]
                                    TextBox.create [
                                        TextBox.width 30.
                                        TextBox.text (string i.Tuning.[3])
                                    ]
                                    TextBox.create [
                                        TextBox.width 30.
                                        TextBox.text (string i.Tuning.[4])
                                    ]
                                    TextBox.create [
                                        TextBox.width 30.
                                        TextBox.text (string i.Tuning.[5])
                                    ]
                                ]
                            ]
                            StackPanel.create [
                                StackPanel.orientation Orientation.Horizontal
                                StackPanel.children [
                                    TextBlock.create [
                                        TextBlock.verticalAlignment VerticalAlignment.Center
                                        TextBlock.text "Cent Offset: "
                                        TextBlock.horizontalAlignment HorizontalAlignment.Center
                                    ]
                                    TextBox.create [
                                        TextBox.width 30.
                                        TextBox.text (string i.CentOffset)
                                    ]
                                ]
                            ]
                            StackPanel.create [
                                StackPanel.orientation Orientation.Horizontal
                                StackPanel.isVisible state.Config.ShowAdvanced
                                StackPanel.children [
                                    TextBlock.create [
                                        TextBlock.verticalAlignment VerticalAlignment.Center
                                        TextBlock.text "Scroll Speed: "
                                        TextBlock.horizontalAlignment HorizontalAlignment.Center
                                    ]
                                    NumericUpDown.create [
                                        NumericUpDown.increment 0.1
                                        NumericUpDown.width 65.
                                        NumericUpDown.maximum 5.0
                                        NumericUpDown.minimum 0.5
                                        NumericUpDown.formatString "F1"
                                        NumericUpDown.value i.ScrollSpeed
                                    ]
                                ]
                            ]
                            StackPanel.create [
                                StackPanel.orientation Orientation.Horizontal
                                StackPanel.isVisible state.Config.ShowAdvanced
                                StackPanel.children [
                                    TextBlock.create [
                                        TextBlock.verticalAlignment VerticalAlignment.Center
                                        TextBlock.text "Master ID: "
                                        TextBlock.horizontalAlignment HorizontalAlignment.Center
                                    ]
                                    TextBox.create [
                                        TextBox.width 150.
                                        TextBox.text (string i.MasterID)
                                    ]
                                ]
                            ]
                            StackPanel.create [
                                StackPanel.orientation Orientation.Horizontal
                                StackPanel.isVisible state.Config.ShowAdvanced
                                StackPanel.children [
                                    TextBlock.create [
                                        TextBlock.verticalAlignment VerticalAlignment.Center
                                        TextBlock.text "Persistent ID: "
                                        TextBlock.horizontalAlignment HorizontalAlignment.Center
                                    ]
                                    TextBox.create [
                                        TextBox.width 250.
                                        TextBox.text (i.PersistentID.ToString("N"))
                                    ]
                                ]
                            ]

                        | Vocals v ->
                            CheckBox.create [
                                CheckBox.content "Japanese"
                                CheckBox.isChecked (v.Japanese)
                            ]
                            StackPanel.create [
                                StackPanel.orientation Orientation.Horizontal
                                StackPanel.children [
                                    TextBlock.create [
                                        TextBlock.verticalAlignment VerticalAlignment.Center
                                        TextBlock.text "Custom Font: "
                                        TextBlock.horizontalAlignment HorizontalAlignment.Center
                                    ]
                                    TextBox.create [
                                        TextBox.width 150.
                                        TextBox.text (defaultArg v.CustomFont String.Empty)
                                    ]
                                    Button.create [
                                        Button.content "..."
                                    ]
                                ]
                            ]

                        | Showlights _ -> ()

                    | None ->
                        TextBlock.create [
                            TextBlock.text "Select an arrangement to edit its details"
                            TextBlock.horizontalAlignment HorizontalAlignment.Center
                        ]
                ]
            ]

            DockPanel.create [
                Grid.column 1
                Grid.row 1
                DockPanel.children [
                    Button.create [
                        DockPanel.dock Dock.Top
                        Button.margin (5.0)
                        Button.padding (15.0, 5.0)
                        Button.horizontalAlignment HorizontalAlignment.Left
                        Button.content "Import Tones"
                        Button.onClick (fun _ -> dispatch ImportTones)
                    ]

                    ListBox.create [
                        ListBox.dataItems state.Project.Tones
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
