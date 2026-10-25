# DoomSNES tools 0.9 - music timing, free sample assignment and ROM installation

Windows x64. Keep DoomSNESRecomp.exe, DoomSNESed.exe, DoomSNESconv.exe,
DoomSNESAudio.dll, assets, mods, and editor-data together.

## Music

Conversion uses the SPC driver's actual timer: 50 * 255 / 256 score ticks per
second. Absolute MIDI/MUS timestamps are quantized before channel allocation,
so overlapping notes cannot push later attacks progressively off tempo.
Automatic fitting starts with a 50 Hz note grid, then tries 25 and 10 Hz.

In Instruments, every native music patch is available in every dropdown.
Automatic matching still recommends an instrument appropriate to the part.
Mixed banks pack only the chosen, unmuted instruments and their BRR samples,
loop pointers, volume/pitch/effect curves into SPC memory. If that selection
cannot fit, the converter explains the limit before writing a ROM.
Approximate or uncertain instrument identifications are labeled explicitly.

Priority runs from -10 to 10; higher values preserve that part when channels
are full. Automatic priority favors lead/bass and kick/snare over quiet pads
and cymbal tails. Notes receive initial attack protection, and closed/pedal
hi-hats choke open hats. Low-priority notes can be omitted rather than cutting
a protected melody. Seven music voices still reserve one DSP voice for SFX.
Mute all, Unmute all, and channel muting include every percussion row.

Converted tracks explicitly stop their owned DSP voice at note-off. The
converter installs an ownership-checked E8 stop command into unused diagnostic
text space in the user's SPC driver. Original engine code, song commands and
SFX are retained. The patched driver is compressed back into its original ROM
allocation and verified before export; no extra ROM space is required. Native
preview uses the same driver addition as the exported ROM. Existing staged
scores must be recompiled with Apply settings or Auto convert / fit to gain
the new timing and stops. Keep the .snconv source setup for further editing.

Timing remains limited to native SPC ticks and eight hardware DSP channels.
Notes longer than 255 score ticks currently retrigger at chunk boundaries;
original sample envelopes and natural SPC echo can also affect the sound.
Actual hardware/emulator listening tests remain useful alongside native tests.

## Modified ROMs and reset

The import label is Doom SNES ROM. Compatible USA-based map and asset mods,
including 2/4 MiB files and 512-byte copier headers, can be edited and played
natively. Leave PWAD empty to change music or other regular-ROM assets.

In the launcher, Browse selects the file. Play modified ROM natively validates
it and opens native gameplay; the previous launcher closes after a successful
start. The converter's Play button also handles compatible modified bases.
CLI: DoomSNESRecomp.exe --modified-rom --rom "your-mod.sfc" --no-launcher

Reset ROM installation clears the cached ROM selection and campaign patch.
The active patch is renamed to a timestamped .bak. ROM files, projects, exports,
SRAM, save states, and bundled engine assets are preserved. The app reads assets
from the selected ROM; there is no extracted commercial-asset folder to delete.
Choose another ROM after resetting.

Support requires the compiled retail USA engine layout. RL5934.sfc relocates
its engine, map/asset tables and loader code using the older source tools. It
is not compatible with this retail recomp build and is explicitly rejected.
Supporting that ROM requires its matching engine layout/recompilation; bypassing
the ROM hash alone would not make it playable. Other arbitrary engine/code
hacks are likewise outside the supported data-modification profile.

## Wall and sprite replacements

The Super FX addresses original-ROM wall/sprite storage, not the CPU's expanded
ROM banks. The converter now respects that distinction. It moves selected,
unchanged compressed CPU-uploaded audio chunks into expansion space, preserving
all upload destinations and contents, and uses the released original-ROM space
for wall/sprite pixels. Existing shared graphic columns are protected; literal
runs and repeats use native compression. This fixes corrupted 128x128 door
replacements caused by inaccessible texture storage. New artwork still must
match the displayed dimensions and fit the available Super FX window.

Screens, HUD graphics, text and audio continue to use CPU-addressable expansion
space. The allocation meter includes the storage needed for moved chunks and
tables. Original ROMs are never overwritten. Old exports with texture pointers
already placed in inaccessible banks should be rebuilt from their original base
and saved setup; the missing original allocations cannot be inferred safely.

A ROM-only rebuild also compares map content independently of JSON field order,
preventing false reports that an unchanged map was edited.

## Validation

The release was checked against the original SPC driver and native recomp:
- native onset timing through 60 seconds, every selectable patch's DSP pitch
  and released voice state, manual priority under dense competing notes;
- mixed sample banks: preview RAM equals the ROM's native module uploads;
- 128x128 wall round trips, incompressible test artwork, Super FX bank bounds,
  and byte-identical preservation of all 48 original audio uploads;
- original editor/converter/audio/graphics regression suites and transactional
  native patch rejection, including unauthorized SPC driver changes.

Earlier feature guides: DOOMSNES-TOOLS-0.8.md, DOOMSNES-TOOLS-0.7.md and
DOOMSNES-TOOLS-0.6.md.
