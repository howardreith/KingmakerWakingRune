<#
.SYNOPSIS
  Builds the mod (Release), runs the focused tests, and packages an installable UMM zip.
  Output: artifacts\KingmakerWakingRune-<version>.zip containing KingmakerWakingRune/{Info.json,KingmakerWakingRune.dll}
  (portraits and icons are embedded in the DLL), and its build receipt <zip>.build.json.
#>
[CmdletBinding()]
param([switch] $SkipTests, [switch] $ReplacePackage)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$Root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
. (Join-Path $PSScriptRoot 'KwrBuild.Common.ps1')

# The installed runtime may be newer; the product always targets the same oldest supported API.
$loaderReferences = & (Join-Path $PSScriptRoot 'Get-PinnedLoaderReferences.ps1')
$loaderPins = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'loader-reference-pins.json') | ConvertFrom-Json
$info = Get-Content (Join-Path $Root 'Info.json') -Raw | ConvertFrom-Json

# What produced the package: the commit and dirty state read before building, the compiler inventory MSBuild evaluates
# for the production project and the build-input fingerprint; none of them may change while building.
$commit = (& git -C $Root rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $commit -cnotmatch '^[0-9a-f]{40}$') { throw 'Unable to read Git HEAD.' }
$dirty = [bool](& git -C $Root status --porcelain)
# A working-tree build never replaces a version's package: it goes to its own -dirty file, so a tested zip survives.
$zip = Join-Path $Root ("artifacts\KingmakerWakingRune-{0}{1}.zip" -f $info.Version, $(if ($dirty) { '-dirty' } else { '' }))
$receiptPath = $zip + '.build.json'
# A clean package, once written, is a candidate: it is never replaced unless the owner explicitly authorizes it.
if (-not $dirty -and ((Test-Path -LiteralPath $zip) -or (Test-Path -LiteralPath $receiptPath)) -and -not $ReplacePackage) {
    throw "$zip already exists and may be a tested candidate; bump the version, or pass -ReplacePackage only with the owner's authorization."
}
$compilerInputs = @(Get-KwrCompilerInputs)
$buildInputs = Get-KwrBuildInputFingerprint $compilerInputs

$msbuild = Get-KwrMsBuild
$productionInventory = Join-Path ([IO.Path]::GetTempPath()) ('kwr-production-inputs-' + [Guid]::NewGuid().ToString('N') + '.txt')
try {
    & $msbuild (Join-Path $Root 'src\KingmakerWakingRune\KingmakerWakingRune.csproj') -nologo -v:minimal -p:Configuration=Release "-p:CompilerReferenceListPath=$productionInventory"
    if ($LASTEXITCODE -ne 0) { throw 'Mod build failed.' }
    $produced = @(ConvertFrom-KwrCompilerInputList $productionInventory)
}
finally { Remove-Item -LiteralPath $productionInventory -Force -ErrorAction SilentlyContinue }
if ((ConvertTo-KwrCompilerInventoryText $produced) -cne (ConvertTo-KwrCompilerInventoryText $compilerInputs)) {
    throw 'The production compile used different compiler inputs from the evaluated inventory; nothing is packaged.'
}

if (-not $SkipTests) {
    & $msbuild (Join-Path $Root 'tests\KingmakerWakingRune.Tests.csproj') -nologo -v:minimal -p:Configuration=Release
    if ($LASTEXITCODE -ne 0) { throw 'Test build failed.' }
    & (Join-Path $Root 'bin\Tests\KingmakerWakingRune.Tests.exe')
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
}
if ((& git -C $Root rev-parse HEAD).Trim() -cne $commit -or (Get-KwrBuildInputFingerprint) -cne $buildInputs) {
    throw 'HEAD or a build input changed while building; nothing is packaged.'
}

$stage = Join-Path $Root 'artifacts\stage\KingmakerWakingRune'
Remove-Item -LiteralPath (Join-Path $Root 'artifacts\stage') -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $stage | Out-Null
Copy-Item (Join-Path $Root 'Info.json') $stage
Copy-Item (Join-Path $Root 'bin\Release\KingmakerWakingRune.dll') $stage
# The approved art is embedded in the DLL (no loose Portraits or Icons folders): every expected image must be present
# and decode, or the package is refused.
Add-Type -AssemblyName System.Drawing
$dllBytes = [IO.File]::ReadAllBytes((Join-Path $stage 'KingmakerWakingRune.dll'))
$assembly = [Reflection.Assembly]::Load($dllBytes)
$assemblyReferences = @($assembly.GetReferencedAssemblies())
foreach ($pin in $loaderPins.references) {
    $reference = @($assemblyReferences | Where-Object Name -CEQ $pin.Name)
    if ($reference.Count -ne 1 -or $reference[0].Version.ToString() -cne $pin.Version) {
        throw "Built DLL does not reference the pinned $($pin.Name) $($pin.Version)."
    }
}
$resources = @{}
foreach ($name in $assembly.GetManifestResourceNames()) { $resources[$name.Replace([char]92, [char]47)] = $name }
$expected = @('Icons/RefugeToken.png')
foreach ($id in 'SheilaHeidmarch', 'AmbrusValsin', 'DecemvirateMember', 'Quartermaster', 'SocietyCoordinator', 'RunelordKrune') {
    foreach ($file in 'Small.png', 'Medium.png', 'Fulllength.png') { $expected += "Portraits/$id/$file" }
}
foreach ($path in $expected) {
    if (-not $resources.ContainsKey("KWR/$path")) { throw "Embedded image $path is missing from the DLL." }
    $stream = $assembly.GetManifestResourceStream($resources["KWR/$path"])
    try { $image = [Drawing.Image]::FromStream($stream); $size = "$($image.Width)x$($image.Height)"; $image.Dispose() }
    catch { throw "Embedded image $path does not decode: $_" }
    finally { $stream.Dispose() }
    if ($size -eq '0x0') { throw "Embedded image $path is empty." }
}
Write-Host "Embedded art verified: $($expected.Count) images."

# The ZIP and its receipt are written and validated as a pair in a private staging folder, and only then placed at their
# final paths; any failure leaves no new file at the candidate paths.
$pending = Join-Path $Root ('artifacts\.pending-' + [Guid]::NewGuid().ToString('N'))
$pendingZip = Join-Path $pending ([IO.Path]::GetFileName($zip))
$pendingReceipt = $pendingZip + '.build.json'
try {
    New-Item -ItemType Directory -Path $pending | Out-Null
    # Entries are written with forward slashes (the zip standard): Compress-Archive in Windows PowerShell 5.1 writes
    # backslashes, which some extractors turn into flat file names instead of folders.
    Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
    $archive = [IO.Compression.ZipFile]::Open($pendingZip, [IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($file in Get-ChildItem -LiteralPath $stage -Recurse -File) {
            $entry = 'KingmakerWakingRune/' + $file.FullName.Substring($stage.Length + 1).Replace([char]92, [char]47)
            [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file.FullName, $entry, [IO.Compression.CompressionLevel]::Optimal)
        }
    }
    finally { $archive.Dispose() }
    $receipt = [ordered]@{
        commit = $commit
        dirty = $dirty
        version = $info.Version
        minimumManagerVersion = $info.ManagerVersion
        loaderReferences = @($loaderPins.references | ForEach-Object {
            [ordered]@{
                name = $_.Name
                assemblyVersion = $_.Version
                sha256 = (Get-FileHash -LiteralPath (Join-Path $loaderReferences $_.File) -Algorithm SHA256).Hash.ToLowerInvariant()
            }
        })
        buildInputsSha256 = $buildInputs
        compilerInputs = $compilerInputs
        dllSha256 = (Get-FileHash (Join-Path $stage 'KingmakerWakingRune.dll') -Algorithm SHA256).Hash.ToLowerInvariant()
        zip = $zip
        zipSha256 = (Get-FileHash $pendingZip -Algorithm SHA256).Hash.ToLowerInvariant()
    }
    # The receipt binds these exact bytes to the pinned references and the producing source; the standalone validator
    # (also run by KwrRuntime.ps1 before any install) checks the packaged DLL and, with the receipt, its provenance.
    $receipt | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $pendingReceipt -Encoding UTF8
    # Separate process: this script has already loaded the DLL bytes above.
    $validation = @('-NoLogo', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', (Join-Path $PSScriptRoot 'Validate-Package.ps1'), '-PackagePath', $pendingZip, '-ReceiptPath', $pendingReceipt)
    if (-not $dirty) { $validation += '-RequireCurrentProvenance' }
    & powershell.exe @validation
    if ($LASTEXITCODE -ne 0) { throw 'Standalone package validation failed; nothing is placed at the candidate paths.' }
    if ($ReplacePackage) { Remove-Item -LiteralPath $zip, $receiptPath -Force -ErrorAction SilentlyContinue }
    elseif ($dirty) { Remove-Item -LiteralPath $zip, $receiptPath -Force -ErrorAction SilentlyContinue }
    # File.Move refuses an existing destination: a clean candidate that appeared meanwhile is never overwritten.
    [IO.File]::Move($pendingZip, $zip)
    try { [IO.File]::Move($pendingReceipt, $receiptPath) }
    catch { [IO.File]::Delete($zip); throw }
}
finally {
    if ([IO.Directory]::Exists($pending)) { [IO.Directory]::Delete($pending, $true) }
}
$receipt | ConvertTo-Json -Depth 5
