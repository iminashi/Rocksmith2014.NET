module SectionWriter

open BinaryFileWriter

let writeSection (name: string) (type': byte) (startTime: int) (endTime: int) (flags: uint) =
    binaryWriter {
        // Name
        name

        // Type
        type'

        // Start time (or data)
        startTime

        // End time (or data)
        endTime

        // Flags
        flags
    }
