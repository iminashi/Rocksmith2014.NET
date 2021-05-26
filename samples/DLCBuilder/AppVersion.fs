module DLCBuilder.AppVersion

open System.Reflection

/// The current version of the program.
let current =
    let thisAsm = Assembly.GetExecutingAssembly()
    thisAsm.GetName().Version

/// Creates a three part version string prefixed with "v".
let createVersionString () = $"v{current.ToString(3)}"
