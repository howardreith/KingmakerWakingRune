<#
.SYNOPSIS
  Standalone check of a Waking Rune UMM package before it is installed (KwrRuntime.ps1 runs it before
  every install; run it by hand on any supplied ZIP).
.DESCRIPTION
  Accepts only KingmakerWakingRune/Info.json and KingmakerWakingRune/KingmakerWakingRune.dll (no loader, Harmony,
  game or Unity DLL can be packaged). The packaged manifest must declare the pinned baseline ManagerVersion, and the
  packaged DLL's actual AssemblyRef table must name exactly the pinned UnityModManager, 0Harmony12 and 0Harmony
  versions: lowering Info.json on a DLL compiled against a newer loader is refused. With -ReceiptPath, the build
  receipt written by Build.ps1 must describe these exact ZIP and DLL bytes and the pinned reference identities.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string] $PackagePath,
    [string] $ReceiptPath,
    [string] $ReportPath
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$Root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$pins = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'loader-reference-pins.json') | ConvertFrom-Json
$expectedInfo = Get-Content -Raw -LiteralPath (Join-Path $Root 'Info.json') | ConvertFrom-Json
$PackagePath = [IO.Path]::GetFullPath($PackagePath)
if (-not (Test-Path -LiteralPath $PackagePath -PathType Leaf)) { throw "Package not found: $PackagePath" }
# Inspect and hash one immutable private copy, so the recorded ZIP digest and the inspected DLL come from the same bytes.
$privateDirectory = Join-Path ([IO.Path]::GetTempPath()) ('kwr-validate-' + [Guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($privateDirectory)
$inspected = Join-Path $privateDirectory 'package.zip'
[IO.File]::Copy($PackagePath, $inspected)
try {
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

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead($inspected)
try {
    # Directory records are allowed only as the empty record of the package folder; one carrying data, or any other
    # directory record, is refused before files are counted.
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

# Read the DLL's actual AssemblyRef table (metadata only; nothing is executed). Windows PowerShell has no metadata
# reader and a reflection-only load refuses a second same-identity assembly in one process, so it reads the metadata
# in a separate process (several packages can then be validated in one session).
if ($PSVersionTable.PSEdition -eq 'Core') {
    Add-Type -AssemblyName System.Reflection.Metadata
    $peStream = [IO.MemoryStream]::new($dllBytes, $false)
    $peReader = [System.Reflection.PortableExecutable.PEReader]::new($peStream)
    try {
        $metadata = [System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($peReader)
        $mvid = $metadata.GetGuid($metadata.GetModuleDefinition().Mvid).ToString()
        $references = @($metadata.AssemblyReferences | ForEach-Object {
            $reference = $metadata.GetAssemblyReference($_)
            [pscustomobject]@{ Name = $metadata.GetString($reference.Name); Version = $reference.Version }
        })
    }
    finally { $peReader.Dispose(); $peStream.Dispose() }
}
else {
    $temporaryDll = Join-Path ([IO.Path]::GetTempPath()) ('kwr-metadata-' + [Guid]::NewGuid().ToString('N') + '.dll')
    [IO.File]::WriteAllBytes($temporaryDll, $dllBytes)
    try {
        $reader = "`$a = [Reflection.Assembly]::ReflectionOnlyLoad([IO.File]::ReadAllBytes('" + $temporaryDll.Replace("'", "''") + "')); " +
            "[pscustomobject]@{ Mvid = `$a.ManifestModule.ModuleVersionId.ToString(); References = @(`$a.GetReferencedAssemblies() | " +
            "ForEach-Object { [pscustomobject]@{ Name = `$_.Name; Version = `$_.Version.ToString() } }) } | ConvertTo-Json -Compress -Depth 4"
        $previous = $ErrorActionPreference
        $ErrorActionPreference = 'Continue'
        try { $json = $null | & powershell.exe -NoLogo -NoProfile -NonInteractive -EncodedCommand ([Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($reader))) 2>&1 | ForEach-Object { "$_" } }
        finally { $ErrorActionPreference = $previous }
        if ($LASTEXITCODE -ne 0) { throw "Packaged DLL metadata could not be read: $($json -join ' ')" }
        $identity = ($json -join "`n") | ConvertFrom-Json
        $mvid = [string]$identity.Mvid
        $references = @($identity.References)
    }
    finally { Remove-Item -LiteralPath $temporaryDll -Force -ErrorAction SilentlyContinue }
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

$packageSha256 = (Get-FileHash -LiteralPath $inspected -Algorithm SHA256).Hash.ToLowerInvariant()
$dllSha256 = Get-BytesSha256 $dllBytes
if ($ReceiptPath) {
    $receipt = Get-Content -Raw -LiteralPath $ReceiptPath | ConvertFrom-Json
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
}

$result = [ordered]@{
    status = 'package-validated'
    package = $PackagePath
    package_sha256 = $packageSha256
    version = [string]$info.Version
    manager_version = [string]$info.ManagerVersion
    dll_sha256 = $dllSha256
    dll_mvid = $mvid
    dll_loader_references = $observed
    receipt_checked = [bool]$ReceiptPath
}
if ($ReportPath) { $result | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $ReportPath -Encoding UTF8 }
Write-Host "Package validation passed: $PackagePath"
Write-Host "Package SHA-256: $packageSha256; DLL SHA-256: $dllSha256; MVID: $mvid; loader references: $(($observed | ForEach-Object { $_.name + ' ' + $_.version }) -join ', ')"
}
finally { if ([IO.Directory]::Exists($privateDirectory)) { [IO.Directory]::Delete($privateDirectory, $true) } }
