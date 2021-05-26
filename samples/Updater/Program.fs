open System
open System.IO
open System.Diagnostics

let rec waitForBuilderExit count = async {
    let processes =
        Process.GetProcessesByName "DLCBuilder"
        
    if processes.Length > 0 then
        if count >= 5 then
            ()
        else
            do! Async.Sleep 200
            do! waitForBuilderExit (count + 1) }

[<EntryPoint>]
let main argv =
    try
        if argv.Length >= 2 then
            printfn "Waiting for the DLC Builder process to exit..."

            async { do! waitForBuilderExit 0 } |> Async.RunSynchronously

            printfn "Copying files..."

            let sourceDirectory = argv.[0]
            let targetDirectory = argv.[1]

            Directory.EnumerateFiles(sourceDirectory, "*.*", SearchOption.AllDirectories)
            |> Seq.iter (fun path ->
                let targetPath =
                    let rel = Path.GetRelativePath(sourceDirectory, path)
                    Path.Combine(targetDirectory, rel)

                Directory.CreateDirectory(Path.GetDirectoryName targetPath) |> ignore

                File.Copy(path, targetPath, overwrite=true))

            let builderPath = Path.Combine(targetDirectory, "DLCBuilder")
            let startInfo = ProcessStartInfo(FileName = builderPath)
            use dlcBuilder = new Process(StartInfo = startInfo)
            dlcBuilder.Start() |> ignore
        0
    with e ->
        printfn $"Update failed: {e.Message}"
        printfn "%s" e.StackTrace

        printfn "Press any key..."
        Console.ReadLine() |> ignore
        1
