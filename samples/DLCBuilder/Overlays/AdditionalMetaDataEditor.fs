module DLCBuilder.Views.AdditionalMetaDataEditor

open Avalonia.Controls
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open DLCBuilder
open Rocksmith2014.DLCProject

let view dispatch state =
    DockPanel.create [
        DockPanel.children [
            // Close button
            Button.create [
                DockPanel.dock Dock.Bottom
                Button.fontSize 16.
                Button.padding (50., 10.)
                Button.horizontalAlignment HorizontalAlignment.Center
                Button.content (translate "Close")
                Button.onClick (fun _ -> (CloseOverlay OverlayCloseMethod.OverlayButton) |> dispatch)
                Button.isDefault true
            ]

            // Title
            locText "AdditionalMetadata" [
                DockPanel.dock Dock.Top
                TextBlock.fontSize 18.
                TextBlock.horizontalAlignment HorizontalAlignment.Center
            ]

            // Author
            vStack [
                hStack [
                    locText "Author" [ TextBlock.verticalAlignment VerticalAlignment.Center ]

                    FixedTextBox.create [
                        FixedTextBox.text (state.Project.Author |> Option.toObj)
                        FixedTextBox.watermark state.Config.CharterName
                        FixedTextBox.width 250.
                        FixedTextBox.onTextChanged (SetAuthor >> EditProject >> dispatch)
                        ToolTip.tip (translate "AdditionalMetadataAuthorToolTip")
                    ]
                ]
            ]
        ]
    ] |> generalize
