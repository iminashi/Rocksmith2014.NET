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
open System
open System.Reflection
open Microsoft.Extensions.FileProviders

//type MainControl() as this =
//    inherit HostControl()
//    do
//        Elmish.Program.mkProgram MainView.init MainView.update MainView.view
//        |> Program.withHost this
//        |> Program.run

type MainWindow() as this =
    inherit HostWindow()
    do
        let embeddedProvider = EmbeddedFileProvider(Assembly.GetExecutingAssembly())
        use iconData = embeddedProvider.GetFileInfo("Assets/icon.ico").CreateReadStream()
        base.Icon <- WindowIcon(iconData)
        base.Title <- "Rocksmith 2014 DLC Builder"
        base.Width <- 1100.0
        base.Height <- 850.0
        base.MinWidth <- 900.0
        base.MinHeight <- 670.0

        let handleHotkeys dispatch (event: KeyEventArgs) =
            match event.KeyModifiers, event.Key with
            | KeyModifiers.Control, Key.O -> dispatch (Msg.OpenFileDialog("selectProjectFile", Dialogs.projectFilter, OpenProject))
            | KeyModifiers.Control, Key.S -> dispatch ProjectSaveOrSaveAs
            | KeyModifiers.Control, Key.P -> dispatch ImportProfileTones
            | KeyModifiers.Control, Key.T -> dispatch (Msg.OpenFileDialog("selectImportToolkitTemplate", Dialogs.toolkitFilter, ImportToolkitTemplate))
            | KeyModifiers.Control, Key.A -> dispatch (Msg.OpenFileDialog("selectImportPsarc", Dialogs.psarcFilter, SelectImportPsarcFolder))
            | _ -> ()

        let hotKeysSub _initialModel =
            Cmd.ofSub (fun dispatch -> this.KeyDown.Add(handleHotkeys dispatch))
      
        //this.VisualRoot.VisualRoot.Renderer.DrawFps <- true
        //this.VisualRoot.VisualRoot.Renderer.DrawDirtyRects <- true

        let arg =
            match Environment.GetCommandLineArgs() with
            | [| _; arg |] -> Some arg
            | _ -> None

        let view' = Main.view this

        Program.mkProgram Main.init Main.update view'
        |> Program.withHost this
        |> Program.withSubscription hotKeysSub
        |> Program.runWith arg
        
type App() =
    inherit Application()

    //interface ILiveView with
    //    member _.CreateView(window: Window) = MainControl() :> obj

    override this.Initialize() =
        this.Styles.Load "avares://Avalonia.Themes.Default/DefaultTheme.xaml"
        this.Styles.Load "avares://Avalonia.Themes.Default/Accents/BaseDark.xaml"
        this.Styles.Load "avares://DLCBuilder/Styles.xaml"
        this.Name <- "Rocksmith 2014 DLC Builder"

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
            //.With(AvaloniaNativePlatformOptions(UseGpu = false))
            .UseSkia()
            .StartWithClassicDesktopLifetime(args)
