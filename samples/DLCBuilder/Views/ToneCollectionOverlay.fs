module DLCBuilder.Views.ToneCollectionOverlay

open Avalonia
open Avalonia.Controls
open Avalonia.Controls.Shapes
open Avalonia.FuncUI
open Avalonia.FuncUI.Components
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open Avalonia.Media
open DLCBuilder
open DLCBuilder.Media
open DLCBuilder.ToneCollection
open Rocksmith2014.Common
open Avalonia.Input

let private translateDescription (description: string) =
    description.Split('|')
    |> Array.map translate
    |> String.concat " "
    
let private officialToneTemplate dispatch (api: ITonesApi) =
    DataTemplateView<OfficialTone>.create (fun dbTone ->
        hStack [
            Button.create [
                Button.content "+"
                Button.padding (10., 5.)
                Button.verticalAlignment VerticalAlignment.Stretch
                Button.onClick (fun _ -> dispatch (AddDbTone (api, dbTone.Id)))
            ]

            Path.create [
                Path.verticalAlignment VerticalAlignment.Center
                Path.fill (if dbTone.BassTone then Brushes.bass else Brushes.lead)
                Path.data Media.Icons.guitar
            ]

            StackPanel.create [
                StackPanel.margin 4.
                StackPanel.children [
                    TextBlock.create [ TextBlock.text $"{dbTone.Artist} - {dbTone.Title}" ]
                    TextBlock.create [ TextBlock.text dbTone.Name ]
                    TextBlock.create [ TextBlock.text (translateDescription dbTone.Description) ]
                ]
            ]
        ])

let view dispatch (collectionState: ToneCollection.State) =
    DockPanel.create [
        DockPanel.children [
            // Search text box
            AutoFocusSearchBox.create [
                DockPanel.dock Dock.Top
                AutoFocusSearchBox.onTextChanged (SearchOfficialTones >> dispatch)
            ]

            // Close button
            //Button.create [
            //    DockPanel.dock Dock.Bottom
            //    Button.fontSize 16.
            //    Button.padding (50., 10.)
            //    Button.horizontalAlignment HorizontalAlignment.Center
            //    Button.content (translate "close")
            //    Button.onClick (fun _ -> CloseOverlay |> dispatch)
            //]

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
                        TextBlock.text ($"{collectionState.CurrentPage} / {collectionState.TotalPages}")
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

            // Tones list
            ListBox.create [
                ListBox.height 410.
                ListBox.width 500.
                ListBox.dataItems collectionState.Tones
                ListBox.itemTemplate (officialToneTemplate dispatch collectionState.Api)
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
                            | :? OfficialTone as selectedTone ->
                                arg.Handled <- true
                                dispatch (AddDbTone (collectionState.Api, selectedTone.Id))
                            | _ ->
                                ()
                        | _ ->
                            ()
                    | _ ->
                        ()
                )
            ]
        ]
    ] |> generalize
