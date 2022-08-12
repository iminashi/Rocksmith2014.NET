module SectionWriter

open BinaryFileWriter
open EOFTypes

let writeSection (section: EOFSection) =
    binaryWriter {
        // Name
        section.Name

        // Type
        section.Type

        // Start time (or data)
        section.StartTime

        // End time (or data)
        section.EndTime

        // Flags
        section.Flags
    }
