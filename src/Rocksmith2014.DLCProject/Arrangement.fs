namespace Rocksmith2014.DLCProject

open System

type ArrangementName =
    | Lead = 0
    | Combo = 1
    | Rhythm = 2
    | Bass = 3

type RouteMask =
    | None = 0
    | Lead = 1
    | Rhythm = 2
    | Any = 3
    | Bass = 4

type ArrangementPriority =
    | Main = 0
    | Alternative = 1
    | Bonus = 2

type Instrumental =
    { XML : string
      Name : ArrangementName
      RouteMask : RouteMask
      Priority : ArrangementPriority
      ScrollSpeed : float
      BassPicked : bool
      Tuning : int16 array
      TuningPitch : double
      BaseTone : string
      Tones : string list
      MasterID : int
      PersistentID : Guid }

type Vocals =
    { XML : string
      Japanese : bool
      CustomFont : string option
      MasterID : int
      PersistentID : Guid }

type Showlights =
    { XML : string }

type Arrangement =
    | Instrumental of Instrumental
    | Vocals of Vocals
    | Showlights of Showlights

    override this.ToString() =
        match this with
        | Instrumental inst ->
            let prefix =
                match inst.Priority with
                | ArrangementPriority.Main -> String.Empty
                | ArrangementPriority.Alternative -> "Alt. "
                | ArrangementPriority.Bonus -> "Bonus "
                | _ -> failwith "Impossible."

            let extra =
                if inst.Name = ArrangementName.Combo then
                    " (Combo)"
                elif inst.RouteMask = RouteMask.Bass && inst.BassPicked then
                    " (Picked)"
                else
                    String.Empty

            let tuning =
                let t = Utils.getTuningString inst.Tuning
                let p = if inst.TuningPitch <> 440.0 then " " + inst.TuningPitch.ToString() else String.Empty
                if t.Length > 0 then " [" + t + p + "]" else String.Empty

            sprintf "%s%s%s%s" prefix (string inst.RouteMask) extra tuning

        | Vocals v ->
            let prefix =
                if v.Japanese then "Japanese " else String.Empty
            let extra =
                match v.CustomFont with
                | Some _ -> " (Custom Font)"
                | None -> String.Empty
            sprintf "%sVocals%s" prefix extra

        | Showlights _ -> "Show Lights"

module Arrangement =
    let getMasterId = function
        | Vocals v -> v.MasterID
        | Instrumental i -> i.MasterID
        | Showlights _ -> failwith "No"

    let getPersistentId = function
        | Vocals v -> v.PersistentID
        | Instrumental i -> i.PersistentID
        | Showlights _ -> failwith "No"

    let getName (arr: Arrangement) generic =
        match arr with
        | Vocals v when v.Japanese && not generic -> "JVocals"
        | Vocals _ -> "Vocals"
        | Showlights _ -> "Showlights"
        | Instrumental i -> i.Name.ToString()

    let getFile = function
        | Vocals v -> v.XML
        | Instrumental i -> i.XML
        | Showlights s -> s.XML

    let pickInstrumental = function Instrumental i -> Some i | _ -> None
    let pickShowlights = function Showlights s -> Some s | _ -> None

    let sorter = function
        | Instrumental i -> (LanguagePrimitives.EnumToValue i.RouteMask), (LanguagePrimitives.EnumToValue i.Priority)
        | Vocals v -> 5, if v.Japanese then 1 else 0
        | Showlights _ -> 6, 0
