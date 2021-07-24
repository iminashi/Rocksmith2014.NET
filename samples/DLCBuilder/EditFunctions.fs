module DLCBuilder.EditFunctions

open Rocksmith2014.Common
open Rocksmith2014.Common.Manifest
open Rocksmith2014.DLCProject
open Elmish
open System
open ToneGear

let private updateArrangement index updated state =
    let arrangements =
        state.Project.Arrangements
        |> List.updateAt index updated
    { state with Project = { state.Project with Arrangements = arrangements } }

let private updateTone index updated state =
    let tones =
        state.Project.Tones
        |> List.updateAt index updated
    { state with Project = { state.Project with Tones = tones } }

/// Changes the priority of the arrangement if a main arrangement of the type already exists.
let private fixPriority state routeMask inst =
    let samePriorityExists arrangements =
        arrangements
        |> List.choose Arrangement.pickInstrumental
        |> List.exists (fun existing ->
            existing <> inst
            && existing.RouteMask = routeMask
            && existing.Priority = ArrangementPriority.Main)

    if inst.Priority = ArrangementPriority.Main && samePriorityExists state.Project.Arrangements
    then
        ArrangementPriority.Alternative
    else
        inst.Priority

let editInstrumental state edit index inst =
    let updated, cmd =
        match edit with
        | SetArrangementName name ->
            let routeMask =
                match name with
                | ArrangementName.Lead ->
                    RouteMask.Lead
                | ArrangementName.Rhythm ->
                    RouteMask.Rhythm
                | ArrangementName.Combo ->
                    // The name of a bass arrangement cannot currently be changed
                    if inst.RouteMask = RouteMask.Bass then RouteMask.Rhythm else inst.RouteMask
                | ArrangementName.Bass ->
                    RouteMask.Bass
                | _ ->
                    failwith "Impossible failure."
            let priority = fixPriority state routeMask inst
            { inst with Name = name; RouteMask = routeMask; Priority = priority }, Cmd.none

        | SetRouteMask mask ->
            let priority = fixPriority state mask inst
            { inst with RouteMask = mask; Priority = priority }, Cmd.none

        | SetPriority priority ->
            { inst with Priority = priority }, Cmd.none

        | SetBassPicked picked ->
            { inst with BassPicked = picked }, Cmd.none

        | SetTuning (index, newTuning) ->
            { inst with Tuning = inst.Tuning |> Array.updateAt index newTuning }, Cmd.none

        | ChangeTuning (index, direction) ->
            let change = match direction with Up -> 1s | Down -> -1s
            let newTuning = inst.Tuning.[index] + change
            { inst with Tuning = inst.Tuning |> Array.updateAt index newTuning }, Cmd.none

        | ChangeTuningAll direction ->
            let change = match direction with Up -> 1s | Down -> -1s
            { inst with Tuning = inst.Tuning |> Array.map ((+) change) }, Cmd.none

        | SetTuningPitch pitch ->
            { inst with TuningPitch = pitch }, Cmd.none

        | SetBaseTone tone ->
            { inst with BaseTone = tone }, Cmd.none

        | UpdateToneInfo ->
            Arrangement.updateToneInfo inst true, Cmd.none

        | SetScrollSpeed speed ->
            { inst with ScrollSpeed = speed }, Cmd.none

        | SetMasterId id ->
            { inst with MasterID = id }, Cmd.none

        | SetPersistentId id ->
            { inst with PersistentID = id }, Cmd.none

        | SetCustomAudioPath (Some path) ->
            let cmd =
                if state.Config.AutoVolume && not <| String.endsWith ".wem" path then
                    Cmd.ofMsg <| CalculateVolume(CustomAudio(path, inst.PersistentID))
                else
                    Cmd.none

            let customAudio =
                inst.CustomAudio
                |> Option.map (fun audio -> { audio with Path = path })
                |> Option.orElseWith (fun () -> Some { Path = path; Volume = state.Project.AudioFile.Volume })

            { inst with CustomAudio = customAudio }, cmd

        | SetCustomAudioPath None ->
            { inst with CustomAudio = None }, Cmd.none

        | SetCustomAudioVolume volume ->
            { inst with CustomAudio = Option.map (fun x -> { x with Volume = volume }) inst.CustomAudio }, Cmd.none

    updateArrangement index (Instrumental updated) state, cmd

let editConfig edit config =
    match edit with
    | SetCharterName name ->
        { config with CharterName = name }

    | SetAutoVolume autoVolume ->
        { config with AutoVolume = autoVolume }

    | SetShowAdvanced showAdvanced ->
        { config with ShowAdvanced = showAdvanced }

    | SetRemoveDDOnImport removeDD ->
        { config with RemoveDDOnImport = removeDD }

    | SetGenerateDD generateDD ->
        { config with GenerateDD = generateDD }

    | SetDDPhraseSearchEnabled phraseSearch ->
        { config with DDPhraseSearchEnabled = phraseSearch }

    | SetDDPhraseSearchThreshold threshold ->
        { config with DDPhraseSearchThreshold = threshold }

    | SetDDLevelCountGeneration lcg ->
        { config with DDLevelCountGeneration = lcg }

    | SetApplyImprovements improve ->
        { config with ApplyImprovements = improve }

    | SetSaveDebugFiles debug ->
        { config with SaveDebugFiles = debug }

    | SetCustomAppId appId ->
        { config with CustomAppId = appId }

    | SetConvertAudio convert ->
        { config with ConvertAudio = convert }

    | AddReleasePlatform platform ->
        { config with ReleasePlatforms = Set.add platform config.ReleasePlatforms }

    | RemoveReleasePlatform platform ->
        { config with ReleasePlatforms = Set.remove platform config.ReleasePlatforms }

    | SetTestFolderPath path ->
        { config with TestFolderPath = path }

    | SetProjectsFolderPath path ->
        { config with ProjectsFolderPath = path }

    | SetOpenFolderAfterReleaseBuild openFolder ->
        { config with OpenFolderAfterReleaseBuild = openFolder }

    | SetLoadPreviousProject load ->
        { config with LoadPreviousOpenedProject = load }

    | SetAutoSave autoSave ->
        { config with AutoSave = autoSave }

    | SetWwiseConsolePath path ->
        { config with WwiseConsolePath = Option.ofString path }

    | SetProfilePath path ->
        { config with ProfilePath = path }

let editProject edit project =
    match edit with
    | SetDLCKey key ->
        { project with DLCKey = key }

    | SetVersion version ->
        { project with Version = version }

    | SetArtistName artist ->
        { project with ArtistName = { project.ArtistName with Value = artist } }

    | SetArtistNameSort artistSort ->
        { project with ArtistName = { project.ArtistName with SortValue = artistSort } }

    | SetJapaneseArtistName artist ->
        { project with JapaneseArtistName = artist }

    | SetTitle title ->
        { project with Title = { project.Title with Value = title } }

    | SetTitleSort titleSort ->
        { project with Title = { project.Title with SortValue = titleSort } }

    | SetJapaneseTitle title ->
        { project with JapaneseTitle = title }

    | SetAlbumName album ->
        { project with AlbumName = { project.AlbumName with Value = album } }

    | SetAlbumNameSort albumSort ->
        { project with AlbumName = { project.AlbumName with SortValue = albumSort } }

    | SetYear year ->
        { project with Year = year }

    | SetAudioVolume volume ->
        { project with AudioFile = { project.AudioFile with Volume = volume } }

    | SetPreviewVolume volume ->
        { project with AudioPreviewFile = { project.AudioPreviewFile with Volume = volume } }

    | SetPreviewStartTime startTime ->
        { project with AudioPreviewStartTime = Some startTime }

    | SetPitchShift shift ->
        { project with PitchShift = Some shift }

let editTone state edit index =
    let tone = state.Project.Tones.[index]
    let updatedTone =
        match edit with
        | SetName name ->
            { tone with Name = name }

        | SetKey key ->
            // When the name field is hidden, keep the key in sync with the name
            if not state.Config.ShowAdvanced && key <> String.Empty then
                { tone with Key = key; Name = key }
            else
                { tone with Key = key }

        | SetVolume volume ->
            // The volume is displayed as a positive number in the UI and needs to be made negative
            { tone with Volume = -volume }

        | AddDescriptor ->
            { tone with ToneDescriptors = tone.ToneDescriptors |> Array.append [| ToneDescriptor.all.[0].UIName |] }

        | RemoveDescriptor ->
            { tone with ToneDescriptors = tone.ToneDescriptors.[1..] }

        | ChangeDescriptor (index, descriptor) ->
           { tone with ToneDescriptors = tone.ToneDescriptors |> Array.updateAt index descriptor.UIName }

        | RemovePedal ->
            let remove index = Utils.removeAndShift index
            let gearList =
                match state.SelectedGearSlot with
                | PrePedal index ->
                    { tone.GearList with PrePedals = tone.GearList.PrePedals |> remove index }
                | PostPedal index ->
                    { tone.GearList with PostPedals = tone.GearList.PostPedals |> remove index }
                | Rack index ->
                    { tone.GearList with Racks = tone.GearList.Racks |> remove index }
                | Amp | Cabinet ->
                    failwith "Cannot remove amp or cabinet"

            { tone with GearList = gearList }

        | SetPedal gear ->
            let currentPedal =
                match state.SelectedGearSlot with
                | Amp -> Some tone.GearList.Amp
                | Cabinet -> Some tone.GearList.Cabinet
                | PrePedal index -> tone.GearList.PrePedals.[index]
                | PostPedal index -> tone.GearList.PostPedals.[index]
                | Rack index -> tone.GearList.Racks.[index]

            match currentPedal with
            | Some currPedal when currPedal.Key = gear.Key ->
                tone
            | _ ->
                let newPedal = createPedalForGear gear
                let setPedal index = Array.updateAt index (Some newPedal)
                let gearList =
                    match state.SelectedGearSlot with
                    | Amp ->
                        { tone.GearList with Amp = newPedal }
                    | Cabinet ->
                        { tone.GearList with Cabinet = newPedal }
                    | PrePedal index ->
                        { tone.GearList with PrePedals = tone.GearList.PrePedals |> setPedal index }
                    | PostPedal index ->
                        { tone.GearList with PostPedals = tone.GearList.PostPedals |> setPedal index }
                    | Rack index ->
                        { tone.GearList with Racks = tone.GearList.Racks |> setPedal index }

                { tone with GearList = gearList }

        | SetKnobValue (knobKey, value) ->
            match state.SelectedGear with
            | Some _ when state.SelectedGearSlot <> Cabinet ->
                getKnobValuesForGear tone.GearList state.SelectedGearSlot
                // Update the value only if the key exists
                |> Option.map (Map.change knobKey (Option.map (fun _ -> value)))
                |> function
                | None ->
                    tone
                | Some updatedKnobs ->
                    let updateKnobs index =
                        Array.mapi (fun i pedal ->
                            if i = index then
                                pedal |> Option.map (fun p -> { p with KnobValues = updatedKnobs })
                            else 
                                pedal)

                    let gearList =
                        match state.SelectedGearSlot with
                        | Amp ->
                            { tone.GearList with Amp = { tone.GearList.Amp with KnobValues = updatedKnobs } }
                        | Cabinet ->
                            tone.GearList
                        | PrePedal index ->
                            { tone.GearList with PrePedals = tone.GearList.PrePedals |> updateKnobs index }
                        | PostPedal index ->
                            { tone.GearList with PostPedals = tone.GearList.PostPedals |> updateKnobs index }
                        | Rack index ->
                            { tone.GearList with Racks = tone.GearList.Racks |> updateKnobs index }

                    { tone with GearList = gearList }
            | _ ->
                tone

    if updatedTone = tone then
        state, Cmd.none
    else
        updateTone index updatedTone state, Cmd.none

let editVocals state edit index vocals =
    let updated =
        match edit with
        | SetIsJapanese japanese ->
            { vocals with Japanese = japanese }
        | SetCustomFont font ->
            { vocals with CustomFont = font }

    updateArrangement index (Vocals updated) state, Cmd.none
