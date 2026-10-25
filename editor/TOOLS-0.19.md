# DoomSNES tools 0.19 - Windows x64

In DoomSNESed sector mode, the 2D map shows internal sector numbers as #0, #1, etc. The selected sector's zero-based number also appears in the viewport header and as a read-only Internal sector number property, including the right-click sector editor. It is the native sector-table index, not the sector tag. Structural editing can renumber sectors. Labels remain inside visible sector regions where possible; very small or overlapping labels are suppressed until you zoom in. The selected ID remains visible in the header and properties.

Both tools now expose the complete native wall texture table instead of collecting only texture IDs referenced by initial map faces. Switch-on frames used only by the engine's animation were previously omitted. There are 63 entries: two skies and 61 wall textures, including all ten switch-off/on pairs. DoomSNESconv lists the two skies in Skies and the other 61 entries in Textures.

The six switch-on frames missing from the supplied textures.png list are:

| Native wall ID | Texture name |
| --- | --- |
| 96 | SW2BRN2 |
| 98 | SW2BRNGN |
| 100 | SW2EXIT |
| 106 | SW2GSTON |
| 112 | SW2SLAD |
| 114 | SW2WOOD |

Texture thumbnails, the converter list and editor property dropdowns include DoomFX names and switch off/on labels. Native IDs are byte offsets into the wall table (twice the texture index); exported PNG filenames are not native texture IDs. The ordering/names were verified against the local DoomFX WallList and TextureList. Existing ROM pixels and switch animation code are unchanged. The same complete catalog permits PWAD imports and replacement graphics to target these frames.

Version 0.18's reversible size/quality settings are retained. Existing projects and ROMs are preserved. Validation covers complete retail/source texture decoding, every switch preview, picker entries, named-value round trips, PWAD switch mapping, read-only sector IDs, sector-label rendering, hidden-switch replacement/placement and native map reimport.
