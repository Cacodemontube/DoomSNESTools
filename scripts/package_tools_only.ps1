param([string]$Name=('DoomSNESTools-v0_01-'+(Get-Date -Format yyyyMMdd-HHmmss)+'-Windows-x64'),[string]$BuildDirectory='')
$ErrorActionPreference='Stop'
$toolsRoot=Split-Path $PSScriptRoot
$root=$toolsRoot
$build=if($BuildDirectory){[IO.Path]::GetFullPath($BuildDirectory)}else{Join-Path $toolsRoot 'build'}
if($Name -notmatch '^[a-zA-Z0-9_-]+$'){throw 'Invalid package name'}
$stage=Join-Path $toolsRoot "dist/$Name"
if(Test-Path -LiteralPath $stage){throw "Package already exists: $stage"}
& powershell -NoProfile -ExecutionPolicy Bypass -File "$toolsRoot/editor/build_editor.ps1" -OutputDirectory $build -Platform x64
if($LASTEXITCODE){throw 'Tool build failed'}
New-Item -ItemType Directory -Force $stage,"$stage/licenses","$stage/source" | Out-Null
foreach($file in @('DoomSNESed.exe','DoomSNESconv.exe','DoomSNESAudio.dll')){
    $path=Join-Path $build $file
    $bytes=[IO.File]::ReadAllBytes($path)
    if([BitConverter]::ToUInt16($bytes,[BitConverter]::ToInt32($bytes,0x3c)+4) -ne 0x8664){throw "$file is not x64"}
    Copy-Item -LiteralPath $path -Destination $stage
}
Copy-Item -LiteralPath "$build/editor-data","$build/licenses" -Destination $stage -Recurse -Force
Copy-Item -LiteralPath "$build/DOOMSNES-TOOLS.md","$root/LICENSE","$toolsRoot/editor/DoomSNESed/NOTICE.txt" -Destination $stage
foreach($file in Get-ChildItem $build -Filter 'DOOMSNES-TOOLS-*.md' -File){Copy-Item -LiteralPath $file.FullName -Destination $stage}
# Include only this project's complete source. No Git checkout or sibling
# repository is consulted; generated files and private game data stay out.
foreach($folder in @('editor','native','scripts','tests','licenses')){
    foreach($file in Get-ChildItem -LiteralPath (Join-Path $toolsRoot $folder) -Recurse -File){
        $relative=$file.FullName.Substring($toolsRoot.Length+1)
        if($relative -match '^editor[\\/]build[\\/]' -or $file.Extension -in @('.exe','.dll','.pdb','.log','.sfc','.smc','.wad','.spc','.sned','.dse','.snconv','.wav','.mid','.mus')){continue}
        $to=Join-Path "$stage/source" $relative
        New-Item -ItemType Directory -Force (Split-Path $to) | Out-Null
        Copy-Item -LiteralPath $file.FullName -Destination $to
    }
}
foreach($name in @('CMakeLists.txt','build.ps1','README.md','README.txt','LICENSE','.gitignore','Logo2.png','Logo3.png')){Copy-Item -LiteralPath (Join-Path $toolsRoot $name) -Destination "$stage/source"}
Copy-Item -LiteralPath "$toolsRoot/README.md" -Destination $stage
@'
DoomSNESTools V0.01 - Windows x64

Extract the whole ZIP. Run DoomSNESed.exe or DoomSNESconv.exe.
Keep DoomSNESAudio.dll, editor-data and licenses beside the executables.
Windows x64 and .NET Framework 4.x are required.
Supply your own compatible Doom SNES ROM. No ROM or PWAD is included.

Latest maintenance build (file version 0.1.0.2):
- Unchecked campaign maps are removed and their space reused for assets.
- Original intermission/title music restores its shared SPC tables.
- Preview cleanup between imports and a visible Sky (255) picker.
Read DOOMSNES-TOOLS-CAMPAIGN-STORAGE-FIX.md and
DOOMSNES-TOOLS-MUSIC-TRANSITION-FIX.md for these updates.

Earlier development changes: 0.20 drops monster-only blocking and masked portal textures by default.
zoo.wad still needs sector simplification: 233 sectors exceed the 205 limit.
0.19 internal sector numbers and the complete native
texture catalog in both tools, including all ten switch off/on pairs.
Reversible size/quality presets from 0.18 are retained.
Read DOOMSNES-TOOLS.md for details and earlier notes for other features.

This ZIP includes the editor and converter. DoomSNESRecomp is distributed
separately; native Play integration requires that program to be installed.
Exported SFC files can be used with a compatible emulator.
Source-linked engine variants may require an emulator or hardware.

source/ contains the complete standalone source, including native audio.
Music/sound conversion and preview do not require DoomSNESRecomp.
No sibling source checkout or Git submodules are needed.
Build the editor/converter with:
powershell -ExecutionPolicy Bypass -File source/build.ps1 -Platform x64
See source/CMakeLists.txt and source/native/src/doom_audio_preview.c for the audio DLL.
Physical SNES hardware has not been tested.
'@ | Set-Content -LiteralPath "$stage/START HERE.txt" -Encoding UTF8
'DoomSNES tools v0.01 maintenance 0.1.0.2 Windows x64' | Set-Content -LiteralPath "$stage/VERSION"
$revision='standalone project; native provenance in source/native/UPSTREAM.json'
"game-source=$revision`nsource=self-contained source snapshot`nplatform=Windows-x64`ntools=0.1.0.2" | Set-Content "$stage/BUILD-INFO.txt"
$forbidden=Get-ChildItem $stage -Recurse -File | Where-Object {$_.Extension -in @('.sfc','.smc','.srm','.sav','.sned','.dse','.wad','.spc','.wav','.mid','.mus') -or $_.Name -in @('rom.cfg','disc.cfg','config.ini','keybinds.ini','state.toml')}
if($forbidden){throw 'Unexpected ROM or personal state in package'}
$manifest=Get-ChildItem $stage -Recurse -File | Where-Object Name -ne 'SHA256SUMS.txt' | Sort-Object FullName | ForEach-Object {
    (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()+'  '+$_.FullName.Substring($stage.Length+1).Replace('\','/')
}
$manifest | Set-Content "$stage/SHA256SUMS.txt" -Encoding ascii
$zip="$stage.zip"
Compress-Archive -LiteralPath $stage -DestinationPath $zip -CompressionLevel Optimal
$hash=(Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()
($hash+'  '+[IO.Path]::GetFileName($zip)) | Set-Content "$stage.sha256" -Encoding ascii
Write-Output $zip
Write-Output $hash




