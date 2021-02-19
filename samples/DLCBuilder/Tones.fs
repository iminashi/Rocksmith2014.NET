module Tones

open System.Reflection
open System.Text.Json
open System.Text.Json.Serialization
open Rocksmith2014.Common.Manifest
open Microsoft.Extensions.FileProviders

type GearType =
    | Amp 
    | PrePedal of index : int
    | PostPedal of index : int
    | Rack of index : int

type ToneKnob =
    { Name : string
      Key : string
      UnitType : string
      MinValue : float32
      MaxValue : float32
      ValueStep : float32
      DefaultValue : float32
      Index : int
      EnumValues : string[] option }

type ToneGear =
    { Name : string
      Type : string
      Category : string 
      Key : string 
      Knobs : ToneKnob array option }

let getKnobValuesForGear gearType (tone: Tone) =
    let gear = tone.GearList

    match gearType with
    | Amp -> Some gear.Amp
    | PrePedal index -> gear.PrePedals.[index] 
    | PostPedal index -> gear.PostPedals.[index]
    | Rack index -> gear.Racks.[index]
    |> Option.map (fun x -> x.KnobValues)

let loadPedalData () = async {
    let provider = EmbeddedFileProvider(Assembly.GetExecutingAssembly())
    
    let options = JsonSerializerOptions(IgnoreNullValues = true)
    options.Converters.Add(JsonFSharpConverter())
    return! JsonSerializer.DeserializeAsync<ToneGear[]>(provider.GetFileInfo("pedalData.json").CreateReadStream(), options) }

let private getDefaultKnobValues gear =
    match gear.Knobs with
    | Some knobs ->
        knobs
        |> Array.map (fun k -> k.Key, float32 k.DefaultValue)
        |> Map.ofArray
    | None ->
        Map.empty

let createPedalForGear (gear: ToneGear) =
    { Key = gear.Key
      Type = gear.Type
      Category = None
      Skin = None
      SkinIndex = None
      KnobValues = getDefaultKnobValues gear }
