module DLCBuilder.FontGeneratorHelper

open System
open Rocksmith2014.DLCProject

let private fontGeneratedEvent = Event<ArrangementId * string>()

let FontGenerated = fontGeneratedEvent.Publish

let fontGenerated idAndPath = fontGeneratedEvent.Trigger(idAndPath)
