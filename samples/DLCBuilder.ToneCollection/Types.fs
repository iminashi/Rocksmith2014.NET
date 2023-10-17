namespace ToneCollection

open Rocksmith2014.Common.Manifest
open System

[<Struct>]
type UserDataBasePath = UserDataBasePath of string

[<Struct>]
type OfficialDataBasePath = OfficialDataBasePath of string

type QueryOptions =
    { Search: string option
      PageNumber: int }

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
    abstract member GetTones : QueryOptions -> DbTone array

type IUserTonesApi =
    inherit IDisposable
    abstract member GetToneById : int64 -> Tone option
    abstract member GetTones : QueryOptions -> DbTone array
    abstract member GetToneDataById : int64 -> DbToneData option
    abstract member DeleteToneById : int64 -> unit
    abstract member AddTone : DbToneData -> unit
    abstract member UpdateData : DbToneData -> unit

type IDatabaseConnector =
    abstract member TryCreateOfficialTonesApi : unit -> IOfficialTonesApi option
    abstract member CreateUserTonesApi : unit -> IUserTonesApi

[<RequireQualifiedAccess>]
type ActiveCollection =
    | Official of IOfficialTonesApi option
    | User of IUserTonesApi

[<RequireQualifiedAccess>]
type ActiveTab =
    | Official
    | User

type ToneCollectionState =
    { ActiveCollection: ActiveCollection
      Connector: IDatabaseConnector
      Tones: DbTone array
      SelectedToneIndex: int
      QueryOptions: QueryOptions
      EditingUserTone: DbToneData option
      TotalPages: int }

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
    | ChangeCollection of activeTab: ActiveTab
    | AddSelectedToneFromCollection
    | DeleteSelectedUserTone
    | SearchCollection of searchString: string option
    | ChangePage of direction: PageDirection
    | SelectedToneIndexChanged of selectedIndex: int
    | AddOfficialToneToUserCollection
    | ShowUserToneEditor
    | HideUserToneEditor
    | EditUserToneData of edit: UserToneEdit
    | ApplyUserToneEdit
    | DownloadOfficialTonesDatabase

type Effect =
    | Nothing
    | ShowToneAddedToCollectionMessage
    | AddToneToProject of tone: Tone
    | BeginDownloadingTonesDatabase
