param([string]$OutputDirectory="$PSScriptRoot/build",[ValidateSet('anycpu','x64')][string]$Platform='x64')
$ErrorActionPreference='Stop'
& powershell -NoProfile -ExecutionPolicy Bypass -File "$PSScriptRoot/editor/build_editor.ps1" -OutputDirectory $OutputDirectory -Platform $Platform
exit $LASTEXITCODE
