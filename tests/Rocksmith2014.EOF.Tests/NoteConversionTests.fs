module NoteConversionTests

open Expecto
open Rocksmith2014.XML
open EOFTypes
open NoteConverter
open ProGuitarWriter

[<Tests>]
let tests =
    testList "EOF note conversion tests" [
        test "Capo fret is reduced from notes" {
            let inst =
                let notes = [
                    Note(Time = 100, Fret = 0y)
                    Note(Time = 200, Fret = 5y)
                    Note(Time = 300, Fret = 8y)
                    Note(Time = 300, Fret = 7y, String = 2y)
                ]
                let level = Level(Notes = ResizeArray(notes))
                let metadata = MetaData(Capo = 2y)
                InstrumentalArrangement(Levels = ResizeArray.singleton level, MetaData = metadata)

            let notes =
                convertNotes inst
                |> fun (n, _, _) -> prepareNotes Array.empty inst n

            Expect.equal notes[0].Frets[0] 0uy "1st note fret correct"
            Expect.equal notes[1].Frets[0] 3uy "2nd note fret correct"
            Expect.equal notes[2].Frets[0] 6uy "3rd note fret correct"
            Expect.equal notes[2].Frets[1] 5uy "4th note fret correct"
        }
    ]
