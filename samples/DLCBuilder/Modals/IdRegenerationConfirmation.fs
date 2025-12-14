module DLCBuilder.Views.IdRegenerationConfirmation

open Avalonia.Controls
open Avalonia.Controls.Shapes
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open Avalonia.Media
open DLCBuilder
open Rocksmith2014.DLCProject
open System

let private arrangement state =
    DataTemplateView<Arrangement>.create (fun arrangement ->
        let name = ArrangementNameUtils.translateName state.Project ArrangementNameUtils.NameOnly arrangement
        let file = IO.Path.GetFileName(Arrangement.getFile arrangement)

        TextBlock.create [
            TextBlock.text $"{name} ({file})"
            TextBlock.fontSize 16.
        ])

let view state dispatch reply (arrangements: Arrangement list) =
    StackPanel.create [
        StackPanel.spacing 8.
        StackPanel.children [
            StackPanel.create [
                StackPanel.orientation Orientation.Horizontal
                StackPanel.horizontalAlignment HorizontalAlignment.Center
                StackPanel.children [
                    Path.create [
                        Path.fill Brushes.Gray
                        Path.data Media.Icons.help
                        Path.verticalAlignment VerticalAlignment.Center
                        Path.margin (0., 0., 10., 0.)
                    ]
                    locText "RegenerateArrangementIds" [
                        TextBlock.fontSize 18.
                    ]
                ]
            ]

            // Confirmation message
            TextBlock.create [
                TextBlock.fontSize 16.
                TextBlock.text (translate "FollowingIDsShouldBeRegenerated")
                TextBlock.margin 10.0
            ]

            ItemsControl.create [
                ItemsControl.dataItems arrangements
                ItemsControl.horizontalAlignment HorizontalAlignment.Center
                ItemsControl.width 300.
                ItemsControl.itemTemplate (arrangement state)
            ]

            Expander.create [
                Expander.header (translate "AdditionalInformation")
                Expander.width 500.
                Expander.content (
                    TextBlock.create [
                        TextBlock.text (translate "ArrangementIDRegenerationExplanation")
                        TextBlock.maxWidth 500.
                        TextBlock.textWrapping TextWrapping.Wrap
                    ]
                )
            ]

            StackPanel.create [
                StackPanel.orientation Orientation.Horizontal
                StackPanel.horizontalAlignment HorizontalAlignment.Center
                StackPanel.spacing 8.
                StackPanel.children [
                    // Yes button
                    Button.create [
                        Button.fontSize 18.
                        Button.padding (80., 10.)
                        Button.content (translate "Yes")
                        Button.onClick (fun _ ->
                            reply true
                            dispatch (CloseModal ModalCloseMethod.UIButton))
                    ]

                    // No button
                    Button.create [
                        Button.fontSize 18.
                        Button.padding (80., 10.)
                        Button.content (translate "No")
                        Button.onClick (fun _ ->
                            reply false
                            dispatch (CloseModal ModalCloseMethod.UIButton))
                    ]
                ]
            ]
        ]
    ] |> generalize
