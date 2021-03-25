module DLCBuilder.ToneGear

open System.Reflection
open System.Text.Json
open System.Text.Json.Serialization
open Rocksmith2014.Common.Manifest
open Microsoft.Extensions.FileProviders

type GearSlot =
    | Amp
    | Cabinet
    | PrePedal of index : int
    | PostPedal of index : int
    | Rack of index : int

type GearKnob =
    { Name : string
      Key : string
      UnitType : string
      MinValue : float32
      MaxValue : float32
      ValueStep : float32
      DefaultValue : float32
      EnumValues : string[] option }

type GearData =
    { Name : string
      Type : string
      Category : string
      Key : string
      Knobs : GearKnob array option }

/// Returns the knob values for the pedal in the given gear slot.
let getKnobValuesForGear (gearList: Gear) gearSlot =
    match gearSlot with
    | Amp -> Some gearList.Amp
    | Cabinet -> Some gearList.Cabinet
    | PrePedal index -> gearList.PrePedals.[index] 
    | PostPedal index -> gearList.PostPedals.[index]
    | Rack index -> gearList.Racks.[index]
    |> Option.map (fun x -> x.KnobValues)

let private getDefaultKnobValues gear =
    gear.Knobs
    |> Option.map (Array.map (fun knob -> knob.Key, knob.DefaultValue) >> Map.ofArray)
    |> Option.defaultValue Map.empty

/// Creates a pedal with default knob values from the gear data.
let createPedalForGear (gear: GearData) =
    { Key = gear.Key
      Type = gear.Type
      Category = None
      Skin = None
      SkinIndex = None
      KnobValues = getDefaultKnobValues gear }

let private loadGearData () = async {
    let provider = EmbeddedFileProvider(Assembly.GetExecutingAssembly())
    let options = JsonSerializerOptions(IgnoreNullValues = true)
    options.Converters.Add(JsonFSharpConverter())
    use gearDataFile = provider.GetFileInfo("ToneGearData.json").CreateReadStream()
    return! JsonSerializer.DeserializeAsync<GearData[]>(gearDataFile, options) }

let allGear = loadGearData() |> Async.RunSynchronously

let private filterSort type' sortBy = allGear |> Array.filter (fun x -> x.Type = type') |> Array.sortBy sortBy
let private toDict = Array.map (fun x -> x.Key, x) >> readOnlyDict

let amps = filterSort "Amps" (fun x -> x.Name)
let cabinets = filterSort "Cabinets" (fun x -> x.Name)
let pedals = filterSort "Pedals" (fun x -> x.Category, x.Name)
let racks = filterSort "Racks" (fun x -> x.Category, x.Name)

let cabinetChoices = cabinets |> Array.distinctBy (fun x -> x.Name)
let micPositionsForCabinet =
    cabinets
    |> Array.groupBy (fun x -> x.Name)
    |> readOnlyDict

let ampDict = toDict amps
let cabinetDict = toDict cabinets
let pedalDict = toDict pedals
let rackDict = toDict racks

/// Returns the gear data for the pedal in the given gear slot.
let getGearDataForCurrentPedal (gearList: Gear) = function
    | Amp ->
        Some ampDict.[gearList.Amp.Key]
    | Cabinet ->
        Some cabinetDict.[gearList.Cabinet.Key]
    | PrePedal index ->
        gearList.PrePedals.[index] |> Option.map (fun x -> pedalDict.[x.Key])
    | PostPedal index ->
        gearList.PostPedals.[index] |> Option.map (fun x -> pedalDict.[x.Key])
    | Rack index ->
        gearList.Racks.[index] |> Option.map (fun x -> rackDict.[x.Key])
