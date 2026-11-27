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
# The real long filename (a short 8.3 spelling expanded), so the conventional receipt beside the package is derived from
# the name the build wrote.
. (Join-Path $PSScriptRoot 'KwrFileIdentity.ps1')
$PackagePath = [KwrFileIdentityV9]::LongPathOf($PackagePath)
if ($ReceiptPath) {
    $ReceiptPath = Resolve-LiteralFilePath $ReceiptPath
    if (-not [IO.File]::Exists($ReceiptPath)) { throw "Build receipt not found: $ReceiptPath" }
    $ReceiptPath = [KwrFileIdentityV9]::LongPathOf($ReceiptPath)
}
elseif ($RequireCurrentProvenance) { throw '-RequireCurrentProvenance needs the build receipt (-ReceiptPath).' }
if ($ReportPath) {
    $ReportPath = Resolve-LiteralFilePath $ReportPath
    # R10-F1: the first observation of the report destination (its identity, or 'absent'), taken before any other check, is
    # the one the final write is bound to; a destination created or replaced after it (a receipt moved there at any later
    # moment, also before its own identity could be sampled) is refused.
    # Round 13: one identity-bearing open (a missing file reported by that open is 'absent'); no separate existence check.
    $reportStartId = [KwrFileIdentityV9]::IdOrAbsent($ReportPath)
    # Protected inputs: the package, the receipt given, and the conventional receipt beside the package even when it is
    # not given. An existing report destination is compared by file identity (volume and file index), so another
    # spelling, a short name or a hard link of a protected input is refused too.
    # The package is also protected as the file actually read (R8-F2, R9-F3): the snapshot read below returns, from the
    # same handle that reads the bytes, the file's physical path (a file symbolic link or junction resolved to its target)
    # and every hard-link name, and each of them with the conventional receipt beside it is protected before the report is
    # written, so a link changed before the read cannot leave the read ZIP's receipt unprotected. A file with one link
    # needs no enumeration (R9-F6); when the names of a linked file cannot be established, no report is written.
    # The same read also identifies the conventional receipts beside those names before any byte is copied (R10-F1), and
    # those identities stay protected through the final write, so a receipt moved to the report destination afterwards
    # is refused by identity.
    # The final write is bound to the checked identity as well (see the end of this script).
    . (Join-Path $PSScriptRoot 'KwrFileIdentity.ps1')
    $protected = @(@($PackagePath, $ReceiptPath, ($PackagePath + '.build.json')) | Where-Object { $_ } | Select-Object -Unique)
    $refuseProtectedReport = {
        param([string[]] $Paths)
        foreach ($inputPath in $Paths) {
            $same = [string]::Equals([IO.Path]::GetFullPath($ReportPath), [IO.Path]::GetFullPath($inputPath), [StringComparison]::OrdinalIgnoreCase)
            if (-not $same -and [IO.File]::Exists($ReportPath) -and [IO.File]::Exists($inputPath)) { $same = [KwrFileIdentityV9]::Of($ReportPath) -ceq [KwrFileIdentityV9]::Of($inputPath) }
            if ($same) { throw "The report path must not be an input of the validation: $inputPath" }
        }
    }
    & $refuseProtectedReport $protected
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
# The identities of the protected inputs are taken when they are read (before and after the snapshot copy), and the final
# report write refuses them as well as the inputs' current identities, so an input moved to the report path after its
# snapshot is still protected.
$identitiesNow = { if ($ReportPath) { [string[]]@($protected | Where-Object { [IO.File]::Exists($_) } | ForEach-Object { [KwrFileIdentityV9]::Of($_) }) } else { [string[]]@() } }
$snapshotIds = @(& $identitiesNow)
. (Join-Path $PSScriptRoot 'KwrFileIdentity.ps1')
# Each supplied input's bytes are read through one open handle together with that handle's identity, so the protected
# identities include the exact files whose bytes were inspected even if a path was replaced between the samples.
$readId = $null; $readFinal = $null; $readNames = $null; $readReceiptIds = $null
$packageBytes = [KwrFileIdentityV9]::ReadAllBound($PackagePath, [bool]$ReportPath, [ref]$readId, [ref]$readFinal, [ref]$readNames, [ref]$readReceiptIds)
$snapshotIds += $readId
$snapshotIds += @($readReceiptIds)
if ($ReportPath) {
    $readProtected = @(@($readFinal, ($readFinal + '.build.json')) + @($readNames | ForEach-Object { $_; $_ + '.build.json' }) | Select-Object -Unique)
    & $refuseProtectedReport $readProtected
    $protected = @(@($protected) + $readProtected | Select-Object -Unique)
    # R10-F1/R12-F1 (round 13): a package reached through a link (more than one hard-link name, or a physical path other
    # than the path given) may have had a receipt moved away from its names before validation first observed the report
    # destination, and such a receipt can no longer be tied to it; its report is written only to a destination that did not
    # exist then. An ordinary single-name package keeps writing over an existing distinct report.
    $linkedInput = @($readNames).Count -gt 1 -or -not [string]::Equals([string]$readFinal, $PackagePath, [StringComparison]::OrdinalIgnoreCase)
    if ($linkedInput -and $reportStartId -cne 'absent') {
        throw "The package $PackagePath is reached through a link (names: $(@($readNames) -join ', ')); its report is written only to a new file, and the report path $ReportPath already existed when validation started."
    }
}
[IO.File]::WriteAllBytes($inspected, $packageBytes)
$receiptBytes = $null
if ($ReceiptPath) { $receiptBytes = [KwrFileIdentityV9]::ReadAll($ReceiptPath, [ref]$readId); $snapshotIds += $readId }
$snapshotIds = @($snapshotIds + @(& $identitiesNow) | Select-Object -Unique)

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
    if (-not $receipt.PSObject.Properties['version'] -or -not ($receipt.version -is [string]) -or [string]$receipt.version -cne [string]$info.Version) {
        throw "Build receipt version does not match the packaged manifest ($($info.Version)); rebuild."
    }
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
        # The recorded commit must exist and its material inputs must be the files present now.
        $previousPreference = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
        try { $null = & git -C $Root cat-file -e ($receipt.commit + '^{commit}') 2>&1; $commitKnown = $LASTEXITCODE -eq 0 } finally { $ErrorActionPreference = $previousPreference }
        if (-not $commitKnown) { throw "Build receipt names a commit this repository does not have ($($receipt.commit)); rebuild." }
        # Material: the fixed build paths plus every tracked repository file the compiler inventory names (the embedded
        # art and any other resource, source or file-valued option). The inventory MSBuild evaluates now is authoritative
        # (R10-F2): its repository paths are always compared, so a receipt that records a tracked source as external (or
        # omits it) cannot remove it from the comparison; paths the receipt's inventory names are compared as well, and
        # the two inventories must then be equal (below).
        $currentInputs = @(Get-KwrCompilerInputs)
        # The embedded artwork is material through the project's own EmbeddedResource patterns (assets/portraits/*/*.png and
        # assets/icons/*.png), not the whole assets folder: a documentation file there does not count, while an image the
        # compiler would embed counts whether Git tracks, ignores or has never seen it.
        # (Earlier wording, kept for the record:) The whole embedded-art tree (assets/) is material, so an added, removed or changed image is detected whether or
        # not the receipt names it, and an untracked file under any material path refuses current qualification.
        $materialPaths = @(@('src', ':(glob,icase)assets/portraits/*/*.png', ':(glob,icase)assets/icons/*.png', 'Info.json', 'LoaderReferences.targets', 'scripts/loader-reference-pins.json', 'scripts/Get-PinnedLoaderReferences.ps1', 'scripts/Build.ps1', 'scripts/KwrBuild.Common.ps1', 'scripts/KwrFileIdentity.ps1') + @(@(Get-KwrInventoryRepositoryPaths $currentInputs) + @(Get-KwrInventoryRepositoryPaths $receipt.compilerInputs) | ForEach-Object { ':(literal)' + $_ }) | Select-Object -Unique)
        $untrackedMaterial = @(& git -C $Root ls-files --others --exclude-standard -- @materialPaths)
        if ($untrackedMaterial.Count -gt 0) { throw "Untracked files under the material inputs ($($untrackedMaterial -join ', ')); rebuild from a clean commit." }
        # Windows matches the EmbeddedResource patterns in any letter case (LocalOnly.PNG is embedded), so Git matches with icase.
        $untrackedArt = @(& git -C $Root ls-files --others -- ':(glob,icase)assets/portraits/*/*.png' ':(glob,icase)assets/icons/*.png')
        if ($LASTEXITCODE -ne 0) { throw 'git ls-files failed while checking the embedded artwork.' }
        if ($untrackedArt.Count -gt 0) { throw "Untracked or ignored files match the embedded artwork ($($untrackedArt -join ', ')); rebuild from a clean commit." }
        $previousPreference = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
        try { $null = & git -C $Root diff --quiet $receipt.commit -- @materialPaths 2>&1; $materialSame = $LASTEXITCODE -eq 0 } finally { $ErrorActionPreference = $previousPreference }
        if (-not $materialSame) { throw "Build inputs changed since this package was built: the material inputs differ from the receipt's commit $($receipt.commit); rebuild." }
        if (@($receipt.compilerInputs | Where-Object { [string]$_.sha256 -ceq 'absent' }).Count -ne 0) { throw 'Build receipt has an unidentified compiler input; rebuild.' }
        # Every repository-authored source the compiler reads must be tracked, whatever the ignore rules say (the project's
        # **\*.cs item compiles an ignored file under a nested obj folder). Generated inputs (the intermediate folder,
        # *.AssemblyAttributes.cs) and files outside the repository keep their existing handling.
        # Membership is decided on each compile entry's physical repository path (not its reduced name or the spelling that
        # selected it), resolved to Git's tracked spelling in its lossless listing of tracked files.
        $spellings = Get-KwrTrackedSpellings
        $untrackedSources = @($currentInputs | Where-Object { [string]$_.kind -ceq 'compile' -and -not [string]::IsNullOrEmpty([string]$_.repositoryPath) } |
            Where-Object { -not (Resolve-KwrTrackedSpelling $spellings ([string]$_.repositoryPath)) } | ForEach-Object { [string]$_.repositoryPath })
        if ($untrackedSources.Count -gt 0) { throw "Compiler sources are not tracked by Git ($($untrackedSources -join ', ')); rebuild from a clean commit." }
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
if ($ReportPath) {
    # Bound write: the protected identities are read now, and the destination is opened once, checked through that handle
    # (not a protected identity, and still the destination present when validation started) and only then truncated and
    # written, so a destination replaced during validation is refused too.
    $protectedIds = [string[]]@(@($snapshotIds) + @(& $identitiesNow) | Where-Object { $_ } | Select-Object -Unique)
    $reportText = $result | ConvertTo-Json -Depth 5
    $reportBytes = [byte[]](@([Text.Encoding]::UTF8.GetPreamble()) + @([Text.Encoding]::UTF8.GetBytes($reportText + "`r`n")))
    [void][KwrFileIdentityV9]::WriteChecked($ReportPath, $protectedIds, $reportStartId, $reportBytes)
}
Write-Host "Package validation passed: $PackagePath"
Write-Host "Package SHA-256: $packageSha256; DLL SHA-256: $dllSha256; MVID: $mvid; loader references: $(($observed | ForEach-Object { $_.name + ' ' + $_.version }) -join ', '); source provenance: $provenance"
}
finally { if ([IO.Directory]::Exists($privateDirectory)) { [IO.Directory]::Delete($privateDirectory, $true) } }
