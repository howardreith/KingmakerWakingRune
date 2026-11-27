# Package copy and staging used by KwrRuntime.ps1 (Begin and Reinstall). Kept separate so it can be exercised on a
# temporary filesystem destination without touching the game.
Set-StrictMode -Version Latest

# Resolves the caller's package once (PowerShell literal path semantics), copies it (and the build receipt beside it,
# when present) into a private directory, validates that copy with the standalone validator and returns the identities
# of the validated bytes. A receipt makes this the frozen-candidate path: the receipt must also name a clean producing
# commit and the current build inputs. A ZIP without a receipt is staged as external-content-inspected. The private
# directory is removed on any failure.
# -Diagnostic: a receipt-backed diagnostic build (for example a dirty working tree) is staged with the ZIP, DLL and pin
# checks and the receipt matched to the package, without the frozen-candidate provenance requirement.
function New-KwrValidatedPackageCopy([string] $PackagePath, [switch] $Diagnostic) {
    $requested = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($PackagePath)
    if (-not [IO.File]::Exists($requested)) { throw "Package not found: $requested" }
    . (Join-Path $PSScriptRoot 'KwrFileIdentity.ps1')
    $private = Join-Path ([IO.Path]::GetTempPath()) ('KingmakerWakingRune-package-' + [Guid]::NewGuid().ToString('N'))
    # Ownership is a property of the file actually read, not of the path used to name it (R5-F1). The ZIP is opened once,
    # without write sharing (its bytes cannot change while the handle is open), and everything that classifies it is
    # observed through that handle: its identity, its physical path (a file symbolic link or junction resolved to its
    # target), its hard-link count and every hard-link name; the private copy is read through the same handle.
    # A file one of whose names lies in this repository's artifacts folder (recognized by the physical identity of the
    # folder) is build-owned: it is read only under that name's publish lock and with that name's receipt (the canonical
    # pair), whatever path the caller used and whatever receipt lies beside it. Under the lock the canonical name must
    # still be the file this handle reads, and the copy and the receipt are taken while the lock is held, so a publisher
    # cannot place or roll back a pair between the classification and the read (another name of the file can still be
    # removed while the handle is open; the lock, not the handle, excludes the publisher). While a build is publishing
    # there the read is declined, and a build-owned ZIP without its canonical receipt is declined too. A ZIP no name of which is
    # build-owned but which has other hard-link names is declined as an ambiguous linked input. An independent file (one
    # name, outside the artifacts folder) is copied with the receipt beside its physical path when present (the
    # frozen-candidate path) or inspected by content only.
    $artifactsDirectory = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\artifacts'))
    $artifactsId = if ([IO.Directory]::Exists($artifactsDirectory)) { [KwrFileIdentityV5]::DirectoryOf($artifactsDirectory) } else { $null }
    $isBuildOwnedName = {
        param([string] $Name)
        if ($null -eq $artifactsId) { return $false }
        $directory = [IO.Path]::GetDirectoryName($Name)
        while ($directory) {
            if ([IO.Directory]::Exists($directory) -and [KwrFileIdentityV5]::DirectoryOf($directory) -ceq $artifactsId) { return $true }
            $parent = [IO.Path]::GetDirectoryName($directory)
            if (-not $parent -or $parent -eq $directory) { break }
            $directory = $parent
        }
        return $false
    }
    try {
        [void][IO.Directory]::CreateDirectory($private)
        $copy = Join-Path $private 'package.zip'
        $report = Join-Path $private 'validation.json'
        $publishLock = $null
        $stream = [KwrFileIdentityV5]::OpenRead($requested)
        try {
            $held = [KwrFileIdentityV5]::InfoOf($stream)
            $names = @([KwrFileIdentityV5]::LinkNamesOf($held.FinalPath))
            if ($names.Count -ne [int]$held.LinkCount) { throw "The package $requested reports $($held.LinkCount) hard-link names but $($names.Count) were listed; its ownership cannot be established, so it is not installed." }
            $owned = @($names | Where-Object { & $isBuildOwnedName $_ })
            if ($owned.Count -gt 1) { throw "The package $requested has more than one name in the build-owned folder ($($owned -join ', ')); its publication cannot be established, so it is not installed." }
            $buildOwned = $owned.Count -eq 1
            if ($buildOwned) {
                $source = [KwrFileIdentityV5]::LongPathOf($owned[0])
                try { $publishLock = [IO.FileStream]::new(($source + '.publish.lock'), [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None, 1, [IO.FileOptions]::DeleteOnClose) }
                catch [IO.IOException] { throw "A build is publishing $source; try again after it finishes." }
                # Under the lock, the canonical name must still be the file this handle reads.
                if (-not [IO.File]::Exists($source) -or [KwrFileIdentityV5]::Of($source) -cne $held.Id) { throw "The build-owned package $source was replaced while it was being read; it is not installed." }
            }
            elseif ([int]$held.LinkCount -gt 1) {
                throw "The package $requested is a file link (another hard-link name exists: $(@($names) -join ', ')); its publication cannot be established, so it is not installed. Use an independent copy."
            }
            else { $source = $held.FinalPath }
            $receiptSource = $source + '.build.json'
            $out = [IO.File]::Open($copy, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
            try { $stream.CopyTo($out) } finally { $out.Dispose() }
            $hasReceipt = [IO.File]::Exists($receiptSource)
            if ($hasReceipt) { [IO.File]::Copy($receiptSource, $copy + '.build.json') }
        }
        finally {
            $stream.Dispose()
            if ($null -ne $publishLock) { $publishLock.Dispose() }
        }
        if ($buildOwned -and -not $hasReceipt) { throw "A build-owned package has no build receipt ($source); it is not installed. Rebuild it." }
        if ($hasReceipt) {
            if ($Diagnostic) { & (Join-Path $PSScriptRoot 'Validate-Package.ps1') -PackagePath $copy -ReceiptPath ($copy + '.build.json') -ReportPath $report | Out-Host }
            else { & (Join-Path $PSScriptRoot 'Validate-Package.ps1') -PackagePath $copy -ReceiptPath ($copy + '.build.json') -RequireCurrentProvenance -ReportPath $report | Out-Host }
        }
        else { & (Join-Path $PSScriptRoot 'Validate-Package.ps1') -PackagePath $copy -ReportPath $report | Out-Host }
        $validation = Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
        if ((Get-FileHash -LiteralPath $copy -Algorithm SHA256).Hash.ToLowerInvariant() -cne [string]$validation.package_sha256) { throw 'The validated copy changed after validation.' }
        # The recorded qualification follows from what the validator established: a clean pair with current provenance is
        # a frozen candidate; a receipt matched to its package under -Diagnostic is a diagnostic build; a ZIP without a
        # receipt had its contents inspected only.
        $installMode = switch ([string]$validation.source_provenance) {
            'verified-current' { 'frozen-candidate' }
            'receipt-matches-package' { if ($Diagnostic) { 'diagnostic' } else { throw 'A receipt-backed package was not verified as current.' } }
            'not-checked' { 'external-content-inspected' }
            default { throw "Unknown validator provenance: $($validation.source_provenance)" }
        }
        return [pscustomobject]@{
            Source = $source
            Copy = $copy
            PrivateDirectory = $private
            Sha256 = [string]$validation.package_sha256
            DllSha256 = [string]$validation.dll_sha256
            SourceProvenance = [string]$validation.source_provenance
            InstallMode = $installMode
        }
    }
    catch {
        if ([IO.Directory]::Exists($private)) { [IO.Directory]::Delete($private, $true) }
        throw
    }
}

# Extracts exactly the validated copy into a staging folder beside the private copy and verifies it (the DLL must be the
# validated DLL) before anything is installed; returns the staged mod folder.
function Expand-KwrValidatedPackageCopy($Validated) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $staged = Join-Path $Validated.PrivateDirectory 'staged'
    [IO.Compression.ZipFile]::ExtractToDirectory($Validated.Copy, $staged)
    $mod = Join-Path $staged 'KingmakerWakingRune'
    $dll = Join-Path $mod 'KingmakerWakingRune.dll'
    if (-not [IO.File]::Exists($dll) -or -not [IO.File]::Exists((Join-Path $mod 'Info.json'))) { throw 'Package did not stage the expected mod folder.' }
    if ((Get-FileHash -LiteralPath $dll -Algorithm SHA256).Hash.ToLowerInvariant() -cne $Validated.DllSha256) { throw 'Staged DLL differs from the validated package.' }
    return $mod
}

# Installs the verified staged folder as <DestinationRoot>\KingmakerWakingRune (the destination must not exist).
function Install-KwrValidatedPackageCopy($Validated, [string] $DestinationRoot) {
    $mod = Expand-KwrValidatedPackageCopy $Validated
    $target = Join-Path $DestinationRoot 'KingmakerWakingRune'
    if (Test-Path -LiteralPath $target) { throw "Install target already exists: $target" }
    Move-Item -LiteralPath $mod -Destination $target
    if ((Get-FileHash -LiteralPath (Join-Path $target 'KingmakerWakingRune.dll') -Algorithm SHA256).Hash.ToLowerInvariant() -cne $Validated.DllSha256) { throw 'Installed DLL differs from the validated package.' }
}

function Remove-KwrValidatedPackageCopy($Validated) {
    if ($null -eq $Validated) { return }
    $private = [string]$Validated.PrivateDirectory
    if ((Split-Path -Leaf $private) -like 'KingmakerWakingRune-package-*' -and [IO.Directory]::Exists($private)) { [IO.Directory]::Delete($private, $true) }
}
