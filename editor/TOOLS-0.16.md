# DoomSNES tools 0.16 — Windows x64

MIDI/MUS conversion now rounds each event once onto the native score clock. The previous conversion first rounded to a separate note grid, then rounded again to the SPC clock; the combination could move notes unevenly. Old project settings requesting a coarse 10/25 Hz grid no longer impose that extra rounding. Auto fit now reduces velocity detail before shortening a score; it does not make a song fit by degrading the rhythm to a coarse grid.

For regular scores, the converter selects a beat-aligned score clock using the retail driver's existing FA tempo command. Constant MIDI tempo metadata helps choose it; MUS and older projects can use their event spacing. A clock is selected only when it substantially improves onset alignment and its tempo rounding stays below 0.15%. Irregular scores retain the finest absolute clock. Existing MIDI tempo changes remain represented by their absolute event times. The report shows the selected native clock and tempo rounding.

The retail driver still services music on approximately 20 ms hardware ticks, and processes simultaneous voices in sequence. These changes remove avoidable conversion errors; they do not make the original SNES engine sample-accurate. The same sequence is used by SPC preview, the exported ROM and native gameplay. No new SPC driver patch or hardware timer change is introduced in this release.

## Toms and timpani

Automatic mapping now selects the actual native **Toms (patch 43)** for GM drum-channel tom notes 41, 43, 45, 47, 48 and 50. It also maps **Melodic Tom (GM program 118; internal program 117)** to the tom sample and **Timpani (GM program 48; internal program 47)** to **native patch 67**. The converter brings those samples into a mixed bank when the selected home bank lacks them, instead of silently using a snare or unrelated melodic instrument. Tom drum-note octave calibration and melodic timpani pitch are preserved.

Instrument rows now name the six tom notes, melodic tom and timpani. Manual sample selection remains unrestricted. Native sample identities were checked against the user's ROM banks and ArchiveBKUP's PatchList; existing retail sequence traces were also consulted for pitch ranges.

For an existing import, use **Apply settings** or **Auto convert / fit** to rebuild its timing. To replace older saved snare assignments on tom notes, open **Instruments / channels > Auto match closest bank**, then apply. Keep-assignment mode intentionally preserves manual sample choices. Merely opening a saved ROM does not reconstruct MIDI timing from its baked sequence.

The updated Grabbag example remains a full-length 61.39-second, eight-voice, 2 MiB ROM. It now includes the actual tom sample. That sample consumes additional SPC RAM, so fitting the full song uses one velocity level (4,907 of 4,947 available score bytes), while retaining the improved timing. The original inputs and earlier examples are preserved separately.

Validation includes a dense six-voice steady-beat fixture, bounds on native attack timing, migration from saved coarse grids, tom/timpani assignments and tuning, native audio rendering, ROM/preview memory parity, the full converter regression suite, and finite native gameplay/restart checks. Physical SNES hardware has not been tested. Copyright editing and source-layout RL5934 support from 0.15 are retained.
