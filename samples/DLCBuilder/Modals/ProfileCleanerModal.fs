module DLCBuilder.Views.ProfileCleanerModal

open Avalonia.Controls
open Avalonia.Controls.Shapes
open Avalonia.Media
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open DLCBuilder
open System
open System.IO

let private errorMessage dispatch locString focusedSetting =
    vStack [
        StackPanel.create [
            StackPanel.orientation Orientation.Horizontal
            StackPanel.horizontalAlignment HorizontalAlignment.Center
            StackPanel.margin 8.
            StackPanel.children [
                Path.create [
                    Path.fill Brushes.Gray
                    Path.data Media.Icons.alertRound
                    Path.verticalAlignment VerticalAlignment.Center
                    Path.margin (0., 0., 14., 0.)
                ]
                TextBlock.create [
                    TextBlock.horizontalAlignment HorizontalAlignment.Center
                    TextBlock.verticalAlignment VerticalAlignment.Center
                    TextBlock.text (translate locString)
                ]
            ]
        ]

        Button.create [
            Button.horizontalAlignment HorizontalAlignment.Center
            Button.padding (25., 5.)
            Button.content (translate "OpenConfiguration")
            Button.onClick (fun _ -> ShowModal (ConfigEditor (Some focusedSetting)) |> dispatch)
        ]
    ]

let private cleaningInProgressView locString progressOpt =
    StackPanel.create [
        StackPanel.horizontalAlignment HorizontalAlignment.Center
        StackPanel.verticalAlignment VerticalAlignment.Center
        StackPanel.children [
            TextBlock.create [
                TextBlock.text (translate locString)
                TextBlock.margin (0., 4.)
            ]

            ProgressBar.create [
                match progressOpt with
                | ValueSome progress ->
                    ProgressBar.value progress
                    ProgressBar.isIndeterminate false
                | ValueNone ->
                    ProgressBar.isIndeterminate true
                ProgressBar.height 18.
            ]
        ]
    ]

let private removedStatsRow row text (stat: int) =
    [
        TextBlock.create [
            Grid.row row
            Grid.column 0
            TextBlock.text text
        ] |> generalize
        TextBlock.create [
            Grid.row row
            Grid.column 2
            TextBlock.text (string stat)
        ] |> generalize
    ]

let private configuration isEnabled dispatch state =
    Grid.create [
        DockPanel.dock Dock.Bottom
        Grid.horizontalAlignment HorizontalAlignment.Center
        Grid.isEnabled isEnabled
        Grid.rowDefinitions "*,*"
        Grid.columnDefinitions "*,*,*"
        Grid.children [
            CheckBox.create [
                Grid.column 1
                CheckBox.horizontalAlignment HorizontalAlignment.Center
                CheckBox.content (translate "DryRun")
                CheckBox.isChecked state.ProfileCleanerState.IsDryRun
                CheckBox.onChecked (fun _ -> true |> SetProfileCleanerDryRun |> ToolsMsg |> dispatch)
                CheckBox.onUnchecked (fun _ -> false |> SetProfileCleanerDryRun |> ToolsMsg |> dispatch)
            ]

            HelpButton.create [
                Grid.column 2
                HelpButton.helpText (translate "DryRunHelp")
            ]

            TextBlock.create [
                Grid.row 1
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text (translate "Parallelism")
            ]

            FixedNumericUpDown.create [
                Grid.row 1
                Grid.column 1
                FixedNumericUpDown.width 120.
                FixedNumericUpDown.minimum 1.0m
                FixedNumericUpDown.maximum (decimal Environment.ProcessorCount)
                FixedNumericUpDown.formatString "F0"
                FixedNumericUpDown.value (decimal state.Config.ProfileCleanerIdParsingParallelism)
                FixedNumericUpDown.onValueChanged (int >> SetProfileCleanerParallelism >> EditConfig >> dispatch)
            ]

            HelpButton.create [
                Grid.row 1
                Grid.column 2
                HelpButton.helpText (translate "ProfileCleanerParallelismHelp")
            ]
        ]
    ]

let view dispatch (state: State) =
    let isRunning =
        match state.ProfileCleanerState.CurrentStep with
        | ProfileCleanerStep.Idle
        | ProfileCleanerStep.Completed _ ->
            false
        | ProfileCleanerStep.ReadingIds _
         | ProfileCleanerStep.CleaningProfile ->
            true

    let profileFileExists () =
        String.endsWith "_PRFLDB" state.Config.ProfilePath && File.Exists(state.Config.ProfilePath)

    let dlcFolderExists () =
        Directory.Exists(state.Config.DlcFolderPath)

    DockPanel.create [
        DockPanel.minHeight 200.
        DockPanel.width 430.
        DockPanel.children [
            // Title
            TextBlock.create [
                DockPanel.dock Dock.Top
                TextBlock.text (translate "ProfileCleaner")
                TextBlock.fontSize 20.
                TextBlock.margin (0., 0., 0., 8.)
                TextBlock.horizontalAlignment HorizontalAlignment.Center
            ]

            StackPanel.create [
                DockPanel.dock Dock.Bottom
                StackPanel.orientation Orientation.Horizontal
                StackPanel.horizontalAlignment HorizontalAlignment.Center
                StackPanel.children [
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
                        Button.onClick (fun _ -> (CloseModal ModalCloseMethod.UIButton) |> dispatch)
                        Button.isDefault true
                    ]
                ]
            ]

            configuration (not isRunning) dispatch state

            match state.ProfileCleanerState.CurrentStep with
            | ProfileCleanerStep.Idle ->
                vStack [
                    TextBlock.create [
                        TextBlock.text (translate "ProfileCleanerInfo")
                        TextBlock.textWrapping TextWrapping.Wrap
                    ]

                    if not <| profileFileExists () then
                        errorMessage dispatch "ProfilePathNotCorrectlySet" FocusedSetting.ProfilePath
                    elif not <| dlcFolderExists () then
                        errorMessage dispatch "DLCDirectoryPathIsNotCorrectlySet" FocusedSetting.DLCFolder
                ]
            | ProfileCleanerStep.ReadingIds progress ->
                cleaningInProgressView "ReadingIDs" (ValueSome progress)
            | ProfileCleanerStep.CleaningProfile ->
                cleaningInProgressView "CleaningProfile" ValueNone
            | ProfileCleanerStep.Completed (wasDryRun, result) ->
                Grid.create [
                    Grid.columnDefinitions "auto,8,*"
                    Grid.rowDefinitions "*,*,*,*,*"
                    Grid.horizontalAlignment HorizontalAlignment.Center
                    Grid.children [
                        TextBlock.create [
                            Grid.columnSpan 3
                            TextBlock.margin (0., 4.)
                            TextBlock.text (translate (if wasDryRun then "DryRunCompleted" else "CleaningCompleted"))
                        ]

                        yield! removedStatsRow 1 "Stats:" result.Stats
                        yield! removedStatsRow 2 "Songs:" result.Songs
                        yield! removedStatsRow 3 "Score Attack:" result.ScoreAttack
                        yield! removedStatsRow 4 "Playnexts:" result.PlayNext
                    ]
                ]
        ]
    ] |> generalize
