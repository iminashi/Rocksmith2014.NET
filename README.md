# Libraries for Rocksmith 2014 CDLC

.NET libraries for Rocksmith 2014 custom DLC creation. Based largely on the [Song Creator Toolkit for Rocksmith](https://github.com/rscustom/rocksmith-custom-song-toolkit).

## Libraries

### Common

Contains various types and functions used by the other libraries. Also has functionality for reading and writing profile files.

### XML

For reading, editing and writing XML files. Originally created for [DDC Improver](https://github.com/iminashi/DDCImprover).

### SNG

For reading and writing SNG files.

### Conversion

Contains functionality for converting XML to SNG and vice versa.

### PSARC

For opening and creating PSARC archives.

### DLCProject

Contains the Arrangement and DLCProject types and also miscellaneous functionality needed for creating CDLC (Wwise and DDS conversion, showlight generation, etc,).

## Samples

### Misc Tools

Cross-platform desktop app mainly for testing the functionality of the libraries.

### DLC Builder

Cross-platform desktop app for creating CDLC.

### Profile Debloater

Console app for removing obsolete IDs from profile files.

### Empty Title Fix

Console app for fixing empty Japanese titles in PSARCs created with a buggy version of the Toolkit.
