# Rocksmith 2014 DLC Builder

## System Requirements

A 64-bit OS:

- Windows 8.1 or greater with [.NET 5 runtime](https://dotnet.microsoft.com/download) installed
- macOS 10.13 or greater
- Linux: a distro that can run GUI applications created with [Avalonia](https://github.com/AvaloniaUI/Avalonia)

## Wwise 2019/2021

Converting audio files to wem requires Wwise 2019 or 2021 to be installed.

During the installation, check only "Authoring" in Packages and leave all Deployment Platforms and Plug-ins unchecked (unless you need them for something else).

**After the installation, you may need to run Wwise once, otherwise the automatic conversion may not work.**

On Windows the program will try to find the Wwise installation automatically using the WWISEROOT environment variable, which can be set from the Wwise Launcher, or from the default installation path in Program Files. You can also manually set the Wwise console executable path in the configuration.

On Linux, Wine is used to run the Wwise console executable.

## Test Build vs Release Build

### Test Build

The package for the current platform will be built into the folder specified in the configuration (creating a subfolder in the RS DLC folder is recommended). Generation of DD levels may be disabled and the App ID can be changed to a custom one.

- If RS is running, new packages will be created that have a different DLC key and arrangement IDs, with the version in the song title. This allows you to load a new test version into the game by triggering the DLC enumeration.
- If RS is not running, any previous test builds for the project will be replaced with a single package.

### Release Build

Will build the package(s) (both Mac and PC by default) into the project folder. DD levels will be generated if the XML files do not already have them and the App ID is hard-coded to Cherub Rock.

## Arrangement Improving Features from DDC Improver

Will be used when "Apply Improvements" is enabled.

- Fix incorrect crowd events (E0, E1, E2 -> e0, e1, e2)
- Fix chord names (Amin -> Am)
- Shorten handshapes for chord slides that include the slide-to notes
- Shorten handshapes that are too close to the next handshape
- Remove beats that come after the audio has ended
- Add crowd events (one initial crowd tempo event + intro and outro applauses) unless the arrangement already has them
- Allow moving of phrases/sections off-beat with a special phrase name "mover"
- Custom events "w3" (width 3 anchor), "removebeats", "so" (slide-out handshape)

### Features not (Currently) Ported

- Moving of phrases with special "moveto" phrase name ("mover" is more convenient to use and should not fail in case the sync is adjusted)
- "FIXOPEN" special chord name (rarely needed, the implementation was not very good)
- "OF" (one fret) special chord name (rarely needed, can also be created manually in most cases)
- Removing of "anchor placeholder" notes
- Creation of tone change events (pointless)

## Using the Phrase Mover

Phrases can be moved off beat by giving the phrase a special name: "mover#".

This will move the phrase right by # notes, where the number must be 1 or more. 1 will move the phrase to the next note/chord.

Chords and split chords count as one. If the phrase is located on the same timecode as a note/chord (sustain of a previous note does not count), it counts as one.

![Screenshot of phrase moving](https://i.imgur.com/xM1zF6g.png)

In the above case, the phrase and section will be moved to start at the G power chord.

If an anchor (FHP) is set at the start of the phrase, it will be moved to the new position. The anchor at the old position is deleted if there is no note at that position.

## Custom Events

You can place events in EOF by selecting a beat and pressing Shift+P. Enter the event text and check "Rocksmith event marker".

### so (Slide-out Handshape)

![Screenshot of custom event](https://i.imgur.com/IPhlU42.png)

When placed on a chord that has an unpitched slide out, an empty handshape near the end of the slide will be generated.
This will cause the fingers in the handshape to slide down or up with the slide. It is a minor effect that is sometimes used in oDLC.

If the chord is on a beat you can simply use the event "so" on that beat.
If the chord is not on a beat, you must place the event on a beat left of the target chord/notes and add a number to the event name (for example "so1").
This number specifies how many notes right of the target is from the event. It works the same as the number in "moveR" phrases.

### w3 (Anchor Zone Width 3)

This event will cause the anchor (FHP) that it is placed on, or the next anchor if there is no anchor at the event time, to have a width of three.

You can also specify a fret number in this format: "w3-22". This allows you to create an anchor at the fret 22.

### removebeats

Removes the beats starting from the beat that is placed on until the end.

## Differences to the [Toolkit](https://www.rscustom.net/)

### Performance

Depending on your hardware and the project, building of packages can be 6-10 times faster compared to the Toolkit + DDC.

Memory use is more efficient. As an extreme example, building a complete discography chart: DLC Builder ~250MB vs Toolkit ~2.4GB (the 32-bit version runs out of memory). In regular use however, the DLC Builder may use more memory due to the 64-bit architecture, the UI library used, use of memory pooling, etc.

Reading the available tones from a profile file, opening the tone editor and removing DD levels are much faster.

### Features of DLC Builder Not Available in the Toolkit

- Automatic calculation of volume values for audio
- Optional checking of the arrangements for various issues
- Creation of a preview audio file that has fade-in and fade-out similar to official files
- Per-arrangement custom audio files
- Multi-part tone descriptions with some extra options to select from
- Possibility to easily build a version of the project where the tuning is changed using a pitch shift pedal
- Tone collection for storing tones or adding an official tone into a project
- Tool for creating a Japanese vocals arrangement by matching the lyrics to a romaji arrangement
- Inject tones into a profile file

### Features of the Toolkit Not Available in DLC Builder

- Support for console platforms
- Support for Rocksmith 1

### Quality of Life

- Album art image is displayed in the UI
- Importing tones from the profile is more convenient
- Common hotkeys: Ctrl+N, Ctrl+O, Ctrl+S, ...
- Drag & drop files into the program window
- Remembers the five recently opened projects
- Better at opening templates created with old versions of the Toolkit than the Toolkit itself
- The UI prevents you from creating two main arrangements (represent = 1) for the same path
- Option to remove DD levels when importing a PSARC
- Nicer looking tone editor that prevents gaps between gear slots
- Command to generate new IDs per arrangement or for all arrangements
- No arbitrary limitations on what characters can be used in artist/song names or their sort values
- The program does not stop responding while building a package
- The currently open project is not closed when the configuration is changed
- The UI is localizable into different languages

### Generated Package Minutiae

Since the Toolkit generates packages that work just fine, most of it probably does not matter, but some of the details in the generated manifest, SNG and aggregate graph files are closer to official ones.

## Differences in the DD Generator Compared to DDC

- Will not create or move phrases (there is a separate feature that creates phrases and sections for arrangements that do not have them)
- Will always generate at least two difficulty levels for any kind of phrase that has notes/chords
- Preserves anchors in noguitar sections
- Should not create cases where a note is followed by a pull-off note on the same fret/string

## External Tools Used

- [ww2ogg](https://github.com/hcs64/ww2ogg)
- [revorb](https://github.com/jonboydell/revorb-nix)
