# DoomSNESTools

Independent Windows tools for editing Doom SNES ROMs and converting Doom 1 PWAD content:

- **DoomSNESed**: map editor, sector/line properties and texture browser.
- **DoomSNESconv**: PWAD conversion plus graphics, text, MIDI/MUS and WAV replacement.
- **DoomSNESAudio.dll**: the local SPC700/APU/DSP preview engine.

DoomSNESRecomp is **not required** to build or use the tools, including music/sound conversion and preview. The user supplies a compatible Doom SNES ROM for the game's assets and sound driver. ROMs, soundbanks and game media are not included in this source project.

## Build on Windows x64

Requirements: .NET Framework 4.x, CMake, Ninja and a MinGW-w64 x64 C compiler. Tools are found on PATH; a standard `C:\msys64\mingw64\bin` installation is also detected.

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

This compiles the native audio DLL from `native/src` and both C# executables into `build`. No sibling repository, Git checkout, submodule or download is needed. `editor/build_editor.ps1` also builds the DLL. To build just the DLL:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build_audio.ps1
```

Native intermediates go into `build-native`. Keep the executables, audio DLL, editor-data and licenses together. The native DLL is Windows x64 even if the optional C# `anycpu` target is selected. MinGW-w64 is the tested native compiler.

## Source layout

- `editor/DoomSNESed`: shared C# source and vendored geometry code.
- `native/src/doom_audio_preview.c`: the audio bridge exported to C#.
- `native/src/snes`: vendored SPC700/APU/DSP code and required headers.
- `native/UPSTREAM.json`: source provenance and hashes of the native snapshot.
- `licenses`: component licenses and original attribution.
- `tests`: offline audio comparison helper; requires a user-supplied ROM.
- `scripts`: independent native build and ZIP packaging.

The native subset was copied from the existing local DoomSNESRecomp/SNESRecomp working tree. It is maintained here independently. Original files remain in the game project so that its build is unaffected. Neither project needs the other's source to compile.

## Test and package

```powershell
.\build\DoomSNESed.exe --self-test "C:\path\Doom.sfc" .\build-evidence\self-test
powershell -ExecutionPolicy Bypass -File .\scripts\package_tools_only.ps1
```

The ZIP contains the programs, native DLL, data, notices and corresponding standalone source. Packaging reads only this project. Generated files, ROMs and user projects are ignored by Git and excluded from packaging. Do not commit proprietary game assets. Some optional regression fixtures are private user ROMs/WADs and are not distributed.

## Optional game integration

Play and Apply integration can use a separate DoomSNESRecomp installation beside the tools or in a sibling `DoomSNESRecomp/build` directory. Without it, normal editing, MIDI/MUS conversion, WAV replacement, audio preview and SFC export work independently. The updated game's launcher can find a sibling `DoomSNESTools/build` installation.

## Licenses and attribution

Existing component licenses are retained; this extraction does not relicense upstream code. Read `LICENSE`, `editor/DoomSNESed/NOTICE.txt` and `licenses/`. These include GPL notices for DoomFX/geometry-derived material and SNESRecomp's PolyForm Noncommercial license plus its retained third-party attribution. Keep these notices with source and binary releases. Technical independence does not change those terms.
