module DLCBuilder.Views.InstrumentalXmlDetails

open Avalonia.Controls
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open DLCBuilder
open Rocksmith2014.XML
open System

let checkBox col row (isChecked: bool) (text: string) =
    CheckBox.create [
        Grid.column col
        Grid.row row
        CheckBox.isEnabled false
        CheckBox.isChecked isChecked
        CheckBox.content text
        CheckBox.margin (2., 0.)
    ]

let view dispatch (arrangement: InstrumentalArrangement) =
    let arrProps = arrangement.MetaData.ArrangementProperties

    StackPanel.create [
        StackPanel.spacing 8.
        StackPanel.children [
            TextBlock.create [
                TextBlock.text arrangement.MetaData.Arrangement
                TextBlock.fontSize 22.
            ]

            StackPanel.create [
                StackPanel.margin (12., 0., 0., 0.)
                StackPanel.spacing 4.
                StackPanel.children [
                    TextBlock.create [
                        TextBlock.text $"Average tempo: {arrangement.MetaData.AverageTempo}"
                    ]

                    let startBeatStr = TimeSpan.FromMilliseconds(arrangement.StartBeat).ToString("mm\:ss\.fff")

                    TextBlock.create [
                        TextBlock.text $"First Beat Time: {startBeatStr}"
                    ]

                    TextBlock.create [
                        TextBlock.text $"Phrases: {arrangement.PhraseIterations.Count}"
                    ]

                    TextBlock.create [
                        TextBlock.text $"Sections: {arrangement.Sections.Count}"
                    ]

                    TextBlock.create [
                        TextBlock.text $"Levels: {arrangement.Levels.Count}"
                    ]

                    if arrangement.Levels.Count = 1 then
                        TextBlock.create [
                            TextBlock.text $"Notes: {arrangement.Levels[0].Notes.Count}"
                        ]
                        TextBlock.create [
                            TextBlock.text $"Chords: {arrangement.Levels[0].Chords.Count}"
                        ]
                ]
            ]

            TextBlock.create [
                TextBlock.text $"Arrangement Properties"
                TextBlock.fontSize 18.
            ]

            Grid.create [
                Grid.columnDefinitions "*,*,*"
                Grid.rowDefinitions (Seq.replicate 8 "*" |> String.concat ",")
                Grid.children [
                    checkBox 0 0 arrProps.BarreChords "Barre Chords"
                    checkBox 0 1 arrProps.Bends "Bends"
                    checkBox 0 2 arrProps.DoubleStops "Double Stops"
                    checkBox 0 3 arrProps.DropDPower "Drop D Power Chords"
                    checkBox 0 4 arrProps.FifthsAndOctaves "Fifths and Octaves"
                    checkBox 0 5 arrProps.FingerPicking "Finger Picking"
                    checkBox 0 6 arrProps.Harmonics "Natural Harmonics"
                    checkBox 0 7 arrProps.PinchHarmonics "Pinch Harmonics"
                    checkBox 1 0 arrProps.SlapPop "Slap/Pop"
                    checkBox 1 1 arrProps.Sustain "Sustains"
                    checkBox 1 2 arrProps.Tapping "Tapping"
                    checkBox 1 3 arrProps.TwoFingerPicking "Two Finger Picking"
                    checkBox 1 4 arrProps.PalmMutes "Palm Mutes"
                    checkBox 1 5 arrProps.FretHandMutes "Frethand Mutes"
                    checkBox 1 6 arrProps.Hopo "HOPO"
                    checkBox 1 7 arrProps.NonStandardChords "Non-Standard Chords"
                    checkBox 2 0 arrProps.OpenChords "Open Chords"
                    checkBox 2 1 arrProps.PowerChords "Power Chords"
                    checkBox 2 2 arrProps.Slides "Slides"
                    checkBox 2 3 arrProps.UnpitchedSlides "Unpitched Slides"
                    checkBox 2 4 arrProps.Syncopation "Syncopation"
                    checkBox 2 5 arrProps.Tremolo "Tremolo Picking"
                    checkBox 2 6 arrProps.Vibrato "Vibrato"
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
