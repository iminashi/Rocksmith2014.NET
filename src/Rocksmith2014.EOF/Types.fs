module EOFTypes

open Rocksmith2014.XML

type ProGuitarTrack =
    | ExistingTrack of InstrumentalArrangement
    | EmptyTrack of name: string

type EOFTrack =
    | Legacy of name: string * behavior: byte * type': byte * lanes: byte
    | Vocals of name: string * vocals: Vocal seq
    | ProGuitar of guitarTrack: ProGuitarTrack

type EOFEvent =
    { Text: string
      BeatNumber: int
      // TODO: track number
      Flag: uint16 }

type EOFTimeSignature =
    | ``TS 2 | 4``
    | ``TS 3 | 4``
    | ``TS 4 | 4``
    | ``TS 5 | 4``
    | ``TS 6 | 4``
    | CustomTS of denominator: uint * nominator: uint

type IniStringType =
    | Custom = 0uy
    //| Album = 1uy
    | Artist = 2uy
    | Title = 3uy
    | Frettist = 4uy
    //| (unused)
    | Year = 6uy
    | LoadingText = 7uy
    | Album = 8uy
    | Genre = 9uy
    | TrackNumber = 10uy

type IniString =
    { StringType: IniStringType
      Value: string }
