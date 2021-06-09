module DLCBuilder.AppVersion

open System.Reflection

/// The current version of the program.
let current =
    let thisAsm = Assembly.GetExecutingAssembly()
    thisAsm.GetName().Version

/// A three part version string prefixed with "v".
let versionString= $"v{current.ToString(3)}"
