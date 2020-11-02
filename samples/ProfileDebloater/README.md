# Profile Debloater

Scans the PSARC files in a given directory and its subdirectories for arrangement IDs and removes from a profile file stats for arrangements that have IDs that are not found in those files.
The program includes the IDs for on-disc/RS1 imported arrangements, so their stats will be preserved.

A backup of the profile file will be created with a .backup extension in the same folder as the profile file.

# Use

ProfileDebloater "Path to a profile file" "Path to DLC directory"

e.g.

ProfileDebloater.exe "C:\Program Files (x86)\Steam\userdata\xxxxx\221680\remote\XXXXXXXX_PRFLDB" "some path\steamapps\common\Rocksmith2014\dlc"
