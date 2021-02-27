module DLCBuilder.DefaultTones

open Microsoft.Extensions.FileProviders
open System.Reflection
open Rocksmith2014.Common.Manifest

let private loadTone path = async {
    let provider = EmbeddedFileProvider(Assembly.GetExecutingAssembly())    
    return! Tone.fromJsonStream (provider.GetFileInfo($"Tones/default_%s{path}.json").CreateReadStream()) }

let Lead = lazy Async.RunSynchronously (loadTone "lead")

let Rhythm = lazy Async.RunSynchronously (loadTone "rhythm")

let Bass = lazy Async.RunSynchronously (loadTone "bass")
