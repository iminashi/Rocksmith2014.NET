namespace DLCBuilder

open Elmish
open Avalonia
open Avalonia.Input
open Avalonia.FuncUI
open Avalonia.FuncUI.Elmish
open Avalonia.FuncUI.Components.Hosts
open Avalonia.Controls
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Themes.Fluent
open System
open System.Reflection
open Microsoft.Extensions.FileProviders

type MainWindow() as this =
    inherit HostWindow()
    do
        let embeddedProvider = EmbeddedFileProvider(Assembly.GetExecutingAssembly())
        use iconData = embeddedProvider.GetFileInfo("Assets/icon.ico").CreateReadStream()
        base.Icon <- WindowIcon(iconData)
        base.Title <- "Rocksmith 2014 DLC Builder"
        base.Width <- 1200.0
        base.Height <- 850.0
        base.MinWidth <- 1100.0
        base.MinHeight <- 750.0

        let handleHotkeys dispatch (event: KeyEventArgs) =
            match event.KeyModifiers, event.Key with
            | KeyModifiers.Control, Key.O -> dispatch (Msg.OpenFileDialog("selectProjectFile", Dialogs.projectFilter, OpenProject))
            | KeyModifiers.Control, Key.S -> dispatch ProjectSaveOrSaveAs
            | KeyModifiers.Control, Key.P -> dispatch ImportProfileTones
            | KeyModifiers.Control, Key.N -> dispatch NewProject
            | KeyModifiers.Control, Key.T -> dispatch (Msg.OpenFileDialog("selectImportToolkitTemplate", Dialogs.toolkitFilter, ImportToolkitTemplate))
            | KeyModifiers.Control, Key.A -> dispatch (Msg.OpenFileDialog("selectImportPsarc", Dialogs.psarcFilter, SelectImportPsarcFolder))
            | KeyModifiers.None, Key.Escape -> dispatch CloseOverlay
            | _ -> ()

        let hotKeysSub _initialModel = Cmd.ofSub (handleHotkeys >> this.KeyDown.Add)
      
        //this.VisualRoot.VisualRoot.Renderer.DrawFps <- true
        //this.VisualRoot.VisualRoot.Renderer.DrawDirtyRects <- true

        let arg =
            match Environment.GetCommandLineArgs() with
            | [| _; arg |] -> Some arg
            | _ -> None

        let view' = Views.Main.view this

        Program.mkProgram Main.init Main.update view'
        |> Program.withHost this
        |> Program.withSubscription hotKeysSub
        |> Program.runWith arg
        
type App() =
    inherit Application()

    override this.Initialize() =
        //this.Styles.Load "avares://Avalonia.Themes.Default/DefaultTheme.xaml"
        //this.Styles.Load "avares://Avalonia.Themes.Default/Accents/BaseDark.xaml"
        this.Styles.Add (FluentTheme(baseUri = null, Mode = FluentThemeMode.Dark))
        this.Styles.Load "avares://DLCBuilder/Styles.xaml"
        this.Name <- "Rocksmith 2014 DLC Builder"

    override this.OnFrameworkInitializationCompleted() =
        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktopLifetime ->
            desktopLifetime.MainWindow <- MainWindow()
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
