namespace ToneCollection

open Rocksmith2014.Common.Manifest
open System

[<CLIMutable>]
type DbTone =
    { Id: int64
      Artist: string
      Title: string
      Name: string
      BassTone: bool
      Description: string
      TotalRows: int64 }

type DbToneData =
    { Id: int64
      Artist: string
      ArtistSort: string
      Title: string
      TitleSort: string
      Name: string
      BassTone: bool
      Description: string
      Definition: string }

type IOfficialTonesApi =
    inherit IDisposable
    abstract member GetToneById : int64 -> Tone option
    abstract member GetToneDataById : int64 -> DbToneData option
    abstract member GetTones : string option * int -> DbTone array

type IUserTonesApi =
    inherit IDisposable
    abstract member GetToneById : int64 -> Tone option
    abstract member GetTones : string option * int -> DbTone array
    abstract member GetToneDataById : int64 -> DbToneData option
    abstract member DeleteToneById : int64 -> unit
    abstract member AddTone : DbToneData -> unit
    abstract member UpdateData : DbToneData -> unit

[<RequireQualifiedAccess>]
type ActiveCollection =
    | Official of IOfficialTonesApi option
    | User of IUserTonesApi

[<RequireQualifiedAccess>]
type ActiveTab =
    | Official
    | User

type ToneCollectionState =
    { ActiveCollection : ActiveCollection
      Tones : DbTone array
      SelectedTone : DbTone option
      SearchString : string option
      EditingUserTone : DbToneData option
      CurrentPage : int
      TotalPages : int }

type PageDirection = Left | Right

[<RequireQualifiedAccess>]
type UserToneEdit =
    | SetArtist of string
    | SetArtistSort of string
    | SetTitle of string
    | SetTitleSort of string
    | SetName of string
    | SetIsBass of bool
    | RemoveArtistInfo

type Msg =
    | ChangeToneCollection of activeTab : ActiveTab
    | AddSelectedToneFromCollection
    | DeleteSelectedUserTone
    | SearchToneCollection of searchString : string option
    | ChangeToneCollectionPage of direction : PageDirection
    | ToneCollectionSelectedToneChanged of selectedTone : DbTone option
    | AddOfficialToneToUserCollection
    | ShowUserToneEditor
    | HideUserToneEditor
    | EditUserToneData of UserToneEdit
    | ApplyUserToneEdit

type Effect =
    | Nothing
    | ShowToneAddedToCollectionMessage
    | AddToneToProject of tone : Tone
