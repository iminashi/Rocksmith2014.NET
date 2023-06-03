module DLCBuilder.Views.ProfileCleanerOverlay

open Avalonia.Controls
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open DLCBuilder
open System.IO

let view dispatch (state: State) =
    let isRunning =
        match state.ProfileCleanerState with
        | ProfileCleanerState.Idle
        | ProfileCleanerState.Completed _ ->
            false
        | ProfileCleanerState.ReadingIds _
         | ProfileCleanerState.CleaningProfile ->
            true

    let profileFileExists () =
        String.endsWith "_PRFLDB" state.Config.ProfilePath && File.Exists(state.Config.ProfilePath)

    let dlcFolderExists () =
        Directory.Exists(state.Config.DlcFolderPath)

    StackPanel.create [
        StackPanel.children [
            vStack [
                match state.ProfileCleanerState with
                | ProfileCleanerState.Idle ->
                    vStack [
                        TextBlock.create [ TextBlock.text "...Instructions..." ]

                        if not <| profileFileExists () then
                            vStack [
                                TextBlock.create [ TextBlock.text "Profile path is not correctly set" ]

                                Button.create [
                                    Button.content (translate "OpenConfiguration")
                                    Button.onClick (fun _ -> ShowOverlay (ConfigEditor (Some FocusedSetting.ProfilePath)) |> dispatch)
                                ]
                            ]
                        elif not <| dlcFolderExists () then
                            vStack [
                                TextBlock.create [ TextBlock.text "DLC directory path is not correctly set" ]

                                Button.create [
                                    Button.content (translate "OpenConfiguration")
                                    Button.onClick (fun _ -> ShowOverlay (ConfigEditor (Some FocusedSetting.DLCFolder)) |> dispatch)
                                ]
                            ]
                    ]
                | ProfileCleanerState.ReadingIds progress ->
                    vStack [
                        TextBlock.create [ TextBlock.text "Reading IDs..." ]

                        ProgressBar.create [
                            ProgressBar.value progress
                            ProgressBar.height 16.
                        ]
                    ]
                | ProfileCleanerState.CleaningProfile ->
                    vStack [
                        TextBlock.create [ TextBlock.text "Cleaning profile..." ]

                        ProgressBar.create [
                            ProgressBar.isIndeterminate true
                            ProgressBar.height 16.
                        ]
                    ]
                | ProfileCleanerState.Completed result ->
                    Grid.create [
                        Grid.columnDefinitions "auto,4,*"
                        Grid.rowDefinitions "*,*,*,*,*"
                        Grid.children [
                            TextBlock.create [
                                Grid.columnSpan 3
                                TextBlock.margin (0., 4.)
                                TextBlock.text "Cleaning completed.\n\nRecords removed:"
                            ]

                            TextBlock.create [
                                Grid.row 1
                                Grid.column 0
                                TextBlock.text "Stats:"
                            ]
                            TextBlock.create [
                                Grid.row 1
                                Grid.column 2
                                TextBlock.text (string result.Stats)
                            ]

                            TextBlock.create [
                                Grid.row 2
                                Grid.column 0
                                TextBlock.text "Songs:"
                            ]
                            TextBlock.create [
                                Grid.row 2
                                Grid.column 2
                                TextBlock.text (string result.Songs)
                            ]

                            TextBlock.create [
                                Grid.row 3
                                Grid.column 0
                                TextBlock.text "Score Attack:"
                            ]
                            TextBlock.create [
                                Grid.row 3
                                Grid.column 2
                                TextBlock.text (string result.ScoreAttack)
                            ]

                            TextBlock.create [
                                Grid.row 4
                                Grid.column 0
                                TextBlock.text "Playnexts:"
                            ]
                            TextBlock.create [
                                Grid.row 4
                                Grid.column 2
                                TextBlock.text (string result.PlayNext)
                            ]
                        ]
                    ]
            ]

            hStack [
                // Start button
                Button.create [
                    Button.fontSize 16.
                    Button.padding (50., 10.)
                    Button.margin 2.
                    Button.horizontalAlignment HorizontalAlignment.Center
                    Button.content (translate "Start")
                    Button.onClick (fun _ -> StartProfileCleaner |> ToolsMsg |> dispatch)
                    Button.isEnabled (not isRunning && profileFileExists () && dlcFolderExists ())
                ]

                // Close button
                Button.create [
                    Button.fontSize 16.
                    Button.padding (50., 10.)
                    Button.margin 2.
                    Button.horizontalAlignment HorizontalAlignment.Center
                    Button.content (translate "Close")
                    Button.onClick (fun _ -> (CloseOverlay OverlayCloseMethod.OverlayButton) |> dispatch)
                    Button.isDefault true
                ]
            ]
        ]
    ] |> generalize
