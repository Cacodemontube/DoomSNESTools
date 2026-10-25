# DoomSNESconv 0.5 and DoomSNESed campaign support

The executables are in `C:\projects\DoomSNESRecomp\build`. Rebuild both tools with `editor/build_editor.ps1`. DoomSNESconv embeds Logo3.png as its Windows icon and uses that embedded icon in the green window banner. It needs no loose PNG or ICO at runtime. The input label is **PWAD**; compatible Doom 1 IWADs remain accepted.

## Convert and play natively

1. Open DoomSNESconv.exe, or **Convert WAD levels - DoomSNESconv** in the game launcher.
2. Select the original Doom USA SNES ROM and your PWAD. Select levels in the Maps tab. For WADs containing more than 22 maps, First only is selected initially.
3. Choose options in Optimization and click **Analyze selection** to check geometry, byte budgets and per-skill object pools without exporting a ROM/project/patch.
4. Click **Build campaign**. It creates `.sfc`, `.dse`, `.sned`, and `.conversion.txt` files beside the chosen output.
5. Click **Play in DoomSNESRecomp**. The game verifies the original base ROM and applies the converted campaign in memory. Start a new game in episode 1. No emulator is involved in this path.

**Open campaign** accepts an existing `.dse` or converted `.sfc` with its matching `.dse` sidecar. Keep that sidecar with the ROM. Emulator users can use the `.sfc` directly. Neither the base ROM nor the PWAD is modified. A converted ROM without its sidecar must be rebuilt with this converter for native play.

Command-line native play:

```powershell
.\build\DoomSNESRecomp.exe --rom "Doom (USA).sfc" --campaign "custom.dse"
```

`--campaign "custom.sfc"` also selects `custom.dse`. Campaign selection applies to that game process and overrides the editor's active patch for that process. Launching the game normally uses its usual editor/original-level selection. Custom campaigns are local play only.

The recomp accepts a dedicated campaign patch format, verifies source and result hashes, and permits level data plus specific campaign routing, first-map alias and checksum fields. It still verifies the original ROM identity. An arbitrary modified ROM or arbitrary program-code patch is not accepted.

## Optimization options

**Auto configure**, beside Build campaign, checks every PWAD map and selects maps that fit native allocations with conservative object/BSP limits. It enables preferred geometry optimizations and reports why each map was selected or disabled. Visual lighting changes, monster removal and scenery removal remain off. Review the report and selected levels before building; a storage recommendation does not prove that every custom puzzle remains solvable.

## Import graphics

Choose the base ROM, then open **Textures**, **Skies**, **Sprites** or **Screens** and click **Load from base ROM**. Every list entry shows its required pixel dimensions. Select an entry to preview it; **Export PNG** creates an editing template. **Import PNG / BMP** stages its replacement; **Restore original** removes that replacement.

- Wall textures and both sky panoramas retain their original dimensions and use the original 256-colour gameplay palette. Imported pixels are matched to that palette. Sky data uses the engine's raw 128 × 128 bottom-up column format, independently of wall RLE.
- All 232 native sprite frames are available. Verified monster frame names are shown where known; other frames retain their native number. PNG alpha below 128 becomes transparent. Each animation frame is replaced individually; dimensions, animation timing and object collision remain unchanged. Replaced frames take precedence over the recomp's optional PC rotation artwork.
- Williams and title images require 256 × 224 pixels and are quantized to 16 colours. Export an original screen to use as a size/layout reference. Import builds SNES 4bpp tiles, a tile map and BGR555 palette. Existing overlays and other palette banks are preserved. Highly detailed artwork may need simplification to fit available contiguous allocations.

Click **Analyze selection** to validate graphics alongside the selected levels, then **Build campaign**. Replacements are included in the `.sfc`, native `.dse` patch and editable `.sned` project. The map editor's wall previews also show imported wall artwork when that project is opened.

The bottom meter measures the last Analyze/Build: green maps, blue graphics, purple music plus shared audio tables, orange sound samples, and grey free ROM space. All imports share the unused tails of converted map allocations. Fixed sky replacements use their existing space. Per-section audio meters also show SPC limits. A failed conversion shows red and leaves the source files unchanged. Total ROM size remains 2 MiB.

## Closest supported action replacements

Enabled by default, **Replace unsupported actions with the closest SNES behavior** uses the PC action descriptions and DoomFX native behavior. Examples:

| PC behavior | SNES replacement |
| --- | --- |
| Fast walk-once door open/stay (109) | Normal-speed walk-once door open/stay (2) |
| Fast repeating lift (120) | Normal-speed repeating lift (88) |
| Fast 16-unit stairs (100/127) | Normal-speed 8-unit stairs (8/7) |
| Secret walk exit (124) | Normal walk exit (52), following selected campaign order |
| Random sector flicker (17) | Random sector blink (1) |
| Timed sector door (10/14) | Initially closed, manually operated open/wait/close door, when boundary actions can be preserved |

The conversion report names each source and target action. Approximation can change speed, activation, repetition, texture changes, damage or stair height. W1/WR mean walk once/repeat; S1/SR mean Use once/repeat; D1/DR mean manual door once/repeat. A switch-to-walk approximation on a one-sided wall is rejected because it would be unusable. A timed-door approximation stops if it would overwrite existing boundary actions.

There is no blind replacement for unknown effects or remote key behavior with no suitable native operation. Conversion stops by default. **Allow reported substitutions for unsupported content** explicitly permits remaining unsupported actions/objects to be omitted and unknown textures to use a fallback. Inspect those reports and playtest progression. Custom scripts, UDMF/Hexen maps, arbitrary generalized actions, PC bitmap flats remain palette-colour surfaces. Custom WAD graphics are not imported automatically; use the asset tabs for PNG/BMP replacements.

## Continue editing in DoomSNESed

Open the generated `.sned` with the original base ROM. It retains campaign order, the first-map storage alias and the final ending. The editor selects the first converted map's storage slot. Rebuilding or editing a sector keeps the routing; Apply/export automatically writes the campaign patch format. Apply then launch the game normally to play the edited active campaign, or export a named `.dse` and open it in the converter. Older 0.3 converter projects lacked this metadata; reconvert them to retain campaign behavior.

## CLI and validation

```powershell
Start-Process -Wait build/DoomSNESconv.exe -ArgumentList '--convert "Doom (USA).sfc" "levels.wad" "custom.sfc" --first --merge --allow-substitutions'
Start-Process -Wait build/DoomSNESconv.exe -ArgumentList '--analyze "Doom (USA).sfc" "levels.wad" --first --merge --lighting=16 --monsters=48 --remove-scenery --allow-substitutions'
```

Specific map markers can replace `--first`. Other switches are `--strict-actions`, `--no-weld`, `--no-collapse`, `--no-compact-tags`, `--no-best-fit`, and `--no-reject`. CLI `--merge` enables sector merging. The last CLI report is saved as DoomSNESconv-last-run.txt beside the tool; failures write DoomSNESconv-error.log.

Self-tests cover action fallback/rejection, timed-door conversion, safe wall collapse, tag compaction, visibility bits, object/lighting options, campaign project round-trip, embedded icons and protected source files. `tests/validate_doomsnesconv.ps1` runs converted campaigns in DoomSNESRecomp, including a use exit, converted secret walk exit, two-level progression, actual episode 3 ending, and a larger-slot alias when its sample export is present. Arbitrary PWADs still require gameplay testing.

## MIDI / MUS music

The converter has separate **Maps**, **Optimization**, **Textures**, **Skies**, **Sprites**, **Screens**, **Music**, **Sounds** and **Report** tabs. Maps also has **Save converter setup** / **Open converter setup**. A `.snconv` setup preserves checked levels, options and staged asset bytes; keep the original base ROM and PWAD available. Exported `.sned` projects preserve native audio when edited in DoomSNESed. To change voice/time options after reopening a setup, reimport the source score.

1. Load your PWAD, then select a map in **Music**.
2. Choose 1..7 music voices (default 6, leaving room for effects). Set an optional duration limit; zero keeps the whole score. Choose whether it loops.
3. Click **Import MID / MUS**. **Apply settings** recompiles the source imported in this session. **Assign to included maps** shares the same song among currently checked maps. **Use original music** removes an assignment.
4. Review the conversion report and SPC meter, then Analyze and Build. Only included maps receive music assignments. Play in DoomSNESRecomp to hear the native result.

Supported input is Standard MIDI format 0/1 with PPQN timing, or PC Doom MUS. The reader handles tempo changes, program changes, sustain, channel volume/expression and velocity. Pitch bend is approximated at note onset. MIDI format 2, SMPTE timing, custom SysEx instruments, modulation and General MIDI reverb/chorus are not reproduced. Files are limited to 16 MiB and one hour of score time.

This version uses the ROM's existing E1M1 guitar/bass/percussion bank. Bass programs map to bass, drums to the closest included percussion, and other GM timbres to guitar. It does not import soundfonts. Notes are quantized to a 30 Hz timing grid and eight velocity levels. Overlapping notes beyond the voice limit are omitted or shortened, with counts reported. Long sustained notes retrigger when their duration exceeds the command limit. These are audible concessions; test the result in game.

The native sequence window is **9383 bytes**. Compact relative/absolute note commands reduce size; identical assigned scores share one ROM payload/module. Scores exceeding the SPC window must use fewer voices, a shorter duration or simpler passages. The full PC E1M1 MUS score was checked as a larger example. Map-slot aliases also carry their music assignment into the episode-1 start.

## WAV sound replacement

1. In **Sounds**, click **Load native sample slots** and choose a waveform. Names, resident/streamed status and shared effect aliases are shown.
2. Choose silence trimming, optional peak normalization and one-shot/loop playback. **Fit duration** is off by default; enabling it explicitly allows truncating the tail to the SPC limit.
3. Click **Import WAV** and review the sample report/meter. **Preview original** and **Preview replacement** play decoded samples; gameplay retains the original effect envelope and pitch variations. **Restore original** removes the staged replacement.
4. Analyze/Build to include it in both the hardware-format `.sfc` and native `.dse` patch.

WAV input accepts PCM 8/16/24/32-bit or 32-bit float, 1..8 channels, 4..192 kHz, up to 64 MiB / two minutes. Audio is mixed to mono, low-pass resampled to 11025 Hz, optionally normalized and encoded with adaptive native BRR filters. One-shot samples receive a short end fade. Looping samples loop from the beginning; edit the source to make the seam smooth.

All **36 native waveform slots** can be viewed, including the original silent/unused resident placeholders. Streamed effects have a **3321-byte** SPC slot (roughly half a second at 11025 Hz). Resident slots keep their original, often smaller allocations. A replacement affects all aliases listed for that waveform, including distance variants. The report explicitly records truncation. Replacing a placeholder may affect any instruments that reference it; ordinary effects should use their named slots.

## Audio allocation and native compatibility

The bottom ROM meter includes compiled maps, extra graphics, converted sequences, shared audio tables, BRR samples and remaining allocation. Music and Sounds also have independent SPC meters. ROM space and SPC space are separate constraints: a large ROM allocation does not increase the sequence window or sound buffers. Oversized imports stop before export. The unmodified base ROM is hash checked and is never rewritten.

The complete ArchiveBKUP `MUSIC_C`, `MUSIC_I`, `MUSIC_L`, `MUSIC_T`, `MUSIC_O.C`, boot and driver artifacts match this retail ROM byte for byte. The implementation follows DoomFX `spmus2.asm` module/chunk lists, `musicdrv.a` upload/decompression, `rlsound.a` streamed sample slots and `doommus.asm` / `dcom.i` metadata. Original instruments and driver code are taken from the user's ROM. New scores, resident waveform uploads and streamed BRR data use those native paths; playback is not replaced with host MIDI/WAV audio.

The new `DSEAUD4` patch profile permits only the relevant data and verified table operands. Native validation checks table relationships, chunk lengths, SPC ranges, song selections, hashes and the ROM checksum. Graphics-only and older map/campaign patches remain supported. Native playback has been tested in DoomSNESRecomp; physical SNES hardware has not been tested here.

CLI examples (PowerShell):

```powershell
.\build\DoomSNESconv.exe --convert "Doom (USA).sfc" "levels.wad" "custom.sfc" E1M1 --merge "--music=E1M1:C:\music\score.mid" "--wav=10:C:\sounds\pistol.wav"
.\build\DoomSNESconv.exe --analyze "Doom (USA).sfc" "levels.wad" E1M1 "--music=E1M1:C:\music\score.mus" --music-voices=4 --music-seconds=60
```

Additional audio options: `--one-shot-music`, `--fit-sounds`, `--loop-sounds`, `--keep-silence`, `--no-normalize`. Waveform numbers are shown in the Sounds tab. CLI status/report files remain beside DoomSNESconv.exe.

Validation: editor self-tests include music parsing, BRR quality, retail waveform decoding, explicit fit/rejection, voice limits, project round-trip and first-map music aliasing. `tests/validate_doomsnesconv_audio.ps1` verifies custom sequence upload, active DSP voices, resident WAV upload and pistol BRR streaming in the native runtime. `SNESRECOMP_DUMP_AUDIO=1` adds ARAM/DSP snapshots to scripted state dumps for this check.
