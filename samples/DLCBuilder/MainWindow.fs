namespace DLCBuilder

open Avalonia
open Avalonia.Controls
open Avalonia.FuncUI.Elmish
open Avalonia.FuncUI.Hosts
open Avalonia.Platform
open Elmish
open Microsoft.Extensions.FileProviders
open System
open System.IO
open System.Reactive.Disposables
open System.Reactive.Linq
open System.Reactive.Subjects
open System.Reflection
open System.Text.Json
open ToneCollection

[<CLIMutable>]
type WindowStatus =
    { X: int
      Y: int
      Width: float
      Height: float
      State: WindowState }

    static member SavePath = Path.Combine(Configuration.appDataFolder, "window_state.json")

    static member TryLoad() =
        try
            if File.Exists(WindowStatus.SavePath) then
                use file = File.OpenRead(WindowStatus.SavePath)
                JsonSerializer.Deserialize<WindowStatus>(file) |> Some
            else
                None
        with _ ->
            None

    static member Save(state: WindowStatus) =
        try
            use file = File.Create(WindowStatus.SavePath)
            JsonSerializer.Serialize(file, state)
        with _ ->
            ()

type private WindowClosingState =
    | ClosingMayRequireConfirmation
    | ClosingAllowed

type MainWindow(commandLineArgs: string array) as this =
    inherit HostWindow()

    let autoSaveSubject = new Subject<unit>()

    let shouldAutoSave newState oldState msg =
        match msg with
        | ProjectLoaded _ ->
            false
        | _ ->
            newState.Config.AutoSave
            && newState.OpenProjectFile.IsSome
            && newState.Project <> oldState.Project

    let mutable windowPosition = this.Position
    let mutable windowSize = Size(this.Width, this.Height)
    let mutable windowClosingState = ClosingMayRequireConfirmation

    let saveWindowState () =
        { X = windowPosition.X
          Y = windowPosition.Y
          Width = windowSize.Width
          Height = windowSize.Height
          State =
            if this.WindowState = WindowState.Maximized then
                WindowState.Maximized
            else
                WindowState.Normal }
        |> WindowStatus.Save

    do
        this
            .GetObservable(Window.WidthProperty)
            .Add(fun w ->
                if this.WindowState = WindowState.Normal then
                    windowSize <- windowSize.WithWidth w)

        this
            .GetObservable(Window.HeightProperty)
            .Add(fun h ->
                if this.WindowState = WindowState.Normal then
                    windowSize <- windowSize.WithHeight h)

        this
            .PositionChanged
            .Add(fun args ->
                if this.WindowState = WindowState.Normal then
                    windowPosition <- args.Point)

        let embeddedProvider = EmbeddedFileProvider(Assembly.GetExecutingAssembly())
        use iconData = embeddedProvider.GetFileInfo("Assets/icon.ico").CreateReadStream()

        base.Icon <- WindowIcon(iconData)
        base.Title <- "Rocksmith 2014 DLC Builder"

        let customTitleBar, minHeight =
            if OperatingSystem.IsWindowsVersionAtLeast(10) then
                base.ExtendClientAreaToDecorationsHint <- true
                base.ExtendClientAreaChromeHints <- ExtendClientAreaChromeHints.NoChrome
                base.ExtendClientAreaTitleBarHeightHint <- -1.

                this.GetObservable(Window.WindowStateProperty)
                    .Add(fun state ->
                        if state = WindowState.Maximized then
                            this.Padding <- Thickness(8.)
                        else
                            this.Padding <- Thickness(0.))

                Some(TitleBarButtons(this)), 640.0
            else
                None, 670.0

        base.MinWidth <- 970.0
        base.MinHeight <- minHeight

        match WindowStatus.TryLoad() with
        | None ->
            base.WindowStartupLocation <- WindowStartupLocation.CenterScreen
            base.Width <- 1150.0
        | Some status ->
            base.Width <- max status.Width base.MinWidth
            base.Height <- max status.Height base.MinHeight
            base.Position <- PixelPoint(max 0 status.X, max 0 status.Y)
            base.WindowState <- status.State

        let subscriptions _model =
            let handleHotKey dispatch =
                HotKeys.handleEvent dispatch |> this.KeyDown.Subscribe

            let programClosing dispatch =
                this.Closing.Subscribe(fun args ->
                    match windowClosingState with
                    | ClosingMayRequireConfirmation ->
                        args.Cancel <- true
                        dispatch ProgramClosing
                    | ClosingAllowed ->
                        args.Cancel <- false
                        saveWindowState ())

            let autoSave dispatch =
                autoSaveSubject
                    .Throttle(TimeSpan.FromSeconds(1.))
                    .Subscribe(fun () -> dispatch AutoSaveProject)

            let progressReport dispatch : IDisposable =
                let dispatchProgress task progress = TaskProgressChanged(task, progress) |> dispatch
                let disposables = [
                    ProgressReporters.ArrangementCheck.ProgressChanged.Subscribe(dispatchProgress ArrangementCheckAll)
                    ProgressReporters.PsarcImport.ProgressChanged.Subscribe(dispatchProgress PsarcImport)
                    ProgressReporters.PsarcUnpack.ProgressChanged.Subscribe(dispatchProgress PsarcUnpack)
                    ProgressReporters.PackageBuild.ProgressChanged.Subscribe(dispatchProgress BuildPackage)
                    ProgressReporters.DownloadFile.ProgressChanged.Subscribe(fun (id, progress) -> dispatchProgress (FileDownload id) progress)
                    ProgressReporters.ProfileCleaner.ProgressChanged.Subscribe(ProfileCleanerProgressChanged >> ToolsMsg >> dispatch)
                ]
                new CompositeDisposable(disposables)

            let idRegenerationConfirmation dispatch : IDisposable =
                let disposables = [
                    IdRegenerationHelper.RequestConfirmation.Subscribe(ConfirmIdRegeneration >> dispatch)
                    IdRegenerationHelper.NewIdsGenerated.Subscribe(SetNewArrangementIds >> dispatch)
                ]
                new CompositeDisposable(disposables)

            let fontGenerated dispatch =
                FontGeneratorHelper.FontGenerated.Subscribe(FontGenerated >> dispatch)

            [
                [ "hotkeys" ], handleHotKey
                [ "programclosing" ], programClosing
                [ "autosave" ], autoSave
                [ "progressreport" ], progressReport
                [ "idregenerationconfirmation" ], idRegenerationConfirmation
                [ "fontgenerated" ], fontGenerated
            ]

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
                    FocusHelper.storeFocusedElement ()
                | { Overlay = overlay }, { Overlay = NoOverlay } when overlay <> NoOverlay ->
                    FocusHelper.restoreRootFocus ()
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
                    { state with
                        StatusMessages = List.empty
                        RunningTasks = Set.empty
                        Overlay = ErrorMessage(errorMessage, Some exnInfo) }

                newState, Cmd.none

        let databaseConnector =
            let appDataTones = Path.Combine(Configuration.appDataFolder, "tones")

            Database.createConnector
                (OfficialDataBasePath <| Path.Combine(appDataTones, "official.db"))
                (UserDataBasePath <| Path.Combine(appDataTones, "user.db"))

        let exitHandler =
            { new IExitHandler with
                member _.Exit () =
                    windowClosingState <- ClosingAllowed
                    this.Close() }

        let init' =
            InitState.init (Localization.toInterface ()) (AvaloniaBitmapLoader.createInterface ()) databaseConnector exitHandler

        FocusHelper.init this

        Program.mkProgram init' update' view'
        |> Program.withHost this
        |> Program.withSubscription subscriptions
        #if DEBUG
        |> Program.withTrace (fun msg _state _subs -> Diagnostics.Debug.WriteLine(msg))
        #endif
        |> Program.runWithAvaloniaSyncDispatch commandLineArgs
