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
open System.Reactive.Linq
open System.Reactive.Subjects
open System.Reflection
open Microsoft.Extensions.FileProviders
open ToneCollection

type MainWindow(commandLineArgs: string array) as this =
    inherit HostWindow()

    let autoSaveSubject = new Subject<unit>()

    let shouldAutoSave newState oldState msg =
        match msg with
        | ProjectLoaded _ ->
            false
        | _ ->
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
        //base.ExtendClientAreaChromeHints <- ExtendClientAreaChromeHints.NoChrome
        base.Width <- 1150.0
        base.MinWidth <- 970.0

        let customTitleBar =
            if OperatingSystem.IsWindowsVersionAtLeast(8) then
                base.ExtendClientAreaToDecorationsHint <- true
                base.ExtendClientAreaTitleBarHeightHint <- 0.0
                base.MinHeight <- 640.0

                this.GetObservable(Window.WindowStateProperty)
                    .Add(fun state ->
                        if state = WindowState.Maximized then
                            this.Padding <- Thickness(8.)
                        else
                            this.Padding <- Thickness(0.))

                Some (TitleBarButtons(this))
            else
                base.MinHeight <- 670.0
                None

        let hotKeysSub _initialModel = Cmd.ofSub (HotKeys.handleEvent >> this.KeyDown.Add)

        let programClosingSub _ =
            Cmd.ofSub <| fun dispatch -> this.Closing.Add(fun _ -> dispatch ProgramClosing)

        let autoSaveSub _ =
            let sub dispatch =
                autoSaveSubject
                    .Throttle(TimeSpan.FromSeconds 1.)
                    .Add(fun () -> dispatch AutoSaveProject)
            Cmd.ofSub sub

        let progressReportingSub _ =
            let sub dispatch =
                let dispatchProgress task progress = TaskProgressChanged(task, progress) |> dispatch
                ProgressReporters.ArrangementCheck.ProgressChanged.Add(dispatchProgress ArrangementCheckAll)
                ProgressReporters.PsarcImport.ProgressChanged.Add(dispatchProgress PsarcImport)
                ProgressReporters.PsarcUnpack.ProgressChanged.Add(dispatchProgress PsarcUnpack)
                ProgressReporters.PackageBuild.ProgressChanged.Add(dispatchProgress BuildPackage)
            Cmd.ofSub sub

        let idRegenerationConfirmationSub _ =
            let sub dispatch =
                IdRegenerationHelper.RequestConfirmation.Add(ConfirmIdRegeneration >> dispatch)
                IdRegenerationHelper.NewIdsGenerated.Add(SetNewArrangementIds >> dispatch)
            Cmd.ofSub sub

        //this.VisualRoot.VisualRoot.Renderer.DrawFps <- true
        //this.VisualRoot.VisualRoot.Renderer.DrawDirtyRects <- true

        let view' = Views.Main.view customTitleBar this

        // Add an exception handler to the update function
        let update' msg state =
            try
                let newState, cmd =
                    match msg with
                    | ShowDialog dialog ->
                        state, Dialogs.showDialog this dialog state
                    | _ ->
                        Main.update msg state
                if shouldAutoSave newState state msg then autoSaveSubject.OnNext()

                // Workaround for focus issues when opening / closing overlays
                match state, newState with
                | { Overlay = NoOverlay }, { Overlay = overlay } when overlay <> NoOverlay ->
                    FocusHelper.storeFocusedElement()
                | { Overlay = overlay }, { Overlay = NoOverlay } when overlay <> NoOverlay ->
                    FocusHelper.restoreRootFocus()
                | _ ->
                    ()

                newState, cmd
            with ex ->
                // Close the DB connection in case of an unexpected error
                match state.Overlay with
                | ToneCollection cs ->
                    CollectionState.disposeCollection cs.ActiveCollection
                | _ ->
                    ()

                let errorMessage =
                    $"Unhandled exception in the update function.\nMessage: {msg}\nException: {ex.Message}"
                let exnInfo =
                    Utils.createExceptionInfoString ex
                let newState =
                    { state with StatusMessages = List.empty
                                 RunningTasks = Set.empty
                                 Overlay = ErrorMessage(errorMessage, Some exnInfo) }
                newState, Cmd.none

        let databaseConnector =
            let appDataTones = Path.Combine(Configuration.appDataFolder, "tones")
            Database.createConnector
                (OfficialDataBasePath <| Path.Combine(appDataTones, "official.db"))
                (UserDataBasePath <| Path.Combine(appDataTones, "user.db"))

        let init' = InitState.init (Localization.toInterface()) (AvaloniaBitmapLoader.createInterface()) databaseConnector

        Program.mkProgram init' update' view'
        |> Program.withHost this
        |> Program.withSubscription hotKeysSub
        |> Program.withSubscription progressReportingSub
        |> Program.withSubscription autoSaveSub
        |> Program.withSubscription idRegenerationConfirmationSub
        |> Program.withSubscription programClosingSub
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
        | _ ->
            ()

module Program =
    [<EntryPoint; STAThread>]
    let main(args: string[]) =
        // Set up logging for unhandled exceptions
        AppDomain.CurrentDomain.UnhandledException.Add <| fun args ->
            let logMessage =
                match args.ExceptionObject with
                | :? Exception as e ->
                    let baseInfo = $"Unhandled exception ({DateTime.Now})\n{e.GetType().Name}\nMessage: {e.Message}\nSource: {e.Source}\nTarget Site: {e.TargetSite}\nStack Trace:\n{e.StackTrace}"
                    if notNull e.InnerException then
                        let inner = e.InnerException
                        $"{baseInfo}\n\nInner Exception:\nMessage:{inner.Message}\nSource: {inner.Source}\nTarget Site: {inner.TargetSite}\nStack Trace:\n{inner.StackTrace}"
                    else
                        baseInfo
                | unknown ->
                    $"Unknown exception object: {unknown}"
            File.WriteAllText(Configuration.crashLogPath, logMessage)

        AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .UseSkia()
            .StartWithClassicDesktopLifetime(args)
