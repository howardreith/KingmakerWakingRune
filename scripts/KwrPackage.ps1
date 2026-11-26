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
    $source = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($PackagePath)
    if (-not [IO.File]::Exists($source)) { throw "Package not found: $source" }
    $private = Join-Path ([IO.Path]::GetTempPath()) ('KingmakerWakingRune-package-' + [Guid]::NewGuid().ToString('N'))
    # A package at a build-owned destination (this repository's artifacts folder) is copied with its receipt under that
    # destination's publish lock, so the pair read is one published pair; while a build is publishing there the read is
    # declined. A ZIP elsewhere is copied without a lock (content inspection only when it has no receipt).
    $buildOwned = $source.StartsWith(([IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\artifacts')).TrimEnd('\') + '\'), [StringComparison]::OrdinalIgnoreCase)
    try {
        [void][IO.Directory]::CreateDirectory($private)
        $copy = Join-Path $private 'package.zip'
        $report = Join-Path $private 'validation.json'
        $receiptSource = $source + '.build.json'
        $publishLock = $null
        if ($buildOwned) {
            try { $publishLock = [IO.FileStream]::new(($source + '.publish.lock'), [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None, 1, [IO.FileOptions]::DeleteOnClose) }
            catch [IO.IOException] { throw "A build is publishing $source; try again after it finishes." }
        }
        try {
            [IO.File]::Copy($source, $copy)
            $hasReceipt = [IO.File]::Exists($receiptSource)
            if ($hasReceipt) { [IO.File]::Copy($receiptSource, $copy + '.build.json') }
        }
        finally { if ($null -ne $publishLock) { $publishLock.Dispose() } }
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
