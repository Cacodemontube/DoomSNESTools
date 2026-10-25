param([string]$OutputDirectory="$PSScriptRoot/../build",[string]$NativeBuildDirectory="$PSScriptRoot/../build-native")
$ErrorActionPreference='Stop'
$toolsRoot=[IO.Path]::GetFullPath((Split-Path $PSScriptRoot))
$OutputDirectory=[IO.Path]::GetFullPath($OutputDirectory)
$NativeBuildDirectory=[IO.Path]::GetFullPath($NativeBuildDirectory)
# A standard MinGW installation is optional; otherwise use the compiler on PATH.
if(!(Get-Command gcc -ErrorAction SilentlyContinue) -and (Test-Path 'C:/msys64/mingw64/bin/gcc.exe')){$env:PATH='C:/msys64/mingw64/bin;'+$env:PATH}
if(!(Get-Command cmake -ErrorAction SilentlyContinue)){throw 'Install CMake and a Windows x64 C compiler to build the audio DLL.'}
$arguments=@('-S',$toolsRoot,'-B',$NativeBuildDirectory,'-DCMAKE_BUILD_TYPE=Release',"-DCMAKE_RUNTIME_OUTPUT_DIRECTORY=$OutputDirectory","-DCMAKE_RUNTIME_OUTPUT_DIRECTORY_RELEASE=$OutputDirectory")
if(!(Test-Path -LiteralPath "$NativeBuildDirectory/CMakeCache.txt")){
    if(Get-Command ninja -ErrorAction SilentlyContinue){$arguments+=@('-G','Ninja')}
    if(Get-Command gcc -ErrorAction SilentlyContinue){$arguments+="-DCMAKE_C_COMPILER=$((Get-Command gcc).Source)"}
}
& cmake @arguments
if($LASTEXITCODE){throw 'Standalone native audio configuration failed.'}
& cmake --build $NativeBuildDirectory --config Release --target DoomSNESAudio --parallel 4
if($LASTEXITCODE){throw 'Standalone native audio build failed.'}
$dll=Join-Path $OutputDirectory 'DoomSNESAudio.dll'
$bytes=[IO.File]::ReadAllBytes($dll)
if([BitConverter]::ToUInt16($bytes,[BitConverter]::ToInt32($bytes,0x3c)+4) -ne 0x8664){throw 'Build the audio DLL with a Windows x64 compiler.'}
Write-Output "Built independent Windows x64 audio DLL: $dll"
