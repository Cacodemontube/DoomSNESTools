Current build: **DoomSNESed 0.6 + DoomSNESconv 0.6**. See [TOOLS-0.6.md](TOOLS-0.6.md) for new controls, converter features, output paths and current limitations.

# DoomSNESed 0.2 — Reality Engine test

DoomSNESed is an experimental Windows editor for the original SNES Doom Reality Engine. It uses adapted two-dimensional geometry routines from the local UltimateDoomBuilder-latest source and a dedicated native ROM compiler. It is not a complete port of Ultimate Doom Builder. The application UI has no upstream branding; copyright and GPL attribution remain with the source.

## Start editing

1. Extract the entire package and open DoomSNESRecomp.exe.
2. Add your unmodified Doom (USA).sfc, then click **Edit levels - DoomSNESed**. A 512-byte copier header is accepted. No ROM is included.
3. Select an existing level, or choose **New map**. Start empty, start with a room, or use the working door/lift examples.
4. Press **D** to draw. Click each outline corner, then click the first corner or press Enter to close it. Adjacent outlines sharing an edge automatically become connected sectors. Draw a two-point line between boundary points and press Enter to split a sector. Esc cancels; Backspace removes the last point. Nested outlines and multi-segment cutting paths are not supported in this test; use adjacent sectors and individual cuts.
5. Use **1 Things**, **2 Vertices**, **3 Linedefs**, **4 Sectors**, or **5 Surfaces**. Select on the canvas or in the right-hand list, then change properties. Drag things/vertices; wheel zooms; middle/right drag pans. Grid sizes range from 4 to 128 units.
6. Press **F5** to check the native build and see the selected slot's byte budget. Press **F6** to apply it, then restart the game. The source ROM remains unchanged.

Apply saves editor/active.sned plus editor/active.dse. The editable project is restored when the editor reopens. File > Save project keeps another .sned copy. Original levels disables the active patch. File > Export patched ROM copy writes a separate .sfc with a corrected SNES checksum for external testing. Applied patches verify the original and result SHA-256 hashes before loading into memory.

## Native editing features

- All 22 original map slots load directly from ROM, with no PC IWAD required.
- Custom polygon sectors, including concave outlines; automatic shared portals; straight linedef sector splits; grid snapping; movable vertices; undo/redo.
- The compiler rebuilds native BSP nodes, convex areas, split segments, collision blockmap, sector/line/face/object data, reject data, and supported platform tables.
- Sector floor/ceiling heights, solid palette colours, ceiling sky, lighting, native damage/secret/light effects, and tags.
- Door, lift, moving-floor, stair and crusher presets. Presets create a starting action/tag arrangement; inspect the linked line action and sector properties to customize it. Door presets need adjoining sectors. A floor raised by the moving-floor preset needs a higher neighbour.
- Native manual/keyed/tagged doors; lifts; floor raises/lowers and changes; eight-unit stairs; supported crushers; teleport, light, normal exit and secret exit line actions. The action dropdown distinguishes Use/Open controls from walk-over triggers and once/repeat behavior. Tagged actions need a matching nonzero sector tag. Teleports also need a native destination thing in the target sector.
- SNES monsters, pickups, scenery and per-skill flags. Double-click in Things mode to place an object, then choose its type. The player start cannot be deleted. Only player/teleport starts have a stored facing angle in this format.
- ROM texture thumbnails and wall previews, front/back surfaces, offsets and a palette browser. SNES Doom floors and ceilings use solid colours rather than PC flat textures.

Surface, object, lighting and sector-effect edits preserve original geometry until you edit geometry, heights, line actions or tags. Such edits convert that map to the native rebuilding path. Original maps may exceed the compiler's output budget after conversion; F5 reports the reason. Restore selected map from ROM or Undo recovers the earlier version.

## 3D mode

Press **F3** to toggle the textured 3D preview. WASD moves, right-drag/arrows look, Q/E flies and Shift moves faster. Left-click selects the aimed surface. Ctrl + wheel changes its sector's floor height; Shift + wheel changes the ceiling. View > Preview doors open lets you inspect connected rooms behind doors; View > Reset 3D camera returns to the player start.

This is an editing preview, with ROM wall textures, solid floor/ceiling colours, approximate lighting and a sky placeholder. It does not run the game, render actor sprites, simulate platform animations, or enforce player collision. Test gameplay with Apply and the launcher.

## Reality Engine limits

New maps replace existing slots; they do not add episode slots. Output must fit that slot's original geometry and data allocations. Limits include 205 sectors, 1,118 linedefs, 1,056 compiled vertices, 2,438 segments, 512 areas, 63 collision lines per block and the native object pools. New/rebuilt maps reserve movable object capacity for runtime activity. Complex outlines can add BSP vertices and segments.

Custom graphics, PC WAD import/export, scripting, slopes, stacked/3D floors, unsupported PC actions and full Ultimate Doom Builder feature parity are not included. This test intentionally leaves the Reality Engine unchanged. Existing original maps and platform data remain preserved until conversion. Keep all package folders together.

## Icon, source and validation

The icon is embedded in DoomSNESed.exe as a native Windows icon resource. There is no loose .ico dependency. The build script uses icon2.png if present, otherwise the existing Logo2.png supplied in the workspace. This build uses Logo2.png because icon2.png was not present. The green gradient UI follows the launcher's palette.

Build with:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File editor/build_editor.ps1
```

The editor requires Windows .NET Framework 4.7.2 or later, with no NuGet packages. The complete corresponding editor source, build script, notices and GPL license accompany the package. The launcher integration uses DOOM_SNESED_TEST=ON.

Self-tests cover all original maps' byte-for-byte round trip, native room compilation/reimport across all 22 slots, project validation, ROM texture decoding, concave sectors, portal welding, sector splits, native platform output, 3D surface picking and embedded icon presence. tests/validate_doomsnesed_geometry.ps1 runs generated maps in the actual game to check door/lift behavior, collision, exit transitions and concave BSP traversal. tests/validate_doomsnesed.ps1 retains the original room regression. Runtime evidence is generated locally and is not shipped with ROM content.

Format and action references are Randal Linden's DoomFX rle.i, rllevels.a, exmy.i, rlobjects.i, rlobjects.a, rlmove.a, rlmove7.a, rlsectors.a, rluse2.a and ripdoom3.asm; RL.SEC defines the verified USA ROM allocation boundaries. See DoomSNESed/NOTICE.txt for attribution.
