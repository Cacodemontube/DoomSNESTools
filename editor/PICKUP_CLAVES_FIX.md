# Pickup beep and E1M2 claves

The Windows x64 tools in `build/` were rebuilt from the current working tree.
The shared editor/converter audio code is updated; the game renderer and its
configuration were not changed by this task.

## Pickup sound

The Sounds list now identifies resident waveform **03 – Item pickup beep**.
Its selectable aliases are effects 5, 6 and 7 (`itemup`, `itemup2`, `itemup3`).
They share one replacement waveform and retain their native driver envelope
and playback differences. Preview, original/replacement playback, WAV import,
pitch compensation and ROM export work through the existing sound workflow.

The old importer followed only the instrument's nominal waveform byte. These
effects select BSN 2 with nominal wave zero, but the running native SPC driver
selects resident wave 3 through its envelope. A DSP-state probe confirmed the
BRR start at SPC address 14338, matching wave 3's directory entry. No new sound
or gameplay hook was introduced.

## Claves

GM channel-10 percussion note **75 (Claves)** now discovers and selects the
native E1M2 instrument **56**, waveform **49**. It is named
`Claves (E1M2) / woodblock` in the instrument list. MIDI percussion labels also
identify claves and high/low woodblocks.

Wave 49 is embedded in the same upload chunk as wave 48. The old exact-chunk-
start check omitted instrument 56, and bank relocation could not extract it.
Bank discovery now accepts contained waveform entries; relocation extracts
their bounded BRR stream. Existing exact-start samples retain their original
upload data. The E1M2 embedded waveform has a three-byte header and 1440 BRR
bytes, starting at SPC address 33361.

The original E1M2 SPC sequence plays the claves at DSP pitch 1824, a BRR playback
rate of 14250 samples/s. Existing native note 67 calibration produces that same
rate for imported GM claves; no synthetic substitute or retuning was added.

## Verification and rollback

`PickupClavesTests` covers all pickup aliases, native audibility, pickup WAV
replacement preview/export PCM equality, embedded sample boundaries, GM75
automatic mapping, E1M2 pitch, byte-identical BRR relocation, mixed-bank native
playback and ROM-upload/preview equality. It is included in the editor self-test.
The existing all-instrument relocation/timing checks and steady-beat,
tom/timpani tests also pass.

Full rendered PCM is not asserted identical across different music bank layouts:
sequencer layout and bank initialization can change playback phase. Sample
bytes, native playback rate and ROM/preview parity are checked explicitly.

Source backups, the previous converter executable, native DSP probes and test
results are in `build-evidence/pickup-claves-20261019/`. No ROM, WAV or MIDI
files were copied into the build's runtime asset directory.
