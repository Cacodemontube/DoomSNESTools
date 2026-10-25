# DoomSNES tools 0.17 - Windows x64

Graphics imports now use minimum-size native column packets, omit redundant transparent sprite tops, share matching columns and reuse existing ROM columns. A full repack is tried when reuse would fragment the available space. Audio is relocated only when the graphics actually need that space; relocation is counted as music allocation.

Screens use native compression when smaller than raw data, and share horizontally/vertically flipped tiles. Pointer-loaded HUD and font graphics also use native compression when beneficial. Unchanged transfers are preserved. These optimizations preserve decoded artwork and use existing game formats.

The allocation meter now labels the blue section **graphics extra**: new graphics allocations minus reclaimed original column bytes, floored at zero. Its tooltip separates allocations, reclaimed columns and reused data. This represents additional use of the editable allocation pool, not the ROM's total graphics footprint. Bank boundaries and fragmentation can still prevent a large import from fitting.

Measured user examples, preserving all pixels and keeping 2 MiB ROMs:

| Replacement | Before | Packed | Saved |
| --- | ---: | ---: | ---: |
| KICK sprites | 5,884 B | 5,548 B | 336 B |
| Icon wall texture 52 | 27,967 B | 27,912 B | 55 B |

The original texture 52 occupies 11,463 B and has 115 unique columns. The replacement has 256 unique columns and more detail. Native lossless compression cannot promise the same size for different artwork; the meter must show this real cost. Further substantial reduction would require simplifying the artwork.

Rebuild existing imports to use the new packing. The separately saved packed example projects use their corresponding original modified ROMs as the base: KICK.sfc or iconofshit.sfc. Original inputs are unchanged.

Validation: full converter suite, all 43 native tests, decoded pixel parity for both user ROMs, compressed screen/font round trips, native patch application, and a finite native boot with compressed graphics. Physical SNES hardware has not been tested. Features from 0.16 and earlier remain available; source-linked RL5934 remains emulator/hardware-only.
