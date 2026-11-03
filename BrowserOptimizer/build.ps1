# SoNG Browser Optimizer - Windows .NET Framework build
# Run in a Windows PowerShell terminal; no administrator rights or NuGet packages required.
$ErrorActionPreference = 'Stop'
$project = Split-Path -Parent $MyInvocation.MyCommand.Path
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path $compiler)) {
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
if (!(Test-Path $compiler)) { throw 'C# compiler not found. Install .NET Framework developer tools.' }
$src = Join-Path $project 'src\Program.cs'
if (!(Test-Path $src)) { throw 'Missing src\Program.cs' }
$outDir = Join-Path $project 'dist'
New-Item -ItemType Directory -Path $outDir -Force | Out-Null
$out = Join-Path $outDir 'SoNG_Browser_Optimizer.exe'
& $compiler /nologo /target:winexe /platform:anycpu /optimize+ /out:$out /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Core.dll $src
if ($LASTEXITCODE -ne 0) { throw "Compilation failed with exit code $LASTEXITCODE" }
Write-Host "Built: $out"
Get-FileHash -Algorithm SHA256 $out | Format-List Hash, Path
