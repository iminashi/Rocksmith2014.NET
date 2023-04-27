module DLCBuilder.DefaultTones

open Microsoft.Extensions.FileProviders
open Rocksmith2014.Common.Manifest
open System.Reflection

let private loadTone path =
    task {
        let provider =
            EmbeddedFileProvider(Assembly.GetExecutingAssembly())

        use toneFile =
            provider.GetFileInfo($"Tones/default_%s{path}.json").CreateReadStream()

        return! Tone.fromJsonStream toneFile
    }

let Lead =
    lazy (loadTone "lead").Result

let Rhythm =
    lazy (loadTone "rhythm").Result

let Bass =
    lazy (loadTone "bass").Result
