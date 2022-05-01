module Rocksmith2014.DLCProject.AppId

open Rocksmith2014.Common
open System.Globalization

let [<Literal>] private CherubRockValue = 248750UL

let CherubRock = AppId CherubRockValue

/// Returns None if the string matches the App ID of Cherub Rock.
let tryCustom str =
    match String.trim str with
    | UInt64 CherubRockValue -> None
    | UInt64 id -> Some(AppId id)
    | _ -> None

let ofString str =
    match str with
    | UInt64 id -> Some(AppId id)
    | _ -> None

let toString (AppId value) = value.ToString(NumberFormatInfo.InvariantInfo)
