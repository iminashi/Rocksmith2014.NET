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
                    $"Unhandled exception (%O{DateTime.Now})\n\
                      %s{e.GetType().Name}\n\
                      Message: %s{e.Message}\n\
                      Source: %s{e.Source}\n\
                      Target Site: %O{e.TargetSite}\n\
                      Stack Trace:\n%s{e.StackTrace}"

                if notNull e.InnerException then
                    let inner = e.InnerException
                    $"%s{baseInfo}\n\nInner Exception:\n\
                      Message: %s{inner.Message}\n\
                      Source: %s{inner.Source}\n\
                      Target Site: %O{inner.TargetSite}\n\
                      Stack Trace:\n%s{inner.StackTrace}"
                else
                    baseInfo
            | unknown ->
                $"Unknown exception object: %A{unknown}"

        File.WriteAllText(Configuration.crashLogPath, logMessage)

[<EntryPoint; STAThread>]
let main(args: string array) =
    setupCrashLogging ()

    AppBuilder
        .Configure<App>()
        .UsePlatformDetect()
        .UseSkia()
        .StartWithClassicDesktopLifetime(args)
