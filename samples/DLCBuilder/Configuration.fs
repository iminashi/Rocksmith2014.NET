namespace DLCBuilder

open System

type Configuration =
    { ProfilePath : string
      TestFolderPath : string
      ProjectsFolderPath : string
      CharterName : string
      ShowAdvanced : bool }

    static member Default =
        { ProfilePath = String.Empty
          TestFolderPath = String.Empty
          ProjectsFolderPath = String.Empty
          CharterName = String.Empty
          ShowAdvanced = true }
