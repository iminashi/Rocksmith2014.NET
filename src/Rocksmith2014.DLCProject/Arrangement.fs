namespace Rocksmith2014.DLCProject

open System

type ArrangementName =
    | Lead = 0
    | Combo = 1
    | Rhythm = 2
    | Bass = 3

type Instrumental =
    { XML : string
      ArrangementName : ArrangementName
      RouteMask : RouteMask
      ScrollSpeed : int
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
        | Showlights -> failwith "No"

    let getPersistentId = function
        | Vocals v -> v.PersistentID
        | Instrumental i -> i.PersistentID
        | Showlights -> failwith "No"

    let getName (arr: Arrangement) generic =
        match arr with
        | Vocals v when v.Japanese && not generic -> "JVocals"
        | Vocals -> "Vocals"
        | Showlights -> "Showlights"
        | Instrumental i -> i.ArrangementName.ToString()
