module DLCBuilder.Views.PsarcBatchImportSummary

open Avalonia.Controls
open Avalonia.Controls.Shapes
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open Avalonia.Media
open DLCBuilder

let private fileList (header: string) (isExpanded: bool) (items: string list) =
    Expander.create [
        Expander.header header
        Expander.isExpanded isExpanded
        Expander.isEnabled (not items.IsEmpty)
        Expander.horizontalAlignment HorizontalAlignment.Stretch
        Expander.content (
            ScrollViewer.create [
                ScrollViewer.maxHeight 200.
                ScrollViewer.content (
                    ItemsControl.create [
                        ItemsControl.dataItems items
                    ]
                )
            ]
        )
    ]

let view dispatch (result: PsarcBatchImportResult) =
    let failed =
        result.Failed
        |> List.map (fun (fileName, error) -> $"%s{fileName}: %s{error}")

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
                        Path.data Media.Icons.import
                        Path.verticalAlignment VerticalAlignment.Center
                        Path.margin (0., 0., 10., 0.)
                    ]
                    locText "PsarcBatchImportFinished" [
                        TextBlock.fontSize 18.
                    ]
                ]
            ]

            fileList (translatef "PsarcBatchImported" [| result.Imported.Length |]) false result.Imported
            fileList (translatef "PsarcBatchSkipped" [| result.Skipped.Length |]) false result.Skipped
            fileList (translatef "PsarcBatchFailed" [| result.Failed.Length |]) true failed

            StackPanel.create [
                StackPanel.orientation Orientation.Horizontal
                StackPanel.horizontalAlignment HorizontalAlignment.Center
                StackPanel.spacing 8.
                StackPanel.children [
                    // Open folder button
                    Button.create [
                        Button.fontSize 18.
                        Button.padding (40., 10.)
                        Button.content (translate "OpenFolder")
                        Button.onClick (fun _ -> dispatch (OpenWithShell result.TargetFolder))
                    ]

                    // OK button
                    Button.create [
                        Button.fontSize 18.
                        Button.padding (80., 10.)
                        Button.content (translate "OK")
                        Button.onClick (fun _ -> dispatch (CloseModal ModalCloseMethod.UIButton))
                    ]
                ]
            ]
        ]
    ] |> generalize
