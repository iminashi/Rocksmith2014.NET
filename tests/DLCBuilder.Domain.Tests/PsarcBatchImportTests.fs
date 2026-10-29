module PsarcBatchImportTests

open Expecto
open DLCBuilder
open System
open System.IO
open System.Threading.Tasks

let private createTempFolder () =
    let path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())
    Directory.CreateDirectory(path).FullName

/// Import stub that creates a project file in the project folder.
let private importStub (report: float -> unit) (projectFolder: string) (_: string) : Task =
    Directory.CreateDirectory(projectFolder) |> ignore
    File.WriteAllText(Path.Combine(projectFolder, "project.rs2dlc"), "")
    report 100.
    Task.CompletedTask

let private runBatch importOne targetFolder psarcPaths =
    let reported = ResizeArray<float>()
    let result =
        (StateUtils.importPsarcBatch importOne reported.Add targetFolder psarcPaths).GetAwaiter().GetResult()
    result, List.ofSeq reported

let private testPsarcFolder =
    Path.Combine(__SOURCE_DIRECTORY__, "..", "Rocksmith2014.IntegrationTests", "psarc")

[<Tests>]
let tests =
    testList "PSARC Batch Import Tests" [
        testCase "Each PSARC is imported into its own project folder" <| fun _ ->
            let target = createTempFolder ()

            let result, reported = runBatch importStub target [| "/in/a_p.psarc"; "/in/b_p.psarc" |]

            Expect.equal result.Imported [ "a_p.psarc"; "b_p.psarc" ] "Both files were imported"
            Expect.isEmpty result.Skipped "No files were skipped"
            Expect.isEmpty result.Failed "No files failed"
            Expect.isTrue (File.Exists(Path.Combine(target, "a_p", "project.rs2dlc"))) "Project folder a_p was created"
            Expect.isTrue (File.Exists(Path.Combine(target, "b_p", "project.rs2dlc"))) "Project folder b_p was created"
            Expect.equal (List.last reported) 100. "Progress ends at 100"
            Expect.isTrue (reported |> List.pairwise |> List.forall (fun (a, b) -> a <= b)) "Progress never goes backwards"
            Expect.isTrue (reported |> List.forall (fun p -> p <= 100.)) "Progress never exceeds 100"

        testCase "A PSARC whose project folder contains a project file is skipped" <| fun _ ->
            let target = createTempFolder ()
            importStub ignore (Path.Combine(target, "a_p")) "" |> ignore
            let mutable importedPaths = []
            let importOne report folder (path: string) =
                importedPaths <- path :: importedPaths
                importStub report folder path

            let result, _ = runBatch importOne target [| "/in/a_p.psarc"; "/in/b_p.psarc" |]

            Expect.equal result.Skipped [ "a_p.psarc" ] "a_p was skipped"
            Expect.equal result.Imported [ "b_p.psarc" ] "b_p was imported"
            Expect.equal importedPaths [ "/in/b_p.psarc" ] "The importer was not called for the skipped file"

        testCase "A project folder without a project file (interrupted import) is imported again" <| fun _ ->
            let target = createTempFolder ()
            Directory.CreateDirectory(Path.Combine(target, "a_p")) |> ignore

            let result, _ = runBatch importStub target [| "/in/a_p.psarc" |]

            Expect.equal result.Imported [ "a_p.psarc" ] "a_p was imported"
            Expect.isEmpty result.Skipped "Nothing was skipped"

        testCase "A failed import does not stop the batch and its partial folder is removed" <| fun _ ->
            let target = createTempFolder ()
            let importOne report (folder: string) (path: string) : Task =
                if path.Contains("bad") then
                    Directory.CreateDirectory(folder) |> ignore
                    File.WriteAllText(Path.Combine(folder, "partial.xml"), "")
                    raise (InvalidOperationException("Corrupt file"))
                else
                    importStub report folder path

            let result, reported = runBatch importOne target [| "/in/bad_p.psarc"; "/in/good_p.psarc" |]

            Expect.equal result.Failed [ "bad_p.psarc", "Corrupt file" ] "The failure and its error are reported"
            Expect.equal result.Imported [ "good_p.psarc" ] "The next file was still imported"
            Expect.isFalse (Directory.Exists(Path.Combine(target, "bad_p"))) "The partial project folder was removed"
            Expect.equal (List.last reported) 100. "Progress ends at 100"

        testCase "A failed import keeps a project folder that existed before" <| fun _ ->
            let target = createTempFolder ()
            let existing = Path.Combine(target, "bad_p")
            Directory.CreateDirectory(existing) |> ignore
            File.WriteAllText(Path.Combine(existing, "notes.txt"), "keep me")
            let importOne _ _ _ : Task = raise (InvalidOperationException("Corrupt file"))

            let result, _ = runBatch importOne target [| "/in/bad_p.psarc" |]

            Expect.equal result.Failed.Length 1 "The import failed"
            Expect.isTrue (File.Exists(Path.Combine(existing, "notes.txt"))) "The existing folder was not deleted"

        testCase "Real PSARC files are imported, a corrupt one fails" <| fun _ ->
            let target = createTempFolder ()
            let source = createTempFolder ()
            let corrupt = Path.Combine(source, "corrupt_p.psarc")
            File.WriteAllText(corrupt, "not a psarc")
            let psarcs =
                [| Path.Combine(testPsarcFolder, "test_p.psarc")
                   corrupt
                   Path.Combine(testPsarcFolder, "test_m.psarc") |]
            let importOne report folder path =
                StateUtils.importPsarcWithProgress report Configuration.Default folder path :> Task

            let result, _ = runBatch importOne target psarcs
            let secondRun, _ = runBatch importOne target psarcs

            Expect.equal result.Imported [ "test_p.psarc"; "test_m.psarc" ] "Both valid files were imported"
            Expect.equal (result.Failed |> List.map fst) [ "corrupt_p.psarc" ] "The corrupt file failed"
            Expect.isNonEmpty (Directory.GetFiles(Path.Combine(target, "test_p"), "*.rs2dlc")) "test_p project file exists"
            Expect.isNonEmpty (Directory.GetFiles(Path.Combine(target, "test_m"), "*.rs2dlc")) "test_m project file exists"
            Expect.equal secondRun.Skipped [ "test_p.psarc"; "test_m.psarc" ] "A second run skips the imported files"
            Expect.isEmpty secondRun.Imported "A second run imports nothing new"

        testCase "Dropped PSARC files open a single target folder dialog" <| fun _ ->
            let messages = ResizeArray<Msg>()

            StateUtils.handleFilesDrop Configuration.Default [ "/in/a_p.psarc"; "/in/b_p.psarc" ]
            |> Seq.iter (List.iter (fun sub -> sub messages.Add))

            match List.ofSeq messages with
            | [ ShowDialog (Dialog.FolderTarget (FolderTarget.PsarcImportTarget paths)) ] ->
                Expect.equal paths [| "/in/a_p.psarc"; "/in/b_p.psarc" |] "Both files are in the dialog target"
            | other ->
                failwithf "Unexpected messages: %A" other
    ]
