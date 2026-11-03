# SoNG Browser Optimizer - offline Windows .NET Framework build; no NuGet packages.
$ErrorActionPreference = 'Stop'
$project = Split-Path -Parent $MyInvocation.MyCommand.Path
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path $compiler)) { $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
if (!(Test-Path $compiler)) { throw 'C# compiler not found. Install .NET Framework developer tools.' }
$src = Join-Path $project 'src\Program.cs'
$iconMaker = Join-Path $project 'tools\IconMaker.cs'
if (!(Test-Path $src) -or !(Test-Path $iconMaker)) { throw 'Missing source file.' }
$outDir = Join-Path $project 'dist'
$assetsDir = Join-Path $project 'assets'
New-Item -ItemType Directory -Path $outDir,$assetsDir -Force | Out-Null
$icon = Join-Path $assetsDir 'SoNG_Browser_Optimizer.ico'
$generator = Join-Path $env:TEMP 'SoNG_Browser_Optimizer_IconMaker.exe'
& $compiler /nologo /target:exe /platform:anycpu /optimize+ /out:$generator /reference:System.Drawing.dll $iconMaker
if ($LASTEXITCODE -ne 0) { throw "Icon generator build failed with exit code $LASTEXITCODE" }
& $generator $icon
if ($LASTEXITCODE -ne 0 -or !(Test-Path $icon)) { throw 'Icon generation failed.' }
$out = Join-Path $outDir 'SoNG_Browser_Optimizer.exe'
& $compiler /nologo /target:winexe /platform:anycpu /optimize+ "/win32icon:$icon" /out:$out /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Core.dll $src
if ($LASTEXITCODE -ne 0) { throw "Compilation failed with exit code $LASTEXITCODE" }
Write-Host "Built: $out"
Get-FileHash -Algorithm SHA256 $out | Format-List Hash,Path
