module DLCBuilder.Program

open Avalonia
open System
open System.IO

/// Sets up logging for unhandled exceptions.
let setupCrashLogging () =
    AppDomain.CurrentDomain.UnhandledException.Add <| fun args ->
        let logMessage =
            match args.ExceptionObject with
            | :? Exception as e ->
                let baseInfo =
                    $"Unhandled exception ({DateTime.Now})\n{e.GetType().Name}\nMessage: {e.Message}\nSource: {e.Source}\nTarget Site: {e.TargetSite}\nStack Trace:\n{e.StackTrace}"

                if notNull e.InnerException then
                    let inner = e.InnerException
                    $"{baseInfo}\n\nInner Exception:\nMessage:{inner.Message}\nSource: {inner.Source}\nTarget Site: {inner.TargetSite}\nStack Trace:\n{inner.StackTrace}"
                else
                    baseInfo
            | unknown ->
                $"Unknown exception object: {unknown}"

        File.WriteAllText(Configuration.crashLogPath, logMessage)

[<EntryPoint; STAThread>]
let main(args: string[]) =
    setupCrashLogging ()

    AppBuilder
        .Configure<App>()
        .UsePlatformDetect()
        .UseSkia()
        .StartWithClassicDesktopLifetime(args)
