module DLCBuilder.Views.ProjectDetails

open Avalonia
open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.Input
open Avalonia.Layout
open Avalonia.Media
open Avalonia.Media.Imaging
open Avalonia.Platform
open Rocksmith2014.DLCProject
open System
open DLCBuilder
open Media

let private coverArtPlaceholder =
    let assets = AvaloniaLocator.Current.GetService<IAssetLoader>()
    new Bitmap(assets.Open(Uri("avares://DLCBuilder/Assets/coverart_placeholder.png")))

let private buildControls state dispatch =
    let canBuild = StateUtils.canStartBuild state

    Grid.create [
        Grid.verticalAlignment VerticalAlignment.Center
        Grid.horizontalAlignment HorizontalAlignment.Center
        Grid.columnDefinitions "auto,auto,*"
        Grid.children [
            match state.QuickEditData with
            | Some data ->
                // Apply edits
                Button.create [
                    Button.padding (20., 8.)
                    Button.margin 4.
                    Button.fontSize 16.
                    Button.content (translate "FinishEditing")
                    Button.isEnabled canBuild
                    Button.onClick ((fun _ -> data |> ReplacePsarc |> Build |> dispatch), OnChangeOf data)
                ]

                // Cancel edit
                Button.create [
                    Grid.column 1
                    Button.padding (20., 8.)
                    Button.margin 4.
                    Button.fontSize 16.
                    Button.content (translate "Cancel")
                    Button.isEnabled canBuild
                    Button.onClick (fun _ -> dispatch NewProject)
                ]

                // Build options quick access menu
                Menus.buildOptions true state dispatch
            | None ->
                // Build test
                Button.create [
                    Button.padding (20., 8.)
                    Button.margin 4.
                    Button.fontSize 16.
                    Button.content (translate "BuildTest")
                    Button.isEnabled canBuild
                    Button.onClick (fun _ -> dispatch <| Build Test)
                ]

                // Build release
                Button.create [
                    Grid.column 1
                    Button.padding (20., 8.)
                    Button.margin 4.
                    Button.fontSize 16.
                    Button.content (translate "BuildRelease")
                    Button.isEnabled canBuild
                    Button.onClick (fun _ -> dispatch <| Build Release)
                ]

                // Build options quick access menu
                Menus.buildOptions false state dispatch
        ]
    ]

let private projectInfo state dispatch =
    Grid.create [
        DockPanel.dock Dock.Top
        Grid.columnDefinitions "*,auto"
        Grid.rowDefinitions "auto,auto,auto,auto,auto"
        //Grid.showGridLines true
        Grid.children [
            // DLC Key
            TitledTextBox.create "DLCKey" [ Grid.column 0; Grid.row 0 ] [
                FixedTextBox.text state.Project.DLCKey
                TextBox.onTextInput (fun e -> e.Text <- StringValidator.dlcKey e.Text)
                FixedTextBox.onTextChanged (StringValidator.dlcKey >> SetDLCKey >> EditProject >> dispatch)
                ToolTip.tip (translate "DLCKeyToolTip")
            ]

            // Version
            TitledTextBox.create "Version" [ Grid.column 1; Grid.row 0 ] [
                TextBox.horizontalAlignment HorizontalAlignment.Left
                TextBox.width 65.
                FixedTextBox.text state.Project.Version
                FixedTextBox.onTextChanged (SetVersion >> EditProject >> dispatch)
            ]

            // Artist name
            TitledTextBox.create "ArtistName"
                [ Grid.column 0
                  Grid.row 1
                  StackPanel.isVisible (not state.ShowSortFields && not state.ShowJapaneseFields) ]
                [ FixedTextBox.text state.Project.ArtistName.Value
                  FixedTextBox.onTextChanged (StringValidator.field >> SetArtistName >> EditProject >> dispatch)
                ]

            // Artist name sort
            TitledTextBox.create "ArtistNameSort"
                [ Grid.column 0
                  Grid.row 1
                  StackPanel.isVisible (state.ShowSortFields && not state.ShowJapaneseFields) ]
                [ FixedTextBox.text state.Project.ArtistName.SortValue
                  FixedTextBox.watermark (state.Project.ArtistName.Value |> StringValidator.FieldType.ArtistName |> StringValidator.convertToSortField)
                  TextBox.onLostFocus (fun e ->
                    let txtBox = e.Source :?> TextBox
                    let validValue = StringValidator.sortField txtBox.Text
                    txtBox.Text <- validValue

                    validValue |> SetArtistNameSort |> EditProject |> dispatch)
                ]

            // Japanese artist name
            TitledTextBox.create "JapaneseArtistName"
                [ Grid.column 0
                  Grid.row 1
                  StackPanel.isVisible state.ShowJapaneseFields ]
                [ FixedTextBox.text (defaultArg state.Project.JapaneseArtistName String.Empty)
                  TextBox.fontFamily Fonts.japanese
                  TextBox.fontSize 15.
                  FixedTextBox.onTextChanged (StringValidator.field >> Option.ofString >> SetJapaneseArtistName >> EditProject >> dispatch)
                ]

            // Title
            TitledTextBox.create "Title"
                [ Grid.column 0
                  Grid.row 2
                  StackPanel.isVisible (not state.ShowSortFields && not state.ShowJapaneseFields) ]
                [ FixedTextBox.text state.Project.Title.Value
                  FixedTextBox.onTextChanged (StringValidator.field >> SetTitle >> EditProject >> dispatch)
                ]

            // Title sort
            TitledTextBox.create "TitleSort"
                [ Grid.column 0
                  Grid.row 2
                  StackPanel.isVisible state.ShowSortFields ]
                [ FixedTextBox.text state.Project.Title.SortValue
                  FixedTextBox.watermark (state.Project.Title.Value |> StringValidator.FieldType.Title |> StringValidator.convertToSortField)
                  TextBox.onLostFocus (fun e ->
                    let txtBox = e.Source :?> TextBox
                    let validValue = StringValidator.sortField txtBox.Text
                    txtBox.Text <- validValue

                    validValue |> SetTitleSort |> EditProject |> dispatch)
                ]

            // Additional metadata
            Button.create [
                Grid.column 1
                Grid.row 2
                Button.verticalAlignment VerticalAlignment.Bottom
                Button.horizontalAlignment HorizontalAlignment.Center
                Button.onClick (fun _ -> dispatch (ShowOverlay AdditionalMetaDataEditor))
                Button.classes [ "borderless-btn" ]
                Button.padding (10., 8.)
                Button.content (
                    PathIcon.create [
                        PathIcon.data Icons.ellipsisCircle
                        PathIcon.foreground (if state.Project.Author.IsSome then Brushes.White else Brushes.DarkGray)
                    ])
                ToolTip.tip (translate "AdditionalMetadataButtonToolTip")
            ]

            // Japanese title
            TitledTextBox.create "JapaneseTitle"
                [ Grid.column 0
                  Grid.row 2
                  StackPanel.isVisible state.ShowJapaneseFields ]
                [ FixedTextBox.text (defaultArg state.Project.JapaneseTitle String.Empty)
                  TextBox.fontFamily Fonts.japanese
                  TextBox.fontSize 15.
                  FixedTextBox.onTextChanged (StringValidator.field >> Option.ofString >> SetJapaneseTitle >> EditProject >> dispatch)
                ]

            // Album name
            TitledTextBox.create "AlbumName"
                [ Grid.column 0
                  Grid.row 3
                  StackPanel.isVisible (not state.ShowSortFields) ]
                [ FixedTextBox.text state.Project.AlbumName.Value
                  FixedTextBox.onTextChanged (StringValidator.field >> SetAlbumName >> EditProject >> dispatch)
                ]

            // Album name sort
            TitledTextBox.create "AlbumNameSort"
                [ Grid.column 0
                  Grid.row 3
                  StackPanel.isVisible state.ShowSortFields ]
                [ FixedTextBox.text state.Project.AlbumName.SortValue
                  FixedTextBox.watermark (state.Project.AlbumName.Value |> StringValidator.FieldType.AlbumName |> StringValidator.convertToSortField)
                  TextBox.onLostFocus (fun e ->
                    let txtBox = e.Source :?> TextBox
                    let validValue = StringValidator.sortField txtBox.Text
                    txtBox.Text <- validValue

                    validValue |> SetAlbumNameSort |> EditProject |> dispatch)
                ]

            // Year
            TitledTextBox.create "Year"
                [ Grid.column 1
                  Grid.row 3 ]
                [ TextBox.horizontalAlignment HorizontalAlignment.Left
                  TextBox.width 65.
                  FixedTextBox.text (string state.Project.Year)
                  FixedTextBox.onTextChanged (fun text ->
                    match Int32.TryParse(text) with
                    | true, year ->
                        year |> SetYear |> EditProject |> dispatch
                    | false, _ ->
                        ())
                ]

            StackPanel.create [
                Grid.columnSpan 2
                Grid.row 4
                StackPanel.orientation Orientation.Horizontal
                StackPanel.horizontalAlignment HorizontalAlignment.Center
                StackPanel.children [
                    // Show sort fields
                    CheckBox.create [
                        CheckBox.content (translate "ShowSortFields")
                        CheckBox.isChecked (state.ShowSortFields && not state.ShowJapaneseFields)
                        CheckBox.onChecked (fun _ -> true |> ShowSortFields |> dispatch)
                        CheckBox.onUnchecked (fun _ -> false |> ShowSortFields |> dispatch)
                    ]

                    // Show Japanese fields
                    CheckBox.create [
                        CheckBox.margin (8., 0.,0., 0.)
                        CheckBox.content (translate "ShowJapaneseFields")
                        CheckBox.isChecked (state.ShowJapaneseFields && not state.ShowSortFields)
                        CheckBox.onChecked (fun _ -> true |> ShowJapaneseFields |> dispatch)
                        CheckBox.onUnchecked (fun _ -> false |> ShowJapaneseFields |> dispatch)
                    ]
                ]
            ]
        ]
    ]

let private coverArt state dispatch =
    let albumArt = AvaloniaBitmapLoader.getBitmap ()
    let brush, toolTip =
        match Option.ofString state.Project.AlbumArtFile, albumArt with
        | Some path, None ->
            Brushes.DarkRed, translatef "LoadingCoverArtFailed" [| IO.Path.GetFileName(path) |]
        | Some path, Some _ ->
            let fileName = IO.Path.GetFileName(path)
            let help = translate "SelectCoverArtToolTip"
            Brushes.Black, $"{fileName}\n\n{help}"
        | None, _ ->
            Brushes.Black, translate "SelectCoverArtToolTip"

    Border.create [
        DockPanel.dock Dock.Top
        Border.borderThickness 2.
        Border.horizontalAlignment HorizontalAlignment.Center
        Border.borderBrush brush
        Border.child (
            Image.create [
                Image.source (albumArt |> Option.defaultValue coverArtPlaceholder)
                Image.width 200.
                Image.height 200.
                Image.onTapped (fun _ -> Dialog.CoverArt |> ShowDialog |> dispatch)
                Image.onKeyDown (fun args ->
                    if args.Key = Key.Space then
                        args.Handled <- true
                        Dialog.CoverArt |> ShowDialog |> dispatch)
                Image.cursor Cursors.hand
                Image.focusable true
                ToolTip.tip toolTip
            ])
    ]

let view state dispatch =
    DockPanel.create [
        Grid.rowSpan 2
        DockPanel.children [
            coverArt state dispatch

            projectInfo state dispatch

            AudioControls.view state dispatch

            buildControls state dispatch
        ]
    ]
