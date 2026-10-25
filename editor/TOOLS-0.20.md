# DoomSNES tools 0.20 - Windows x64

PC monster-only blocking is now dropped during conversion. It no longer becomes native impassable-for-everyone. An explicit PC impassable flag is preserved, including when both flags were present. One-sided walls remain solid. The flag is dropped before closest-action compatibility is checked, so a now-traversable two-sided line can retain a supported walk trigger. The report lists the change.

Masked middle textures on two-sided portals (fences/grilles) are omitted by default, even when Allow reported substitutions for unsupported content is off. The front and back masked images are both omitted. Existing upper/lower surfaces, sector links, geometry, actions and explicit blocking remain. A visually removed fence can therefore still be impassable if its PC line was explicitly flagged impassable.

These defaults apply to Analyze, Build and Auto configure. The general substitutions option is still required for other unsupported content, such as PC objects absent from the SNES game; it is not silently enabled by these changes. Reconvert the source PWAD to apply the new flag behavior to an existing conversion.

## zoo.wad diagnosis

The supplied zoo.wad now passes the old fence-texture failure. It contains unsupported PC objects (including thing 41), so general substitutions must be enabled to omit those objects. With the existing sector-merging options, it has 233 sectors, exceeding the engine limit of 205. Testing all existing lighting quantization levels (0, 8, 16, 32 and 64) did not lower that count. Monster trimming reduces objects, not sectors. The map needs further sector simplification in its source PWAD before it can be exported safely. No working zoo ROM is claimed or included.

Capacity failures now report the actual sector, vertex and linedef counts alongside their limits. Expanding the SFC file does not remove these engine limits.

Version 0.19's full texture catalog and internal sector numbers, and 0.18's size/quality presets, are retained. Validation covers all combinations of monster-only/explicit blocking, strict-mode front/back fence omission, secret/automap flags, portal connectivity, walk-trigger mapping, native build/reimport and the supplied zoo map's remaining sector limit. Original inputs and previous releases are unchanged.
