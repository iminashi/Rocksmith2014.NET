module DLCBuilder.AppVersion

open System.Reflection

let current =
    let thisAsm = Assembly.GetExecutingAssembly()
    thisAsm.GetName().Version

let createVersionString () = $"v{current.ToString(3)}"
