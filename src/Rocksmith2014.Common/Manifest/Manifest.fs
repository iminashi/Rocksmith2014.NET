namespace Rocksmith2014.Common.Manifest

open System
open System.IO
open System.Text.Json
open System.Text.Encodings.Web
open System.Text.Json.Serialization
open Rocksmith2014.Common

type Manifest =
    { Entries: Map<string, AttributesContainer>
      ModelName: string
      IterationVersion: Nullable<int>
      InsertRoot: string }

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

    /// Creates a manifest from the given attributes.
    let create (attrs: Attributes) =
        createInternal [ attrs ] (Some "RSEnumerable_Song") (Some 2) "Static.Songs.Entries"

    /// Creates a manifest header from the given list of attributes objects.
    let createHeader (attrs: Attributes list) =
        createInternal attrs None None "Static.Songs.Headers"

    /// Returns the attributes from a manifest expected to have only one entry.
    let getSingletonAttributes (manifest: Manifest) =
        manifest.Entries
        |> Map.toArray
        |> Array.head
        |> fun (_, container) -> container.Attributes

    let private options () =
        FSharpJsonOptions.Create(indent = true, ignoreNull = JsonIgnoreCondition.WhenWritingNull)
        |> apply (fun o -> o.Encoder <- JavaScriptEncoder.UnsafeRelaxedJsonEscaping)

    /// Serializes the manifest into a JSON string.
    let toJsonString (manifest: Manifest) =
        JsonSerializer.Serialize(manifest, options ())

    /// Serializes the manifest as JSON into the output stream.
    let toJsonStream (output: Stream) (manifest: Manifest) =
        JsonSerializer.SerializeAsync(output, manifest, options ())

    /// Deserializes a manifest from a JSON string.
    let fromJsonString (str: string) =
        JsonSerializer.Deserialize<Manifest>(str, options ())

    /// Deserializes a manifest from a JSON stream.
    let fromJsonStream (input: Stream) =
        JsonSerializer.DeserializeAsync<Manifest>(input, options ())

    /// Deserializes a manifest from a file.
    let fromJsonFile (path: string) =
        backgroundTask {
            use file =
                new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    4096,
                    FileOptions.SequentialScan ||| FileOptions.Asynchronous
                )

            return! fromJsonStream file
        }
