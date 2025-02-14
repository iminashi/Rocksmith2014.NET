module DLCBuilder.Views.ErrorMessage

open Avalonia
open Avalonia.Controls
open Avalonia.Controls.Primitives
open Avalonia.Controls.Shapes
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open Avalonia.Media
open DLCBuilder

let private moreInformation moreInfo =
    Expander.create [
        Expander.header (
            hStack [
                locText "AdditionalInformation" []
                iconButton Media.Icons.copy [
                    ToolTip.tip (translate "CopyInformation")
                    Button.onClick (fun e ->
                        match e.Source with
                        | :? Visual as v ->
                            TopLevel.GetTopLevel(v).Clipboard.SetTextAsync(moreInfo)
                            |> ignore
                        | _ ->
                            ()
                    )
                ]
            ]
        )
        Expander.horizontalAlignment HorizontalAlignment.Stretch
        Expander.maxHeight 400.
        Expander.maxWidth 600.
        Expander.background "#181818"
        Expander.content (
            TextBox.create [
                TextBox.horizontalScrollBarVisibility ScrollBarVisibility.Auto
                TextBox.verticalScrollBarVisibility ScrollBarVisibility.Auto
                TextBox.text moreInfo
                TextBox.isReadOnly true
            ]
        )
    ]

let view dispatch data =
    StackPanel.create [
        StackPanel.spacing 8.
        StackPanel.maxWidth 750.
        StackPanel.minWidth 500.
        StackPanel.children [
            StackPanel.create [
                StackPanel.orientation Orientation.Horizontal
                StackPanel.horizontalAlignment HorizontalAlignment.Center
                StackPanel.children [
                    Path.create [
                        Path.fill Brushes.Gray
                        Path.data Media.Icons.alertRound
                        Path.verticalAlignment VerticalAlignment.Center
                        Path.margin (0., 0., 14., 0.)
                    ]
                    locText "Error" [
                        TextBlock.fontSize 18.
                    ]
                ]
            ]

            ScrollViewer.create [
                ScrollViewer.maxHeight 540.
                ScrollViewer.content (
                    StackPanel.create [
                        StackPanel.margin (5., 2.)
                        StackPanel.children [
                            for msg, info in data do
                                // Message
                                TextBlock.create [
                                    TextBlock.fontSize 16.
                                    TextBlock.text msg
                                    TextBlock.margin 10.0
                                    TextBlock.textWrapping TextWrapping.Wrap
                                ]

                                match info with
                                | None ->
                                    ()
                                | Some moreInfo ->
                                    moreInformation moreInfo
                        ]
                    ]
                )

            ]

            // OK button
            Button.create [
                Button.fontSize 18.
                Button.padding (80., 10.)
                Button.horizontalAlignment HorizontalAlignment.Center
                Button.content (translate "OK")
                Button.onClick (fun _ -> dispatch (CloseOverlay OverlayCloseMethod.OverlayButton))
            ]
        ]
    ] |> generalize
