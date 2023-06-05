## 2.0.0 (Released 2023-06-xx)

- Updated to .NET 7.
- Added a new tool "profile cleaner" to the tools menu.
- A cancel button is now shown when in quick edit "mode".
- Added possibility to launch the custom font generator from the UI.
- Added an option to start audio conversion in the background automatically when an audio file is added to the project.
- Added an option for the naming of the automatically-created base tone key.
- Added an option to compare phrase level counts to the previous build version for test builds also.
- Phrase level file is now created also when a PSARC is imported.
- Added a new validation check for hammer-ons/pull-offs that are on the same fret as the previous note on the same string.
- Added a new validation check for a finger changing during a slide.
- Added a new validation check for position shift into a pull-off.
- Fixed false positive in validation for anchor position in relation to chord fingering with chords that use the thumb.
- A better error message is shown when loading an "EXT" vocals file fails.
- Master ID and persistent ID can now be manually edited also for vocals arrangements when "show advanced features" is enabled.
- Duplicate tone keys will be prevented when changing the tone key in the UI.
- Minor UI improvements.
- Minor optimizations to package generation.
- Updated the UI framework to new minor version.
- Internal changes related to PSARC encryption and asynchronous code.

## 1.6.2 (Released 2023-05-01)

- Fixed a bug that caused fret numbers of a chord to be in wrong order in the EOF file.

## 1.6.1 (Released 2023-04-11)

- Fixed a bug in writing the ogg path to the EOF file.

## 1.6.0 (Released 2023-03-26)

- Added support for Wwise 2022.
- Audio files are set to wem when importing PSARC even if audio conversion is enabled.
- Wave/Ogg audio will be used for volume calculation when project audio files are set to wem and wav/ogg file with same name exist.
- Minor UI improvement.

## 1.5.7 (Released 2023-03-08)

- Changed the naming of the XML files saved when importing a PSARC to match the files EOF saves.
- Fixed a bug where unused tones with the same key would prevent building the project.

## 1.5.6 (Released 2023-02-12)

- Fixed a bug that would cause a corrupted EOF project file to be created.

## 1.5.5 (Released 2023-02-05)

- Fixed an issue where the generated EOF project could not be opened in EOF if the 5th or 6th string was used in a bass arrangement.

## 1.5.4 (Released 2023-01-15)

- Added a validation issue for more than 100 phrases in an arrangement.
- Fixed a bug that caused issues with the last beat in the generated EOF project.

## 1.5.3 (Released 2022-11-27)

- Improvements to the EOF project creation:
- Only beats where the tempo changes are now anchored, instead of all the beats.
- Zero strength bend values at the note time will not be imported as tech notes.

## 1.5.2 (Released 2022-11-09)

- Fixed a bug in test build sort values caused by the previous fix.

## 1.5.1 (Released 2022-11-06)

- Fixed a bug in automatically created song title sort values in test builds.
- Updated the NVorbis library to the latest version.

## 1.5.0 (Released 2022-09-18)

- Added an option to automatically create an EOF project when importing a PSARC.
- Added "double drop" tunings to the recognized tunings.
- Minor improvement to the DD generation algorithm.

## 1.4.1 (Released 2022-07-25)

- Fixed wrong filename for release build PSARCs when the sort fields were left empty.

## 1.4.0 (Released 2022-07-23)

- The arrangement properties in the info dialog can now be edited.
- The capo fret of an arrangement is now shown in the info dialog.
- The sort fields can now be left empty, in which case they will be automatically created from the regular fields.
- Minor improvements to the phrase generator.

## 1.3.0 (Released 2022-06-19)

- Added a button for viewing the lyrics of a vocals arrangement.
- Added a button for viewing some information about a guitar/bass arrangement.
- The version of the Toolkit or DLC Builder used to build the imported PSARC is now shown in the additional metadata dialog.
- Fixed a bug where disabling DD generation for test builds affected PSARC quick edit import.

## 1.2.0 (Released 2022-05-14)

- The audio duration is now shown in the UI.
- Removed the "projects folder" setting since it was not being used for anything meaningful.
- The filename of the cover art is now shown in the tooltip.
- Added a link to the release notes into the help menu.
- Added a new validation check for chords with open strings in the middle of a barre.
- Added a new validation check for non-muted chords that have muted strings.

## 1.1.1 (Released 2022-05-08)

- Fixed bug in directory creation when importing a PSARC.

## 1.1.0 (Released 2022-05-08)

- The start time for the preview audio is now entered in minutes, seconds and milliseconds.
- The preview audio file can now be selected separately from the main audio file.
- Wem conversion is automatically done again if the wav/ogg audio file is newer than the existing wem file.
- Added a new automatic fix for chords that have different sustains on the chord notes.
- Added a new validation check for chords that have "impossible" fingerings.

## 1.0.0 (Released 2022-05-01)

- .NET 6 runtime now needs to be installed in order to run the program (on Windows).
- Added "quick edit" PSARC import feature: the files will be extracted into a temporary folder and the original PSARC file will be replaced when the package is built.
- The author of a CDLC is preserved when importing a PSARC and the value for the charter name set in the configuration can be overridden for a project.
- Volume values can now be calculated even if the audio file is a wem file (via conversion to temporary ogg file).
- The issue viewer can be opened by double clicking an arrangement in the list.
- Minor UI improvements.

## 0.9.3 (Released 2022-03-20)

- Setting a custom fonts for both the regular vocals and Japanese vocals is now supported.

## 0.9.2 (Released 2022-02-13)

- Individual validation issues can now be set as ignored.
- Minor improvements to error handling.
- Updated Avalonia version to fix crashes on Linux.

## 0.9.1 (Released 2022-01-04)

- If auto-save is enabled, a save prompt will be displayed when first adding an arrangement to a new project.
- Minor improvements to the "delete test builds" feature.
- Fixed an issue with the previous cover art not being removed from the UI when opening a project that has no cover art set.
- Fixed a crash when pasting into a textbox when the clipboard contains something other than text.

## 0.9.0 (Released 2021-12-03)

- Preview audio can now be created even if the main audio is set to a wem file.
- Preview audio will be created automatically if not found when building a package.
- Added a new issue for lyrics that contain no line breaks.
- Fixed a minor issue in the PSARC import.

## 0.8.0 (Released 2021-10-20)

- The official tone collection no longer needs to be installed manually.
- New program icon created by Masel89.
- Modified the XML to SNG conversion to ignore certain linknext errors.
- Minor UI improvements.

## 0.7.5 (Released 2021-10-03)

- The "Unpack PSARC" dialog now allows you to select multiple files.
- Improved the automatic fix that ensures that there is an anchor at the start of each phrase.
- Fixed import of PSARC files that contain null values among the tones.
- Fixed DD generation failing for files that contain chord templates without any notes.

## 0.7.4 (Released 2021-09-29)

- Updated the vorbis library to fix a "Could not initialize container" error with some ogg files.

## 0.7.3 (Released 2021-09-19)

- Fixed an issue in the auto update.

## 0.7.2 (Released 2021-09-19)

- Fixed a bug where the window would be placed off-screen when starting the program.

## 0.7.1 (Released 2021-09-17)

- Fixed incorrect behavior when opening multiple instances of the program.
- Fixed import of PSARC files that contain invalid tones.

## 0.7.0 (Released 2021-09-15)

- The window position, size and maximized state is now preserved when closing the program.
- A folder with the package name is now created when a PSARC is imported.
- Improved the validation for package filenames.
- Minor improvements to the Japanese lyrics tool.
- Added some instructions on how to use the Japanese lyrics tool to the ReadMe.
- Minor UI improvements.

## 0.6.6 (Released 2021-09-09)

- Fixed import/unpack of some PSARCs created with the Toolkit failing.

## 0.6.5 (Released 2021-09-07)

- Added explanations of the custom events to the ReadMe.
- Unpacking audio.psarc works again.
- Fixed wem to ogg conversion for files that contain the word "error".

## 0.6.4 (Released 2021-08-29)

- Fixed bug in detecting Wwise from WWISEROOT environment variable. 

## 0.6.3 (Released 2021-08-28)

- Disabled the custom title bar on Windows 7.
- Minor adjustments to the UI.
- Improved the drag & drop to allow multiple files to be dropped at once.
- The crash log can be opened from the Help menu if the file exists.

## 0.6.2 (Released 2021-08-22)

- Added some help texts.
- Minor improvements to the phrase generation.
- Fixed creation of preview audio from files that are shorter than 28 seconds.
- Fixed an edge case where the arrangement validation could fail with an error.

## 0.6.1 (Released 2021-08-19)

- A warning is now displayed when a tone has more than four effects.
- Tone effects can be moved up or down in the editor.
- Minor UI improvements.
- Fixed a minor bug in the phrase generation.

## 0.6.0 (Released 2021-08-17)

- A Linux version is now available.
- Added a tool for creating a Japanese vocals arrangement from a romaji arrangement.
- Added phrase/section generation for arrangements that do not have them.
- Added drag & drop support for project, PSARC, tone, cover art, audio, arrangement and Toolkit template files.
- Added a customized window title bar (Windows only).
- User experience improvements for first-time users.
- Minor UI fixes.
- Fixed cover art not being displayed in the UI when importing a Toolkit template.
- Fixed tone XML export not working correctly on Unix platforms.

## 0.5.0 (Released 2021-08-08)

- The metadata for the tones in the user collection can now be edited (via context menu or E key).
- The tone collection now shows what gear the selected tone uses.
- Added a feature that compares the DD levels of a release build to the previous one and asks if the arrangement IDs should be regenerated when necessary.
- Added "Open Project Folder" to the project menu.
- Added "Validate Arrangement Again" button to the issue viewer.
- Various UI and usability improvements.
- Fixed an exception when editing a tone that contains out-of-bounds values.
- Added logging for program crashes.

## 0.4.1 (Released 2021-07-27)

- Fixed bug in loading of the ML model.

## 0.4.0 (Released 2021-07-26)

- Added a tone collection feature where the user can save tones or add official tones (separate download) into a project.
- Added a new automatic fix that removes the linknext attribute from chord notes that are not immediately followed by a note on the same string.
- The tone volume setting now uses positive numbers with the range being: 0.1 (very quiet) ... 36.0 (very loud)
- The textbox for the tone name is now shown only when the "Show Advanced Features" option is enabled.
- Added new shortcut keys: Ctrl+B - Build Test, Ctrl+R - Build Release, Ctrl+V - Validate Arrangements.
- Changed the shortcut key "Toolkit Import" from Ctrl+T to Ctrl+I.
- Added "Pack Directory into PSARC" and "Convert Audio to Wem" into the tools menu.
- The tuning of a string can be changed with the up and down arrow keys.
- Added buttons for changing the tuning of all strings simultaneously.
- Fixed an exception when entering a very low drop tuning.
- Various UI improvements.

## 0.3.1 (Released 2021-06-15)

- The program will no longer create packages that crash the game if there are lyrics that are too long for the binary format used by the game.
- Fixed a bug in the showlight generator that caused it to create duplicates.
- Fixed import of PSARC files where the main audio is also used for the preview.
- Restored Magick.NET to use the latest version on Windows.
- Added missing localization string.

## 0.3.0 (Released 2021-06-13)

- The Windows version now uses an installer and automatic update from earlier versions will not work. Please download the installer from the GitHub page.
- Added "Inject Tones into Profile" feature (in the Tools menu).
- Added option to choose the method for determining the number of levels to generate for a phrase (simple or machine learning model).
- The DD generation no longer fails with an exception when the arrangement contains certain linknext errors.
- Downgraded Magick.NET version to fix a crash on macOS 10.13.
- Minor UI fixes.

## 0.2.3 (Released 2021-06-01)

- Fixed a possible null reference exception when adding crowd events.
- Improved the auto-update.

## 0.2.2 (Released 2021-05-27)

- Test release for setting up GitHub Action.

## 0.2.1 (Released 2021-05-26)

- Fix temporary file deletion in the auto-updater.

## 0.2.0 (Released 2021-05-26)

- Add new checks for notes inside the first phrase and a missing END phrase.
- Fix the auto-updater.
