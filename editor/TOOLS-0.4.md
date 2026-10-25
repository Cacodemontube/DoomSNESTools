# DoomSNESconv 0.4 and DoomSNESed campaign support

The executables are in `C:\projects\DoomSNESRecomp\build`. Rebuild both tools with `editor/build_editor.ps1`. DoomSNESconv embeds Logo3.png as its Windows icon and uses that embedded icon in the green window banner. It needs no loose PNG or ICO at runtime. The input label is **PWAD**; compatible Doom 1 IWADs remain accepted.

## Convert and play natively

1. Open DoomSNESconv.exe, or **Convert WAD levels - DoomSNESconv** in the game launcher.
2. Select the original Doom USA SNES ROM and your PWAD. Select levels in the left list. For WADs containing more than 22 maps, First only is selected initially.
3. Choose optimization options and click **Analyze selection** to check geometry, byte budgets and per-skill object pools without exporting a ROM/project/patch.
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

Open **Textures / skies / sprites**, choose the base ROM, then **Load ROM graphics**. The selector offers walls/skies, native sprite frames, and title/Williams artwork. Every list entry shows its required pixel dimensions. Select an entry to preview it; **Export PNG** creates an editing template. **Import PNG / BMP** stages its replacement; **Restore original** removes that replacement.

- Wall textures and both sky panoramas retain their original dimensions and use the original 256-colour gameplay palette. Imported pixels are matched to that palette. Sky data uses the engine's raw 128 × 128 bottom-up column format, independently of wall RLE.
- All 232 native sprite frames are available. Verified monster frame names are shown where known; other frames retain their native number. PNG alpha below 128 becomes transparent. Each animation frame is replaced individually; dimensions, animation timing and object collision remain unchanged. Replaced frames take precedence over the recomp's optional PC rotation artwork.
- Williams and title images require 256 × 224 pixels and are quantized to 16 colours. Export an original screen to use as a size/layout reference. Import builds SNES 4bpp tiles, a tile map and BGR555 palette. Existing overlays and other palette banks are preserved. Highly detailed artwork may need simplification to fit available contiguous allocations.

Click **Analyze selection** to validate graphics alongside the selected levels, then **Build campaign**. Replacements are included in the `.sfc`, native `.dse` patch and editable `.sned` project. The map editor's wall previews also show imported wall artwork when that project is opened.

The bottom meter reports the **last analysis/build**: green is compiled maps, blue is additional graphics, and grey is unused space in those selected map allocations. A failed conversion shows red and gives details in the report. Graphics borrow only unused tails of converted map allocations; unselected retail maps are preserved. Fixed sky replacements use their existing allocations and consume no additional space. Total ROM size stays 2 MiB. This meter does **not** yet account for imported audio: MID/MUS music conversion, WAV sample replacement and their allocation categories are not implemented in this build. The reference tree uses external Sculptured Software music assets/tools; no host-only audio is exported as supposedly hardware-compatible data.

| Option | Effect |
| --- | --- |
| Merge matching static sectors | Joins adjacent sectors with identical heights, surfaces and lighting. Preserves tagged/action-linked sectors and blocking boundaries. |
| Weld identical vertices | Unifies exact coordinate matches. Unused vertices/faces are cleaned up independently. |
| Collapse collinear walls | Joins degree-two, solid wall segments only when flags, sectors, textures and texture phase agree. Preserves junctions and action/tag lines. |
| Disconnected-area visibility | Builds the native REJECT mask to skip enemy sight tests between disconnected portal components. Closed doors remain connected; this does not hide potentially visible rooms behind doors. |
| Compact tags | Renumbers matching line/sector groups into 1..255, including PC tags above 255. Zero stays zero. More than 255 distinct groups still require source-map changes. |
| Smallest fitting allocation | Conserves large native slots. E1M1 is preferred for the first map when it fits. Larger first maps can be stored elsewhere and aliased into the game start. |
| Static lighting steps | Optional 8/16/32/64-unit quantization of normal untagged sectors. Changes appearance and may enable more sector merging. |
| Monsters per skill | Optional 16/32/48/64/80 cap. Removes distant ordinary monsters per skill; preserves Barons, Cyberdemons, Spiderdemons, player/teleport starts, keys and pickups. Changes combat. |
| Remove scenery | Removes decorative types 50..63, including their collision. Keeps barrels, keys, pickups and starts. Changes scenery/navigation. |

Lighting, monster reduction and scenery removal are off by default. Reports show before/after counts, compiled vertices/segments/subsectors, geometry/data bytes versus capacity, object pools per skill, and substitutions. Optimizations do not remove rooms or change wall coordinates to force a map into ROM. Complex geometry, action/platform limits and engine RAM limits still apply.

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

There is no blind replacement for unknown effects or remote key behavior with no suitable native operation. Conversion stops by default. **Allow reported substitutions for unsupported content** explicitly permits remaining unsupported actions/objects to be omitted and unknown textures to use a fallback. Inspect those reports and playtest progression. Custom scripts, UDMF/Hexen maps, arbitrary generalized actions, PC flats and custom graphics are not implemented as equivalent native features.

## Continue editing in DoomSNESed

Open the generated `.sned` with the original base ROM. It retains campaign order, the first-map storage alias and the final ending. The editor selects the first converted map's storage slot. Rebuilding or editing a sector keeps the routing; Apply/export automatically writes the campaign patch format. Apply then launch the game normally to play the edited active campaign, or export a named `.dse` and open it in the converter. Older 0.3 converter projects lacked this metadata; reconvert them to retain campaign behavior.

## CLI and validation

```powershell
Start-Process -Wait build/DoomSNESconv.exe -ArgumentList '--convert "Doom (USA).sfc" "levels.wad" "custom.sfc" --first --merge --allow-substitutions'
Start-Process -Wait build/DoomSNESconv.exe -ArgumentList '--analyze "Doom (USA).sfc" "levels.wad" --first --merge --lighting=16 --monsters=48 --remove-scenery --allow-substitutions'
```

Specific map markers can replace `--first`. Other switches are `--strict-actions`, `--no-weld`, `--no-collapse`, `--no-compact-tags`, `--no-best-fit`, and `--no-reject`. CLI `--merge` enables sector merging. The last CLI report is saved as DoomSNESconv-last-run.txt beside the tool; failures write DoomSNESconv-error.log.

Self-tests cover action fallback/rejection, timed-door conversion, safe wall collapse, tag compaction, visibility bits, object/lighting options, campaign project round-trip, embedded icons and protected source files. `tests/validate_doomsnesconv.ps1` runs converted campaigns in DoomSNESRecomp, including a use exit, converted secret walk exit, two-level progression, actual episode 3 ending, and a larger-slot alias when its sample export is present. Arbitrary PWADs still require gameplay testing.
