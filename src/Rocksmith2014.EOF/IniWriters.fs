module IniWriters

open BinaryFileWriter
open EOFTypes

let writeIniStrings (iniStrings: IniString array) =
    binaryWriter {
        // Number of INI strings
        iniStrings.Length |> uint16

        for str in iniStrings do
            str.StringType |> byte
            str.Value
    }

let writeIniBooleans =
    binaryWriter {
        // Number of INI booleans
        1us

        // Type + enabled
        // 11 = Accurate TS
        11uy ||| (1uy <<< 7)
    }

let writeIniNumbers =
    binaryWriter {
        // Number
        1us

        // Type
        // 2 = Band difficulty level
        2uy
        // Value
        255u
    }
