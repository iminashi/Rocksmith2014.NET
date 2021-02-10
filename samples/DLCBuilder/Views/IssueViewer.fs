module DLCBuilder.Views.IssueViewer

open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Controls
open Avalonia.Controls.Shapes
open Avalonia.Layout
open Avalonia.Media
open Rocksmith2014.XML.Processing.Utils
open Rocksmith2014.XML.Processing.ArrangementChecker
open DLCBuilder

let private getIssueHeaderAndHelp issueType =
    match issueType with
    | EventBetweenIntroApplause eventCode ->
        translatef "EventBetweenIntroApplause" [| eventCode |],
        translate "EventBetweenIntroApplauseHelp"
    | AnchorNotOnNote distance ->
        translatef "AnchorNotOnNote" [| distance |],
        translate "AnchorNotOnNoteHelp"
    | LyricWithInvalidChar invalidChar ->
        translatef "LyricWithInvalidChar" [| invalidChar |],
        translate "LyricWithInvalidCharHelp"
    | other ->
        let locStr = string other
        translate locStr,
        translate (locStr + "Help")

let private isImportant = function
    | LinkNextMissingTargetNote
    | LinkNextSlideMismatch
    | LinkNextFretMismatch
    | LinkNextBendMismatch
    | IncorrectLinkNext
    | UnpitchedSlideWithLinkNext
    | MissingIgnore
    | MissingBendValue
    | MissingLinkNextChordNotes
    | InvalidShowlights -> true
    | _ -> false

let private viewForIssue issueType times =
    let header, help = getIssueHeaderAndHelp issueType

    StackPanel.create [
        StackPanel.margin (0., 5.)
        StackPanel.children [
            Expander.create [
                Expander.header (
                    TextBlock.create [
                        TextBlock.text header
                        TextBlock.fontSize 16.
                        TextBlock.fontWeight (if isImportant issueType then FontWeight.Bold else FontWeight.Normal)
                    ]
                )
                Expander.content (
                    TextBlock.create [
                        TextBlock.padding (8., 0.)
                        TextBlock.maxWidth 450.
                        TextBlock.textWrapping TextWrapping.Wrap
                        TextBlock.fontSize 14.
                        TextBlock.text help
                    ]
                )
            ]

            TextBlock.create [
                TextBlock.text times
                TextBlock.fontSize 14.
                TextBlock.maxWidth 550.
                TextBlock.textWrapping TextWrapping.Wrap
            ]
        ]
    ] :> IView

let view dispatch (issues: Issue list) =
    let issues =
        issues
        |> List.groupBy (fun issue -> issue.Type)
        |> List.map (fun (issueType, issues) ->
            let issueTimes =
                issues
                |> List.map (fun issue -> timeToString issue.TimeCode)
                |> List.reduce (fun acc elem -> acc + ", " + elem)
            viewForIssue issueType issueTimes)

    StackPanel.create [
        StackPanel.spacing 8.
        StackPanel.children [
            StackPanel.create [
                StackPanel.orientation Orientation.Horizontal
                StackPanel.horizontalAlignment HorizontalAlignment.Center
                StackPanel.children [
                    Path.create [
                        Path.fill Brushes.Gray
                        Path.data Media.Icons.alertTriangle
                        Path.verticalAlignment VerticalAlignment.Center
                        Path.margin (0., 0., 14., 0.)
                    ]
                    TextBlock.create [
                        TextBlock.fontSize 18.
                        TextBlock.text (translate "issues")
                    ]
                ]
            ]
            
            // Issues
            ScrollViewer.create [
                ScrollViewer.maxHeight 500.
                ScrollViewer.maxWidth 600.
                ScrollViewer.content (
                    StackPanel.create [
                        StackPanel.children issues
                    ]
                )
            ]

            // OK button
            Button.create [
                Button.fontSize 18.
                Button.padding (80., 10.)
                Button.horizontalAlignment HorizontalAlignment.Center
                Button.content (translate "ok")
                Button.onClick (fun _ -> dispatch CloseOverlay)
            ]
        ]
    ] :> IView
