module DLCBuilder.IssueViewer

open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Controls
open Avalonia.Controls.Shapes
open Avalonia.Layout
open Avalonia.Media
open Rocksmith2014.XML.Processing.Utils
open Rocksmith2014.XML.Processing.ArrangementChecker

let private getIssueHeaderAndHelp (loc: ILocalization) issueType =
    match issueType with
    | EventBetweenIntroApplause eventCode ->
        loc.Format "EventBetweenIntroApplause" [| eventCode |],
        loc.GetString "EventBetweenIntroApplauseHelp"
    | AnchorNotOnNote distance ->
        loc.Format "AnchorNotOnNote" [| distance |],
        loc.GetString "AnchorNotOnNoteHelp"
    | LyricWithInvalidChar invalidChar ->
        loc.Format "LyricWithInvalidChar" [| invalidChar |],
        loc.GetString "LyricWithInvalidCharHelp"
    | other ->
        let locStr = string other
        loc.GetString locStr,
        loc.GetString (locStr + "Help")

let private viewForIssue (loc: ILocalization) issueType times =
    let header, help = getIssueHeaderAndHelp loc issueType

    StackPanel.create [
        StackPanel.margin (0., 5.)
        StackPanel.children [
            StackPanel.create [
                StackPanel.orientation Orientation.Horizontal
                StackPanel.children [
                    Path.create [
                        Path.fill Brushes.CadetBlue
                        Path.data Media.Icons.help
                        Path.margin (5., 0.)
                        ToolTip.tip help
                    ]
                    TextBlock.create [
                        TextBlock.text header
                        TextBlock.fontSize 14.
                    ]
                ]
            ]
            TextBlock.create [
                TextBlock.text times
                TextBlock.fontSize 14.
                TextBlock.maxWidth 550.
                TextBlock.textWrapping TextWrapping.Wrap
            ]
        ]
    ] :> IView
    

let view state dispatch (issues: Issue list) =
    let issues =
        issues
        |> List.groupBy (fun issue -> issue.Type)
        |> List.map (fun (issueType, issues) ->
            let issueTimes =
                issues
                |> List.map (fun issue -> timeToString issue.TimeCode)
                |> List.reduce (fun acc elem -> acc + ", " + elem)
            viewForIssue state.Localization issueType issueTimes)

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
                        TextBlock.text (state.Localization.GetString "issues")
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
                Button.content (state.Localization.GetString "ok")
                Button.onClick (fun _ -> dispatch CloseOverlay)
            ]
        ]
    ] :> IView
