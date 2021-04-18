module DLCBuilder.DefaultTones

open Microsoft.Extensions.FileProviders
open System.Reflection
open Rocksmith2014.Common.Manifest

let private loadTone path = async {
    let provider = EmbeddedFileProvider(Assembly.GetExecutingAssembly())
    use toneFile = provider.GetFileInfo($"Tones/default_%s{path}.json").CreateReadStream()
    return! Tone.fromJsonStream toneFile }

let Lead = lazy Async.RunSynchronously (loadTone "lead")

let Rhythm = lazy Async.RunSynchronously (loadTone "rhythm")

let Bass = lazy Async.RunSynchronously (loadTone "bass")
