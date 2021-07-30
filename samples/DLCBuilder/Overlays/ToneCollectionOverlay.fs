module DLCBuilder.Views.ToneCollectionOverlay

open Avalonia.Controls
open Avalonia.Controls.Shapes
open Avalonia.FuncUI
open Avalonia.FuncUI.Components
open Avalonia.FuncUI.DSL
open Avalonia.Input
open Avalonia.Layout
open Avalonia.Media
open DLCBuilder
open DLCBuilder.Media
open DLCBuilder.ToneCollection
open Rocksmith2014.Common
open System

let private translateDescription (description: string) =
    description.Split('|')
    |> Array.map translate
    |> String.concat " "

let private toneTemplate dispatch isOfficial =
    DataTemplateView<DbTone>.create (fun dbTone ->
        let brush =
            if dbTone.BassTone then
                Brushes.bass
            elif dbTone.Name.Contains("lead", StringComparison.OrdinalIgnoreCase)
                 || dbTone.Name.Contains("solo", StringComparison.OrdinalIgnoreCase)
            then
                Brushes.lead
            else
                Brushes.rhythm

        StackPanel.create [
            StackPanel.contextMenu (
                ContextMenu.create [
                    ContextMenu.viewItems [
                        if isOfficial then
                            MenuItem.create [
                                MenuItem.header (translate "addToUserTones")
                                MenuItem.onClick (fun _ -> AddOfficalToneToUserCollection |> dispatch)
                            ]
                        else
                            MenuItem.create [
                                MenuItem.header (translate "edit")
                                MenuItem.onClick (fun _ -> ShowUserToneEditor |> dispatch)
                            ]

                            MenuItem.create [ MenuItem.header "-" ]

                            MenuItem.create [
                                MenuItem.header (translate "remove")
                                MenuItem.onClick (fun _ -> DeleteSelectedUserTone |> dispatch)
                            ]
                    ]
                ])
            StackPanel.background Brushes.Transparent
            StackPanel.width 470.
            StackPanel.orientation Orientation.Horizontal
            StackPanel.onDoubleTapped (fun _ -> AddSelectedToneFromCollection |> dispatch)
            StackPanel.children [
                Button.create [
                    Button.content "+"
                    Button.padding (10., 5.)
                    Button.verticalAlignment VerticalAlignment.Stretch
                    Button.onClick (fun _ -> AddSelectedToneFromCollection |> dispatch)
                ]

                Path.create [
                    Path.verticalAlignment VerticalAlignment.Center
                    Path.fill brush
                    Path.data Icons.guitar
                ]

                StackPanel.create [
                    StackPanel.margin 4.
                    StackPanel.children [
                        TextBlock.create [
                            TextBlock.text (
                                [ dbTone.Artist; dbTone.Title ]
                                |> List.filter String.notEmpty
                                |> String.concat " - ")
                        ]
                        TextBlock.create [ TextBlock.text dbTone.Name ]
                        TextBlock.create [ TextBlock.text (translateDescription dbTone.Description) ]
                    ]
                ]
            ]
        ])

let tonesList dispatch collectionState isOfficial =
    ListBox.create [
        ListBox.height 410.
        ListBox.width 500.
        ListBox.dataItems collectionState.Tones
        ListBox.itemTemplate (toneTemplate dispatch isOfficial)
        ListBox.onSelectedItemChanged (function
            | :? DbTone as tone ->
                tone |> Some |> ToneCollectionSelectedToneChanged |> dispatch
            | _ ->
                None |> ToneCollectionSelectedToneChanged |> dispatch)
        ListBox.onKeyDown (fun arg ->
            arg.Handled <- true
            match arg.Key with
            | Key.Left ->
                ChangeToneCollectionPage Left |> dispatch
            | Key.Right ->
                ChangeToneCollectionPage Right |> dispatch
            | Key.Enter ->
                AddSelectedToneFromCollection |> dispatch
            | Key.Delete when not isOfficial ->
                DeleteSelectedUserTone |> dispatch
            | Key.E when not isOfficial ->
                ShowUserToneEditor |> dispatch
            | _ ->
                arg.Handled <- false
        )
    ]

let private paginationControls dispatch (collectionState: ToneCollectionState) =
    Grid.create [
        DockPanel.dock Dock.Bottom
        Grid.horizontalAlignment HorizontalAlignment.Center
        Grid.margin 4.
        Grid.columnDefinitions "*,auto,*"
        Grid.children [
            Border.create [
                let isEnabled = collectionState.CurrentPage > 1
                Border.background Brushes.Transparent
                Border.isEnabled isEnabled
                Border.cursor (if isEnabled then Cursors.hand else Cursors.arrow)
                Border.onTapped (fun _ -> ChangeToneCollectionPage Left |> dispatch)
                Border.child (
                    Path.create [
                        Path.data Icons.chevronLeft
                        Path.fill (if isEnabled then Brushes.DarkGray else Brushes.DimGray)
                        Path.margin (8., 4.)
                    ]
                )
            ]
            TextBlock.create [
                Grid.column 1
                TextBlock.margin 8.
                TextBlock.minWidth 80.
                TextBlock.textAlignment TextAlignment.Center
                TextBlock.text (
                    if collectionState.TotalPages = 0 then
                        String.Empty
                        else
                        $"{collectionState.CurrentPage} / {collectionState.TotalPages}")
            ]
            Border.create [
                let isEnabled = collectionState.CurrentPage < collectionState.TotalPages
                Grid.column 2
                Border.background Brushes.Transparent
                Border.isEnabled isEnabled
                Border.cursor (if isEnabled then Cursors.hand else Cursors.arrow)
                Border.onTapped (fun _ -> ChangeToneCollectionPage Right |> dispatch)
                Border.child (
                    Path.create [
                        Path.data Icons.chevronRight
                        Path.fill (if isEnabled then Brushes.DarkGray else Brushes.DimGray)
                        Path.margin (8., 4.)
                    ]
                )
            ]
        ]
    ]

let private collectionView dispatch (collectionState: ToneCollectionState) =
    DockPanel.create [
        DockPanel.children [
            // Search text box
            AutoFocusSearchBox.create [
                DockPanel.dock Dock.Top
                AutoFocusSearchBox.onTextChanged (Option.ofString >> SearchToneCollection >> dispatch)
            ]

            // Pagination
            paginationControls dispatch collectionState

            match collectionState.ActiveCollection with
            // Database file not found message
            | ActiveCollection.Official None ->
                TextBlock.create [
                    TextBlock.horizontalAlignment HorizontalAlignment.Center
                    TextBlock.verticalAlignment VerticalAlignment.Center
                    TextBlock.text (translate "officialTonesDbNotFound")
                ]
            // Tones list
            | ActiveCollection.Official _ ->
                tonesList dispatch collectionState true
            | ActiveCollection.User _ ->
                tonesList dispatch collectionState false
        ]
    ]

let private userToneEditor dispatch data =
    Grid.create [
        Grid.verticalAlignment VerticalAlignment.Center
        Grid.rowDefinitions "auto,auto,auto,auto,auto,auto,auto,auto"
        Grid.children [
            Button.create [
                Grid.row 0
                Button.content (translate "removeArtistInfo")
                Button.horizontalAlignment HorizontalAlignment.Center
                Button.padding (20., 5.)
                Button.onClick (fun _ -> UserToneEdit.RemoveArtistInfo |> EditUserToneData |> dispatch)
            ]

            TitledTextBox.create "artistName" [ Grid.row 1 ]
                [ FixedTextBox.text data.Artist
                  FixedTextBox.onTextChanged (UserToneEdit.SetArtist >> EditUserToneData >> dispatch) ]

            TitledTextBox.create "artistNameSort" [ Grid.row 2 ]
                [ FixedTextBox.text data.ArtistSort
                  FixedTextBox.onTextChanged (UserToneEdit.SetArtistSort >> EditUserToneData >> dispatch) ]

            TitledTextBox.create "title" [ Grid.row 3 ]
                [ FixedTextBox.text data.Title
                  FixedTextBox.onTextChanged (UserToneEdit.SetTitle >> EditUserToneData >> dispatch) ]

            TitledTextBox.create "titleSort" [ Grid.row 4 ]
                [ FixedTextBox.text data.TitleSort
                  FixedTextBox.onTextChanged (UserToneEdit.SetTitleSort >> EditUserToneData >> dispatch) ]

            TitledTextBox.create "name" [ Grid.row 5 ]
                [ FixedTextBox.text data.Name
                  TextBox.onTextInput (fun e -> e.Text <- Rocksmith2014.DLCProject.StringValidator.toneName e.Text)
                  FixedTextBox.onTextChanged (UserToneEdit.SetName >> EditUserToneData >> dispatch) ]

            CheckBox.create [
                Grid.row 6
                CheckBox.content (translate "bassTone")
                CheckBox.isChecked data.BassTone
                CheckBox.onChecked (fun _ -> true |> UserToneEdit.SetIsBass |> EditUserToneData |> dispatch)
                CheckBox.onUnchecked (fun _ -> false |> UserToneEdit.SetIsBass |> EditUserToneData |> dispatch)
            ]

            StackPanel.create [
                Grid.row 7
                StackPanel.orientation Orientation.Horizontal
                StackPanel.horizontalAlignment HorizontalAlignment.Center
                StackPanel.children [
                    Button.create [
                        Button.margin 4.
                        Button.fontSize 16.
                        Button.padding (20., 5.)
                        Button.content (translate "save")
                        Button.isEnabled (String.notEmpty data.Name)
                        Button.onClick (fun _ -> ApplyUserToneEdit |> dispatch)
                    ]
                    Button.create [
                        Button.margin 4.
                        Button.fontSize 16.
                        Button.padding (20., 5.)
                        Button.content (translate "cancel")
                        Button.onClick (fun _ -> HideUserToneEditor |> dispatch)
                    ]
                ]
            ]
        ]
    ]

let view dispatch collectionState =
    let dispatch' = ToneCollectionMsg >> dispatch
    Panel.create [
        Panel.children [
            TabControl.create [
                TabControl.width 520.
                TabControl.height 550.
                TabControl.isEnabled collectionState.EditingUserTone.IsNone
                TabControl.viewItems [
                    // Official tab
                    TabItem.create [
                        TabItem.header (translate "official")
                        TabItem.content (
                            match collectionState.ActiveCollection with
                            | ActiveCollection.Official _ ->
                                collectionView dispatch' collectionState
                                |> generalize
                            | _ ->
                                Panel.create [] |> generalize)
                        TabItem.onIsSelectedChanged (fun isSelected ->
                            if isSelected then
                                ActiveTab.Official |> ChangeToneCollection |> dispatch'
                        )
                    ]

                    // User tab
                    TabItem.create [
                        TabItem.header (translate "user")
                        TabItem.content (
                            match collectionState.ActiveCollection with
                            | ActiveCollection.User _ ->
                                collectionView dispatch' collectionState
                                |> generalize
                            | _ ->
                                Panel.create [] |> generalize
                        )
                        TabItem.onIsSelectedChanged (fun isSelected ->
                            if isSelected then
                                ActiveTab.User |> ChangeToneCollection |> dispatch'
                        )
                    ]
                ]
            ]

            match collectionState.EditingUserTone with
            | Some data ->
                Panel.create [
                    Panel.background "#343434"
                    Panel.children [ userToneEditor dispatch' data ]
                ]
            | None ->
                ()

            Border.create [
                Border.cursor Cursors.hand
                Border.background Brushes.Transparent
                Border.horizontalAlignment HorizontalAlignment.Right
                Border.verticalAlignment VerticalAlignment.Top
                Border.focusable true
                Border.onTapped (fun _ -> dispatch CloseOverlay)
                Border.onKeyUp (fun args ->
                    if args.Key = Key.Space then
                        args.Handled <- true
                        dispatch CloseOverlay)
                Border.child (
                    Path.create [
                        Path.data Icons.x
                        Path.fill Brushes.DarkGray
                    ])
            ]
        ]
    ] |> generalize
