# DoomSNES tools 0.14 — Windows x64

Music conversion now uses the retail sound driver's envelope releases by default. MIDI note ends release the sound instead of immediately silencing the voice. The allocator uses available voices before stealing held notes. This applies to both the converter's SPC playback and exported ROMs; no emulator-specific patch is required.

In Music, leave **Natural note releases** checked, then use **Auto convert / fit** or rebuild through the instrument settings to reconvert an existing import. Previously saved sequence bytes do not change merely by opening a project. Uncheck the option for the older tight, hard-stop behavior.

**Extra tail protection (ms)** optionally reserves a voice briefly after a note ends. Its default is zero: native envelopes still release naturally, and normal voice reuse keeps score storage compact. Increasing it can reduce tail interruptions but may need more sequence space or displace held notes. Eight voices remain optional; six preserves two voices for game sound effects.

Importing a ROM now exposes **Title music** and **Intermission music**, alongside its 22 level slots. You can preview, replace, save and reopen these assignments without a PWAD. Assigning a song to included maps preserves the separate title and intermission choices. Native recompiler patch validation accepts the two verified scene music operands and rejects invalid references or altered surrounding instructions.

The SPC preview now clears discarded score data consistently with the ROM uploader. Preview and exported-ROM APU memory match for the tested scene replacements.

The refreshed Grabbag example retains the full 61.39-second source, uses eight voices, a 50 Hz timing grid and eight velocity levels, with natural release. It fits its 6,689-byte music allocation (6,495 bytes used) in a 2 MiB ROM. Dense overlapping passages still require voice omissions; native envelopes and the source samples also limit how closely it can reproduce PC MIDI.

Validation: 8,917 converter assertions; 43 native tests; natural versus hard release audio measurements; bounded release, voice allocation, project round trips, title/intermission ROM routing and preview parity; UI selection and conversion checks. Physical SNES hardware has not been tested. The 0.13 driver workspace and streaming-player fixes are retained.
