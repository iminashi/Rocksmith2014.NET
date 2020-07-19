module TestApp.Tools

open Avalonia
open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.Threading
open Rocksmith2014.SNG
open Rocksmith2014.SNG.Types
open System.Threading.Tasks
open Avalonia.Layout

let private window =
    lazy ((Application.Current.ApplicationLifetime :?> ApplicationLifetimes.ClassicDesktopStyleApplicationLifetime).MainWindow)

let sngFilters =
    let filter = FileDialogFilter(Extensions = ResizeArray(seq { "sng" }), Name = "SNG Files")
    ResizeArray(seq { filter })

let openFileDialogSingle title filters dispatch = 
    Dispatcher.UIThread.InvokeAsync(
        fun () ->
            OpenFileDialog(Title = title, AllowMultiple = false, Filters = filters)
               .ShowAsync(window.Force())
               .ContinueWith(fun (t: Task<string[]>) -> 
                   match t.Result with
                   | [| file |] -> file |> dispatch
                   | _ -> ())
        ) |> ignore

let ofd = openFileDialogSingle "Select File" sngFilters

type State = { Status:string; Platform:Platform }

let init = { Status = ""; Platform = PC }

type Msg =
    | UnpackFile of file:string
    | ConvertVocals of file:string
    | ConvertInstrumental of file:string
    | RoundTrip of file:string
    | ChangePlatform of Platform

let update (msg: Msg) (state: State) : State =
    match msg with
    | RoundTrip file ->
        try
            SNGFile.readPacked file state.Platform
            |> SNGFile.savePacked (file + "re") state.Platform
            state
        with e -> { state with Status = e.Message }

    | UnpackFile file ->
        try
            SNGFile.unpackFile file state.Platform; state
        with e -> { state with Status = e.Message }

    | ConvertVocals file ->
        try
            Rocksmith2014.Conversion.ConvertVocals.convertSngFileToXml file state.Platform
            state
        with e -> { state with Status = e.Message }

    | ConvertInstrumental file ->
        try
            Rocksmith2014.Conversion.ConvertInstrumental.convertSngFileToXml file state.Platform
            state
        with e -> { state with Status = e.Message }

    | ChangePlatform platform ->
        { state with Platform = platform }

let view (state: State) dispatch =
    StackPanel.create [
        StackPanel.margin 5.0
        StackPanel.spacing 5.0
        StackPanel.children [
            StackPanel.create [
                StackPanel.orientation Orientation.Horizontal
                StackPanel.horizontalAlignment HorizontalAlignment.Center
                StackPanel.children [
                    RadioButton.create [
                        RadioButton.groupName "Platform"
                        RadioButton.content "PC"
                        RadioButton.isChecked (state.Platform = PC)
                        RadioButton.onIsPressedChanged (fun p -> if p then dispatch (ChangePlatform PC))
                    ]
                    RadioButton.create [
                        RadioButton.groupName "Platform"
                        RadioButton.content "Mac"
                        RadioButton.isChecked (state.Platform = Mac)
                        RadioButton.onIsPressedChanged (fun p -> if p then dispatch (ChangePlatform Mac))
                    ]
                ]
            ]
            Button.create [
                Button.onClick (fun _ ->  ofd (RoundTrip >> dispatch))
                Button.content "Round-trip Packed File..."
            ]

            Button.create [
                Button.onClick (fun _ -> ofd (UnpackFile >> dispatch))
                Button.content "Unpack File..."
            ]

            Button.create [
                Button.onClick (fun _ -> ofd (ConvertVocals >> dispatch))
                Button.content "Convert Vocals SNG to XML..."
            ]
            
            Button.create [
                Button.onClick (fun _ -> ofd (ConvertInstrumental >> dispatch))
                Button.content "Convert Instrumental SNG to XML..."
            ]

            TextBlock.create [
                TextBlock.fontSize 28.0
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
                TextBlock.text (string state.Status)
            ]
        ]
    ]       