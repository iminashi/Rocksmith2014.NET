#r "nuget: Fake.DotNet.Cli"
#r "nuget: Fake.IO.FileSystem"
// Fix: Unsupported log file format. Latest supported version is 9, the log file has version 13.
#r "nuget: MSBuild.StructuredLogger"

open Fake.DotNet
open Fake.IO.Globbing.Operators

DotNet.restore id "Rocksmith2014.sln"

let setOptions (o: DotNet.TestOptions) =
    { o with NoRestore = true
             Configuration = DotNet.BuildConfiguration.Release }

!! "tests/**/*.fsproj"
|> Seq.iter (DotNet.test setOptions)

!! "tests/**/*.csproj"
|> Seq.iter (DotNet.test setOptions)
