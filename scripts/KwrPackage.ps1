# Package copy and staging used by KwrRuntime.ps1 (Begin and Reinstall). Kept separate so it can be exercised on a
# temporary filesystem destination without touching the game.
Set-StrictMode -Version Latest

# Resolves the caller's package once (PowerShell literal path semantics), copies it (and the build receipt beside it,
# when present) into a private directory, validates that copy with the standalone validator and returns the identities
# of the validated bytes. A receipt makes this the frozen-candidate path: the receipt must also name a clean producing
# commit and the current build inputs. The private directory is removed on any failure.
function New-KwrValidatedPackageCopy([string] $PackagePath) {
    $source = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($PackagePath)
    if (-not [IO.File]::Exists($source)) { throw "Package not found: $source" }
    $private = Join-Path ([IO.Path]::GetTempPath()) ('KingmakerWakingRune-package-' + [Guid]::NewGuid().ToString('N'))
    try {
        [void][IO.Directory]::CreateDirectory($private)
        $copy = Join-Path $private 'package.zip'
        [IO.File]::Copy($source, $copy)
        $report = Join-Path $private 'validation.json'
        $receiptSource = $source + '.build.json'
        if ([IO.File]::Exists($receiptSource)) {
            [IO.File]::Copy($receiptSource, $copy + '.build.json')
            & (Join-Path $PSScriptRoot 'Validate-Package.ps1') -PackagePath $copy -ReceiptPath ($copy + '.build.json') -RequireCurrentProvenance -ReportPath $report | Out-Host
        }
        else { & (Join-Path $PSScriptRoot 'Validate-Package.ps1') -PackagePath $copy -ReportPath $report | Out-Host }
        $validation = Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
        if ((Get-FileHash -LiteralPath $copy -Algorithm SHA256).Hash.ToLowerInvariant() -cne [string]$validation.package_sha256) { throw 'The validated copy changed after validation.' }
        return [pscustomobject]@{
            Source = $source
            Copy = $copy
            PrivateDirectory = $private
            Sha256 = [string]$validation.package_sha256
            DllSha256 = [string]$validation.dll_sha256
            SourceProvenance = [string]$validation.source_provenance
        }
    }
    catch {
        if ([IO.Directory]::Exists($private)) { [IO.Directory]::Delete($private, $true) }
        throw
    }
}

# Extracts exactly the validated copy into the destination root; the extracted DLL must be the validated DLL.
function Install-KwrValidatedPackageCopy($Validated, [string] $DestinationRoot) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [IO.Compression.ZipFile]::ExtractToDirectory($Validated.Copy, $DestinationRoot)
    $dll = Join-Path $DestinationRoot 'KingmakerWakingRune\KingmakerWakingRune.dll'
    if (-not [IO.File]::Exists($dll)) { throw 'Package did not install the expected mod folder.' }
    if ((Get-FileHash -LiteralPath $dll -Algorithm SHA256).Hash.ToLowerInvariant() -cne $Validated.DllSha256) { throw 'Installed DLL differs from the validated package.' }
}

function Remove-KwrValidatedPackageCopy($Validated) {
    if ($null -eq $Validated) { return }
    $private = [string]$Validated.PrivateDirectory
    if ((Split-Path -Leaf $private) -like 'KingmakerWakingRune-package-*' -and [IO.Directory]::Exists($private)) { [IO.Directory]::Delete($private, $true) }
}
