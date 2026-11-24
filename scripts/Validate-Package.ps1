<#
.SYNOPSIS
  Standalone check of a Waking Rune UMM package before it is installed (KwrRuntime.ps1 runs it before
  every install; run it by hand on any supplied ZIP).
.DESCRIPTION
  Accepts only KingmakerWakingRune/Info.json and KingmakerWakingRune/KingmakerWakingRune.dll (no loader, Harmony,
  game or Unity DLL can be packaged). The repository's pin definition must pass the same contract the build uses
  (offline). The packaged manifest must declare the pinned baseline ManagerVersion; the packaged DLL must define the
  assembly KingmakerWakingRune and its actual AssemblyRef table must name exactly the pinned UnityModManager, 0Harmony12
  and 0Harmony versions and no other UnityModManager or Harmony assembly: lowering Info.json on a DLL compiled against
  a newer loader is refused. With -ReceiptPath, the build receipt written by Build.ps1 must describe these exact ZIP and
  DLL bytes and the pinned reference identities. With -RequireCurrentProvenance (the frozen-candidate path), the receipt
  must also name a clean producing commit and the current build inputs (source, manifest, pins, build scripts and the
  compiler inventory MSBuild evaluates now); the report states whether source provenance was verified.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string] $PackagePath,
    [string] $ReceiptPath,
    [string] $ReportPath,
    [switch] $RequireCurrentProvenance
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$Root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
# PowerShell literal filesystem semantics: relative to the current PowerShell location, no wildcard expansion.
function Resolve-LiteralFilePath([string] $Path) { return $PSCmdlet.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Path) }
$PackagePath = Resolve-LiteralFilePath $PackagePath
if (-not [IO.File]::Exists($PackagePath)) { throw "Package not found: $PackagePath" }
if ($ReceiptPath) {
    $ReceiptPath = Resolve-LiteralFilePath $ReceiptPath
    if (-not [IO.File]::Exists($ReceiptPath)) { throw "Build receipt not found: $ReceiptPath" }
}
elseif ($RequireCurrentProvenance) { throw '-RequireCurrentProvenance needs the build receipt (-ReceiptPath).' }
if ($ReportPath) {
    $ReportPath = Resolve-LiteralFilePath $ReportPath
    foreach ($inputPath in @($PackagePath, $ReceiptPath) | Where-Object { $_ }) {
        if ([string]::Equals([IO.Path]::GetFullPath($ReportPath), [IO.Path]::GetFullPath($inputPath), [StringComparison]::OrdinalIgnoreCase)) {
            throw "The report path must not be an input of the validation: $inputPath"
        }
    }
}
# The same pin-definition and manifest contract as the build (offline; no download, no cache needed).
[void](& (Join-Path $PSScriptRoot 'Get-PinnedLoaderReferences.ps1') -DefinitionOnly)
$pins = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'loader-reference-pins.json') | ConvertFrom-Json
$expectedInfo = Get-Content -Raw -LiteralPath (Join-Path $Root 'Info.json') | ConvertFrom-Json
$allowed = @('KingmakerWakingRune/Info.json', 'KingmakerWakingRune/KingmakerWakingRune.dll')

function Get-BytesSha256([byte[]] $Bytes) {
    $hasher = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($hasher.ComputeHash($Bytes))).Replace('-', '').ToLowerInvariant() }
    finally { $hasher.Dispose() }
}
function Read-EntryBytes($Entry) {
    $stream = $Entry.Open()
    $buffer = [IO.MemoryStream]::new()
    try { $stream.CopyTo($buffer); return , $buffer.ToArray() }
    finally { $buffer.Dispose(); $stream.Dispose() }
}

# Inspect and hash one private snapshot of the ZIP (and of the receipt), created inside the cleanup scope, so the recorded
# digests and the inspected bytes are the same even if the caller's files change meanwhile.
$privateDirectory = Join-Path ([IO.Path]::GetTempPath()) ('kwr-validate-' + [Guid]::NewGuid().ToString('N'))
$inspected = Join-Path $privateDirectory 'package.zip'
try {
[void][IO.Directory]::CreateDirectory($privateDirectory)
[IO.File]::Copy($PackagePath, $inspected)
$receiptBytes = if ($ReceiptPath) { [IO.File]::ReadAllBytes($ReceiptPath) } else { $null }

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead($inspected)
try {
    # Directory records are allowed only as the decoded-empty record of the package folder.
    foreach ($directory in @($archive.Entries | Where-Object { -not $_.FullName -or $_.FullName.EndsWith('/') -or $_.FullName.EndsWith('\') })) {
        $directoryStream = $directory.Open()
        try { $decodedByte = $directoryStream.ReadByte() } finally { $directoryStream.Dispose() }
        if ($directory.Length -ne 0 -or $decodedByte -ne -1 -or $directory.FullName -cne 'KingmakerWakingRune/') {
            throw "Unexpected or data-bearing package directory record: '$($directory.FullName)'"
        }
    }
    $files = @($archive.Entries | Where-Object { $_.FullName -and -not $_.FullName.EndsWith('/') -and -not $_.FullName.EndsWith('\') })
    $names = @($files | ForEach-Object { $_.FullName })
    if ($files.Count -ne $allowed.Count) {
        throw "Package must contain exactly $($allowed.Count) files; found $($files.Count): $($names -join ', ')"
    }
    foreach ($name in $names) {
        if ($allowed -cnotcontains $name) { throw "Unexpected package entry: $name" }
    }
    if (@($names | Select-Object -Unique).Count -ne $names.Count) { throw 'Duplicate package entry.' }
    $infoBytes = Read-EntryBytes ($files | Where-Object { $_.FullName -ceq $allowed[0] })
    $dllBytes = Read-EntryBytes ($files | Where-Object { $_.FullName -ceq $allowed[1] })
}
finally { $archive.Dispose() }

$info = [Text.Encoding]::UTF8.GetString($infoBytes).TrimStart([char]0xFEFF) | ConvertFrom-Json
foreach ($field in @('Id', 'AssemblyName', 'EntryMethod', 'Version', 'ManagerVersion')) {
    if (-not $info.PSObject.Properties[$field] -or -not ($info.$field -is [string])) { throw "Packaged Info.json $field must be a JSON string." }
}
if ([string]$info.Id -cne 'KingmakerWakingRune' -or [string]$info.AssemblyName -cne 'KingmakerWakingRune.dll' -or
    [string]$info.EntryMethod -cne [string]$expectedInfo.EntryMethod -or [string]$info.Version -cne [string]$expectedInfo.Version) {
    throw "Packaged Info.json identity/version does not match the repository ($($info.Id) $($info.Version))."
}
if ([string]$info.ManagerVersion -cne [string]$pins.minimumManagerVersion) {
    throw "Packaged ManagerVersion '$($info.ManagerVersion)' does not equal the pinned baseline $($pins.minimumManagerVersion)."
}
if ($dllBytes.Length -lt 2 -or $dllBytes[0] -ne 0x4D -or $dllBytes[1] -ne 0x5A) { throw 'Packaged DLL lacks an MZ header.' }

# Read the DLL's assembly definition and AssemblyRef table (metadata only; nothing is executed). Windows PowerShell has no
# metadata reader and a reflection-only load refuses a second same-identity assembly in one process, so it reads the
# metadata in a separate process (several packages can then be validated in one session).
if ($PSVersionTable.PSEdition -eq 'Core') {
    Add-Type -AssemblyName System.Reflection.Metadata
    $peStream = [IO.MemoryStream]::new($dllBytes, $false)
    $peReader = [System.Reflection.PortableExecutable.PEReader]::new($peStream)
    try {
        $metadata = [System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($peReader)
        $mvid = $metadata.GetGuid($metadata.GetModuleDefinition().Mvid).ToString()
        $assemblyName = $metadata.GetString($metadata.GetAssemblyDefinition().Name)
        $references = @($metadata.AssemblyReferences | ForEach-Object {
            $reference = $metadata.GetAssemblyReference($_)
            [pscustomobject]@{ Name = $metadata.GetString($reference.Name); Version = $reference.Version }
        })
    }
    finally { $peReader.Dispose(); $peStream.Dispose() }
}
else {
    $temporaryDll = Join-Path $privateDirectory 'metadata.dll'
    [IO.File]::WriteAllBytes($temporaryDll, $dllBytes)
    $reader = "`$a = [Reflection.Assembly]::ReflectionOnlyLoad([IO.File]::ReadAllBytes('" + $temporaryDll.Replace("'", "''") + "')); " +
        "[pscustomobject]@{ Name = `$a.GetName().Name; Mvid = `$a.ManifestModule.ModuleVersionId.ToString(); References = @(`$a.GetReferencedAssemblies() | " +
        "ForEach-Object { [pscustomobject]@{ Name = `$_.Name; Version = `$_.Version.ToString() } }) } | ConvertTo-Json -Compress -Depth 4"
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try { $json = $null | & powershell.exe -NoLogo -NoProfile -NonInteractive -EncodedCommand ([Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($reader))) 2>&1 | ForEach-Object { "$_" } }
    finally { $ErrorActionPreference = $previous }
    if ($LASTEXITCODE -ne 0) { throw "Packaged DLL metadata could not be read: $($json -join ' ')" }
    $identity = ($json -join "`n") | ConvertFrom-Json
    $mvid = [string]$identity.Mvid
    $assemblyName = [string]$identity.Name
    $references = @($identity.References)
}
if ($assemblyName -cne 'KingmakerWakingRune') {
    throw "Packaged DLL defines assembly '$assemblyName', not KingmakerWakingRune; the assembly definition must be the product."
}
$observed = @()
foreach ($pin in $pins.references) {
    $matches = @($references | Where-Object { $_.Name -ceq [string]$pin.Name })
    if ($matches.Count -ne 1 -or [string]$matches[0].Version -cne [string]$pin.Version) {
        $seen = ($matches | ForEach-Object { [string]$_.Version }) -join ', '
        throw "Packaged DLL must reference pinned $($pin.Name) $($pin.Version) (found: $seen); lowering Info.json alone is not sufficient."
    }
    $observed += [ordered]@{ name = [string]$pin.Name; version = [string]$matches[0].Version }
}
$pinnedNames = @($pins.references | ForEach-Object { [string]$_.Name })
$extraLoader = @($references | Where-Object { [string]$_.Name -match '^(0?Harmony|UnityModManager)' -and $pinnedNames -cnotcontains [string]$_.Name })
if ($extraLoader.Count -ne 0) {
    throw "Packaged DLL references an unpinned loader or Harmony assembly: $(@($extraLoader | ForEach-Object { [string]$_.Name + ' ' + [string]$_.Version }) -join ', ')"
}

$packageSha256 = (Get-FileHash -LiteralPath $inspected -Algorithm SHA256).Hash.ToLowerInvariant()
$dllSha256 = Get-BytesSha256 $dllBytes
$provenance = 'not-checked'
if ($ReceiptPath) {
    $receipt = [Text.Encoding]::UTF8.GetString($receiptBytes).TrimStart([char]0xFEFF) | ConvertFrom-Json
    foreach ($field in @('zipSha256', 'dllSha256', 'minimumManagerVersion', 'loaderReferences')) {
        if (-not $receipt.PSObject.Properties[$field]) { throw "Build receipt has no $field; rebuild." }
    }
    # Types first: an array or Boolean would make PowerShell comparisons filter or coerce instead of compare.
    foreach ($field in @('zipSha256', 'dllSha256')) {
        if (-not ($receipt.$field -is [string]) -or $receipt.$field -cnotmatch '^[0-9a-f]{64}$') { throw "Build receipt $field must be a lowercase SHA-256 string; rebuild." }
    }
    if (-not ($receipt.minimumManagerVersion -is [string])) { throw 'Build receipt minimumManagerVersion must be a string; rebuild.' }
    foreach ($entry in @($receipt.loaderReferences)) {
        foreach ($field in @('name', 'assemblyVersion', 'sha256')) {
            if ($null -eq $entry -or -not $entry.PSObject.Properties[$field] -or -not ($entry.$field -is [string])) { throw "Build receipt loader reference field $field must be a string; rebuild." }
        }
    }
    if ([string]$receipt.zipSha256 -cne $packageSha256) { throw 'Build receipt does not describe this package (zip SHA-256 differs); rebuild.' }
    if ([string]$receipt.dllSha256 -cne $dllSha256) { throw 'Build receipt does not describe the packaged DLL (SHA-256 differs); rebuild.' }
    if ([string]$receipt.minimumManagerVersion -cne [string]$pins.minimumManagerVersion) { throw 'Build receipt records a different minimum UMM version; rebuild.' }
    $recorded = @($receipt.loaderReferences)
    if ($recorded.Count -ne @($pins.references).Count) { throw 'Build receipt has an unexpected loader reference count; rebuild.' }
    foreach ($pin in $pins.references) {
        $match = @($recorded | Where-Object { [string]$_.name -ceq [string]$pin.Name })
        if ($match.Count -ne 1 -or [string]$match[0].assemblyVersion -cne [string]$pin.Version -or [string]$match[0].sha256 -cne [string]$pin.Sha256) {
            throw "Build receipt does not match pinned reference $($pin.Name); rebuild."
        }
    }
    $provenance = 'receipt-matches-package'
    if ($RequireCurrentProvenance) {
        . (Join-Path $PSScriptRoot 'KwrBuild.Common.ps1')
        foreach ($field in @('commit', 'dirty', 'buildInputsSha256', 'compilerInputs')) {
            if (-not $receipt.PSObject.Properties[$field]) { throw "Build receipt has no $field (source provenance); rebuild." }
        }
        if (-not ($receipt.commit -is [string]) -or $receipt.commit -cnotmatch '^[0-9a-f]{40}$') { throw 'Build receipt commit must be a 40-character lowercase commit SHA; rebuild.' }
        if (-not ($receipt.dirty -is [bool])) { throw 'Build receipt dirty must be a JSON Boolean; rebuild.' }
        if ($receipt.dirty) { throw 'Build receipt is from a dirty working tree; only a clean build is a frozen candidate.' }
        if (-not ($receipt.buildInputsSha256 -is [string]) -or $receipt.buildInputsSha256 -cnotmatch '^[0-9a-f]{64}$') { throw 'Build receipt buildInputsSha256 must be a lowercase SHA-256 string; rebuild.' }
        foreach ($entry in @($receipt.compilerInputs)) {
            foreach ($field in @('kind', 'name', 'sha256')) {
                if ($null -eq $entry -or -not $entry.PSObject.Properties[$field] -or -not ($entry.$field -is [string])) { throw "Build receipt compiler input field $field must be a string; rebuild." }
            }
        }
        $currentInputs = @(Get-KwrCompilerInputs)
        if ((ConvertTo-KwrCompilerInventoryText $receipt.compilerInputs) -cne (ConvertTo-KwrCompilerInventoryText $currentInputs) -or
            [string]$receipt.buildInputsSha256 -cne (Get-KwrBuildInputFingerprint $currentInputs)) {
            throw 'Build inputs changed since this package was built (source, manifest, pins, build scripts, resolved references or compiler settings); rebuild.'
        }
        $provenance = 'verified-current'
    }
}

$result = [ordered]@{
    status = 'package-validated'
    package = $PackagePath
    package_sha256 = $packageSha256
    version = [string]$info.Version
    manager_version = [string]$info.ManagerVersion
    dll_assembly_name = $assemblyName
    dll_sha256 = $dllSha256
    dll_mvid = $mvid
    dll_loader_references = $observed
    receipt_checked = [bool]$ReceiptPath
    source_provenance = $provenance
}
if ($ReportPath) { $result | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $ReportPath -Encoding UTF8 }
Write-Host "Package validation passed: $PackagePath"
Write-Host "Package SHA-256: $packageSha256; DLL SHA-256: $dllSha256; MVID: $mvid; loader references: $(($observed | ForEach-Object { $_.name + ' ' + $_.version }) -join ', '); source provenance: $provenance"
}
finally { if ([IO.Directory]::Exists($privateDirectory)) { [IO.Directory]::Delete($privateDirectory, $true) } }
