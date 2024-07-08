module DLCBuilder.EditFunctions

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
            let updateNonBassString =
                not state.Config.ShowAdvanced && inst.RouteMask = RouteMask.Bass

            let newTuning =
                if updateNonBassString && index = 3 then
                    // Update 5th and 6th string to match the 4th string
                    inst.Tuning
                    |> Array.mapi (fun i currentTuning -> if i >= index then newTuning else currentTuning)
                else
                    inst.Tuning |> Array.updateAt index newTuning

            { inst with Tuning = newTuning }, Cmd.none

        | ChangeTuning (index, direction) ->
            let change = match direction with Up -> 1s | Down -> -1s
            let newTuning = inst.Tuning[index] + change
            { inst with Tuning = inst.Tuning |> Array.updateAt index newTuning }, Cmd.none

        | ChangeTuningAll direction ->
            let change = match direction with Up -> 1s | Down -> -1s
            { inst with Tuning = inst.Tuning |> Array.map ((+) change) }, Cmd.none

        | SetTuningPitch pitch ->
            { inst with TuningPitch = pitch }, Cmd.none

        | SetBaseTone tone ->
            { inst with BaseTone = tone }, Cmd.none

        | UpdateToneInfo ->
            Arrangement.updateToneInfo true inst, Cmd.none

        | SetScrollSpeed speed ->
            { inst with ScrollSpeed = speed }, Cmd.none

        | SetMasterId id ->
            { inst with MasterId = id }, Cmd.none

        | SetPersistentId id ->
            { inst with PersistentId = id }, Cmd.none

        | SetCustomAudioPath (Some path) ->
            let notWemFile = not <| String.endsWith ".wem" path
            let cmds =
                [
                    if notWemFile then StateUtils.getOptionalWemConversionCmd state path
                    if state.Config.AutoVolume && notWemFile then
                        Cmd.ofMsg <| CalculateVolume(CustomAudio(path, inst.Id))
                ]

            let customAudio =
                inst.CustomAudio
                |> Option.map (fun audio -> { audio with Path = path })
                |> Option.orElseWith (fun () -> Some { Path = path; Volume = state.Project.AudioFile.Volume })

            { inst with CustomAudio = customAudio }, Cmd.batch cmds

        | SetCustomAudioPath None ->
            { inst with CustomAudio = None }, Cmd.none

        | SetCustomAudioVolume volume ->
            { inst with CustomAudio = Option.map (fun x -> { x with Volume = volume }) inst.CustomAudio }, Cmd.none

        | ToggleArrangementPropertiesOverride arrProp ->
            match inst.ArrangementProperties with
            | Some _ ->
                { inst with ArrangementProperties = None }, Cmd.none
            | None ->
                { inst with ArrangementProperties = Some <| ArrangementPropertiesOverride.fromArrangementProperties arrProp }, Cmd.none

        | ToggleArrangementProperty op ->
            match inst.ArrangementProperties with
            | Some flags ->
                let newFlags =
                    match op with
                    | ArrPropOp.Enable flag -> flags ||| flag
                    | ArrPropOp.Disable flag -> flags &&& ~~~flag
                { inst with ArrangementProperties = Some newFlags }, Cmd.none
            | None ->
                inst, Cmd.none

    updateArrangement index (Instrumental updated) state, cmd

let editConfig edit config =
    match edit with
    | SetCharterName name ->
        { config with CharterName = name }

    | SetAutoVolume autoVolume ->
        { config with AutoVolume = autoVolume }

    | SetAutoAudioConversion autoConversion ->
        { config with AutoAudioConversion = autoConversion }

    | SetShowAdvanced showAdvanced ->
        { config with ShowAdvanced = showAdvanced }

    | SetRemoveDDOnImport removeDD ->
        { config with RemoveDDOnImport = removeDD }

    | SetCreateEOFProjectOnImport createEofProject ->
        { config with CreateEOFProjectOnImport = createEofProject }

    | SetQuickEditOnPsarcDragAndDrop useQuickEdit ->
        { config with QuickEditOnPsarcDragAndDrop = useQuickEdit }

    | SetGenerateDD generateDD ->
        { config with GenerateDD = generateDD |> ValueOption.defaultValue (not config.GenerateDD) }

    | SetComparePhraseLevelsOnTestBuild compareLevels ->
        { config with ComparePhraseLevelsOnTestBuild = compareLevels |> ValueOption.defaultValue (not config.ComparePhraseLevelsOnTestBuild) }

    | SetDDPhraseSearchEnabled phraseSearch ->
        { config with DDPhraseSearchEnabled = phraseSearch }

    | SetDDPhraseSearchThreshold threshold ->
        { config with DDPhraseSearchThreshold = threshold }

    | SetDDLevelCountGeneration lcg ->
        { config with DDLevelCountGeneration = lcg }

    | SetApplyImprovements improve ->
        { config with ApplyImprovements = improve |> ValueOption.defaultValue (not config.ApplyImprovements) }

    | SetForcePhraseCreation opt ->
        { config with ForcePhraseCreation = opt |> ValueOption.defaultValue (not config.ForcePhraseCreation) }

    | SetSaveDebugFiles debug ->
        { config with SaveDebugFiles = debug |> ValueOption.defaultValue (not config.SaveDebugFiles) }

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

    | SetDlcFolderPath path ->
        { config with DlcFolderPath = path }

    | SetOpenFolderAfterReleaseBuild openFolder ->
        { config with OpenFolderAfterReleaseBuild = openFolder }

    | SetValidateBeforeReleaseBuild validate ->
        { config with ValidateBeforeReleaseBuild = validate |> ValueOption.defaultValue (not config.ValidateBeforeReleaseBuild) }

    | SetLoadPreviousProject load ->
        { config with LoadPreviousOpenedProject = load }

    | SetAutoSave autoSave ->
        { config with AutoSave = autoSave }

    | SetWwiseConsolePath path ->
        { config with WwiseConsolePath = Option.ofString path }

    | SetFontGeneratorPath path ->
        { config with FontGeneratorPath = Option.ofString path }

    | SetProfilePath path ->
        { config with ProfilePath = path }

    | SetBaseToneNaming scheme ->
        { config with BaseToneNamingScheme = scheme }

    | SetProfileCleanerParallelism value ->
        { config with ProfileCleanerIdParsingParallelism = value }

let editProject edit project =
    match edit with
    | SetDLCKey key ->
        { project with DLCKey = key }

    | SetVersion version ->
        { project with Version = version }

    | SetAlbumArt path ->
        { project with AlbumArtFile = path }

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
        { project with AudioFile = { project.AudioFile with Volume = float volume } }

    | SetPreviewVolume volume ->
        { project with AudioPreviewFile = { project.AudioPreviewFile with Volume = float volume } }

    | SetPreviewStartTime timeComponent ->
        let currentTime = project.AudioPreviewStartTime |> Option.defaultValue (TimeSpan())
        let startTime =
            match timeComponent with
            | Minutes min ->
                TimeSpan(0, 0, int min, currentTime.Seconds, currentTime.Milliseconds)
            | Seconds sec ->
                TimeSpan(0, 0, currentTime.Minutes, int sec, currentTime.Milliseconds)
            | Milliseconds ms ->
                TimeSpan(0, 0, currentTime.Minutes, currentTime.Seconds, int ms)

        { project with AudioPreviewStartTime = Some startTime }

    | SetPitchShift shift ->
        { project with PitchShift = Some shift }

    | SetAuthor author ->
        { project with Author = Option.ofString author }

let editTone state edit index =
    let tone = state.Project.Tones[index]

    let updatedTone =
        match edit with
        | SetName name ->
            { tone with Name = name }

        | SetKey key ->
            StateUtils.updateToneKey state.Config key tone

        | SetVolume volume ->
            // The volume is displayed as a positive number in the UI and needs to be made negative
            { tone with Volume = -volume }

        | AddDescriptor ->
            let newToneDescriptor =
                tone.ToneDescriptors
                |> Array.tryHead
                |> Option.defaultValue ToneDescriptor.all[0].UIName

            { tone with
                ToneDescriptors = Array.insertAt 0 newToneDescriptor tone.ToneDescriptors }

        | RemoveDescriptor ->
            { tone with ToneDescriptors = tone.ToneDescriptors[1..] }

        | ChangeDescriptor (index, descriptor) ->
           { tone with ToneDescriptors = tone.ToneDescriptors |> Array.updateAt index descriptor.UIName }

        | MovePedal (gearSlot, direction) ->
            let change = match direction with Up -> -1 | Down -> +1

            let isValidMove index =
                let newIndex = index + change
                newIndex >= 0 && newIndex < 4

            let move array index =
                let newIndex = index + change
                let newArray = Array.copy array
                newArray[newIndex] <- array[index]
                newArray[index] <- array[newIndex]
                newArray

            let gearList =
                match gearSlot with
                | PrePedal index when isValidMove index ->
                    { tone.GearList with PrePedals = move tone.GearList.PrePedals index }
                | PostPedal index when isValidMove index ->
                    { tone.GearList with PostPedals = move tone.GearList.PostPedals index }
                | Rack index when isValidMove index ->
                    { tone.GearList with Racks = move tone.GearList.Racks index }
                | _ ->
                    tone.GearList

            { tone with GearList = gearList }

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
                | PrePedal index -> tone.GearList.PrePedals[index]
                | PostPedal index -> tone.GearList.PostPedals[index]
                | Rack index -> tone.GearList.Racks[index]

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
            if state.SelectedGearSlot = Cabinet then
                // Cabinets do not have knobs
                tone
            else
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

    if updatedTone = tone then
        state, Cmd.none
    else
        updateTone index updatedTone state, Cmd.none

let editVocals state edit index (vocals: Vocals) =
    let updated =
        match edit with
        | SetIsJapanese japanese ->
            { vocals with Japanese = japanese }
        | SetCustomFont font ->
            { vocals with CustomFont = font }
        | SetVocalsMasterId mid ->
            { vocals with MasterId = mid }
        | SetVocalsPersistentId pid ->
            { vocals with PersistentId = pid }

    updateArrangement index (Vocals updated) state, Cmd.none

let editPostBuildTask (edit: PostBuildTaskEdit) (postBuildTask: PostBuildCopyTask) =
    match edit with
    | SetOnlyCurrentPlatform b ->
        { postBuildTask with OnlyCurrentPlatform = b }
    | SetOpenFolder b ->
        { postBuildTask with OpenFolder = b }
    | SetTargetPath path ->
        { postBuildTask with TargetPath = path }
    | SetCreateSubFolder sub ->
        { postBuildTask with CreateSubfolder = sub }
