# DoomSNES tools 0.18 - Windows x64

## Default size optimization for texture and sprite imports

The Textures and Sprites tabs now default to **Balanced size** for new PNG/BMP imports. This deliberately reduces the artwork to up to 32 native palette colours and averages horizontal pairs of pixels. Image dimensions and every sprite transparency position stay unchanged. Preview shows the actual converted pixels, with a before/after native strip estimate.

Select another size / quality preset to immediately update a staged replacement:

- **Preserve quality (lossless):** restores the original imported pixels, mapped to the ROM palette. Native lossless compression still applies.
- **Balanced size:** up to 32 colours, 2 x 1 pixel detail blocks. Default for new texture/sprite imports.
- **Smaller:** up to 16 colours, 2 x 2 blocks.
- **Smallest:** up to 8 colours, 4 x 2 blocks; visibly coarser artwork.
- **Colours only:** up to 32 colours with full pixel detail.

Select existing ROM artwork and click **Apply to selected artwork** to optimize it without a source PNG. This stages a replacement; the input ROM is protected. To compare or restore full quality, choose Preserve quality. Restore original removes the replacement and returns to the base ROM. Original imported pixels are retained in saved projects/setups, so changing presets does not progressively degrade the image. Existing projects keep their already selected artwork until you explicitly apply a preset.

The optimizer uses deterministic weighted colour clustering, preserves transparent sprite pixels and does not add dithering. If a preset would fail to reduce independent strip size, the original image is kept. Reduced colour count alone does not change the native 8-bit pixel format: savings come from longer runs and more shared strips. Horizontal detail reduction is particularly effective because adjacent texture columns can then share data.

PNG compression does not travel into the ROM. PNG and BMP files with the same decoded colours produce the same native texture indices. Noisy detail, antialiasing and dithering can make a decoded image expensive even when its PNG is small. Palette mapping, detail reduction and native packing determine ROM usage.

Screens/HUD continue to use automatic native lossless compression where their format supports it, with flipped-tile sharing for screens. They are not passed through these wall/sprite presets: fonts and per-tile HUD palettes have different requirements. Skies retain their fixed raw native storage, so reducing sky colours does not free ROM bytes.

## Reference tools and ROM

The local DoomFX and ArchiveBKUP sources were compared. `doom-fx-reference/source/mkwall2.asm`, BuildWallImageStrip / BuildImageStrip / BuildWallStrip2, describes bottom-up signed run packets and sprite skip/repeat packets. FindWallImageStrip searches existing bank bytes for complete contained strips and partially overlapping endings. `Archivebkup/HD/source/mkwall2.asm` contains the corresponding strip database search; `mkwallvar.asm` documents new, reused and partially reused strips. RIPDOOM's `ripdoom5.asm` applies DoomReMapTable before writing indexed IFF art. The tool scripts use BaseRGBReMap. None of these make PNG bytes directly usable as texture data.

This release additionally shares complete encoded strips contained inside newly packed longer strips, following MKWALL's byte-stream search principle. It retains the 0.17 minimum packet encoder and native column reuse. It does not claim to reproduce the old tool's complete bank-global overlap algorithm or to use a new runtime texture format.

The supplied Duke3d.sfc was decoded as a source-linked ROM. Many replacement textures are smaller than retail, while others are larger. Texture 52 uses 12,843 stored bytes in Duke3d, compared with 11,463 in retail and 27,967 in the supplied Icon ROM. Different artwork has different native costs. Duke3d's source-linked engine remains emulator/hardware-only; optimizing its art does not make that engine compatible with the native recompiler.

## Measured Icon texture example

For the supplied Icon texture 52, estimates after optimal column encoding and identical-column sharing are:

| Setting | Native strip bytes | Saving versus full quality |
| --- | ---: | ---: |
| Preserve quality | 27,912 | 0% |
| Balanced | 13,178 | 52.8% |
| Smaller | 9,066 | 67.5% |
| Smallest | 3,296 | 88.2% |

Additional contained-strip / cross-asset sharing and native bank placement affect final allocation. Analyze or Build updates the actual allocation meter. Strong presets visibly reduce image quality; the preview and Preserve quality option are provided for that choice. Original image data retained in a project is not copied into the game ROM.

Validation includes native substring sharing, reversible presets, transparent masks, PNG/BMP equivalence, exact changed/unchanged texture/sprite decoding after packing, 2 MiB ROM output, project rebuilds and converter UI checks. Prior release notes are retained. Physical SNES hardware has not been tested.
