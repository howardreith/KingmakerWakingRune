<#
.SYNOPSIS
  Builds the mod (Release), runs the focused tests, and packages an installable UMM zip.
  Output: artifacts\KingmakerWakingRune-<version>.zip containing KingmakerWakingRune\{Info.json,KingmakerWakingRune.dll}.
#>
[CmdletBinding()]
param([switch] $SkipTests)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$Root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))

$vswhere = 'C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe'
$msbuild = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (-not $msbuild) { throw 'MSBuild not found (Visual Studio Build Tools required).' }

& $msbuild (Join-Path $Root 'src\KingmakerWakingRune\KingmakerWakingRune.csproj') -nologo -v:minimal -p:Configuration=Release
if ($LASTEXITCODE -ne 0) { throw 'Mod build failed.' }

if (-not $SkipTests) {
    & $msbuild (Join-Path $Root 'tests\KingmakerWakingRune.Tests.csproj') -nologo -v:minimal -p:Configuration=Release
    if ($LASTEXITCODE -ne 0) { throw 'Test build failed.' }
    & (Join-Path $Root 'bin\Tests\KingmakerWakingRune.Tests.exe')
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
}

$info = Get-Content (Join-Path $Root 'Info.json') -Raw | ConvertFrom-Json
$stage = Join-Path $Root 'artifacts\stage\KingmakerWakingRune'
Remove-Item -LiteralPath (Join-Path $Root 'artifacts\stage') -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $stage | Out-Null
Copy-Item (Join-Path $Root 'Info.json') $stage
Copy-Item (Join-Path $Root 'bin\Release\KingmakerWakingRune.dll') $stage
$zip = Join-Path $Root ("artifacts\KingmakerWakingRune-{0}.zip" -f $info.Version)
Remove-Item -LiteralPath $zip -Force -ErrorAction SilentlyContinue
Compress-Archive -Path $stage -DestinationPath $zip

$commit = (& git -C $Root rev-parse HEAD).Trim()
$dirty = [bool](& git -C $Root status --porcelain)
[ordered]@{
    commit = $commit
    dirty = $dirty
    version = $info.Version
    dllSha256 = (Get-FileHash (Join-Path $stage 'KingmakerWakingRune.dll') -Algorithm SHA256).Hash.ToLowerInvariant()
    zip = $zip
    zipSha256 = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
} | ConvertTo-Json
