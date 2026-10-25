# DoomSNES tools 0.7 - native music mapping and editable HUD artwork

Windows x64. Keep DoomSNESconv.exe beside DoomSNESRecomp.exe,
DoomSNESAudio.dll and editor-data. See DOOMSNES-TOOLS-0.6.md for the rest of
music, sound, map, story and credits editing.

## Screens, font, weapons and status bar

In Screens, click Load from base ROM. The list now includes the text font,
status bar background, digits, all 32 faces, weapon indicators, keys, and master
tile sheets for all eight weapons. Every entry states its required image size.
Export PNG first, edit that template, then Import PNG / BMP. Restore original
removes the selected replacement. PNG transparency is retained; colours are
matched to each asset's native palette, including weapon flash palettes.

The font uses 8 x 8 glyphs in ASCII order starting at space (32). Its padded
last row contains unused image space. Replacing it updates the story, credits
and menu font together. Ending Text and Credits previews use the staged font.
The faces sheet is eight 24 x 32 faces across and four rows down. Weapon sheets
contain native 8 x 8 master tiles shared by the existing animation frames;
editing a shared tile changes every frame using it. Frame shapes, positioning
and palette assignments are preserved. Padded tiles at the end of a sheet are
not stored. These imports work in ROM-only and custom campaign exports.

## Music corrections and muting

Native BSN instrument IDs are now used for matching, rather than the numeric
proximity of BRR waveform slots. In particular, patch 45 is bass and patch 46
is the kick drum. Automatic melodic assignments exclude percussion patches.
Drum conversion uses native kit pitches (for example snare 75 and hi-hat 68),
with separate kick, snare, tom, crash, ride and hi-hat choices. Missing GM
instruments and unavailable drums still use approximations within the chosen
bank; audition and Transpose remain available for adjustment.

Instruments / channels has Mute all, Unmute all, Mute channel and Unmute channel.
Muting channel 10 includes every percussion row. Checkboxes commit immediately;
bank changes and Auto match preserve mute selections. Source MIDI playback,
SPC playback and ROM compilation now use the same mute decisions. All-muted
scores compile to valid silent tracks rather than leaving an older score staged.

For a project saved by 0.6, open Instruments / channels and use Auto match
closest bank, then Use assignments. Existing saved manual assignments are
retained until you rematch; they are not silently rewritten. Auto fit preserves
mute selections even when instrument reassignment is enabled.

Reference analysis used retail SPC-driver execution and music banks extracted
from the user's original USA ROM, the original DOOM.WAD MUS scores, and
DoomFX/ArchiveBKUP source. C:\projects\midireference was empty during this build;
no external MIDI or .spc reference files were available. Hardware playback has
not been tested. ROMs and reference music are not included in this distribution.
