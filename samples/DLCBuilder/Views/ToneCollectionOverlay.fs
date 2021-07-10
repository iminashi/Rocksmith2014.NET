module DLCBuilder.Views.ToneCollectionOverlay

open Avalonia
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
    
let private toneTemplate dispatch =
    DataTemplateView<DbTone>.create (fun dbTone ->
        let brush =
            if dbTone.BassTone then
                Brushes.bass
            elif dbTone.Name.Contains("lead", StringComparison.OrdinalIgnoreCase)
                 || dbTone.Name.Contains("solo", StringComparison.OrdinalIgnoreCase) then
                Brushes.lead
            else
                Brushes.rhythm

        hStack [
            Button.create [
                Button.content "+"
                Button.padding (10., 5.)
                Button.verticalAlignment VerticalAlignment.Stretch
                Button.onClick (fun _ -> dispatch (AddDbTone dbTone.Id))
            ]

            Path.create [
                Path.verticalAlignment VerticalAlignment.Center
                Path.fill brush
                Path.data Media.Icons.guitar
            ]

            StackPanel.create [
                StackPanel.margin 4.
                StackPanel.children [
                    TextBlock.create [
                        TextBlock.text (
                            if String.notEmpty dbTone.Artist && String.notEmpty dbTone.Title then
                                $"{dbTone.Artist} - {dbTone.Title}"
                            else
                                String.Empty)
                    ]
                    TextBlock.create [ TextBlock.text dbTone.Name ]
                    TextBlock.create [ TextBlock.text (translateDescription dbTone.Description) ]
                ]
            ]
        ])

let tonesList dispatch collectionState isOfficial =
    ListBox.create [
        ListBox.height 410.
        ListBox.width 500.
        ListBox.dataItems collectionState.Tones
        ListBox.itemTemplate (toneTemplate dispatch)
        ListBox.onKeyDown (fun arg ->
            match arg.Key with
            | Key.Left ->
                arg.Handled <- true
                ChangeToneCollectionPage Left |> dispatch
            | Key.Right ->
                arg.Handled <- true
                ChangeToneCollectionPage Right |> dispatch
            | Key.Enter ->
                match arg.Source with
                | :? ListBoxItem as item ->
                    match item.DataContext with
                    | :? DbTone as selectedTone ->
                        arg.Handled <- true
                        AddDbTone selectedTone.Id |> dispatch
                    | _ ->
                        ()
                | _ ->
                    ()
            | Key.Delete when not isOfficial ->
                match arg.Source with
                | :? ListBoxItem as item ->
                    match item.DataContext with
                    | :? DbTone as selectedTone ->
                        arg.Handled <- true
                        DeleteUserTone selectedTone.Id |> dispatch
                    | _ ->
                        ()
                | _ ->
                    ()
            | _ ->
                ()
        )
    ]

let private collectionView dispatch (collectionState: ToneCollection.State) =
    DockPanel.create [
        DockPanel.children [
            // Search text box
            AutoFocusSearchBox.create [
                DockPanel.dock Dock.Top
                AutoFocusSearchBox.onTextChanged (Option.ofString >> SearchToneCollection >> dispatch)
            ]

            // Pagination
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

            match collectionState.ActiveCollection with
            // Database file not found message
            | ActiveCollection.Official None ->
                TextBlock.create [
                    TextBlock.horizontalAlignment HorizontalAlignment.Center
                    TextBlock.verticalAlignment VerticalAlignment.Center
                    TextBlock.text "Official tones file not found."
                ]

            // Tones list
            | ActiveCollection.Official _ ->
                tonesList dispatch collectionState true
            | ActiveCollection.User _ ->
                tonesList dispatch collectionState false
        ]
    ]

let view dispatch collectionState =
    TabControl.create [
        TabControl.width 520.
        TabControl.height 550.
        TabControl.viewItems [
            TabItem.create [
                TabItem.header (translate "official")
                TabItem.content (
                    match collectionState.ActiveCollection with
                    | ActiveCollection.Official _ ->
                        collectionView dispatch collectionState
                        |> generalize
                    | _ ->
                        Panel.create [] |> generalize)
                TabItem.onIsSelectedChanged (fun isSelected ->
                    if isSelected then
                        ActiveTab.Official |> ChangeToneCollection |> dispatch
                )
            ]

            TabItem.create [
                TabItem.header (translate "user")
                TabItem.content (
                    match collectionState.ActiveCollection with
                    | ActiveCollection.User _ ->
                        collectionView dispatch collectionState
                        |> generalize
                    | _ ->
                        Panel.create [] |> generalize
                )
                TabItem.onIsSelectedChanged (fun isSelected ->
                    if isSelected then
                        ActiveTab.User |> ChangeToneCollection |> dispatch
                )
            ]
        ]
    ] |> generalize
