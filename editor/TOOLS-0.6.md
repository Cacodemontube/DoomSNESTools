# DoomSNESconv 0.6 - music workstation and original-ROM assets

Run `DoomSNESconv.exe` beside `DoomSNESAudio.dll`, `DoomSNESRecomp.exe` and
`editor-data/`. Windows x64 and .NET Framework 4.x are required. Existing map,
graphics and sound conversion features are described in `TOOLS-0.5.md` in source.

## Edit the original game without a PWAD

Choose an unmodified Doom (USA) ROM. Leave PWAD empty, or click **Use original
ROM levels (no PWAD)** in Maps. Music lists the 22 retail map slots. Import music,
sounds or graphics, Analyze, then **Build ROM**. The source stays unchanged.
The export includes `.sfc`, `.dse`, `.sned` and a conversion report. Save/open
converter setup (`.snconv`) keeps staged imports and editable music source notes.

ROM-only exports expand to 4 MiB, storing new assets in Super FX banks $60-$7D;
$7E/$7F are excluded because they are CPU RAM. Every original map allocation,
episode route and difficulty gate is preserved. Play in DoomSNESRecomp loads the
original ROM and verified `.dse` sidecar. This requires the accompanying updated
recomp. Compatible emulators can use the exported `.sfc`; physical hardware has
not been tested, and cartridge/flashcart support for expanded Super FX ROMs varies.

## Music

* **Import MID / MUS** reads a score and chooses a native instrument bank.
* **Instruments / channels** assigns each MIDI channel/program combination to a
  native sample/patch, with transpose (-48..48 semitones) and mute. Channel 10
  exposes individual GM drum notes so kick, snare and hi-hat remain independent. Choose among
  18 original level/intermission banks. Instrument audition runs the retail SPC
  driver. Bank-local sample sets and sequence capacities differ; arbitrary samples
  from different banks cannot be combined in one song in this version.
* **Auto match closest bank** compares GM instrument families to the ROM's
  available timbres. This is a musical approximation, not soundfont matching.
  Generic native patch labels retain their patch/wave numbers for auditioning.
* **Apply settings** recompiles manual assignments with the voice/time limits.
* **Auto convert / fit** tries the full duration,
  reducing voices, timing detail and velocity levels to fit SPC sequence RAM.
  If necessary it shortens the score. The report names every concession.
  **Keep instrument assignments when fitting** is enabled by default; disable it
  to also choose a new bank automatically.
* **Play source** uses the Windows MIDI synthesizer for the interpreted source
  notes; MUS timing is interpreted directly. The PC soundfont may differ from the
  original author's. Modulation/reverb/SysEx and continuous pitch bends are not
  reproduced by the importer.
* **Play converted (SPC)** renders the original SPC700 driver and DSP through the
  same audio core as gameplay. It also plays original map music when no replacement
  is staged. Preview length is adjustable up to 600 seconds; Stop cancels playback
  and discards a pending render. Instrument/score rendering runs off the UI thread.

Conversion has 1..7 music voices, with at least one DSP voice available for effects.
Dense notes can be shortened or omitted. Native timing is 30 Hz; fitting can use
coarser note placement. Sequence meters measure the selected bank's actual capacity.

## Sound effects

Load the native sample list, then choose a shared effect alias. **Preserve WAV pitch
for selected effect** measures that effect's hardware playback rate using the
original SPC driver, then resamples the WAV at that rate before BRR encoding.
This compensates for effects that play above or below 11025 Hz. Aliases sharing a
waveform still retain their intentional pitch differences. Imports are tuned to
the selected alias; a variable-pitch effect can still vary during gameplay.

Both sound preview buttons execute the original driver, including its pitch and
envelope. No editor-only playback pitch is written into game code. Silence trimming,
normalization, looping and explicit tail fitting remain available. Streamed samples
retain their 3321-byte SPC limit; resident slots retain their original allocations.

## Episode intermission artwork

Screens now lists **Episode 1**, **Episode 2** and **Episode 3 intermission** alongside
the Williams logo and title. Import a 256 x 224 PNG/BMP. Images are quantized to 16
SNES colors and converted to native 4bpp tiles, map and palette transfers. Existing
score text, splats and overlays remain part of the original game. Export PNG and
Restore original work for each screen. These screens work with PWAD campaigns and
ROM-only projects; a one-level campaign ending at the final text may never display
a normal episode-map intermission.

## Ending Text and Credits

**Ending Text** edits all three episode stories independently. Changes refresh
automatically over the corresponding original/staged intermission image using
the actual decompressed ROM font. Each story starts at column 1, row 4 and allows
30 characters per line, up to 24 rows. **Wrap to 30 columns** wraps long paragraphs.
The original byte allocations are 469, 495 and 531 bytes, including line commands
and the terminator. The status shows usage; shorten text if it exceeds the limit.
ASCII space through `~` is supported; unsupported characters are rejected.

**Credits** selects page 1 or 2 and edits text, row and column for each native entry,
with a live preview over the title artwork. Pages are independent even when the
original entries shared strings. Keep text within the 32-column, 28-row screen;
blank text hides an entry. Native entry counts are retained. New strings are
allocated in the campaign/expanded asset area. Title/background changes affect
both pages, matching the original game's shared title artwork. The original game
controls when its credits cycle appears; text editing does not change that schedule.

Invalid drafts are shown in red and block Analyze, Build and setup saving until
fixed or restored. Valid stories/credits persist in converter setups and projects.
Preview text uses native glyphs; hardware blending and display scaling can differ
slightly from the editor's enlarged image.

## Developer commands and references

```
DoomSNESconv.exe --mod-rom original.sfc modded.sfc --music=E1M1:score.mid --auto-music
DoomSNESconv.exe --mod-rom original.sfc modded.sfc --wav=10:sound.wav --fit-sounds
DoomSNESconv.exe --analyze-rom original.sfc unused --music=E1M1:score.mus --auto-music
```

The CLI uses the automatic GM/native bank matcher. Per-alias WAV pitch compensation
is currently exposed in the UI; CLI WAV imports retain the existing 11025 Hz default.

References: Randal Linden's DoomFX `musicdrv.a`, `score.a`; ArchiveBKUP's original
`MUSIC_I/L/T/O.C`, `MUSIC_B.C`, `MUSIC_D.C`, Sculptured Software `dcom.i`, `PatchList`
and `midinfo.txt`. Driver/sample bytes are read from the user's original ROM;
none are embedded in the tools or audio library.

Validation includes original-map round trips, native playback in all 18 banks,
long-score fitting, rate-correct BRR duration, expanded-ROM patch validation and
transactional rejection of altered code/maps/routes, plus episode image round trips.
The native runtime checks all three episode images/stories and both credits pages.
