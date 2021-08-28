namespace DLCBuilder

open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.FuncUI
open Avalonia.Themes.Fluent
open System
open System.IO

type App() =
    inherit Application()

    override this.Initialize() =
        this.Styles.Add (FluentTheme(baseUri = null, Mode = FluentThemeMode.Dark))
        this.Styles.Load "avares://DLCBuilder/Styles.xaml"
        this.Name <- "Rocksmith 2014 DLC Builder"

    override this.OnFrameworkInitializationCompleted() =
        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktopLifetime ->
            // Delete the exit check file to indicate that the application exited normally
            desktopLifetime.Exit.Add <| fun _ ->
                if File.Exists Configuration.exitCheckFilePath then
                    File.Delete Configuration.exitCheckFilePath

            desktopLifetime.MainWindow <- MainWindow(desktopLifetime.Args)
            base.OnFrameworkInitializationCompleted()
        | _ ->
            ()
