#r "nuget: Fake.DotNet.Cli"
#r "nuget: Fake.IO.FileSystem"
// Fix: Unsupported log file format. Latest supported version is 9, the log file has version 13.
#r "nuget: MSBuild.StructuredLogger"

open Fake.IO.Globbing.Operators
open Fake.DotNet

DotNet.build id "samples/DLCBuilder/DLCBuilder.fsproj"

!! "tests/**/*.fsproj"
|> Seq.iter (DotNet.test (fun o -> { o with NoBuild = true }))
