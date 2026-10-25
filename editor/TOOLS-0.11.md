# DoomSNES tools 0.11 - Windows x64

Keep all three EXEs, DoomSNESAudio.dll, assets, mods and editor-data together.
Supply your own compatible Doom SNES ROM. PWAD is optional for asset edits.

## ROM compatibility and repairs

Asset-only export now packs graphics, text and audio into a 2 MiB ROM when
possible, preserving native music uploads and shared sample data. Snes9x 1.60
overwrites the expanded area of a 4 MiB Super FX ROM when creating its mirrors;
that broke expanded samples and other relocated assets. Version 0.10 did not
account for that older mapper. Rebuild affected ROMs with this version.

If the assets cannot fit, saving stops with an explanation. The Optimization
tab has an explicit Allow 4 MiB if needed option; expanded output requires a
compatible mapper and is incompatible with Snes9x 1.60. Wall and sprite columns
remain within the Super FX 2 MiB window. Real hardware has not been tested.
The native recompiler supports the new compact asset patch profile, including
loading a compacted ROM or applying its sidecar to the matching base ROM.

## Sound and music

Auto-fit WAV to slot reuses the last WAV selected for that sample, or asks for
one. It enables native pitch compensation, silence trimming, normalization and
slot fitting. Oversized audio may have its tail shortened and faded; read the
conversion report and audition the result before saving.

Instruments / channels now has a Background-to-Keep priority slider. It changes
all instrument/drum rows of the selected MIDI channel. Accepting changes rebuilds
the staged SPC immediately. Priority matters when notes compete for voices;
it does not alter the source MIDI file. Auto-fit tries coarser timing grids
before reducing the requested voice limit, preserving more polyphony when space
permits. Reports distinguish active SPC tracks from the requested voice limit
and source MIDI channel count. Long or complex imports may still need fewer
voices, coarser timing or shortening. Pitch bend remains approximate, and very
long notes may retrigger at the native duration limit.

The music monitor has its own reserved space. MIDI playback shows note velocity
and note names across 16 source channels. Native SPC playback shows eight actual
DSP voice peaks. These are different measurements; SPC display timing follows
the audio buffer. Sample selection still auditions one second through the native
driver, and all music samples remain selectable.

## Graphics

Screens is renamed Graphics. Its asset list now includes the red pickup-message
font used during gameplay, separate from the story font. Export/import its
128 x 32 PNG/BMP atlas: 58 eight-pixel glyphs, ASCII ! through Z, sixteen columns.
Imported colors map to the game's palette; keep the exported layout and size.

## Validation

The editor suite passed 8,917 assertions; 81 revision checks cover the new UI,
font round trips, sample fitting, compact exports and re-editing. Native patch
tests verify exact output, pointer bounds and transactional rejection of altered
code, maps, routes and driver data. Six native gameplay cases loaded successfully;
the repaired pistol fired repeatedly, reducing ammo from 50 to 43. Snes9x 1.60
booted the compact ROM into E1M1; automated firing in that emulator was not
completed. Original user ROM and WAV files remain unchanged.
