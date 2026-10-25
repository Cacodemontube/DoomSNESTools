# DoomSNES tools 0.13 - Windows x64

Keep the EXEs, DoomSNESAudio.dll, assets, mods and editor-data together. Update
the native recompiler and audio DLL along with the converter/editor. Supply your
own compatible Doom SNES ROM; PWAD is optional for asset edits.

## Music corruption on boot and pause/resume

The converter's added note-stop routine was placed at SPC address 1024, in what
looked like unused assembler text in the ROM driver. That area is live workspace
after startup. Grabbag's instrument processing overwrote those instructions;
subsequent note stops could execute changing data as code, producing chirps,
bad volume/panning, unstable timing and other sound corruption. This was a
converter driver-placement bug, not a problem with the supplied MIDI.

The routine now lives at 1eb0..1ecd, after the timer helper and before the
driver's constants. The old location is restored when upgrading an affected
driver. Rebuild affected projects with this version. You can also open an
affected SFC as the base ROM with no PWAD or new music assignments and build a
new SFC: the legacy driver is detected and repaired while preserving its music.
The native recompiler accepts the repaired driver in both normal and compact
asset patches, while continuing to validate code changes and exact base hashes.

The supplied Grabbag project was rebuilt with its existing eight-voice score,
samples and note timing preserved. Fixed SFC, DSE and SNED files are outside the
shared build, in testfiles/fixed-0.13. That DSE/SNED uses the original Doom (USA)
ROM as its exact base. The original supplied files remain untouched.

## Faster SPC preview

Play converted now streams short PCM buffers from a persistent SPC instance.
It no longer renders the whole selected preview before starting playback. Stop
cancels rendering as well as playback, and the selected preview duration still
ends playback automatically. The in-ROM Loop option does not make the converter
preview run indefinitely. Voice meters follow the audio device's playback
position, including buffering, rather than an independent wall clock.

Streaming uses the same driver, instruments, native 32 kHz output and sequence
clock as offline rendering. It does not accelerate the score. Exact sample and
meter comparisons passed. A muted device test started in about 0.085 seconds;
a two-second clip completed in 2.1 seconds, and Stop immediately cancelled a
ten-minute preview. These are measurements on the build machine, not guarantees
for every audio device.

## Validation

All 43 native tests and 8,917 editor assertions passed, including the existing
audio, graphics, modified-ROM and music refinement checks. New mixed-bank tests
exercise eight voices, score loops and eight fade/stop/restart cycles while
checking the note-stop and timer instruction bytes after every second. A legacy
ROM is also repaired without MIDI/SNED and all 48 audio modules remain unchanged.
Grabbag's repaired driver stayed intact in Snes9x 1.60 after boot and repeated
pause/resume inputs. The emulator test was finite, muted and closed afterward.
Hardware listening tests and the user's particular emulator remain unverified.

See DOOMSNES-TOOLS-0.12.md for the six/eight voice presets and fitting options.
