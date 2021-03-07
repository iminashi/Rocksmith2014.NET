namespace DLCBuilder

open Microsoft.Extensions.FileProviders
open System.Collections.Generic
open System.Reflection
open System.Text.Json
open System
open Rocksmith2014.Common

type Locale =
    { Name : string; ShortName : string }

    override this.ToString() = this.Name

module Locales =
    let Default = { Name = "English"; ShortName = "en" }
    let All = [ Default; { Name = "Suomi"; ShortName = "fi" } ]

    let fromShortName shortName =
        All
        |> List.tryFind (fun loc -> loc.ShortName = shortName)
        |> Option.defaultValue Default

[<AutoOpen>]
module Localization =
    let private embeddedProvider = EmbeddedFileProvider(Assembly.GetExecutingAssembly())

    let private loadDictionary locale =
        use json = embeddedProvider.GetFileInfo($"i18n/%s{locale.ShortName}.json").CreateReadStream()
        JsonSerializer.DeserializeAsync<IReadOnlyDictionary<string, string>>(json).AsTask()
        |> Async.AwaitTask
        |> Async.RunSynchronously

    let private defaultDictionary = loadDictionary Locales.Default
    let mutable private localeDictionary = defaultDictionary

    /// Changes the current locale.
    let changeLocale locale =
        localeDictionary <-
            if locale = Locales.Default then defaultDictionary else loadDictionary locale

    /// Returns the localized string for the given key.
    let translate key =
        Dictionary.tryGetValue key localeDictionary
        |> Option.orElseWith (fun () -> Dictionary.tryGetValue key defaultDictionary)
        |> Option.defaultWith (fun () -> $"!!{key}!!")

    /// Returns the localized formatted string for the given key.
    let translatef key args =
        String.Format(translate key, args)
