module DLCBuilder.Views.InstrumentalXmlDetails

open Avalonia.Controls
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open DLCBuilder
open Rocksmith2014.DLCProject
open Rocksmith2014.DLCProject.ArrangementPropertiesOverride
open Rocksmith2014.XML
open System
open DLCBuilder.StateUtils

let view state dispatch (xml: InstrumentalArrangement) =
    let arrProps = xml.MetaData.ArrangementProperties
    let instArrProps =
        match getSelectedArrangement state with
        | Some (Instrumental inst) ->
            inst.ArrangementProperties
        | _ ->
            // Should not happen
            None

    let flags, allowEditing =
        match instArrProps with
        | Some props -> props, true
        | None -> fromArrangementProperties arrProps, false

    let editInstrumental = EditInstrumental >> dispatch

    let checkBox col row (flag: ArrPropFlags) (text: string) =
        let isChecked = (flags &&& flag) = flag
    
        CheckBox.create [
            Grid.column col
            Grid.row row
            CheckBox.isEnabled allowEditing
            CheckBox.isChecked isChecked
            CheckBox.content text
            CheckBox.margin (2., 0.)
            if allowEditing then
                CheckBox.onChecked (fun _ ->
                    ArrPropOp.Enable flag
                    |> ToggleArrangementProperty
                    |> editInstrumental)
                CheckBox.onUnchecked (fun _ ->
                    ArrPropOp.Disable flag
                    |> ToggleArrangementProperty
                    |> editInstrumental)
        ]

    StackPanel.create [
        StackPanel.spacing 8.
        StackPanel.children [
            TextBlock.create [
                TextBlock.text xml.MetaData.Arrangement
                TextBlock.fontSize 22.
            ]

            StackPanel.create [
                StackPanel.orientation Orientation.Horizontal
                StackPanel.children [
                    StackPanel.create [
                        StackPanel.margin (12., 0., 0., 0.)
                        StackPanel.spacing 4.
                        StackPanel.children [
                            TextBlock.create [
                                TextBlock.text (translatef "AverageTempo" [| xml.MetaData.AverageTempo |])
                            ]

                            let startBeatStr = TimeSpan.FromMilliseconds(xml.StartBeat).ToString("mm\:ss\.fff")

                            TextBlock.create [
                                TextBlock.text (translatef "FirstBeatTime" [| startBeatStr |])
                            ]

                            TextBlock.create [
                                TextBlock.text (translatef "Phrases" [| xml.PhraseIterations.Count |])
                            ]

                            TextBlock.create [
                                TextBlock.text (translatef "Sections" [| xml.Sections.Count |])
                            ]

                            TextBlock.create [
                                let capoStr =
                                    if xml.MetaData.Capo > 0y then
                                        translatef "Fret" [| string xml.MetaData.Capo |]
                                    else
                                        translate "Not Used"
                                TextBlock.text (translatef "Capo" [| capoStr |])
                            ]
                        ]
                    ]

                    StackPanel.create [
                        StackPanel.margin (22., 0., 0., 0.)
                        StackPanel.spacing 4.
                        StackPanel.children [
                            TextBlock.create [
                                TextBlock.text (translatef "Levels" [| xml.Levels.Count |])
                            ]

                            if xml.Levels.Count = 1 then
                                TextBlock.create [
                                    TextBlock.text (translatef "Notes" [| xml.Levels[0].Notes.Count |])
                                ]
                                TextBlock.create [
                                    TextBlock.text (translatef "Chords" [| xml.Levels[0].Chords.Count |])
                                ]
                        ]
                    ]
                ]
            ]

            DockPanel.create [
                DockPanel.children [
                    StackPanel.create [
                        DockPanel.dock Dock.Left
                        StackPanel.verticalAlignment VerticalAlignment.Center
                        StackPanel.orientation Orientation.Horizontal
                        StackPanel.children [
                            TextBlock.create [
                                TextBlock.verticalAlignment VerticalAlignment.Center
                                TextBlock.text (translate "ArrangementProperties")
                                TextBlock.fontSize 18.
                            ]

                            HelpButton.create [
                                HelpButton.margin (0., 4.)
                                HelpButton.helpText (translate "ArrangementPropertyHelp")
                            ]
                        ]
                    ]

                    CheckBox.create [
                        DockPanel.dock Dock.Right
                        CheckBox.horizontalAlignment HorizontalAlignment.Right
                        CheckBox.verticalAlignment VerticalAlignment.Center
                        CheckBox.content (translate "Edit")
                        CheckBox.isChecked allowEditing
                        CheckBox.onChecked ((fun _ ->
                            arrProps
                            |> ToggleArrangementPropertiesOverride
                            |> editInstrumental), SubPatchOptions.OnChangeOf arrProps)
                        CheckBox.onUnchecked (fun _ ->
                            arrProps
                            |> ToggleArrangementPropertiesOverride
                            |> editInstrumental)
                    ]
                ]
            ]

            Grid.create [
                Grid.columnDefinitions "*,*,*"
                Grid.rowDefinitions (Seq.replicate 8 "*" |> String.concat ",")
                Grid.children [
                    checkBox 0 0 ArrPropFlags.BarreChords "Barre Chords"
                    checkBox 0 1 ArrPropFlags.Bends "Bends"
                    checkBox 0 2 ArrPropFlags.DoubleStops "Double Stops"
                    checkBox 0 3 ArrPropFlags.DropDPower "Drop D Power Chords"
                    checkBox 0 4 ArrPropFlags.FifthsAndOctaves "Fifths and Octaves"
                    checkBox 0 5 ArrPropFlags.FingerPicking "Finger Picking"
                    checkBox 0 6 ArrPropFlags.Harmonics "Natural Harmonics"
                    checkBox 0 7 ArrPropFlags.PinchHarmonics "Pinch Harmonics"
                    checkBox 1 0 ArrPropFlags.SlapPop "Slap/Pop"
                    checkBox 1 1 ArrPropFlags.Sustain "Sustains"
                    checkBox 1 2 ArrPropFlags.Tapping "Tapping"
                    checkBox 1 3 ArrPropFlags.TwoFingerPicking "Two Finger Picking"
                    checkBox 1 4 ArrPropFlags.PalmMutes "Palm Mutes"
                    checkBox 1 5 ArrPropFlags.FretHandMutes "Frethand Mutes"
                    checkBox 1 6 ArrPropFlags.Hopo "HOPO"
                    checkBox 1 7 ArrPropFlags.NonStandardChords "Non-Standard Chords"
                    checkBox 2 0 ArrPropFlags.OpenChords "Open Chords"
                    checkBox 2 1 ArrPropFlags.PowerChords "Power Chords"
                    checkBox 2 2 ArrPropFlags.Slides "Slides"
                    checkBox 2 3 ArrPropFlags.UnpitchedSlides "Unpitched Slides"
                    checkBox 2 4 ArrPropFlags.Syncopation "Syncopation"
                    checkBox 2 5 ArrPropFlags.Tremolo "Tremolo Picking"
                    checkBox 2 6 ArrPropFlags.Vibrato "Vibrato"
                ]
            ]

            // Close button
            Button.create [
                Button.fontSize 14.
                Button.padding (50., 10.)
                Button.horizontalAlignment HorizontalAlignment.Center
                Button.content (translate "Close")
                Button.onClick (fun _ -> dispatch (CloseOverlay OverlayCloseMethod.OverlayButton))
                Button.isDefault true
            ]
        ]
    ] |> generalize
