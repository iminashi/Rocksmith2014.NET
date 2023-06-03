module DLCBuilder.Views.ProfileCleanerOverlay

open Avalonia.Controls
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open DLCBuilder
open System.IO

let view dispatch (cleanerState: ProfileCleanerState) (globalState: State) =
    let isRunning =
        match cleanerState with ProfileCleanerState.Idle | ProfileCleanerState.Completed _ -> false | _ -> true

    let profileFileExists () =
        String.endsWith "_PRFLDB" globalState.Config.ProfilePath && File.Exists(globalState.Config.ProfilePath)


    let dlcFolderExists () =
        Directory.Exists(globalState.Config.DlcFolderPath)

    StackPanel.create [
        StackPanel.children [
            vStack [
                match cleanerState with
                | ProfileCleanerState.Idle ->
                    vStack [
                        TextBlock.create [ TextBlock.text "...Instructions..." ]

                        if not <| profileFileExists () then
                            vStack [
                                TextBlock.create [ TextBlock.text "Profile path is not correctly set" ]

                                Button.create [
                                    Button.content "Open Configuration"
                                    Button.onClick (fun _ -> ShowOverlay (ConfigEditor (Some FocusedSetting.ProfilePath)) |> dispatch)
                                ]
                            ]
                        elif not <| dlcFolderExists () then
                            vStack [
                                TextBlock.create [ TextBlock.text "DLC directory path is not correctly set" ]

                                Button.create [
                                    Button.content "Open Configuration"
                                    Button.onClick (fun _ -> ShowOverlay (ConfigEditor (Some FocusedSetting.DLCFolder)) |> dispatch)
                                ]
                            ]
                    ]
                | ProfileCleanerState.ReadingIds progress ->
                    vStack [
                        TextBlock.create [ TextBlock.text "Reading IDs..." ]

                        ProgressBar.create [
                            ProgressBar.value (100. * progress)
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
                | ProfileCleanerState.Completed _result ->
                    TextBlock.create [ TextBlock.text "Completed." ]
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
