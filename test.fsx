#r "nuget: Fake.DotNet.Cli"
#r "nuget: Fake.IO.FileSystem"
// Fix: Unsupported log file format. Latest supported version is 9, the log file has version 13.
#r "nuget: MSBuild.StructuredLogger"

open System
open Fake.DotNet
open Fake.IO.Globbing.Operators

DotNet.restore id "Rocksmith2014.sln"

let runtime =
    if OperatingSystem.IsMacOS() then
        "osx-x64"
    elif OperatingSystem.IsLinux() then
        "linux-x64"
    else
        "win-x64"

let runTest (projectPath: string) =
    let rid =
        if projectPath.EndsWith "Audio.Tests.fsproj" ||
           projectPath.EndsWith "DLCProject.Tests.fsproj"
        then
            $" --runtime {runtime}"
        else
            String.Empty

    let result = DotNet.exec id "test" $"\"{projectPath}\" --no-restore -c Release{rid}"
    if result.ExitCode <> 0 then
        failwith "Test failed."

!! "tests/**/*.fsproj"
|> Seq.iter runTest

!! "tests/**/*.csproj"
|> Seq.iter runTest