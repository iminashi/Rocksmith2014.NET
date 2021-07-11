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
open System.IO
open System.Reflection
open Microsoft.Extensions.FileProviders
open Rocksmith2014.Common
open Rocksmith2014.DLCProject

type MainWindow(commandLineArgs: string array) as this =
    inherit HostWindow()

    let shouldAutoSave newState oldState =
        newState.Config.AutoSave &&
        newState.OpenProjectFile.IsSome &&
        newState.Project <> oldState.Project

    do
        let embeddedProvider = EmbeddedFileProvider(Assembly.GetExecutingAssembly())
        use iconData = embeddedProvider.GetFileInfo("Assets/icon.ico").CreateReadStream()
        base.Icon <- WindowIcon(iconData)
        base.Title <- "Rocksmith 2014 DLC Builder"
        //base.TransparencyLevelHint <- WindowTransparencyLevel.AcrylicBlur
        //base.Background <- Brushes.Transparent
        //base.ExtendClientAreaToDecorationsHint <- true
        //base.ExtendClientAreaChromeHints <- ExtendClientAreaChromeHints.PreferSystemChrome
        //base.ExtendClientAreaTitleBarHeightHint <- -1.0
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

        let view' = Views.Main.view this

        // Add an exception handler to the update function
        let update' msg state =
            try
                let newState, cmd = Main.update msg state
                let commands =
                    if shouldAutoSave newState state then
                        let autoSaveCmd = Cmd.OfAsync.optionalResult (Utils.autoSave ())
                        Cmd.batch [ autoSaveCmd; cmd ]
                    else
                        cmd

                newState, commands
            with ex ->
                // Close the DB connection in case of an unexpected error
                match state.Overlay with
                | ToneCollection cs ->
                    ToneCollection.disposeCollection cs.ActiveCollection
                | _ ->
                    ()

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
        #if DEBUG
        |> Program.withTrace (fun msg _state -> Diagnostics.Debug.WriteLine msg)
        #endif
        |> Program.runWith commandLineArgs

type App() =
    inherit Application()

    let deleteExitCheckFile _ =
        if File.Exists Configuration.exitCheckFilePath then
            File.Delete Configuration.exitCheckFilePath

    override this.Initialize() =
        this.Styles.Add (FluentTheme(baseUri = null, Mode = FluentThemeMode.Dark))
        this.Styles.Load "avares://DLCBuilder/Styles.xaml"
        this.Name <- "Rocksmith 2014 DLC Builder"

    override this.OnFrameworkInitializationCompleted() =
        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktopLifetime ->
            // Delete the exit check file to indicate that the application exited normally
            desktopLifetime.Exit.Add deleteExitCheckFile

            desktopLifetime.MainWindow <- MainWindow(desktopLifetime.Args)
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
