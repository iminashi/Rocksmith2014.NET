module EditTests

open Expecto
open System.IO
open Rocksmith2014.PSARC

let copyToMemory (fileName: string) =
    use file = File.OpenRead fileName
    let memory = new MemoryStream(int file.Length)
    file.CopyTo memory
    memory.Position <- 0L
    memory

let options = { Mode = InMemory; EncyptTOC = true }

[<Tests>]
let someTests =
  testList "Edit Files" [

    testCase "Manifest is same after null edit" <| fun _ ->
        use memory = copyToMemory "test_edit_p.psarc"
        use psarc = PSARC.Read memory
        let oldManifest = psarc.Manifest

        psarc.Edit(options, ignore)

        Expect.sequenceEqual psarc.Manifest oldManifest "Manifest is unchanged"

    testCase "Can be read after editing" <| fun _ ->
        use memory = copyToMemory "test_edit_p.psarc"
        let psarc = PSARC.Read memory
        let oldManifest = psarc.Manifest
        let oldToc = psarc.TOC

        psarc.Edit(options, ignore)
        memory.Position <- 0L
        let psarc2 = PSARC.Read memory

        Expect.sequenceEqual psarc2.Manifest oldManifest "Manifest is unchanged"
        for i = 0 to psarc2.TOC.Count - 1 do
            Expect.equal psarc2.TOC.[i].Length oldToc.[i].Length "File length is same"

    testCase "Can remove files" <| fun _ ->
        use memory = copyToMemory "test_edit_p.psarc"
        let psarc = PSARC.Read memory
        let oldManifest = psarc.Manifest
        let oldSize = memory.Length

        // Remove all files ending in "wem" from the archive
        psarc.Edit(options, (fun files -> files.RemoveAll(fun x -> x.Name.EndsWith "wem") |> ignore))
        memory.Position <- 0L
        let psarc2 = PSARC.Read memory

        Expect.equal psarc2.Manifest.Length (oldManifest.Length - 2) "Manifest size is correct"
        Expect.isTrue (memory.Length < oldSize) "Size is smaller"

    testCase "Can add file" <| fun _ ->
        use memory = copyToMemory "test_edit_p.psarc"
        let psarc = PSARC.Read memory
        let oldManifest = psarc.Manifest

        let fileToAdd = { Name = "test/test.dll"; Data = File.OpenRead("Rocksmith2014.PSARC.dll") }

        psarc.Edit(options, (fun files -> files.Add fileToAdd))

        Expect.equal psarc.Manifest.Length (oldManifest.Length + 1) "Manifest size is correct"
        Expect.equal psarc.Manifest.[psarc.Manifest.Length - 1] fileToAdd.Name "Name in manifest is correct"
        Expect.isFalse fileToAdd.Data.CanRead "Data stream has been disposed"

    testCase "Can reorder files" <| fun _ ->
        use memory = copyToMemory "test_edit_p.psarc"
        let psarc = PSARC.Read memory
        let oldManifest = psarc.Manifest

        psarc.Edit(options, (fun files -> let f = files.[0] in files.RemoveAt 0; files.Add f))

        Expect.equal psarc.Manifest.Length oldManifest.Length "Manifest size is same"
        Expect.equal psarc.Manifest.[psarc.Manifest.Length - 1] oldManifest.[0] "First file is now last"
  ]
