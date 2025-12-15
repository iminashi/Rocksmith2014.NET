module Rocksmith2014.XML.Extension

open Rocksmith2014.XML

[<Struct>]
type XmlEntity =
    | XmlNote of XmlNote: Note
    | XmlChord of XmlChord: Chord

let inline getTimeCode (entity: XmlEntity) =
    match entity with
    | XmlNote xn -> xn.Time
    | XmlChord xc -> xc.Time

let getSustain = function
    | XmlNote xn ->
        xn.Sustain
    | XmlChord xc ->
        if xc.HasChordNotes then
            xc.ChordNotes[0].Sustain
        else
            0

/// Creates an XML entity array from the notes and chords in the level.
let createXmlEntityArrayFromLevel (level: Level) =
    let xmlNotes = level.Notes
    let xmlChords = level.Chords

    if xmlChords.Count = 0 then
        Array.init xmlNotes.Count (fun i -> XmlNote xmlNotes[i])
    elif xmlNotes.Count = 0 then
        Array.init xmlChords.Count (fun i -> XmlChord xmlChords[i])
    else
        let entityArray = Array.zeroCreate<XmlEntity> (xmlNotes.Count + xmlChords.Count)

        for i = 0 to xmlNotes.Count - 1 do
            entityArray[i] <- XmlNote xmlNotes[i]
        for i = 0 to xmlChords.Count - 1 do
            entityArray[xmlNotes.Count + i] <- XmlChord xmlChords[i]

        Array.sortInPlaceBy getTimeCode entityArray
        entityArray


/// Creates an XML entity array from the lists of notes and chords.
let createXmlEntityArrayFromLists (xmlNotes: Note list) (xmlChords: Chord list) =
    let xmlNotes = Array.ofList xmlNotes
    let xmlChords = Array.ofList xmlChords

    if xmlChords.Length = 0 then
        Array.map XmlNote xmlNotes
    elif xmlNotes.Length = 0 then
        Array.map XmlChord xmlChords
    else
        let entityArray =
            Array.init
                (xmlNotes.Length + xmlChords.Length)
                (fun i ->
                    if i < xmlNotes.Length then
                        XmlNote xmlNotes[i]
                    else
                        XmlChord xmlChords[i - xmlNotes.Length])

        Array.sortInPlaceBy getTimeCode entityArray
        entityArray
