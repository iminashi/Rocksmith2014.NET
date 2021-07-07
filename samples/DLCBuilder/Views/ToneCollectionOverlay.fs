module DLCBuilder.Views.ToneCollectionOverlay

open Avalonia.Controls
open Avalonia.Controls.Shapes
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open Avalonia.FuncUI
open Avalonia.FuncUI.Components
open DLCBuilder
open DLCBuilder.ToneCollection
open System
open Rocksmith2014.Common
open Avalonia.Media

let private translateDescription (description: string) =
    description.Split('|')
    |> Array.map translate
    |> String.concat " "
    
let private dbToneTemplate dispatch (api: ITonesApi) =
    DataTemplateView<OfficialTone>.create (fun dbTone ->
        hStack [
            Button.create [
                Button.content "ADD"
                Button.padding (10., 5.)
                Button.verticalAlignment VerticalAlignment.Center
                Button.onClick (fun _ -> dispatch (AddDbTone (api, dbTone.Id)))
            ]

            StackPanel.create [
                StackPanel.margin 4.
                StackPanel.children [
                    TextBlock.create [ TextBlock.text $"{dbTone.Artist} - {dbTone.Title}" ]
                    TextBlock.create [ TextBlock.text dbTone.Name ]
                    TextBlock.create [ TextBlock.text (translateDescription dbTone.Description) ]
                    TextBlock.create [ TextBlock.text (if dbTone.BassTone then "BASS" else "GUITAR") ]
                ]
            ]
        ])

let view state dispatch (api: ITonesApi) (tones: OfficialTone array) searchString =
    DockPanel.create [
        DockPanel.children [
            // Search text box
            Panel.create [
                DockPanel.dock Dock.Top
                Panel.children [
                    TextBox.create [
                        TextBox.text searchString
                        TextBox.watermark (translate "search")
                        TextBox.onTextChanged ((fun text ->
                            let tones =
                                text
                                |> Option.ofString
                                |> api.GetTones
                                |> Seq.toArray
                            ToneCollection (api, tones, text)
                            |> ShowOverlay
                            |> dispatch
                        ), OnChangeOf tones)
                    ]
                    Button.create [
                        Button.isVisible (String.notEmpty searchString)
                        Button.background Brushes.Transparent
                        Button.horizontalAlignment HorizontalAlignment.Right
                        Button.verticalAlignment VerticalAlignment.Center
                        Button.margin (0., 0., 10., 2.)
                        Button.padding 0.
                        Button.onClick ((fun _ ->
                            ToneCollection (api, tones, String.Empty)
                            |> ShowOverlay
                            |> dispatch
                        ), OnChangeOf tones)
                        Button.content (
                            Path.create [
                                Path.fill Brushes.Gray
                                Path.data Media.Icons.x
                            ]
                        )
                    ]
                ]
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
                ListBox.itemTemplate (dbToneTemplate dispatch api)
            ]
        ]
    ] |> generalize
