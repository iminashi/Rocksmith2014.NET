[<AutoOpen>]
module DLCBuilder.CustomTitleBarButtons

open Avalonia
open Avalonia.Controls
open Avalonia.Controls.Shapes
open Avalonia.Input
open Avalonia.Layout
open Avalonia.Media
open Rocksmith2014.Common
open Media

let private createIcon data = Path(Data = data, Fill = Brushes.GhostWhite)

let private baseButton onClick =
    Button()
    |> apply (fun b ->
        b.Classes.Add "icon-btn"
        b.SetValue(KeyboardNavigation.IsTabStopProperty, false) |> ignore
        b.Click.Add (fun _ -> onClick())) 

let private iconButton icon onClick =
    baseButton onClick
    |> apply (fun b -> b.Content <- createIcon icon)

let private stack children =
    StackPanel(Orientation = Orientation.Horizontal)
    |> apply (fun p -> p.Children.AddRange children)

[<Sealed>]
type TitleBarButtons(window: Window) =
    inherit UserControl()

    let close =
        iconButton Icons.xThin (fun () -> window.Close())
        |> apply (fun b -> b.Classes.Add "exit-btn")

    let minimize =
        iconButton Icons.minimize (fun () -> window.WindowState <- WindowState.Minimized)

    let maximize =
        baseButton (fun _ -> maximizeOrRestore window)
        |> apply (fun button ->
            let path = Path(Fill = Brushes.GhostWhite)
            let icon =
                window.GetObservable(Window.WindowStateProperty)
                |> Observable.map (fun state -> if state = WindowState.Maximized then Icons.restore else Icons.maximize)
            path.Bind(Path.DataProperty, icon) |> ignore
            button.Content <- path)

    do base.Content <- stack [ minimize; maximize; close ]
