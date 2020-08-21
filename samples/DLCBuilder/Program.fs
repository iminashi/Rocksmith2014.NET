namespace DLCBuilder

open Elmish
open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Input
open Avalonia.FuncUI
open Avalonia.FuncUI.Elmish
open Avalonia.FuncUI.Components.Hosts
open Live.Avalonia
open Avalonia.Controls

//type MainControl() as this =
//    inherit HostControl()
//    do
//        Elmish.Program.mkProgram MainView.init MainView.update MainView.view
//        |> Program.withHost this
//        |> Program.run

type MainWindow() as this =
    inherit HostWindow()
    do
        base.Title <- "Rocksmith 2014 DLC Builder"
        base.Width <- 1000.0
        base.Height <- 850.0
        base.MinWidth <- 900.0
        base.MinHeight <- 650.0

        let handleHotkeys dispatch (event : KeyEventArgs) =
                    match event.KeyModifiers, event.Key with
                    | KeyModifiers.Control, Key.O -> dispatch SelectOpenProjectFile
                    | KeyModifiers.Control, Key.S -> dispatch ProjectSaveOrSaveAs
                    | KeyModifiers.Control, Key.P -> dispatch ImportProfileTones
                    | _ -> ()

        let hotKeysSub _initialModel =
                    Cmd.ofSub (fun dispatch -> this.KeyDown.Add(handleHotkeys dispatch))

        let closed _ =
            let sub dispatch = this.Closing.Add(fun _ -> dispatch SaveConfiguration)
            Cmd.ofSub sub
        
        //this.VisualRoot.VisualRoot.Renderer.DrawFps <- true
        //this.VisualRoot.VisualRoot.Renderer.DrawDirtyRects <- true

        Elmish.Program.mkProgram Main.init Main.update Main.view
        |> Program.withHost this
        |> Program.withSubscription closed
        |> Program.withSubscription hotKeysSub
        |> Program.run
        
type App() =
    inherit Application()

    //interface ILiveView with
    //    member _.CreateView(window: Window) = MainControl() :> obj

    override this.Initialize() =
        this.Styles.Load "avares://Avalonia.Themes.Default/DefaultTheme.xaml"
        this.Styles.Load "avares://Avalonia.Themes.Default/Accents/BaseDark.xaml"
        this.Styles.Load "avares://DLCBuilder/Styles.xaml"

    override this.OnFrameworkInitializationCompleted() =
        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktopLifetime ->
            desktopLifetime.MainWindow <- MainWindow()
            //let window = new LiveViewHost(this, fun msg -> printfn "%s" msg)
            //window.StartWatchingSourceFilesForHotReloading()
            //desktopLifetime.MainWindow <- window
            //window.Title <- "DLCBuilder"
            //window.Width <- 1000.0
            //window.Height <- 800.0
            //window.Show()
            base.OnFrameworkInitializationCompleted()
        | _ -> ()

module Program =

    [<EntryPoint>]
    let main(args: string[]) =
        AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .UseSkia()
            .StartWithClassicDesktopLifetime(args)