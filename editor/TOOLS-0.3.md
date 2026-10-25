# DoomSNESed 0.3 and DoomSNESconv

Both tools build into `C:\projects\DoomSNESRecomp\build`, alongside DoomSNESRecomp.exe. Run `editor/build_editor.ps1` to rebuild them. No new package is staged in dist.

## Editor controls

- Arrow keys pan the 2D grid; Shift pans faster. Middle drag and wheel zoom remain available.
- Ctrl+click linedefs or sectors to toggle multiple selections. Shift/Ctrl selection also works in the item list. Property changes and the surface browser apply to every selected item; differing values show as mixed.
- Right-click in Linedefs or Sectors mode to open a property window. Select a texture/colour field and click **Browse selected texture / colour** for thumbnails. Apply commits the window as one undo step; Cancel restores the previous map.
- Linedef properties include actions, tags, collision/automap flags, front/back textures and offsets. Double-sided state follows **Back sector**: use another sector index, or -1 for a solid wall. This changes topology and must pass the native map check.
- Sector properties include effects, tags, heights, brightness, native darkness, and palette colours. Floors offer 1..254, ceilings 1..255; 255 is ceiling sky. Existing colour 0 still round-trips unchanged.
- Thumbnails and 3D textures now display the ROM's bottom-up columns in the correct orientation.
- Undo stores one map instead of all 22. Held-key repeats no longer add undo/redo actions. Drawing uses a surface lookup, snapping scans vertices once, and decoded textures are cached.

## DoomSNESconv

Open **DoomSNESconv.exe**. It uses the editor's green theme and a default Windows executable icon. Select the unmodified Doom USA ROM and a classic Doom 1 IWAD/PWAD. Uncheck unwanted levels, or use **First only**. Conversion runs in the background.

The converter rebuilds native BSP, segments, collision, platform and object data from classic map lumps. DoomFX TextureList maps PC wall names and aliases to existing ROM wall IDs. FLOORLIST maps flats to solid palette colours; ceiling sky is 255. PC graphics are not copied into the ROM.

Unsupported actions, objects, custom texture names and masked portal textures stop conversion unless **Allow reported substitutions / omissions** is checked. PC unpegging, monster-only blocking and different front/back offsets have native-format limitations, reported with the other substitutions.

**Merge adjacent identical static sectors** joins matching heights, colours, brightness and normal behavior. It preserves action-linked/tagged sectors and blocking boundaries, removes redundant shared walls, and removes unused vertices/faces. It does not delete gameplay areas to force a fit.

Maps must fit the engine's RAM limits and an available native ROM allocation. The converter checks budgets and tries larger available slots. Play starts at E1M1; when necessary, its map tables point to the first converted map's storage slot. Selected maps run in WAD order. Secret exits follow the same campaign. The last exit routes to native level 27 for the episode 3 ending; original early difficulty endings are disabled.

Export creates a separate `.sfc`, `.sned` project and `.conversion.txt` report. The source ROM and WAD cannot be overwritten. Run the exported ROM in a SNES emulator: DoomSNESRecomp expects the original ROM identity. The editor project contains converted geometry in its allocated slots; editor Apply/export does not recreate the converter's campaign routing or start alias. The converter's ROM is the playable campaign artifact.

CLI example:

```powershell
Start-Process -Wait build/DoomSNESconv.exe -ArgumentList '--convert "Doom (USA).sfc" "levels.wad" "custom.sfc" --first --merge --allow-substitutions'
```

Omit `--first` for all maps, or specify markers such as `E1M1 E2M3`. There are at most 22 storage slots; start aliasing can reduce available storage by one. Hexen/UDMF, Doom 2 map markers, custom graphics, and arbitrary PC engine features are unsupported.

Validation covers editor interaction/undo, texture orientation, palette range, original-map round trips, native gameplay regressions, WAD filtering, conservative merging, native reimport, checksums and overwrite protection. Full playthroughs of arbitrary converted WADs and the exported ending sequence still need external emulator testing.

See README.md for the original editor controls and native engine limits. Its older statement that no WAD converter is included is superseded by this document.
