module EditTests

open Expecto
open System.IO
open Rocksmith2014.PSARC

let copyToMemory (fileName: string) = async {
    use file = File.OpenRead fileName
    let memory = new MemoryStream(int file.Length)
    do! file.CopyToAsync memory
    memory.Position <- 0L
    return memory }

let options = { Mode = InMemory; EncryptTOC = true }

[<Tests>]
let someTests =
  testList "Edit Files" [

    testAsync "Manifest is same after null edit" {
        use! memory = copyToMemory "test_edit_p.psarc"
        use psarc = PSARC.Read memory
        let oldManifest = psarc.Manifest

        do! psarc.Edit(options, ignore)

        Expect.sequenceContainsOrder psarc.Manifest oldManifest "Manifest is unchanged" }

    testAsync "Can be read after editing" {
        use! memory = copyToMemory "test_edit_p.psarc"
        let psarc = PSARC.Read memory
        let oldManifest = psarc.Manifest
        let oldToc = psarc.TOC

        do! psarc.Edit(options, ignore)
        memory.Position <- 0L
        let psarc2 = PSARC.Read memory

        Expect.sequenceEqual psarc2.Manifest oldManifest "Manifest is unchanged"
        for i = 0 to psarc2.TOC.Count - 1 do
            Expect.equal psarc2.TOC.[i].Length oldToc.[i].Length "File length is same" }

    testAsync "Can remove files" {
        use! memory = copyToMemory "test_edit_p.psarc"
        let psarc = PSARC.Read memory
        let oldManifest = psarc.Manifest
        let oldSize = memory.Length

        // Remove all files ending in "wem" from the archive
        do! psarc.Edit(options, (fun files -> files.RemoveAll(fun x -> x.Name.EndsWith "wem") |> ignore))
        memory.Position <- 0L
        let psarc2 = PSARC.Read memory

        Expect.equal psarc2.Manifest.Length (oldManifest.Length - 2) "Manifest size is correct"
        Expect.isTrue (memory.Length < oldSize) "Size is smaller" }

    testAsync "Can add a file" {
        use! memory = copyToMemory "test_edit_p.psarc"
        let psarc = PSARC.Read memory
        let oldManifest = psarc.Manifest

        let fileToAdd = { Name = "test/test.dll"; Data = File.OpenRead("Rocksmith2014.PSARC.dll") }

        do! psarc.Edit(options, (fun files -> files.Add fileToAdd))

        Expect.equal psarc.Manifest.Length (oldManifest.Length + 1) "Manifest size is correct"
        Expect.equal psarc.Manifest.[psarc.Manifest.Length - 1] fileToAdd.Name "Name in manifest is correct"
        Expect.isFalse fileToAdd.Data.CanRead "Data stream has been disposed" }

    testAsync "Can reorder files" {
        use! memory = copyToMemory "test_edit_p.psarc"
        let psarc = PSARC.Read memory
        let oldManifest = psarc.Manifest

        do! psarc.Edit(options, (fun files -> let f = files.[0] in files.RemoveAt 0; files.Add f))

        Expect.equal psarc.Manifest.Length oldManifest.Length "Manifest size is same"
        Expect.equal psarc.Manifest.[psarc.Manifest.Length - 1] oldManifest.[0] "First file is now last" }

    testAsync "Can rename files" {
        use! memory = copyToMemory "test_edit_p.psarc"
        let psarc = PSARC.Read memory
        let oldManifest = psarc.Manifest

        do! psarc.Edit(options, (fun files ->
            let f = { files.[0] with Name = "new name" }
            files.[0] <- f))

        Expect.equal psarc.Manifest.Length oldManifest.Length "Manifest size is same"
        Expect.equal psarc.Manifest.[0] "new name" "File name is changed" }
  ]
