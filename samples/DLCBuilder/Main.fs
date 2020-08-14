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
      SelectedTone : Tone option
      ShowSortFields : bool
      ShowJapaneseFields : bool }

let init () =
    let assets = AvaloniaLocator.Current.GetService<IAssetLoader>()
    let coverArt = new Bitmap(assets.Open(new Uri("avares://DLCBuilder/placeholder.png")))

    { Project = DLCProject.Empty
      Config = Configuration.Default
      CoverArt = Some coverArt
      SelectedArrangement = None
      SelectedTone = None
      ShowSortFields = false
      ShowJapaneseFields = false }, Cmd.none

type Msg =
    | SelectOpenArrangement
    | SelectCoverArt
    | SelectAudioFile
    | SelectCustomFont
    | AddArrangements of files : string[] option
    | AddCoverArt of fileName : string option
    | AddAudioFile of fileName : string option
    | AddCustomFontFile of fileName : string option
    | ArrangementSelected of selected : Arrangement option
    | ToneSelected of selected : Tone option
    | DeleteArrangement
    | DeleteTone
    | ImportTones
    | CreatePreviewAudio
    | ShowSortFields of shown : bool
    | ShowJapaneseFields of shown : bool
    | EditInstrumental of edit : (Instrumental -> Instrumental)
    | EditVocals of edit : (Vocals -> Vocals)

let private loadArrangement (fileName: string) =
    let rootName =
        using (XmlReader.Create(fileName))
              (fun reader -> reader.MoveToContent() |> ignore; reader.LocalName)

    match rootName with
    | "song" ->
        let metadata = XML.MetaData.Read fileName
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
                                ArtistName = SortableString.Create (md.ArtistName, md.ArtistNameSort)
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
        
    | AddArrangements None | AddCoverArt None | AddAudioFile None | AddCustomFontFile None ->
        state, Cmd.none

let instrumentalDetailsView (state: State) dispatch (i: Instrumental) =
    Grid.create [
        //Grid.showGridLines true
        Grid.margin (0.0, 4.0)
        Grid.columnDefinitions "*,3*"
        Grid.rowDefinitions "*,*,*,*,*,*,*,*,*,*"
        Grid.children [
            TextBlock.create [
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text "Name: "
                TextBlock.horizontalAlignment HorizontalAlignment.Center
            ]

            ComboBox.create [
                Grid.column 1
                ComboBox.horizontalAlignment HorizontalAlignment.Left
                ComboBox.margin 4.
                ComboBox.width 100.
                ComboBox.dataItems (Enum.GetValues(typeof<ArrangementName>))
                ComboBox.selectedItem i.Name
                ComboBox.onSelectedItemChanged (fun item ->
                    match item with
                    | :? ArrangementName as item ->
                        fun (a:Instrumental) ->
                            let routeMask =
                                match item with
                                | ArrangementName.Lead -> RouteMask.Lead
                                | ArrangementName.Rhythm | ArrangementName.Combo -> RouteMask.Rhythm
                                | ArrangementName.Bass -> RouteMask.Bass
                                | _ -> failwith "Unlikely failure."
                            { a with Name = item; RouteMask = routeMask }
                        |> EditInstrumental |> dispatch
                    | _ -> ()
                )
            ]

            TextBlock.create [
                Grid.row 1
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text "Priority: "
                TextBlock.horizontalAlignment HorizontalAlignment.Center
            ]

            StackPanel.create [
                Grid.column 1
                Grid.row 1
                StackPanel.orientation Orientation.Horizontal
                StackPanel.margin 4.
                StackPanel.children [
                    for priority in [ ArrangementPriority.Main; ArrangementPriority.Alternative; ArrangementPriority.Bonus ] ->
                        RadioButton.create [
                            RadioButton.margin (2.0, 0.0)
                            RadioButton.groupName "Priority"
                            RadioButton.content (string priority)
                            RadioButton.isChecked (i.Priority = priority)
                            RadioButton.onChecked (fun _ -> (fun a -> { a with Priority = priority }) |> EditInstrumental |> dispatch)
                        ]
                ]
            ]

            TextBlock.create [
                Grid.row 2
                TextBlock.isVisible (i.Name = ArrangementName.Combo)
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text "Path: "
                TextBlock.horizontalAlignment HorizontalAlignment.Center
            ]

            StackPanel.create [
                Grid.column 1
                Grid.row 2
                StackPanel.margin 4.
                StackPanel.orientation Orientation.Horizontal
                StackPanel.isVisible (i.Name = ArrangementName.Combo)
                StackPanel.children [
                    for mask in [ RouteMask.Lead; RouteMask.Rhythm ] ->
                        RadioButton.create [
                            RadioButton.margin (2.0, 0.0)
                            RadioButton.groupName "RouteMask"
                            RadioButton.content (string mask)
                            RadioButton.isChecked (i.RouteMask = mask)
                            RadioButton.onChecked (fun _ -> (fun a -> { a with RouteMask = mask }) |> EditInstrumental |> dispatch)
                        ]
                ]
            ]

            TextBlock.create [
                Grid.row 3
                TextBlock.isVisible (i.Name = ArrangementName.Bass)
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text "Picked: "
                TextBlock.horizontalAlignment HorizontalAlignment.Center
            ]

            CheckBox.create [
                Grid.column 1
                Grid.row 3
                CheckBox.margin 4.
                CheckBox.isVisible (i.Name = ArrangementName.Bass)
                CheckBox.isChecked i.BassPicked
                CheckBox.onChecked (fun _ -> (fun a -> { a with BassPicked = true }) |> EditInstrumental |> dispatch)
                CheckBox.onUnchecked (fun _ -> (fun a -> { a with BassPicked = false }) |> EditInstrumental |> dispatch)
            ]

            TextBlock.create [
                Grid.row 4
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text "Tuning: "
                TextBlock.horizontalAlignment HorizontalAlignment.Center
            ]

            StackPanel.create [
                Grid.column 1
                Grid.row 4
                StackPanel.orientation Orientation.Horizontal
                StackPanel.children [
                    for str in 0..5 ->
                        TextBox.create [
                            TextBox.width 30.
                            TextBox.text (string i.Tuning.[str])
                            TextBox.onLostFocus (fun arg ->
                                let txtBox = arg.Source :?> TextBox
                                let success, newTuning = Int16.TryParse(txtBox.Text)
                                if success then
                                    fun a ->
                                        let tuning =
                                            a.Tuning
                                            |> Array.mapi (fun i old -> if i = str then newTuning else old)
                                        { a with Tuning = tuning }
                                    |> EditInstrumental |> dispatch
                            )
                        ]
                ]
            ]

            TextBlock.create [
                Grid.row 5
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text "Cent Offset: "
                TextBlock.horizontalAlignment HorizontalAlignment.Center
            ]

            NumericUpDown.create [
                Grid.column 1
                Grid.row 5
                NumericUpDown.margin 4.
                NumericUpDown.horizontalAlignment HorizontalAlignment.Left
                NumericUpDown.width 65.
                NumericUpDown.value (float i.CentOffset)
                NumericUpDown.minimum -500.0
                NumericUpDown.maximum 500.0
                NumericUpDown.increment 1.0
                NumericUpDown.formatString "F0"
                NumericUpDown.onValueChanged (fun value -> (fun a -> { a with CentOffset = int value }) |> EditInstrumental |> dispatch)
            ]

            TextBlock.create [
                Grid.row 6
                TextBlock.isVisible state.Config.ShowAdvanced
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text "Scroll Speed: "
                TextBlock.horizontalAlignment HorizontalAlignment.Center
            ]

            NumericUpDown.create [
                Grid.column 1
                Grid.row 6
                NumericUpDown.margin 4.
                NumericUpDown.isVisible state.Config.ShowAdvanced
                NumericUpDown.horizontalAlignment HorizontalAlignment.Left
                NumericUpDown.increment 0.1
                NumericUpDown.width 65.
                NumericUpDown.maximum 5.0
                NumericUpDown.minimum 0.5
                NumericUpDown.formatString "F1"
                NumericUpDown.value i.ScrollSpeed
                NumericUpDown.onValueChanged (fun value -> (fun a -> { a with ScrollSpeed = value }) |> EditInstrumental |> dispatch)
            ]

            TextBlock.create [
                Grid.row 7
                TextBlock.isVisible state.Config.ShowAdvanced
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text "Master ID: "
                TextBlock.horizontalAlignment HorizontalAlignment.Center
            ]

            TextBox.create [
                Grid.column 1
                Grid.row 7
                TextBox.isVisible state.Config.ShowAdvanced
                TextBox.horizontalAlignment HorizontalAlignment.Stretch
                TextBox.text (string i.MasterID)
                TextBox.onLostFocus (fun arg ->
                    let txtBox = arg.Source :?> TextBox
                    let success, masterID = Int32.TryParse(txtBox.Text)
                    if success then
                        (fun (a:Instrumental) -> { a with MasterID = masterID }) |> EditInstrumental |> dispatch
                )
            ]

            TextBlock.create [
                Grid.row 8
                TextBlock.isVisible state.Config.ShowAdvanced
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text "Persistent ID: "
                TextBlock.horizontalAlignment HorizontalAlignment.Center
            ]

            TextBox.create [
                Grid.column 1
                Grid.row 8
                TextBox.isVisible state.Config.ShowAdvanced
                TextBox.horizontalAlignment HorizontalAlignment.Stretch
                TextBox.text (i.PersistentID.ToString("N"))
                TextBox.onLostFocus (fun arg ->
                    let txtBox = arg.Source :?> TextBox
                    let success, perID = Guid.TryParse(txtBox.Text)
                    if success then
                        (fun (a:Instrumental) -> { a with PersistentID = perID }) |> EditInstrumental |> dispatch
                )
            ]
        ]
    ]

let vocalsDetailsView state dispatch v =
    Grid.create [
        Grid.columnDefinitions "*,3*"
        Grid.rowDefinitions "*,*,*,*"
        Grid.margin (0.0, 4.0)
        //Grid.showGridLines true
        Grid.children [
            TextBlock.create [
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
                TextBlock.text "Japanese: "
            ]
            CheckBox.create [
                Grid.column 1
                CheckBox.margin 4.0
                CheckBox.isChecked v.Japanese
                CheckBox.onChecked (fun _ -> (fun v -> { v with Japanese = true }) |> EditVocals |> dispatch)
                CheckBox.onUnchecked (fun _ -> (fun v -> { v with Japanese = false }) |> EditVocals |> dispatch)
            ]

            // Custom font
            TextBlock.create [
                Grid.row 1
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
                TextBlock.text "Custom Font: "
            ]
            DockPanel.create [
                Grid.column 1
                Grid.row 1
                DockPanel.children [
                    Button.create [
                        DockPanel.dock Dock.Right
                        Button.margin (0.0, 4.0, 4.0, 4.0)
                        Button.content "X"
                        Button.isVisible (Option.isSome v.CustomFont)
                        Button.onClick (fun _ -> (fun v -> { v with CustomFont = None }) |> EditVocals |> dispatch)
                        ToolTip.tip "Click to remove the custom font from the arrangement."
                    ]
                    Button.create [
                        DockPanel.dock Dock.Right
                        Button.margin (0.0, 4.0, 4.0, 4.0)
                        Button.content "..."
                        Button.onClick (fun _ -> SelectCustomFont |> dispatch)
                        ToolTip.tip "Click to select a custom font file."
                    ]
                    TextBlock.create [
                        TextBlock.verticalAlignment VerticalAlignment.Center
                        TextBlock.horizontalAlignment HorizontalAlignment.Center
                        TextBlock.text (v.CustomFont |> Option.map IO.Path.GetFileName |> Option.defaultValue "None")
                    ]
                ]
            ]

            TextBlock.create [
                Grid.row 2
                TextBlock.isVisible state.Config.ShowAdvanced
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
                TextBlock.text "Master ID: "
            ]

            TextBox.create [
                Grid.column 1
                Grid.row 2
                TextBox.isVisible state.Config.ShowAdvanced
                TextBox.horizontalAlignment HorizontalAlignment.Stretch
                TextBox.text (string v.MasterID)
                TextBox.onLostFocus (fun arg ->
                    let txtBox = arg.Source :?> TextBox
                    let success, masterID = Int32.TryParse(txtBox.Text)
                    if success then
                        (fun (a:Vocals) -> { a with MasterID = masterID }) |> EditVocals |> dispatch
                )
            ]

            TextBlock.create [
                Grid.row 3
                TextBlock.isVisible state.Config.ShowAdvanced
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
                TextBlock.text "Persistent ID: "
            ]

            TextBox.create [
                Grid.column 1
                Grid.row 3
                TextBox.isVisible state.Config.ShowAdvanced
                TextBox.horizontalAlignment HorizontalAlignment.Stretch
                TextBox.text (v.PersistentID.ToString("N"))
                TextBox.onLostFocus (fun arg ->
                    let txtBox = arg.Source :?> TextBox
                    let success, perID = Guid.TryParse(txtBox.Text)
                    if success then
                        (fun (a:Vocals) -> { a with PersistentID = perID }) |> EditVocals |> dispatch
                )
            ]
        ]
    ]

let view (state: State) dispatch =
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
                                TextBox.width 65.
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
                                TextBox.width 65.
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
                                        TextBox.text (IO.Path.GetFileName state.Project.AudioFile.Path)
                                        ToolTip.tip "Audio File"
                                    ]
                                ]
                            ]

                            NumericUpDown.create [
                                Grid.column 1
                                Grid.row 5
                                NumericUpDown.margin (2.0, 2.0, 2.0, 2.0)
                                NumericUpDown.width 65.
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
                                NumericUpDown.width 65.
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
                            | :? Arrangement as arr -> dispatch (ArrangementSelected (Some arr))
                            | null when state.Project.Arrangements.Length = 0 -> dispatch (ArrangementSelected None)
                            | _ -> ()), SubPatchOptions.OnChangeOf(state))
                        ListBox.onKeyDown (fun k -> if k.Key = Key.Delete then dispatch DeleteArrangement)
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
                        | Instrumental i -> instrumentalDetailsView state dispatch i
                        | Vocals v -> vocalsDetailsView state dispatch v
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
                        Button.content "Import Tones"
                        Button.onClick (fun _ -> dispatch ImportTones)
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
