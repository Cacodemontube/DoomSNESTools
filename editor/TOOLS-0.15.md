# DoomSNES tools 0.15 — Windows x64

DoomSNESconv and DoomSNESed now open the source-linked **RL5934.sfc** supplied with this project, as well as the supported retail USA ROM and compatible modifications of either layout. Import a ROM normally; a PWAD is optional. Copier-headered 2 MiB and 4 MiB files are recognized as before.

RL5934 is a different linked layout, with one E1M1 map and relocated maps, artwork, music and engine routines. The tools resolve its metadata separately and read its weapon tile sizes from its own weapon tables. An unchanged editor compile is byte-for-byte identical. Source-layout exports preserve its linked engine and allocate new data after existing referenced assets, within the original 2 MiB Super FX window. Saved projects bind to the exact selected base ROM. Keep that base to reopen a project; exported ROMs can also be imported and edited again.

Source layout support is verified for RL5934 and modifications retaining its metadata/code layout. This does not mean that arbitrary regional versions, engine rewrites or differently linked source builds are accepted. Unrecognized layouts remain rejected instead of interpreting their bytes as retail tables.

**Source-layout exports run in an emulator or on compatible hardware. RL5934's relocated engine still cannot run in DoomSNESRecomp.** No misleading native campaign sidecar is generated for it, and Play in DoomSNESRecomp is disabled for these exports. Source-layout PWAD campaign routing is not supported; its existing map can be edited in DoomSNESed and exported with File > Export patched ROM copy. Retail-layout native support is retained.

## Copyright tab

The new **Copyright** tab edits the red text on black shown at startup. Select the base ROM, open Copyright, and edit the text, row and column of its 18 native entries. Empty text hides an entry. The live preview uses the selected ROM's actual font, including a staged replacement font, and the game's red palette. It supports ASCII text plus **©, ® and ™**. Each line must fit 32 columns; rows are 0–27. Invalid changes block export until corrected or restored.

Changes are included in Build ROM, saved converter setups and editor projects. Restore original removes the staged copyright edit. Copyright changes to retail ROMs also work in the native recompiler. The preview matched its native framebuffer pixel-for-pixel in the integration check.

The source ROM's short Episode 3 ending allocation is respected, preventing long edits from overwriting the following artwork. The other music, graphics and sound features from 0.14 are retained.

Validation includes unchanged retail/source round trips; all wall/sprite decoders and HUD previews; copyright Unicode mapping, live UI staging, invalid input, restoration, project saving and exported-ROM reopening; source music/sample relocation; native copyright rendering and transactional patch rejection checks; and the existing converter/native regression suites. Physical SNES hardware has not been tested.

The previous 0.14 build is preserved in a dated rollback directory beside the preferred build. Original input ROMs remain unchanged.
