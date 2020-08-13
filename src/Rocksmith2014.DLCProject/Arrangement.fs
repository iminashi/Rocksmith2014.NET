namespace Rocksmith2014.DLCProject

open System

type ArrangementName =
    | Lead = 0
    | Combo = 1
    | Rhythm = 2
    | Bass = 3

type ArrangementOrdering =
    | Main = 0
    | Alternative = 1
    | Bonus = 2

type Instrumental =
    { XML : string
      ArrangementName : ArrangementName
      RouteMask : RouteMask
      ArrangementOrdering : ArrangementOrdering
      ScrollSpeed : float
      Tuning : int16 array
      CentOffset : int
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
        | Instrumental i -> i.ArrangementName.ToString()
