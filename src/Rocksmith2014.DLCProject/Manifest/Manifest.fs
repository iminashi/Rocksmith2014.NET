namespace Rocksmith2014.DLCProject.Manifest

open System
open System.Text.Json
open System.Text.Json.Serialization

type Manifest =
    { Entries : Map<string, AttributesContainer>
      ModelName : string
      IterationVersion : Nullable<int>
      InsertRoot : string }

module Manifest =
    let private createInternal (attrs: AttributesBase list) modelName iterationVersion insertRoot =
        let entries =
            attrs
            |> List.map (fun a -> a.PersistentID, { Attributes = a })
            |> Map.ofList
        { Entries = entries
          ModelName = modelName |> Option.toObj
          IterationVersion = iterationVersion |> Option.toNullable
          InsertRoot = insertRoot }

    let create (attrs: AttributesBase list) =
        createInternal attrs (Some "RSEnumerable_Song") (Some 2) "Static.Songs.Entries"

    let createHeader (attrs: AttributesBase list) =
        createInternal attrs None None "Static.Songs.Headers"

    let toJson (manifest: Manifest) =
        let options = JsonSerializerOptions(WriteIndented = true, IgnoreNullValues = true)
        options.Converters.Add(JsonFSharpConverter())
        JsonSerializer.Serialize(manifest, options)

    let fromJson (str: string) =
        let options = JsonSerializerOptions(IgnoreNullValues = true)
        options.Converters.Add(JsonFSharpConverter())
        JsonSerializer.Deserialize<Manifest>(str, options)
