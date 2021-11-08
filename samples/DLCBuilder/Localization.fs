namespace DLCBuilder

open Microsoft.Extensions.FileProviders
open System
open System.Collections.Generic
open System.Reflection
open System.Text.Json

module Locales =
    let All =
        [ Locale.Default
          { Name = "Suomi"; ShortName = "fi" }
          { Name = "日本語"; ShortName = "jp" } ]

    let fromShortName shortName =
        All
        |> List.tryFind (fun loc -> loc.ShortName = shortName)
        |> Option.defaultValue Locale.Default

[<AutoOpen>]
module Localization =
    let private embeddedProvider =
        EmbeddedFileProvider(Assembly.GetExecutingAssembly())

    let private loadDictionary locale =
        use json = embeddedProvider.GetFileInfo($"i18n/%s{locale.ShortName}.json").CreateReadStream()
        JsonSerializer.Deserialize<IReadOnlyDictionary<string, string>>(json)

    let private defaultDictionary = loadDictionary Locale.Default
    let mutable private localeDictionary = defaultDictionary

    /// Changes the current locale.
    let changeLocale locale =
        localeDictionary <-
            if locale = Locale.Default then
                defaultDictionary
            else
                loadDictionary locale

    /// Returns the localized string for the given key.
    let translate key =
        Dictionary.tryGetValue key localeDictionary
        |> Option.orElseWith (fun () -> Dictionary.tryGetValue key defaultDictionary)
        |> Option.defaultWith (fun () -> $"!!{key}!!")

    /// Returns the localized formatted string for the given key.
    let translatef key args = String.Format(translate key, args)

    let toInterface () =
        { new IStringLocalizer with
            member _.Translate(key) = translate key
            member _.TranslateFormat(key, args) = translatef key args
            member _.ChangeLocale(locale) = changeLocale locale
            member _.LocaleFromShortName(shortName) = Locales.fromShortName shortName }
