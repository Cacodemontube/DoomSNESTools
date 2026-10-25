# DoomSNES tools 0.12 - Windows x64

Keep all three EXEs, DoomSNESAudio.dll, assets, mods and editor-data together.
Supply your own compatible Doom SNES ROM. PWAD is optional for asset edits.
Version 0.11's compact-ROM and graphics/audio repairs remain included; see
DOOMSNES-TOOLS-0.11.md for those details.

## Music voice budgets

The default is six simultaneous music voices. Convert with 6 voices rebuilds the
imported MIDI/MUS with that budget. Max voices (8) rebuilds using up to all eight
hardware voices. Eight leaves no dedicated voices for effects; gameplay effects
can interrupt music. These buttons rebuild the staged SPC, not the source MIDI.

Allow fewer voices to keep the full song is now unchecked by default. With it
unchecked, automatic fitting tries reduced timing/velocity detail, then shortens
the score if necessary while retaining the requested voice budget. The report
states the shortened duration. Check it to prefer a complete song with fewer
simultaneous notes instead. SPC sequence memory still limits complex music.

One or two active tracks can be correct for a source with only one or two
overlapping notes, or with other instruments muted. The voice setting is a
maximum; the converter does not duplicate notes merely to fill silent channels.
Changing the selected map restores that song's saved voice budget in the control.

Command line: --music-voices=8 selects eight; --auto-music retains the requested
budget by default. Add --allow-voice-reduction to permit fewer voices during fit.

## Supplied tHE SKY project investigation

The supplied SNED rebuilds byte-for-byte to the supplied SFC. Its DSE has matching
base/target hashes and reconstructs that same ROM exactly. The uploaded SPC RAM
matches the project music data. Original files were left unchanged.

Snes9x 1.60 ran the supplied ROM through a five-minute input movie with movement
and pistol firing. Native recompiler tests also loaded the ROM and its sidecar,
moved and fired. A separate native driver trace remained musically active for
250 seconds, passing two complete score loops. The reported crash was not
reproduced in these tests; an emulator/version and precise trigger are still
needed to identify it. This release does not claim to fix that unobserved crash.

An additional eight-voice version of this project is kept outside the shared
build under testfiles/voice-tests-0.12. It retains the full 117.66-second score.
It is a comparison fixture, not a claimed crash repair.

## Validation

8,917 editor assertions passed, plus existing audio, graphics, modified-ROM and
music refinement checks. New tests measured six and eight actual simultaneous
DSP voices, verified strict fitting under memory pressure, and rejected more
than eight voices. UI tests clicked both preset buttons and checked the rebuilt
tracks. Native gameplay loaded both the eight-voice ROM and its sidecar.
No native runtime changes were required for the new voice setting.
