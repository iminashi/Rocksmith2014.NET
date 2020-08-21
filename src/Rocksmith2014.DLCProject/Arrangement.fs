namespace Rocksmith2014.DLCProject

open System

type ArrangementName =
    | Lead = 0
    | Combo = 1
    | Rhythm = 2
    | Bass = 3

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
      CentOffset : int
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
                    sprintf " (%s)" (string inst.RouteMask)
                elif inst.RouteMask = RouteMask.Bass && inst.BassPicked then
                    " (Picked)"
                else
                    String.Empty
            let tuning =
                let roots = [| "E"; "F"; "F#"; "G"; "Ab"; "A"; "Bb"; "B"; "C"; "C#"; "D"; "Eb" |]
                let first = inst.Tuning.[0]
                if first > -11s && first < 3s && inst.Tuning |> Array.forall ((=) first) then
                    let i = int (first + 12s) % 12
                    " [" + roots.[i] + " Standard]"
                else
                    String.Empty
            sprintf "%s%s%s%s" prefix (string inst.Name) extra tuning

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
