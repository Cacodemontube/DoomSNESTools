# DoomSNES tools 0.8 - open modified ROMs and embedded converter branding

Windows x64. Run DoomSNESconv.exe beside DoomSNESRecomp.exe, DoomSNESAudio.dll
and editor-data. Earlier feature guides are DOOMSNES-TOOLS-0.7.md and
DOOMSNES-TOOLS-0.6.md.

## Open and edit a modified Doom ROM

Browse for the file in Base ROM (USA), just as for an original ROM.
Compatible Doom USA ROMs are accepted at 2 MiB or 4 MiB, including files with
a 512-byte copier header. A previous converter export can be opened without
its .dse, .sned or .snconv sidecars. Leave PWAD empty to edit the music, sounds,
graphics, ending text and credits already in that ROM. Restore original means
restore the artwork/text/sound from the selected base, including its existing
modifications. Changing base files refreshes the cached asset previews.

Build ROM writes a new .sfc and preserves the selected base file. Existing
expanded allocations are reserved; new imports use later expansion banks.
The meter reports the space available for additional imports. Previous data is
not automatically reclaimed, so repeated edits eventually exhaust expansion
space or the native audio table limits. A no-edit rebuild of a supported 4 MiB
converter export is byte-identical. Existing map routes are preserved in asset
mode. Engine signatures and map structures must retain the supported USA
layout; regional revisions and arbitrary engine/layout hacks are not supported.
Campaigns with already changed routing may not support replacing their entire
PWAD map set; use the original base to create a fresh campaign.

The binary ROM retains playable converted music, but the original MIDI/MUS
source and manual channel settings cannot be reconstructed from it. Keep your
.snconv setup if you want to resume editing those source arrangements.

## Native playback and exact-base patches

Play in DoomSNESRecomp automatically launches with the selected compatible
modified base and the new .dse sidecar. The patch is tied to the exact normalized
SHA-256 of that base. It cannot be applied to a different original or modified
ROM. Keep the base file used for each export, or play the full exported .sfc.

Manual commands:

```
DoomSNESRecomp.exe --modified-rom --rom "Modified-base.sfc" --campaign "Next-edit.dse"
DoomSNESRecomp.exe --modified-rom --rom "Full-export.sfc"
```

Modified-ROM mode checks the supported loader/engine layout, then supplies the
selected file's exact SHA-256 and CRC32 to the normal loader. Direct full-ROM
playback ignores an old editor/active.dse patch. Modified ROMs and campaigns
remain local-only. Compatible emulators can also use the exported .sfc.
Physical hardware has not been tested.

## Embedded branding

DoomSNESconv.exe contains the Logo3 Windows icon, an embedded ICO resource and
an embedded original PNG. The title bar and green header load their branding
from the executable, with no loose icon or PNG file required at runtime.

No ROMs, WADs, soundbanks or reference music are included in the build.
