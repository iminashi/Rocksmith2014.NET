﻿module DLCBuilder.IssueViewer

open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Controls
open Avalonia.Controls.Shapes
open Avalonia.Layout
open Avalonia.Media
open System
open Rocksmith2014.XML.Processing.Utils
open Rocksmith2014.XML.Processing.ArrangementChecker

let issueToString (loc: ILocalization) issueType =
    match issueType with
    | EventBetweenIntroApplause eventCode ->
        loc.Format "EventBetweenIntroApplause" [| eventCode |]
    | AnchorNotOnNote distance ->
        loc.Format "AnchorNotOnNote" [| distance |]
    | LyricWithInvalidChar invalidChar ->
        loc.Format "LyricWithInvalidChar" [| invalidChar |]
    | other ->
        loc.GetString (string other)

let view state dispatch (issues: Issue list) =
    let issues =
        issues
        |> List.groupBy (fun issue -> issue.Type)
        |> List.map (fun (issueType, issues) ->
            let issuesStr =
                issues
                |> List.map (fun issue -> timeToString issue.TimeCode)
                |> List.reduce (fun acc elem -> acc + ", " + elem)
            $"{issueToString state.Localization issueType}:\n{issuesStr}")

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
                    TextBlock.create [
                        TextBlock.fontSize 16.
                        TextBlock.text (String.Join("\n\n", issues))
                        TextBlock.margin 10.0
                        TextBlock.maxWidth 580.
                        TextBlock.textWrapping TextWrapping.Wrap
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
