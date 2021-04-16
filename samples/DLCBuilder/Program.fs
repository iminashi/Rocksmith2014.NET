namespace DLCBuilder

open Elmish
open Avalonia
open Avalonia.FuncUI
open Avalonia.FuncUI.Elmish
open Avalonia.FuncUI.Components.Hosts
open Avalonia.Controls
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Media
open Avalonia.Platform
open Avalonia.Themes.Fluent
open System
open System.Reflection
open Microsoft.Extensions.FileProviders
open Rocksmith2014.Common

type MainWindow() as this =
    inherit HostWindow()
    do
        let embeddedProvider = EmbeddedFileProvider(Assembly.GetExecutingAssembly())
        use iconData = embeddedProvider.GetFileInfo("Assets/icon.ico").CreateReadStream()
        base.Icon <- WindowIcon(iconData)
        base.Title <- "Rocksmith 2014 DLC Builder"
        //base.TransparencyLevelHint <- WindowTransparencyLevel.AcrylicBlur
        //base.Background <- Brushes.Transparent
        //base.ExtendClientAreaToDecorationsHint <- true
        //base.ExtendClientAreaChromeHints <- ExtendClientAreaChromeHints.PreferSystemChrome
        base.Width <- 1150.0
        //base.Height <- 850.0
        base.MinWidth <- 970.0
        base.MinHeight <- 700.0

        let hotKeysSub _initialModel = Cmd.ofSub (HotKeys.handleEvent >> this.KeyDown.Add)

        let progressReportingSub _ =
            let sub dispatch =
                let dispatchProgress task progress = TaskProgressChanged(task, progress) |> dispatch
                Main.arrangementCheckProgress.ProgressChanged.Add(dispatchProgress ArrangementCheck)
                Main.psarcImportProgress.ProgressChanged.Add(dispatchProgress PsarcImport)
                Tools.psarcUnpackProgress.ProgressChanged.Add(dispatchProgress PsarcUnpack)
                Utils.packageBuildProgress.ProgressChanged.Add(dispatchProgress BuildPackage)
            Cmd.ofSub sub

        //this.VisualRoot.VisualRoot.Renderer.DrawFps <- true
        //this.VisualRoot.VisualRoot.Renderer.DrawDirtyRects <- true

        let arg =
            match Environment.GetCommandLineArgs() with
            | [| _; arg |] -> Some arg
            | _ -> None

        let view' = Views.Main.view this

        // Add an exception handler to the update function
        let update' msg state =
            try Main.update msg state
            with ex ->
                let errorMessage =
                    $"Unhandled exception in the update function.\nMessage: {msg}\nException: {ex.Message}"
                let newState =
                    { state with StatusMessages = []
                                 RunningTasks = Set.empty
                                 Overlay = ErrorMessage(errorMessage, Option.ofString ex.StackTrace) }
                newState, Cmd.none

        Program.mkProgram Main.init update' view'
        |> Program.withHost this
        |> Program.withSubscription hotKeysSub
        |> Program.withSubscription progressReportingSub
        |> Program.runWith arg

type App() =
    inherit Application()

    override this.Initialize() =
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
