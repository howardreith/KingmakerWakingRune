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
# The product is compiled into a folder this invocation owns (OutDir and IntermediateOutputPath are global properties,
# which no project or local setting overrides), so the packaged DLL is this compile's output: Csc's /out: file in the
# owned intermediate folder and the copied DLL must have the same digest, and those bytes are what is packaged.
$owned = Join-Path ([IO.Path]::GetTempPath()) ('kwr-build-' + [Guid]::NewGuid().ToString('N'))
$productionInventory = Join-Path $owned 'compiler-inputs.txt'
try {
    [void][IO.Directory]::CreateDirectory($owned)
    & $msbuild (Join-Path $Root 'src\KingmakerWakingRune\KingmakerWakingRune.csproj') -nologo -v:minimal -t:Rebuild -p:Configuration=Release "-p:CompilerReferenceListPath=$productionInventory" "-p:OutDir=$owned\bin\" "-p:IntermediateOutputPath=$owned\obj\"
    if ($LASTEXITCODE -ne 0) { throw 'Mod build failed.' }
    $produced = @(ConvertFrom-KwrCompilerInputList $productionInventory)
    $listLines = @(Get-Content -LiteralPath $productionInventory -Encoding UTF8)
    $projectDirectory = $listLines[1].Substring('project|'.Length)
    $outArgument = @($listLines | Where-Object { $_ -match '^arg\|[/-]out:' })
    if ($outArgument.Count -ne 1) { throw 'The production command line does not name exactly one /out: file.' }
    $cscOutput = [IO.Path]::GetFullPath([IO.Path]::Combine($projectDirectory, ($outArgument[0] -replace '^arg\|[/-]out:', '').Trim('"')))
    if (-not $cscOutput.StartsWith(($owned + '\obj\'), [StringComparison]::OrdinalIgnoreCase)) { throw "The compiler wrote outside this invocation's folder: $cscOutput" }
    $builtDll = Join-Path $owned 'bin\KingmakerWakingRune.dll'
    $productDllSha256 = Get-KwrSha256 $builtDll
    if ((Get-KwrSha256 $cscOutput) -cne $productDllSha256) { throw 'The copied product DLL differs from the compiler output.' }
    $productDllBytes = [IO.File]::ReadAllBytes($builtDll)
    Write-Host "Product DLL SHA-256 (this invocation's compile): $productDllSha256"
    if (-not $SkipTests) {
        # The tests are built into this invocation's folder too and run from there (the evaluated output, never a fixed
        # path where an older executable could answer).
        & $msbuild (Join-Path $Root 'tests\KingmakerWakingRune.Tests.csproj') -nologo -v:minimal -p:Configuration=Release "-p:OutDir=$owned\tests\" "-p:OutputPath=$owned\tests\" "-p:IntermediateOutputPath=$owned\tests-obj\"
        if ($LASTEXITCODE -ne 0) { throw 'Test build failed.' }
        $testRunner = Join-Path $owned 'tests\KingmakerWakingRune.Tests.exe'
        if (-not (Test-Path -LiteralPath $testRunner -PathType Leaf)) { throw "The test build did not produce $testRunner." }
        & $testRunner
        if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
    }
}
finally { if ([IO.Directory]::Exists($owned)) { [IO.Directory]::Delete($owned, $true) } }
if ((ConvertTo-KwrCompilerInventoryText $produced) -cne (ConvertTo-KwrCompilerInventoryText $compilerInputs)) {
    throw 'The production compile used different compiler inputs from the evaluated inventory; nothing is packaged.'
}

if ((& git -C $Root rev-parse HEAD).Trim() -cne $commit -or (Get-KwrBuildInputFingerprint) -cne $buildInputs) {
    throw 'HEAD or a build input changed while building; nothing is packaged.'
}

# Staging, the archive, the receipt and their validation all live in a folder private to this invocation.
$pending = Join-Path $Root ('artifacts\.pending-' + [Guid]::NewGuid().ToString('N'))
$keepPending = $false
$stage = Join-Path $pending 'stage\KingmakerWakingRune'
New-Item -ItemType Directory -Force -Path $stage | Out-Null
try {
Copy-Item (Join-Path $Root 'Info.json') $stage
[IO.File]::WriteAllBytes((Join-Path $stage 'KingmakerWakingRune.dll'), $productDllBytes)
if ((Get-KwrSha256 (Join-Path $stage 'KingmakerWakingRune.dll')) -cne $productDllSha256) { throw 'The staged DLL differs from this invocation''s compile.' }
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

# The ZIP and its receipt are written and validated as a pair in the private folder, and only then placed at their final
# paths; any failure leaves no new file at the candidate paths.
$pendingZip = Join-Path $pending ([IO.Path]::GetFileName($zip))
$pendingReceipt = $pendingZip + '.build.json'
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
        dllSha256 = $productDllSha256
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
    # A replaced package (owner-authorized -ReplacePackage, or a -dirty build) is moved aside first; the moves aside and
    # the placements are one rollback scope: every completed step is undone in reverse if any step fails (for example a
    # receipt held open by another process). File.Move refuses an existing destination, so a clean candidate is never
    # overwritten silently. If the rollback itself fails, the staging folder with the previous files is kept and named.
    $previous = Join-Path $pending 'previous'
    $done = [Collections.Generic.List[object]]::new()
    # One publisher at a time for this destination (an exclusive lock file removed when its handle closes), so placement
    # and rollback never act on another build's files.
    $publishLock = $null
    $deadline = [DateTime]::UtcNow.AddSeconds(300)
    while ($null -eq $publishLock) {
        try { $publishLock = [IO.FileStream]::new(($zip + '.publish.lock'), [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None, 1, [IO.FileOptions]::DeleteOnClose) }
        catch [IO.IOException] { if ([DateTime]::UtcNow -gt $deadline) { throw "Another build is publishing $zip; nothing was placed." }; Start-Sleep -Milliseconds 200 }
    }
    try {
        if ($ReplacePackage -or $dirty) {
            New-Item -ItemType Directory -Path $previous | Out-Null
            foreach ($file in @($zip, $receiptPath)) {
                if (Test-Path -LiteralPath $file) { $aside = Join-Path $previous ([IO.Path]::GetFileName($file)); [IO.File]::Move($file, $aside); $done.Add(@($aside, $file)) }
            }
        }
        [IO.File]::Move($pendingZip, $zip); $done.Add(@($zip, $pendingZip))
        [IO.File]::Move($pendingReceipt, $receiptPath); $done.Add(@($receiptPath, $pendingReceipt))
    }
    catch {
        $placementError = $_
        try { for ($i = $done.Count - 1; $i -ge 0; $i--) { [IO.File]::Move($done[$i][0], $done[$i][1]) } }
        catch {
            $keepPending = $true
            throw "Placement failed ($placementError) and the rollback failed ($_); the previous ZIP and receipt are kept in $previous."
        }
        throw $placementError
    }
    finally { $publishLock.Dispose() }
}
finally {
    if (-not $keepPending -and [IO.Directory]::Exists($pending)) { [IO.Directory]::Delete($pending, $true) }
}
$receipt | ConvertTo-Json -Depth 5
