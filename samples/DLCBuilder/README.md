# Rocksmith 2014 DLC Builder

## Wwise 2019

Converting audio files to wem requires Wwise 2019 to be installed.

During the installation, check only "Authoring" in Packages and leave all Deployment Platforms and Plug-ins unchecked (unless you need them for something else).

After the installation, you may need to run Wwise once, otherwise the automatic conversion may not work. **On macOS it seems to be required.**

On Windows the program will try to find the Wwise installation automatically using Environment variables, which can be set from the Wwise Launcher. Alternatively you can set the Wwise console executable path in the configuration.

## Missing Features

- Tone editing
- Support for console platforms (Most likely will not be added)

## External Tools Used

- [ww2ogg](https://github.com/hcs64/ww2ogg)
- [revorb](https://github.com/jonboydell/revorb-nix)
