namespace TestApp

open Elmish
open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.FuncUI
open Avalonia.FuncUI.Elmish
open Avalonia.FuncUI.Hosts

type MainWindow() as this =
    inherit HostWindow()

    do
        base.Title <- "Tools"
        base.Width <- 800.0
        base.Height <- 700.0

        Elmish.Program.mkProgram Tools.init Tools.update Tools.view
        |> Program.withHost this
        |> Program.run

type App() =
    inherit Application()

    override this.Initialize() =
        this.Styles.Load("avares://Avalonia.Themes.Default/DefaultTheme.xaml")
        this.Styles.Load("avares://Avalonia.Themes.Default/Accents/BaseDark.xaml")

    override this.OnFrameworkInitializationCompleted() =
        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktopLifetime ->
            desktopLifetime.MainWindow <- MainWindow()
        | _ ->
            ()

module Program =

    [<EntryPoint>]
    let main(args: string[]) =
        AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .UseSkia()
            .StartWithClassicDesktopLifetime(args)
