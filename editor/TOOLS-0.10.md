# DoomSNES tools 0.10 - Windows x64

Keep the three EXEs, DoomSNESAudio.dll, assets, mods and editor-data together.
Supply your own compatible Doom SNES ROM. PWAD is optional for asset edits.

## Graphics and sound conversion fixes

Enemy sprite columns now include the native end marker used when scaling and
clipping sprites. Wall and sprite pixels stay inside the Super FX 2 MiB window.
Expanded title/logo/intermission, HUD, text and audio data use CPU bank addresses
mapped by GSU2 cartridges, including Snes9x. Old expanded CPU pointers are
normalized when rebuilding; old expanded enemy/wall columns are relocated into
the GSU window when space permits. Rebuild older affected ROMs with this version.

Project save, load, cloning and optimization use the same 128 MiB JSON limit.
This fixes the serialization-size error shown after staging larger sound/graphics
changes. ROM and SPC allocation limits still apply separately.

## Music

In Instruments / channels, selecting a sample auditions one second through the
native SPC driver. Disable Audition selection to browse silently; Stop cancels
pending auditions. Auditions pack only that sample, independently of the song.
Every native music patch remains selectable, with automatic instrument matching.

Select a row and use Background, Normal, Important or Keep to set that MIDI
channel's priority, including all its percussion rows. The numeric -10..10
priority remains editable per instrument; higher values survive voice pressure.
Apply settings recompiles the staged song. At most seven music voices reserve
one DSP voice for game effects. Mute all / Unmute all include percussion.

Play source displays all 16 MIDI channels' active note velocities (activity,
not measured audio volume). Play converted displays actual post-envelope,
post-volume sample peaks for the eight SPC voices, at 50 updates per second.
SPC voice numbers are hardware voices, not MIDI channel numbers. The meters
follow the rendered preview timeline; system playback buffering can add latency.

The SPC timer handler processes elapsed pulses instead of dropping extra pulses
under load. Song startup discards stale setup pulses. Conversion retains absolute
49.8046875 Hz timing, ownership-checked note stops, attack protection and automatic
lead/bass and kick/snare preference. Nonzero notes shorter than one timer tick
receive one tick instead of disappearing. Auto fit tries the finest 50 Hz grid
across voice budgets before reducing timing detail to 25 or 10 Hz.

This remains an adaptation to the original soundbank and voice limits. Pitch
bends are approximated at note onset; modulation and SysEx are omitted. Sustains
longer than 255 ticks still retrigger. Overloaded scores may omit or shorten
lower-priority notes; the conversion report gives the count. This release does
not promise identical playback to a PC General MIDI soundfont.

## Validation and rollback

The editor/converter regression suite passes 8,917 assertions, with native-bank,
sample-release, mixed-bank, HUD and modified-ROM checks. An additional 1,441
checks cover seven-voice timing (840 note attacks over 30 seconds), real DSP
meters, short attacks, sprite terminators, title pixels, mapped CPU pointers,
all 48 audio modules after graphics relocation, and JSON larger than 8 MiB.
The screenshots were available; their exact replacement artwork and failing
MIDI/ROM files were not. Hardware/emulator behavior still needs testing with
those original inputs. The earlier build is preserved in the adjacent dated
build-rollback-0.9 folder.

See DOOMSNES-TOOLS-0.9.md for existing ROM compatibility and installation reset
instructions. RL5934.sfc's relocated engine remains unsupported by the native
recompiler; compatible modified ROMs retain the expected compiled engine layout.
