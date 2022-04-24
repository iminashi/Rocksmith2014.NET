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

            Grid.create [
                Grid.columnDefinitions "auto,*"
                Grid.rowDefinitions "auto,auto"
                Grid.children [
                    // Author
                    locText "Author" [ TextBlock.verticalAlignment VerticalAlignment.Center ]

                    FixedTextBox.create [
                        Grid.column 1
                        FixedTextBox.text (state.Project.Author |> Option.toObj)
                        FixedTextBox.watermark state.Config.CharterName
                        FixedTextBox.onTextChanged (SetAuthor >> EditProject >> dispatch)
                        ToolTip.tip (translate "AdditionalMetadataAuthorToolTip")
                    ]

                    // App ID (when using PSARC quick edit)
                    match state.QuickEditData with
                    | Some data ->
                        locText "AppID" [
                            Grid.row 1
                            TextBlock.verticalAlignment VerticalAlignment.Center
                        ]

                        FixedTextBox.create [
                            Grid.row 1
                            Grid.column 1
                            FixedTextBox.text (data.AppId |> Option.toObj)
                            FixedTextBox.watermark AppId.CherubRock
                            FixedTextBox.onTextChanged (SetEditedPsarcAppId >> dispatch)
                        ]
                    | None ->
                        ()
                ]
            ]
        ]
    ] |> generalize
