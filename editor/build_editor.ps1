param([string]$OutputDirectory = "$PSScriptRoot/../build", [ValidateSet('anycpu','x64')][string]$Platform = 'x64')
$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'DoomSNESed'
$toolsRoot = Split-Path $PSScriptRoot
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
& powershell -NoProfile -ExecutionPolicy Bypass -File "$PSScriptRoot/../scripts/build_audio.ps1" -OutputDirectory $OutputDirectory
if($LASTEXITCODE){throw 'Native audio build failed.'}
New-Item -ItemType Directory -Force $OutputDirectory,"$OutputDirectory/editor-data","$OutputDirectory/licenses" | Out-Null
Add-Type -AssemblyName System.Drawing
$logoPath = Join-Path $toolsRoot 'icon2.png'
if(!(Test-Path -LiteralPath $logoPath)){$logoPath=Join-Path $toolsRoot 'Logo2.png'}
function New-ToolIcon([string]$logoPath,[string]$iconPath) {
$logo = [Drawing.Image]::FromFile($logoPath)
$images = @()
try {
    foreach ($size in @(16,32,48,64,128,256)) {
        $bitmap = New-Object Drawing.Bitmap($size,$size,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $graphics = [Drawing.Graphics]::FromImage($bitmap)
        try {
            $graphics.Clear([Drawing.Color]::Transparent)
            $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $ratio = [Math]::Min($size / $logo.Width, $size / $logo.Height)
            $width = [int]($logo.Width * $ratio); $height = [int]($logo.Height * $ratio)
            $graphics.DrawImage($logo,[int](($size-$width)/2),[int](($size-$height)/2),$width,$height)
            $stream = New-Object IO.MemoryStream
            $writer = New-Object IO.BinaryWriter($stream)
            $writer.Write([int]40); $writer.Write([int]$size); $writer.Write([int]($size*2))
            $writer.Write([int16]1); $writer.Write([int16]32); $writer.Write([int]0)
            $writer.Write([int]($size*$size*4)); $writer.Write([int]0); $writer.Write([int]0); $writer.Write([int]0); $writer.Write([int]0)
            for($y=$size-1;$y -ge 0;$y--) { for($x=0;$x -lt $size;$x++) {
                $pixel=$bitmap.GetPixel($x,$y);$writer.Write([byte]$pixel.B);$writer.Write([byte]$pixel.G);$writer.Write([byte]$pixel.R);$writer.Write([byte]$pixel.A)
            }}
            $writer.Write((New-Object byte[] ([int]([Math]::Ceiling($size/32)*4)*$size)))
            $writer.Flush();$images += ,@{Size=$size;Data=$stream.ToArray()};$writer.Dispose();$stream.Dispose()
        } finally {$graphics.Dispose();$bitmap.Dispose()}
    }
} finally {$logo.Dispose()}

$iconStream = [IO.File]::Create($iconPath);$iconWriter=New-Object IO.BinaryWriter($iconStream)
try {
    $iconWriter.Write([int16]0);$iconWriter.Write([int16]1);$iconWriter.Write([int16]$images.Count)
    $offset=6+16*$images.Count
    foreach($entry in $images) {
        $dimension=if($entry.Size -eq 256){0}else{$entry.Size}
        $iconWriter.Write([byte]$dimension);$iconWriter.Write([byte]$dimension);$iconWriter.Write([byte]0);$iconWriter.Write([byte]0)
        $iconWriter.Write([int16]1);$iconWriter.Write([int16]32);$iconWriter.Write([int]$entry.Data.Length);$iconWriter.Write([int]$offset);$offset+=$entry.Data.Length
    }
    foreach($entry in $images){$iconWriter.Write([byte[]]$entry.Data)}
} finally {$iconWriter.Dispose();$iconStream.Dispose()}

}
$iconPath = Join-Path $OutputDirectory 'DoomSNESed.ico'
$converterIcon = Join-Path $OutputDirectory 'DoomSNESconv.ico'
New-ToolIcon $logoPath $iconPath
New-ToolIcon (Join-Path $toolsRoot 'Logo3.png') $converterIcon
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
if(!(Test-Path $compiler)){throw '.NET Framework C# compiler not found.'}
$sources = @(Get-ChildItem -LiteralPath $source -Filter *.cs -File) + @(Get-ChildItem -LiteralPath "$source/Geometry" -Filter *.cs -File)
& $compiler /nologo /optimize+ /target:winexe /main:DoomSNESed.Program /platform:$Platform "/out:$OutputDirectory/DoomSNESed.exe" "/win32icon:$iconPath" /r:System.Web.Extensions.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll @($sources.FullName)
if($LASTEXITCODE){throw 'DoomSNESed compilation failed.'}
Remove-Item -LiteralPath $iconPath
& $compiler /nologo /optimize+ /target:winexe /main:DoomSNESed.ConverterProgram /define:DOOM_CONVERTER /platform:$Platform "/out:$OutputDirectory/DoomSNESconv.exe" "/win32icon:$converterIcon" "/resource:$converterIcon,DoomSNESconv.Icon.ico" "/resource:$(Join-Path $toolsRoot 'Logo3.png'),DoomSNESconv.Logo.png" /r:System.Web.Extensions.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll @($sources.FullName)
if($LASTEXITCODE){throw 'DoomSNESconv compilation failed.'}
Remove-Item -LiteralPath $converterIcon
Copy-Item -LiteralPath "$source/sections.txt","$source/objects.txt","$source/TextureList.txt","$source/FloorList.txt","$source/PcActions.txt","$source/SpriteNames.txt","$source/AudioEffects.txt" -Destination "$OutputDirectory/editor-data"
Copy-Item -LiteralPath "$source/licenses/Geometry-GPL-3.0.txt","$source/licenses/DoomFX-GPL-3.0.txt" -Destination "$OutputDirectory/licenses"
Copy-Item -LiteralPath "$source/NOTICE.txt" -Destination $OutputDirectory
Copy-Item -LiteralPath "$PSScriptRoot/TOOLS-0.20.md" -Destination "$OutputDirectory/DOOMSNES-TOOLS.md"
Copy-Item -LiteralPath "$PSScriptRoot/TOOLS-0.19.md" -Destination "$OutputDirectory/DOOMSNES-TOOLS-0.19.md"
Copy-Item -LiteralPath "$PSScriptRoot/TOOLS-0.18.md" -Destination "$OutputDirectory/DOOMSNES-TOOLS-0.18.md"
Copy-Item -LiteralPath "$PSScriptRoot/TOOLS-0.17.md" -Destination "$OutputDirectory/DOOMSNES-TOOLS-0.17.md"
Copy-Item -LiteralPath "$PSScriptRoot/TOOLS-0.16.md" -Destination "$OutputDirectory/DOOMSNES-TOOLS-0.16.md"
Copy-Item -LiteralPath "$PSScriptRoot/TOOLS-0.15.md" -Destination "$OutputDirectory/DOOMSNES-TOOLS-0.15.md"
Copy-Item -LiteralPath "$PSScriptRoot/TOOLS-0.14.md" -Destination "$OutputDirectory/DOOMSNES-TOOLS-0.14.md"
Copy-Item -LiteralPath "$PSScriptRoot/TOOLS-0.13.md" -Destination "$OutputDirectory/DOOMSNES-TOOLS-0.13.md"
Copy-Item -LiteralPath "$PSScriptRoot/TOOLS-0.12.md" -Destination "$OutputDirectory/DOOMSNES-TOOLS-0.12.md"
Copy-Item -LiteralPath "$PSScriptRoot/TOOLS-0.11.md" -Destination "$OutputDirectory/DOOMSNES-TOOLS-0.11.md"
Copy-Item -LiteralPath "$PSScriptRoot/TOOLS-0.10.md" -Destination "$OutputDirectory/DOOMSNES-TOOLS-0.10.md"
Copy-Item -LiteralPath "$PSScriptRoot/TOOLS-0.9.md" -Destination "$OutputDirectory/DOOMSNES-TOOLS-0.9.md"
Copy-Item -LiteralPath "$PSScriptRoot/TOOLS-0.8.md" -Destination "$OutputDirectory/DOOMSNES-TOOLS-0.8.md"
Copy-Item -LiteralPath "$PSScriptRoot/TOOLS-0.7.md" -Destination "$OutputDirectory/DOOMSNES-TOOLS-0.7.md"
Copy-Item -LiteralPath "$PSScriptRoot/TOOLS-0.6.md" -Destination "$OutputDirectory/DOOMSNES-TOOLS-0.6.md"
Copy-Item -Path "$toolsRoot/licenses/*" -Destination "$OutputDirectory/licenses" -Force
Write-Output "Built $OutputDirectory/DoomSNESed.exe and DoomSNESconv.exe"








