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
    # Ownership is a property of the file actually read, not of the path used to name it (R5-F1). It is classified from
    # the file's identity, physical path (a file symbolic link or junction resolved to its target), hard-link count and
    # hard-link names (enumerated only when the file has more than one link, R9-F6), observed through short-lived handles
    # that share read, write and delete access, so a reader that is still classifying never holds the ZIP in a way that
    # could stop a publisher from moving it during placement or rollback (R9-F2).
    # A name directly in this repository's artifacts folder (recognized by the physical identity of the folder) is a final
    # publication name; a name anywhere below that folder (the publisher's .pending-* staging and its 'previous' backup) is
    # private to a publisher, and a file with such a name is declined. A file with a final name is read only under that
    # name's publish lock and with that name's receipt (the canonical pair), whatever path the caller used and whatever
    # receipt lies beside it: the ZIP is opened only after the lock is held, and under the lock the canonical name must
    # still be the file classified (same identity and names), so a publisher cannot place or roll back a pair between the
    # classification and the read. While a build is publishing there the read is declined, and a build-owned ZIP without
    # its canonical receipt is declined too. A file with other hard-link names and no build-owned name is declined as an
    # ambiguous linked input. An independent file (one name, outside the artifacts folder) is copied with the receipt
    # beside its physical path when present (the frozen-candidate path) or inspected by content only.
    # A file classified as external is opened sharing read and delete (R9-F2, round 11): until the recheck below confirms
    # that the handle holds the file classified, it may be any file the path now names (a directory junction redirected to
    # the artifacts folder after classification, for example), so it must never stop a publisher from renaming that file
    # during placement or rollback; a handle that now names a build-owned or publisher-private file is refused.
    $artifactsDirectory = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\artifacts'))
    $artifactsId = if ([IO.Directory]::Exists($artifactsDirectory)) { [KwrFileIdentityV9]::DirectoryOf($artifactsDirectory) } else { $null }
    $classOf = {
        param([string] $Name)
        if ($null -eq $artifactsId) { return 'external' }
        $directory = [IO.Path]::GetDirectoryName($Name)
        $depth = 0
        while ($directory) {
            if ([IO.Directory]::Exists($directory)) {
                # A folder whose identity cannot be read (for example the volume's \$Extend folder that holds a file deleted
                # while a handle is open) leaves the file's ownership unknown: declined, not guessed.
                try { $directoryId = [KwrFileIdentityV9]::DirectoryOf($directory) }
                catch { throw "The package $requested lies in a folder whose identity cannot be read ($directory); its publication cannot be established, so it is not installed." }
                if ($directoryId -ceq $artifactsId) { if ($depth -eq 0) { return 'final' } else { return 'private' } }
            }
            $parent = [IO.Path]::GetDirectoryName($directory)
            if (-not $parent -or $parent -eq $directory) { break }
            $directory = $parent; $depth++
        }
        return 'external'
    }
    $namesOf = {
        param([string] $FinalPath, [uint32] $Count)
        if ($Count -le 1) { return , @($FinalPath) }
        $listed = @([KwrFileIdentityV9]::LinkNamesOf($FinalPath))
        if ($listed.Count -ne [int]$Count) { throw "The package $requested reports $Count hard-link names but $($listed.Count) were listed; its ownership cannot be established, so it is not installed." }
        return , $listed
    }
    $classify = {
        param([string] $FinalPath, [uint32] $Count)
        $all = & $namesOf $FinalPath $Count
        $privateNames = @($all | Where-Object { (& $classOf $_) -ceq 'private' })
        if ($privateNames.Count -ne 0) { throw "The package $requested is a file in a publisher's private staging or backup area ($($privateNames -join ', ')); its publication cannot be established, so it is not installed." }
        $finalNames = @($all | Where-Object { (& $classOf $_) -ceq 'final' })
        if ($finalNames.Count -gt 1) { throw "The package $requested has more than one name in the build-owned folder ($($finalNames -join ', ')); its publication cannot be established, so it is not installed." }
        if ($finalNames.Count -eq 0 -and [int]$Count -gt 1) { throw "The package $requested is a file link (another hard-link name exists: $($all -join ', ')); its publication cannot be established, so it is not installed. Use an independent copy." }
        return [pscustomobject]@{ Names = $all; Final = $(if ($finalNames.Count -eq 1) { $finalNames[0] } else { $null }) }
    }
    try {
        [void][IO.Directory]::CreateDirectory($private)
        $copy = Join-Path $private 'package.zip'
        $report = Join-Path $private 'validation.json'
        $publishLock = $null
        $stream = $null
        $receiptHandle = $null
        $classifiedId = [KwrFileIdentityV9]::Of($requested)
        $classified = & $classify ([KwrFileIdentityV9]::FinalPathOf($requested)) ([KwrFileIdentityV9]::LinkCountOf($requested))
        try {
            $buildOwned = $null -ne $classified.Final
            if ($buildOwned) {
                $source = [KwrFileIdentityV9]::LongPathOf($classified.Final)
                try { $publishLock = [IO.FileStream]::new(($source + '.publish.lock'), [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None, 1, [IO.FileOptions]::DeleteOnClose) }
                catch [IO.IOException] { throw "A build is publishing $source; try again after it finishes." }
                # Under the lock: the canonical name is opened and must still be the file classified, with the same names.
                if (-not [IO.File]::Exists($source)) { throw "The build-owned package $source was replaced while it was being read; it is not installed." }
                $stream = [KwrFileIdentityV9]::OpenRead($source)
            }
            else { $stream = [KwrFileIdentityV9]::OpenReadShared($requested) }
            $held = [KwrFileIdentityV9]::InfoOf($stream)
            # The handle must hold the file classified (its identity first, so a different file - also one a publisher has
            # moved or deleted meanwhile - is refused as such), with the same build-owned name and hard-link names.
            if ($held.Id -cne $classifiedId) { throw "The package $requested was replaced or relinked while it was being read; it is not installed." }
            $recheck = & $classify $held.FinalPath $held.LinkCount
            if ([string]$recheck.Final -cne [string]$classified.Final -or (@($recheck.Names | Sort-Object) -join '|') -ine (@($classified.Names | Sort-Object) -join '|')) {
                throw "The package $requested was replaced or relinked while it was being read; it is not installed."
            }
            if (-not $buildOwned) { $source = $held.FinalPath }
            $receiptSource = $source + '.build.json'
            # R14-F1 (round 15): the selected pair is the ZIP held above and the receipt beside its physical path, both
            # observed as part of the selection. The receipt is opened (sharing read and delete, like an external ZIP, so a
            # publisher's rename is never obstructed) right after the ZIP handle was confirmed, and its file identity is
            # recorded with the ZIP's. Its bytes are read later from that same handle, so the receipt validated is the
            # receipt selected, whatever its name points to meanwhile. The pair is in place only while the ZIP's physical
            # path names the held ZIP and the receipt name names the held receipt (or is still absent, R11-F2). That is
            # checked when the selection is made, after the ZIP and receipt bytes are copied, and again after validation,
            # before any provenance is granted: a temporary pair that supplied the receipt cannot pass once the original pair
            # is back (the receipt name then names another file), nor while it stays (the ZIP path names another file). Only
            # the selected ZIP and its adjacent receipt decide; other entries of the folder do not (R13-F1). A build-owned
            # pair is read under its publish lock instead (the lock, not these checks, keeps that pair together).
            try { $receiptHandle = [KwrFileIdentityV9]::OpenReadShared($receiptSource) }
            catch [IO.FileNotFoundException] { $receiptHandle = $null }
            catch [IO.DirectoryNotFoundException] { $receiptHandle = $null }
            $receiptId = if ($null -ne $receiptHandle) { [KwrFileIdentityV9]::InfoOf($receiptHandle).Id } else { 'absent' }
            $pairInPlace = {
                if ($buildOwned) { return }
                $now = $null
                try { if ([IO.File]::Exists($source)) { $now = [KwrFileIdentityV9]::Of($source) } } catch [IO.IOException] { $now = $null }
                if ($now -cne $held.Id) { throw "The package $requested moved while it was being read; it is not installed." }
                $receiptNow = [KwrFileIdentityV9]::IdOrAbsent($receiptSource)
                if ($receiptNow -ceq $receiptId) { return }
                if ($receiptId -ceq 'absent') { throw "A build receipt appeared beside the package $requested after its receipt was looked for ($receiptSource); which receipt belongs to the bytes read cannot be established, so it is not installed." }
                throw "The build receipt beside the package $requested ($receiptSource) is no longer the receipt selected with it; the receipt read cannot be tied to the selected package, so it is not installed."
            }
            & $pairInPlace
            $out = [IO.File]::Open($copy, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
            try { $stream.CopyTo($out) } finally { $out.Dispose() }
            $receiptBytes = $null
            if ($null -ne $receiptHandle) { $receiptBuffer = [IO.MemoryStream]::new(); $receiptHandle.CopyTo($receiptBuffer); $receiptBytes = $receiptBuffer.ToArray() }
            $hasReceipt = $null -ne $receiptBytes
            if ($hasReceipt) { [IO.File]::WriteAllBytes($copy + '.build.json', $receiptBytes) }
            $receiptSha = if ($hasReceipt) { [BitConverter]::ToString([Security.Cryptography.SHA256]::Create().ComputeHash($receiptBytes)).Replace('-', '') } else { $null }
            & $pairInPlace
        }
        finally {
            if ($null -ne $receiptHandle) { $receiptHandle.Dispose() }
            if ($null -ne $stream) { $stream.Dispose() }
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
        # R14-F1 (round 15), R11-F2: before any qualification is granted, the receipt validated must still be the bytes read
        # from the selected receipt, and the selected pair must still be in place (the held ZIP at its physical path, the
        # receipt name naming the receipt read, or still absent when none was there).
        if ($hasReceipt -and (Get-FileHash -LiteralPath ($copy + '.build.json') -Algorithm SHA256).Hash -cne $receiptSha) { throw 'The validated receipt copy changed after validation.' }
        & $pairInPlace
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
