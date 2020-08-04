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
