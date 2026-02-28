module DLCBuilder.AppVersion

open System.Reflection

/// The current version of the program.
let current =
    Assembly.GetExecutingAssembly().GetName().Version

/// A three part version string prefixed with "v".
let versionString = $"v%s{current.ToString(3)}"

/// "DLC Builder vX.X.X"
let programNameWithVersion = $"DLC Builder %s{versionString}"
