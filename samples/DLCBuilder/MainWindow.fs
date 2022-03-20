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

        let hotKeysSub _initialModel = Cmd.ofSub (HotKeys.handleEvent >> this.KeyDown.Add)

        let programClosingSub _ =
            fun dispatch ->
                this.Closing.Add(fun _ ->
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

                    dispatch ProgramClosing)
            |> Cmd.ofSub

        let autoSaveSub _ =
            fun dispatch ->
                autoSaveSubject
                    .Throttle(TimeSpan.FromSeconds(1.))
                    .Add(fun () -> dispatch AutoSaveProject)
            |> Cmd.ofSub

        let progressReportingSub _ =
            fun dispatch ->
                let dispatchProgress task progress = TaskProgressChanged(task, progress) |> dispatch
                ProgressReporters.ArrangementCheck.ProgressChanged.Add(dispatchProgress ArrangementCheckAll)
                ProgressReporters.PsarcImport.ProgressChanged.Add(dispatchProgress PsarcImport)
                ProgressReporters.PsarcUnpack.ProgressChanged.Add(dispatchProgress PsarcUnpack)
                ProgressReporters.PackageBuild.ProgressChanged.Add(dispatchProgress BuildPackage)
                ProgressReporters.DownloadFile.ProgressChanged.Add(fun (id, progress) -> dispatchProgress (FileDownload id) progress)
            |> Cmd.ofSub

        let idRegenerationConfirmationSub _ =
            fun dispatch ->
                IdRegenerationHelper.RequestConfirmation.Add(ConfirmIdRegeneration >> dispatch)
                IdRegenerationHelper.NewIdsGenerated.Add(SetNewArrangementIds >> dispatch)
            |> Cmd.ofSub

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

        let init' =
            InitState.init (Localization.toInterface ()) (AvaloniaBitmapLoader.createInterface ()) databaseConnector

        FocusHelper.init this

        Program.mkProgram init' update' view'
        |> Program.withHost this
        |> Program.withSubscription hotKeysSub
        |> Program.withSubscription progressReportingSub
        |> Program.withSubscription autoSaveSub
        |> Program.withSubscription idRegenerationConfirmationSub
        |> Program.withSubscription programClosingSub
        #if DEBUG
        |> Program.withTrace (fun msg _state -> Diagnostics.Debug.WriteLine(msg))
        #endif
        |> Program.runWith commandLineArgs
