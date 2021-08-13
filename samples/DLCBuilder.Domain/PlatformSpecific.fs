namespace DLCBuilder

open System

type private SupportedPlatform =
    | Windows
    | MacOS
    | Linux

type PlatformSpecific() =
    static let current =
        if OperatingSystem.IsMacOS() then
            MacOS
        elif OperatingSystem.IsLinux() then
            Linux
        else
            Windows

    static member Value(mac, windows, linux) =
        match current with
        | Windows -> windows
        | MacOS -> mac
        | Linux -> linux
