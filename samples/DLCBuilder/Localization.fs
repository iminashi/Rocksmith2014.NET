namespace DLCBuilder

open Microsoft.Extensions.FileProviders
open System.Collections.Generic
open System.Reflection
open System.Text.Json
open System

type Locale =
    { Name : string; ShortName : string }

    override this.ToString() = this.Name

module Locales =
    let English = { Name = "English"; ShortName = "en" }
    let Finnish = { Name = "Suomi"; ShortName = "fi" }

    let fromShortName = function
        | "en" -> English
        | "fi" -> Finnish
        | _ -> English

type ILocalization =
    abstract member GetString : string -> string
    abstract member Format : string -> obj array -> string

type Localization(locale: Locale) =
    static let defaultLocale = Locales.English
    static let embeddedProvider = EmbeddedFileProvider(Assembly.GetExecutingAssembly())

    static let loadDictionary name =
        use json = embeddedProvider.GetFileInfo(name).CreateReadStream()
        JsonSerializer.DeserializeAsync<Dictionary<string, string>>(json).AsAsync()
        |> Async.RunSynchronously

    static let defaultDictionary: Dictionary<string, string> = loadDictionary "i18n/default.json"

    let localeDictionary =
        if locale = defaultLocale then
            defaultDictionary
        else
            sprintf "i18n/%s.json" locale.ShortName
            |> loadDictionary

    interface ILocalization with
        member _.GetString (key: string) =
            Utils.tryGetValue localeDictionary key
            |> Option.orElseWith (fun () -> Utils.tryGetValue defaultDictionary key)
            |> Option.defaultWith (fun () -> $"!!{key}!!")

        member this.Format (key: string) (args: obj array) =
            let formatString = (this :> ILocalization).GetString key
            String.Format(formatString, args)
