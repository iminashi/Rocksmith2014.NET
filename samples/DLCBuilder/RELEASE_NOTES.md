## 0.4.0 (Released 2021-07-xx)

- Added a tone collection feature where the user can save tones or add official tones into a project (separate download).
- Added a new automatic fix that removes the linknext attribute from chord notes that are not immediately followed by a note on the same string.
- The textbox for the tone name is now shown only when "Show Advanced Features" is enabled.
- Added new shortcut keys: Ctrl+B - Build Test, Ctrl+R - Build Release, Ctrl+V - Validate Arrangments.
- Changed shortcut key "Toolkit Import" from Ctrl+T to Ctrl+I.
- Improve start-up time slightly by loading the tone gear data in the background.
- UI improvements.

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
