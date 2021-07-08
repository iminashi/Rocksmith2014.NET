module DLCBuilder.Views.ToneCollectionOverlay

open Avalonia
open Avalonia.Controls
open Avalonia.Controls.Shapes
open Avalonia.FuncUI
open Avalonia.FuncUI.Components
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open DLCBuilder
open DLCBuilder.Media
open DLCBuilder.ToneCollection
open Rocksmith2014.Common

let private translateDescription (description: string) =
    description.Split('|')
    |> Array.map translate
    |> String.concat " "
    
let private officialToneTemplate dispatch (api: ITonesApi) =
    DataTemplateView<OfficialTone>.create (fun dbTone ->
        hStack [
            Button.create [
                Button.content "ADD"
                Button.padding (10., 5.)
                Button.verticalAlignment VerticalAlignment.Center
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

let view state dispatch (api: ITonesApi) (tones: OfficialTone array) searchString =
    DockPanel.create [
        DockPanel.children [
            // Search text box
            AutoFocusSearchBox.create [
                DockPanel.dock Dock.Top
                AutoFocusSearchBox.onTextChanged (fun text ->
                    let tones =
                        text
                        |> Option.ofString
                        |> api.GetTones
                        |> Seq.toArray
                    ToneCollection (api, tones, text)
                    |> ShowOverlay
                    |> dispatch)
            ]

            // Close button
            Button.create [
                DockPanel.dock Dock.Bottom
                Button.fontSize 16.
                Button.padding (50., 10.)
                Button.horizontalAlignment HorizontalAlignment.Center
                Button.content (translate "close")
                Button.onClick (fun _ -> CloseOverlay |> dispatch)
            ]

            ListBox.create [
                ListBox.height 400.
                ListBox.width 500.
                ListBox.dataItems tones
                ListBox.itemTemplate (officialToneTemplate dispatch api)
            ]
        ]
    ] |> generalize
