module Rocksmith2014.DLCProject.SoundBank

open System
open System.IO
open System.Text
open Rocksmith2014.Common
open SoundBankUtils

module private HierarchyID =
    let [<Literal>] Sound = 2y
    let [<Literal>] Action = 3y
    let [<Literal>] Event = 4y
    let [<Literal>] ActorMixer = 7y

module private SoundParameters =
    let [<Literal>] Volume = 0y

/// Creates the data index chunk.
let private dataIndex id dataLength platform =
    let memory = MemoryStreamPool.Default.GetStream()
    let writer = BinaryWriters.getWriter memory platform

    writer.WriteInt32 id
    writer.WriteInt32 0 // File Offset
    writer.WriteInt32 dataLength

    memory

/// Creates the header chunk.
let private header soundbankID platform =
    let memory = MemoryStreamPool.Default.GetStream()
    let writer = BinaryWriters.getWriter memory platform

    writer.WriteUInt32 91u // Sound Bank Version
    writer.WriteUInt32 soundbankID
    writer.WriteUInt32 0u // Language ID
    writer.WriteUInt32 0u // HasFeedback
    for _ = 1 to getHeaderPaddingSize platform do writer.WriteInt32 0

    memory

/// Creates the string ID chunk.
let private stringID soundbankID name platform =
    let memory = MemoryStreamPool.Default.GetStream()
    let writer = BinaryWriters.getWriter memory platform

    let soundbankName = Encoding.ASCII.GetBytes(sprintf "Song_%s" name)

    writer.WriteUInt32 1u // String Type
    writer.WriteUInt32 1u // NumNames
    writer.WriteUInt32 soundbankID
    writer.WriteInt8 (int8 soundbankName.Length)
    writer.WriteBytes soundbankName

    memory

/// Creates a hierarchy sound section.
let private hierarchySound id fileId mixerId volume isPreview platform =
    let soundID = uint32 id
    let pluginID = 262145u
    let streamType = 2u // 2 = Streamed, with zero Latency
    let fileID = uint32 fileId
    let sourceID = fileID
    let languageSpecific = 0y // Sound SFX
    let overrideParent = 0y
    let numFX = 0y
    let parentBusID = RandomGenerator.next() |> uint32
    let directParentID = match platform with PC | Mac -> 65536u
    let unkID1 = if isPreview then 4178100890u else 0u
    let mixerID = uint32 mixerId
    let priorityOverrideParent = 0y
    let priorityApplyDist = 0y
    let overrideMidi = 0y
    let numParam = 3y
    let param1Type = SoundParameters.Volume
    let param2Type = 46y
    let param3Type = 47y
    let param1Value = volume
    let param2Value = 1
    let param3Value = 3
    let numRange = 0y
    let positionOverride = 0y
    let overrideGameAux = 0y
    let useGameAux = 0y
    let overrideUserAux = 0y
    let hasAux = 0y
    let virtualQueueBehavior = 1y // 1 = Play from elapsed time
    let killNewest = if isPreview then 1y else 0y
    let useVirtualBehavior = 0y
    let maxNumInstance = if isPreview then 1s else 0s
    let isGlobalLimit = 0y
    let belowThresholdBehavior = 0y
    let isMaxNumInstOverrideParent = if isPreview then 1y else 0y
    let isVirtualVoiceOptOverrideParent = 0y
    let stateGroupList = 0
    let rtpcList = 0s
    let feedbackBus = 0

    let memory = MemoryStreamPool.Default.GetStream()
    let writer = BinaryWriters.getWriter memory platform

    writer.WriteUInt32 soundID
    writer.WriteUInt32 pluginID
    writer.WriteUInt32 streamType
    writer.WriteUInt32 fileID
    writer.WriteUInt32 sourceID
    writer.WriteInt8 languageSpecific
    writer.WriteInt8 overrideParent
    writer.WriteInt8 numFX
    writer.WriteUInt32 parentBusID
    writer.WriteUInt32 directParentID
    writer.WriteUInt32 unkID1
    writer.WriteUInt32 mixerID
    writer.WriteInt8 priorityOverrideParent
    writer.WriteInt8 priorityApplyDist
    writer.WriteInt8 overrideMidi
    writer.WriteInt8 numParam
    writer.WriteInt8 param1Type
    writer.WriteInt8 param2Type
    writer.WriteInt8 param3Type
    writer.WriteSingle param1Value
    writer.WriteInt32 param2Value
    writer.WriteInt32 param3Value
    writer.WriteInt8 numRange
    writer.WriteInt8 positionOverride
    writer.WriteInt8 overrideGameAux
    writer.WriteInt8 useGameAux
    writer.WriteInt8 overrideUserAux
    writer.WriteInt8 hasAux
    writer.WriteInt8 virtualQueueBehavior
    writer.WriteInt8 killNewest
    writer.WriteInt8 useVirtualBehavior
    writer.WriteInt16 maxNumInstance
    writer.WriteInt8 isGlobalLimit
    writer.WriteInt8 belowThresholdBehavior
    writer.WriteInt8 isMaxNumInstOverrideParent
    writer.WriteInt8 isVirtualVoiceOptOverrideParent
    writer.WriteInt32 stateGroupList
    writer.WriteInt16 rtpcList
    writer.WriteInt32 feedbackBus

    memory

/// Creates a hierarchy actor mixer section.
let private hierarchyActorMixer id soundId platform =
    let mixerID = id
    let overrideParent = 0y
    let numFX = 0y
    let parentBusID = 2616261673u
    let directParentID = 0u
    let unkID1 = 0u
    let unkID2 = 65792u
    let priorityOverrideParent = 0y
    let priorityApplyDist = 0y
    let numParam = 0y
    let numRange = 0y
    let positionOverride = 0y
    let overrideGameAux = 0y
    let useGameAux = 0y
    let overrideUserAux = 0y
    let hasAux = 0y
    let virtualQueueBehavior = 0y
    let killNewest = 0y
    let useVirtualBehavior = 0y
    let maxNumInstance = 0s
    let isGlobalLimit = 0y
    let belowThresholdBehavior = 0y
    let isMaxNumInstOverrideParent = 0y
    let isVirtualVoiceOptOverrideParent = 0y
    let stateGroupList = 0
    let rtpcList = 0s
    let numChild = 1
    let child1 = soundId

    let memory = MemoryStreamPool.Default.GetStream()
    let writer = BinaryWriters.getWriter memory platform

    writer.WriteUInt32 mixerID
    writer.WriteInt8 overrideParent
    writer.WriteInt8 numFX
    writer.WriteUInt32 parentBusID
    writer.WriteUInt32 directParentID
    writer.WriteUInt32 unkID1
    writer.WriteUInt32 unkID2
    writer.WriteInt8 priorityOverrideParent
    writer.WriteInt8 priorityApplyDist
    writer.WriteInt8 numParam
    writer.WriteInt8 numRange
    writer.WriteInt8 positionOverride
    writer.WriteInt8 overrideGameAux
    writer.WriteInt8 useGameAux
    writer.WriteInt8 overrideUserAux
    writer.WriteInt8 hasAux
    writer.WriteInt8 virtualQueueBehavior
    writer.WriteInt8 killNewest
    writer.WriteInt8 useVirtualBehavior
    writer.WriteInt16 maxNumInstance
    writer.WriteInt8 isGlobalLimit
    writer.WriteInt8 belowThresholdBehavior
    writer.WriteInt8 isMaxNumInstOverrideParent
    writer.WriteInt8 isVirtualVoiceOptOverrideParent
    writer.WriteInt32 stateGroupList
    writer.WriteInt16 rtpcList
    writer.WriteInt32 numChild
    writer.WriteInt32 child1

    memory

/// Creates a hierarchy action section.
let private hierarchyAction id objId bankId platform =
    let actionID = id
    let actionScope = 3y // 3 = Game object
    let actionType = 4y // 4 = Play
    let objectID = objId
    let isBus = 0y
    let numParam = 0y
    let numRange = 0y
    let fadeCurve = 4y
    let soundbankID = bankId

    let memory = MemoryStreamPool.Default.GetStream()
    let writer = BinaryWriters.getWriter memory platform

    writer.WriteUInt32 actionID
    writer.WriteInt8 actionScope
    writer.WriteInt8 actionType
    writer.WriteInt32 objectID
    writer.WriteInt8 isBus
    writer.WriteInt8 numParam
    writer.WriteInt8 numRange
    writer.WriteInt8 fadeCurve
    writer.WriteUInt32 soundbankID

    memory

/// Creates a hierarchy event section with a Play event.
let private hierarchyEvent id name platform =
    let eventID = fnvHash (sprintf "Play_%s" name)
    let numEvents = 1u
    let actionID = id

    let memory = MemoryStreamPool.Default.GetStream()
    let writer = BinaryWriters.getWriter memory platform

    writer.WriteUInt32 eventID
    writer.WriteUInt32 numEvents
    writer.WriteUInt32 actionID

    memory

let private copyData output (writer: IBinaryWriter) (data: Stream) =
    writer.WriteInt32 (int32 data.Length)
    data.Position <- 0L
    data.CopyTo output
    data.Dispose()

let private writeHierarchy output (writer: IBinaryWriter) id (hierarchy: Stream) =
    writer.WriteInt8 id
    copyData output writer hierarchy

/// Creates the hierarchy chunk.
let private hierarchy bankId soundId fileId name volume isPreview platform =
    let mixerID = 650605636u
    let actionID = RandomGenerator.next() |> uint32
    let numObjects = 4u

    let memory = MemoryStreamPool.Default.GetStream()
    let writer = BinaryWriters.getWriter memory platform
    let write = writeHierarchy memory writer

    writer.WriteUInt32 numObjects

    write HierarchyID.Sound (hierarchySound soundId fileId mixerID volume isPreview platform)
    write HierarchyID.ActorMixer (hierarchyActorMixer mixerID soundId platform)
    write HierarchyID.Action (hierarchyAction actionID soundId bankId platform)
    write HierarchyID.Event (hierarchyEvent actionID name platform)

    memory

/// Writes the chunk name, length and data into the writer.
let private writeChunk (output: Stream) (writer: IBinaryWriter) name (data: Stream) =
    writer.WriteBytes name
    copyData output writer data

/// Generates a sound bank for the audio stream into the output stream.
let generate name (audioStream: Stream) (output: Stream) volume (platform: Platform) =
    let soundbankID = RandomGenerator.next() |> uint32
    let fileID = abs <| hash name
    let soundID = RandomGenerator.next()

    let writer = BinaryWriters.getWriter output platform
    let write = writeChunk output writer

    let audioReader = BinaryReaders.getReader audioStream platform
    let isPreview = String.endsWith "_Preview" name
    let dataLength = if isPreview then 72000 else 51200

    write "BKHD"B (header soundbankID platform)
    write "DIDX"B (dataIndex fileID dataLength platform)
    write "DATA"B (new MemoryStream(audioReader.ReadBytes dataLength))
    write "HIRC"B (hierarchy soundbankID soundID fileID name volume isPreview platform)
    write "STID"B (stringID soundbankID name platform)

    output.Flush()
    output.Position <- 0L

    string fileID

/// Reads the file ID value from the sound bank in the stream.
let readFileId (stream: Stream) platform = result {
    let reader = initReader stream platform

    do! seekToSection stream reader "DIDX"B |> Result.ignore

    // Read the file ID
    return reader.ReadInt32() }

/// Reads the volume value from the sound bank in the stream.
let readVolume (stream: Stream) platform = result {
    let reader = initReader stream platform

    do! seekToSection stream reader "HIRC"B |> Result.ignore
    do! seekToObject stream reader HierarchyID.Sound
    
    // Skip 46 bytes to get to the parameter count
    seek stream 46L

    let paramCount = reader.ReadInt8()
    let paramTypes = reader.ReadBytes(int32 paramCount)

    match Array.IndexOf(paramTypes, byte SoundParameters.Volume) with
    | -1 ->
        // Volume parameter is not present
        return 0.f
    | index ->
        // Seek to the volume parameter (each parameter is 4 bytes long)
        seek stream (int64 index * 4L)
        return reader.ReadSingle() }
