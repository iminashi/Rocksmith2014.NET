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

[<Tests>]
let someTests =
  testList "Edit Files" [

    testCase "Null Edit" <| fun _ ->
        use memory = copyToMemory "test_edit_p.psarc"
        use psarc = PSARC.Read memory
        let oldManifest = psarc.Manifest

        psarc.Edit(InMemory, (fun files -> ()))

        Expect.sequenceEqual psarc.Manifest oldManifest "Manifest is unchanged"
  ]
