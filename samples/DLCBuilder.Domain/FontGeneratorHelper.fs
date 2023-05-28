module DLCBuilder.FontGeneratorHelper

open System

let private fontGeneratedEvent = Event<Guid * string>()

let FontGenerated = fontGeneratedEvent.Publish

let fontGenerated idAndPath = fontGeneratedEvent.Trigger(idAndPath)
