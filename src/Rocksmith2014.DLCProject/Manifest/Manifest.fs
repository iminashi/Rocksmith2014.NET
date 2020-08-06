namespace Rocksmith2014.DLCProject.Manifest

open System
open System.Text.Json
open System.Text.Json.Serialization
open Rocksmith2014.Common.Manifest
open System.IO

type Manifest =
    { Entries : Map<string, AttributesContainer>
      ModelName : string
      IterationVersion : Nullable<int>
      InsertRoot : string }

module Manifest =
    let private createInternal (attrs: Attributes list) modelName iterationVersion insertRoot =
        let entries =
            attrs
            |> List.map (fun a -> a.PersistentID, { Attributes = a })
            |> Map.ofList
        { Entries = entries
          ModelName = modelName |> Option.toObj
          IterationVersion = iterationVersion |> Option.toNullable
          InsertRoot = insertRoot }

    let create (attrs: Attributes list) =
        createInternal attrs (Some "RSEnumerable_Song") (Some 2) "Static.Songs.Entries"

    let createHeader (attrs: Attributes list) =
        createInternal attrs None None "Static.Songs.Headers"

    let private options =
        let o = JsonSerializerOptions(WriteIndented = true, IgnoreNullValues = true)
        o.Converters.Add(JsonFSharpConverter())
        o

    let toJson (manifest: Manifest) =
        JsonSerializer.Serialize(manifest, options)

    let toJsonStream (output: Stream) (manifest: Manifest) =
        JsonSerializer.SerializeAsync(output, manifest, options)

    let fromJson (str: string) =
        JsonSerializer.Deserialize<Manifest>(str, options)

    let fromJsonStream (input: Stream) =
        JsonSerializer.DeserializeAsync<Manifest>(input, options)
