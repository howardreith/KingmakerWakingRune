<#
.SYNOPSIS
  Failure-boundary tests for the pinned UMM 0.31.1 / Harmony12 1.2.0.1 / Harmony 2.3.1.1 compile baseline.
.DESCRIPTION
  Runs on the committed HEAD in a temporary clone whose path contains a space: the clone is built with scripts\Build.ps1,
  and every check runs in its own PowerShell or MSBuild process, so this repository's artifacts and the installed game
  are never written. Fixtures are downloaded (or supplied) once, hash-checked, and only read through private copies.
#>
[CmdletBinding()]
param(
    [string] $LegacyReleaseZip,
    [string] $WrongLoaderPackage,
    [string] $WrongHarmonyPackage,
    [string] $ReportPath
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$source = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

# Genuine published prerelease, compiled against UnityModManager 0.33.2.0 and 0Harmony 2.3.6.0 (manifest 0.28.2).
$legacyUrl = 'https://github.com/howardreith/KingmakerWakingRune/releases/download/v0.12.3/KingmakerWakingRune-0.12.3.zip'
$legacySha256 = '4dec04872820942797e8e38b360ae8cb44ee31cb0b321791e7fb7da64593b402'
# Known distinct fixtures (never the owner's install): the author's UnityModManager 0.32.4 and Lib.Harmony 2.3.6 (net48).
$wrongLoaderUrl = 'https://api.nuget.org/v3-flatcontainer/unitymodmanager/0.32.4/unitymodmanager.0.32.4.nupkg'
$wrongLoaderPackageSha256 = 'a8438e921ca7b0e5805f5a31c196ae65c90f236543885bea0146457d082c9ec9'
$wrongLoaderDllSha256 = '1387468bc3af41c50fe51859a3bb7af4922891aa8f13a6187e7a348ceaabfd88'
$wrongHarmonyUrl = 'https://api.nuget.org/v3-flatcontainer/lib.harmony/2.3.6/lib.harmony.2.3.6.nupkg'
$wrongHarmonyPackageSha256 = '498f99c5ca97cfec0e6cb277d721c242071ea9d9f019312cf9eaf015762de075'
$wrongHarmonyDllSha256 = 'df043f05c6e43602fa8e8f38f52a3a2cf962aab7a65a9d85573ea25f01a63e80'
$modFolder = 'KingmakerWakingRune'
$pinFiles = @('UnityModManager.dll', '0Harmony12.dll', '0Harmony.dll')

function Get-Sha256([string] $Path) { return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant() }
function Quote([string] $Value) { return "'" + $Value.Replace("'", "''") + "'" }
$commit = (& git -C $source rev-parse HEAD).Trim()
if ([bool](& git -C $source status --porcelain)) { Write-Host "NOTE: uncommitted changes are not tested; the suite tests committed HEAD $commit." }
$sourceArtifactsBefore = @(Get-ChildItem -LiteralPath (Join-Path $source 'artifacts') -File -ErrorAction SilentlyContinue | ForEach-Object { $_.Name + '=' + (Get-Sha256 $_.FullName) }) -join ';'
$vswhere = 'C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe'
$msbuild = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (-not $msbuild) { throw 'MSBuild not found (Visual Studio Build Tools required).' }
[xml] $paths = Get-Content -Raw -LiteralPath (Join-Path $source 'LocalGamePaths.props')
$installDir = [string](@($paths.Project.PropertyGroup) | Where-Object { $_.KingmakerInstallDir } | Select-Object -First 1).KingmakerInstallDir
$managedDir = Join-Path $installDir 'Kingmaker_Data\Managed'
$installedLoader = Join-Path $managedDir 'UnityModManager\UnityModManager.dll'
$temp = Join-Path ([IO.Path]::GetTempPath()) ('kwr-loader-baseline-' + [Guid]::NewGuid().ToString('N'))
$base = Join-Path $temp 'clone with space'
$inputs = Join-Path $temp 'inputs'
New-Item -ItemType Directory -Path $inputs -Force | Out-Null
$cases = [Collections.Generic.List[object]]::new()

# Each check runs in a fresh Windows PowerShell or MSBuild process with an empty stdin; output records are converted one
# by one, so messages are never re-wrapped at console width.
function ConvertTo-PlainText($Records) { return (@($Records | ForEach-Object { "$_" }) -join "`n") }
function Invoke-Isolated([string[]] $Arguments) {
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try { $records = $null | & powershell.exe -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass @Arguments 2>&1 }
    finally { $ErrorActionPreference = $previous }
    return [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = (ConvertTo-PlainText $records) }
}
function Invoke-Script([string] $Text) {
    $wrapped = "try { $Text } catch { [Console]::Out.WriteLine('ERROR: ' + `$_.Exception.Message); exit 1 }"
    return Invoke-Isolated @('-EncodedCommand', [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($wrapped)))
}
function Invoke-MSBuildIsolated([string[]] $Arguments) {
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try { $output = ConvertTo-PlainText ($null | & $msbuild /nologo /v:m /nr:false @Arguments 2>&1) }
    finally { $ErrorActionPreference = $previous }
    return [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = $output }
}
function Add-Case([string] $Name, [bool] $Passed, [string] $Detail) {
    $cases.Add([ordered]@{ name = $Name; passed = $Passed; detail = $Detail })
    if ($Passed) { Write-Host "PASS $Name :: $Detail" } else { Write-Host "FAIL $Name :: $Detail" }
}
function Get-Reason([string] $Output) {
    $line = @($Output -split "`n" | Where-Object { $_ -match '^ERROR: |: error |must|mismatch|missing|does not|Unexpected|exactly|refused|rebuild' -and $_ -notmatch '^PASS ' } | Select-Object -First 1)
    if ($line) { return $line[0].Trim() } else { return ($Output.Trim() -split "`n" | Select-Object -Last 1) }
}
function Expect-Rejected([string] $Name, $Result, [string] $Pattern) {
    $ok = $Result.ExitCode -ne 0 -and ($Result.Output -replace '\s+', ' ') -match $Pattern
    $detail = if ($ok) { 'rejected: ' + (Get-Reason $Result.Output) } else { "exit $($Result.ExitCode); expected /$Pattern/; output: $($Result.Output.Trim())" }
    Add-Case $Name $ok $detail
}
function Expect-Accepted([string] $Name, $Result, [string] $Pattern) {
    $ok = $Result.ExitCode -eq 0 -and (-not $Pattern -or ($Result.Output -replace '\s+', ' ') -match $Pattern)
    $detail = if ($ok -and $Pattern) { 'accepted: ' + [regex]::Match(($Result.Output -replace '\s+', ' '), $Pattern).Value } elseif ($ok) { 'accepted' } else { "exit $($Result.ExitCode); output: $($Result.Output.Trim())" }
    Add-Case $Name $ok $detail
}
function Copy-Pins([string] $From, [string] $Destination) {
    New-Item -ItemType Directory -Force -Path $Destination | Out-Null
    foreach ($file in $pinFiles) { Copy-Item -LiteralPath (Join-Path $From $file) -Destination $Destination }
}
# The boundary suite's alternate product version: the next patch version after the shipping one, so it never equals it.
function Get-AlternateFixtureVersion([string] $Shipping) {
    if ($Shipping -cnotmatch '\A([0-9]+)\.([0-9]+)\.([0-9]+)\z') { throw "Unexpected shipping version: $Shipping" }
    return '{0}.{1}.{2}' -f $Matches[1], $Matches[2], ([int]$Matches[3] + 1)
}
function Edit-Json([string] $Path, [scriptblock] $Edit) {
    $j = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
    & $Edit $j
    $j | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $Path -Encoding UTF8
}
function Edit-Zip([string] $From, [string] $Destination, [scriptblock] $Edit) {
    Copy-Item -LiteralPath $From -Destination $Destination
    $archive = [IO.Compression.ZipFile]::Open($Destination, [IO.Compression.ZipArchiveMode]::Update)
    try { & $Edit $archive } finally { $archive.Dispose() }
}
function Set-ZipEntryFile($Archive, [string] $Name, [string] $Path) {
    $existing = $Archive.GetEntry($Name)
    if ($existing) { $existing.Delete() }
    [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($Archive, $Path, $Name) | Out-Null
}
function Get-ZipEntryText($Archive, [string] $Name) {
    $reader = [IO.StreamReader]::new($Archive.GetEntry($Name).Open(), [Text.Encoding]::UTF8, $true)
    try { return $reader.ReadToEnd() } finally { $reader.Dispose() }
}
function Set-ZipEntryText($Archive, [string] $Name, [string] $Text) {
    $Archive.GetEntry($Name).Delete()
    $writer = [IO.StreamWriter]::new($Archive.CreateEntry($Name).Open(), [Text.UTF8Encoding]::new($false))
    try { $writer.Write($Text) } finally { $writer.Dispose() }
}
function Get-CompilerReferences([string] $Output, [string] $File) {
    $line = @($Output -split "`n" | Where-Object { $_ -match 'csc\.exe' }) -join "`n"
    return @([regex]::Matches($line, '/reference:(?:"([^"]+)"|(\S+))') | ForEach-Object {
        if ($_.Groups[1].Success) { $_.Groups[1].Value } else { $_.Groups[2].Value } } | Where-Object { [IO.Path]::GetFileName($_) -ieq $File })
}
function Get-Fixture([string] $Supplied, [string] $Url, [string] $Name, [string] $Sha) {
    $path = Join-Path $inputs $Name
    if ($Supplied) { Copy-Item -LiteralPath $Supplied -Destination $path } else { Invoke-WebRequest -Uri $Url -OutFile $path -UseBasicParsing }
    if ((Get-Sha256 $path) -ne $Sha) { throw "Fixture hash mismatch: $Name" }
    return $path
}
function Expand-Entry([string] $Zip, [string] $Entry, [string] $Destination, [string] $Sha) {
    $archive = [IO.Compression.ZipFile]::OpenRead($Zip)
    try { [IO.Compression.ZipFileExtensions]::ExtractToFile($archive.GetEntry($Entry), $Destination) } finally { $archive.Dispose() }
    if ($Sha -and (Get-Sha256 $Destination) -ne $Sha) { throw "Fixture entry hash mismatch: $Entry" }
}

try {
    # The repository is private: without -LegacyReleaseZip the release asset is fetched through the authenticated gh CLI.
    if ($LegacyReleaseZip) { $legacyZip = Get-Fixture $LegacyReleaseZip $legacyUrl 'legacy-v0.12.3.zip' $legacySha256 }
    else {
        $legacyDir = Join-Path $inputs 'legacy-download'
        $previous = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
        try { $null | & gh release download v0.12.3 -R howardreith/KingmakerWakingRune -p 'KingmakerWakingRune-0.12.3.zip' -D $legacyDir 2>&1 | Out-Null } finally { $ErrorActionPreference = $previous }
        $legacyZip = Get-Fixture (Join-Path $legacyDir 'KingmakerWakingRune-0.12.3.zip') $legacyUrl 'legacy-v0.12.3.zip' $legacySha256
    }
    $legacyDll = Join-Path $inputs 'legacy.dll'
    Expand-Entry $legacyZip "$modFolder/KingmakerWakingRune.dll" $legacyDll $null
    $wrongUmm = Join-Path $inputs 'UnityModManager-0.32.4.dll'
    Expand-Entry (Get-Fixture $WrongLoaderPackage $wrongLoaderUrl 'umm-0.32.4.nupkg' $wrongLoaderPackageSha256) 'lib/net35/UnityModManager.dll' $wrongUmm $wrongLoaderDllSha256
    $wrongHarmony = Join-Path $inputs '0Harmony-2.3.6.dll'
    Expand-Entry (Get-Fixture $WrongHarmonyPackage $wrongHarmonyUrl 'lib.harmony.2.3.6.nupkg' $wrongHarmonyPackageSha256) 'lib/net48/0Harmony.dll' $wrongHarmony $wrongHarmonyDllSha256
    $owner = [ordered]@{ version = [Reflection.AssemblyName]::GetAssemblyName($installedLoader).Version.ToString(); sha256 = Get-Sha256 $installedLoader }
    Write-Host "Owner-installed UnityModManager (measured only, not used as a fixture): $($owner.version) $($owner.sha256)"

    & git clone -q --no-hardlinks $source $base
    if ($LASTEXITCODE -ne 0) { throw 'git clone failed.' }
    & git -C $base checkout -q --detach $commit
    Add-Content -LiteralPath (Join-Path $base '.git/info/exclude') -Value 'scripts/*-instrumented.ps1'
    Copy-Item -LiteralPath (Join-Path $source 'LocalGamePaths.props') -Destination $base
    # Cold cache: the clone's own resolver downloads and verifies the pins. A different product version is committed in
    # the disposable clone, and the positive artifact name is derived from the clone's manifest.
    $references = Join-Path $base 'obj\loader-references'
    if (Test-Path -LiteralPath $references) { throw 'The clone unexpectedly has a reference cache.' }
    $shippingVersion = [string](Get-Content -LiteralPath (Join-Path $base 'Info.json') -Raw | ConvertFrom-Json).Version
    $shipBuild = Invoke-Isolated @('-File', (Join-Path $base 'scripts\Build.ps1'))
    $shipZip = Join-Path $base "artifacts\KingmakerWakingRune-$shippingVersion.zip"
    $shipCheck = Invoke-Isolated @('-File', (Join-Path $base 'scripts\Validate-Package.ps1'), '-PackagePath', $shipZip, '-ReceiptPath', ($shipZip + '.build.json'), '-RequireCurrentProvenance')
    $shipReceipt = if (Test-Path -LiteralPath ($shipZip + '.build.json')) { Get-Content -LiteralPath ($shipZip + '.build.json') -Raw | ConvertFrom-Json } else { $null }
    Add-Case 'unmodified-candidate-cold-cache-build-and-receipt-check' ($shipBuild.ExitCode -eq 0 -and $shipCheck.ExitCode -eq 0 -and $shipCheck.Output -match 'source provenance: verified-current' -and (Test-Path -LiteralPath (Join-Path $references 'UnityModManager.dll')) -and $shipReceipt.commit -eq $commit -and $shipReceipt.version -eq $shippingVersion) ("shipping version $shippingVersion at $commit (empty cache before the build): ZIP $($shipReceipt.zipSha256) DLL $($shipReceipt.dllSha256); standalone check with current provenance passed")
    $alternateVersion = Get-AlternateFixtureVersion $shippingVersion
    $shipPairBefore = (Get-Sha256 $shipZip) + '/' + (Get-Sha256 ($shipZip + '.build.json'))
    Edit-Json (Join-Path $base 'Info.json') { param($j) $j.Version = $alternateVersion }
    & git -C $base -c user.name=loader-baseline -c user.email=loader-baseline@invalid commit -q -am 'test: disposable product version for the boundary suite'
    if ($LASTEXITCODE -ne 0) { throw 'Version commit in the clone failed.' }
    $testCommit = (& git -C $base rev-parse HEAD).Trim()
    $cloneVersion = [string](Get-Content -LiteralPath (Join-Path $base 'Info.json') -Raw | ConvertFrom-Json).Version
    $build = Invoke-Isolated @('-File', (Join-Path $base 'scripts\Build.ps1'))
    $candidateZip = Join-Path $base "artifacts\KingmakerWakingRune-$cloneVersion.zip"
    $receiptPath = $candidateZip + '.build.json'
    if ($build.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $receiptPath)) { throw "Clone build failed: $($build.Output)" }
    $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json
    $cloneReceipt = $receipt
    $shipPairAfter = (Get-Sha256 $shipZip) + '/' + (Get-Sha256 ($shipZip + '.build.json'))
    Add-Case 'alternate-version-build-keeps-shipping-pair' ($alternateVersion -cne $shippingVersion -and $candidateZip -ne $shipZip -and $shipPairBefore -ceq $shipPairAfter) "shipping $shippingVersion pair unchanged ($shipPairAfter); alternate $alternateVersion"
    Add-Case 'clone-build-at-different-version' ((Test-Path -LiteralPath (Join-Path $references 'UnityModManager.dll')) -and $receipt.version -eq $alternateVersion -and $receipt.commit -eq $testCommit -and -not $receipt.dirty -and $build.Output -match 'Tests: \d+ passed, 0 failed') ("receipt commit $($receipt.commit) dirty $($receipt.dirty); DLL $($receipt.dllSha256); ZIP $($receipt.zipSha256); " + [regex]::Match($build.Output, 'Tests: \d+ passed, 0 failed').Value)
    $verifier = Join-Path $base 'scripts\Get-PinnedLoaderReferences.ps1'
    $validator = Join-Path $base 'scripts\Validate-Package.ps1'
    $project = Join-Path $base 'src\KingmakerWakingRune\KingmakerWakingRune.csproj'
    $pinned = @{}
    foreach ($file in $pinFiles) { $pinned[$file] = [IO.Path]::GetFullPath((Join-Path $references $file)) }

    # 1. Reference cache: genuine passes; corrupt, missing and distinct-fixture bytes fail clearly.
    Expect-Accepted 'cache-genuine-verified' (Invoke-Isolated @('-File', $verifier, '-VerifyOnly', '-ReferenceDirectory', $references)) ''
    $corrupt = Join-Path $temp 'cache corrupt'
    Copy-Pins $references $corrupt
    $bytes = [IO.File]::ReadAllBytes((Join-Path $corrupt '0Harmony.dll'))
    $bytes[$bytes.Length - 1] = $bytes[$bytes.Length - 1] -bxor 0xFF
    [IO.File]::WriteAllBytes((Join-Path $corrupt '0Harmony.dll'), $bytes)
    Expect-Rejected 'cache-corrupt-harmony2-rejected' (Invoke-Isolated @('-File', $verifier, '-VerifyOnly', '-ReferenceDirectory', $corrupt)) 'hash mismatch'
    $missing = Join-Path $temp 'cache missing'
    Copy-Pins $references $missing
    Remove-Item -LiteralPath (Join-Path $missing 'UnityModManager.dll')
    Expect-Rejected 'cache-missing-rejected' (Invoke-Isolated @('-File', $verifier, '-VerifyOnly', '-ReferenceDirectory', $missing)) 'reference missing'
    foreach ($fixture in @(@{ Name = 'umm-0.32.4'; File = 'UnityModManager.dll'; Path = $wrongUmm }, @{ Name = 'harmony2-2.3.6'; File = '0Harmony.dll'; Path = $wrongHarmony })) {
        $dir = Join-Path $temp ('cache ' + $fixture.Name)
        Copy-Pins $references $dir
        Copy-Item -LiteralPath $fixture.Path -Destination (Join-Path $dir $fixture.File) -Force
        Expect-Rejected "cache-$($fixture.Name)-fixture-rejected" (Invoke-Isolated @('-File', $verifier, '-VerifyOnly', '-ReferenceDirectory', $dir)) 'hash mismatch'
    }

    # 2. Pin definition and manifest contract on copies of the clone's scripts.
    function New-ManifestFixture([string] $Name, [scriptblock] $Edit) {
        $fixture = Join-Path $temp $Name
        New-Item -ItemType Directory -Force -Path (Join-Path $fixture 'scripts') | Out-Null
        Copy-Item -LiteralPath $verifier, (Join-Path $base 'scripts\loader-reference-pins.json') -Destination (Join-Path $fixture 'scripts')
        Copy-Item -LiteralPath (Join-Path $base 'Info.json') -Destination $fixture
        & $Edit $fixture
        return (Join-Path $fixture 'scripts\Get-PinnedLoaderReferences.ps1')
    }
    $f = New-ManifestFixture 'manifest control' { param($d) }
    Expect-Accepted 'manifest-control-accepted' (Invoke-Isolated @('-File', $f, '-VerifyOnly', '-ReferenceDirectory', $references)) ''
    $f = New-ManifestFixture 'pin harmony2 dropped' { param($d) Edit-Json (Join-Path $d 'scripts\loader-reference-pins.json') { param($j) $j.references = @($j.references | Where-Object Name -ne '0Harmony') } }
    Expect-Rejected 'pin-definition-without-harmony2-rejected' (Invoke-Isolated @('-File', $f, '-VerifyOnly', '-ReferenceDirectory', $references)) 'must define exactly'
    $f = New-ManifestFixture 'pin harmony2 renamed' { param($d) Edit-Json (Join-Path $d 'scripts\loader-reference-pins.json') { param($j) @($j.references | Where-Object Name -eq '0Harmony')[0].File = '0Harmony2.dll' } }
    Expect-Rejected 'pin-definition-misnamed-harmony2-rejected' (Invoke-Isolated @('-File', $f, '-VerifyOnly', '-ReferenceDirectory', $references)) 'must define 0Harmony exactly once'
    $f = New-ManifestFixture 'manifest raised' { param($d) Edit-Json (Join-Path $d 'Info.json') { param($j) $j.ManagerVersion = '0.33.0' } }
    Expect-Rejected 'manifest-above-compiler-reference-rejected' (Invoke-Isolated @('-File', $f, '-VerifyOnly', '-ReferenceDirectory', $references)) 'ManagerVersion must equal'
    $f = New-ManifestFixture 'manifest array' { param($d) Edit-Json (Join-Path $d 'Info.json') { param($j) $j.ManagerVersion = @('0.31.1') } }
    Expect-Rejected 'manifest-array-version-rejected' (Invoke-Isolated @('-File', $f, '-VerifyOnly', '-ReferenceDirectory', $references)) 'ManagerVersion must equal'
    $f = New-ManifestFixture 'pin raised' { param($d) Edit-Json (Join-Path $d 'scripts\loader-reference-pins.json') { param($j) @($j.references | Where-Object Name -eq 'UnityModManager')[0].Version = '0.33.0.0' } }
    Expect-Rejected 'compiler-pin-above-minimum-rejected' (Invoke-Isolated @('-File', $f, '-VerifyOnly', '-ReferenceDirectory', $references)) 'must equal the declared minimum'

    # 3. Direct and incremental MSBuild of the production project.
    $obj = Join-Path $temp 'direct-obj\'
    $out = Join-Path $temp 'direct-bin\'
    Expect-Rejected 'msbuild-corrupt-cache-rejected' (Invoke-MSBuildIsolated @($project, '/t:Build', '/p:Configuration=Release', "/p:PinnedLoaderReferenceDir=$corrupt", "/p:IntermediateOutputPath=$obj", "/p:OutputPath=$out")) 'hash mismatch'
    $direct = Invoke-MSBuildIsolated @($project, '/t:Rebuild', '/v:n', '/p:Configuration=Release', "/p:IntermediateOutputPath=$obj", "/p:OutputPath=$out")
    $ok = $direct.ExitCode -eq 0
    $detail = @()
    foreach ($file in $pinFiles) {
        $found = @(Get-CompilerReferences $direct.Output $file)
        $ok = $ok -and $found.Count -eq 1 -and $found[0] -ieq $pinned[$file]
        $detail += "$file -> $($found -join ' | ')"
    }
    Add-Case 'msbuild-direct-compiles-against-pinned-inputs' $ok ("exit $($direct.ExitCode); " + ($detail -join '; '))
    $inc = @($project, '/t:Build', '/p:Configuration=Release', "/p:IntermediateOutputPath=$(Join-Path $temp 'incremental-obj\')", "/p:OutputPath=$(Join-Path $temp 'incremental-bin\')")
    Expect-Accepted 'incremental-build-genuine-accepted' (Invoke-MSBuildIsolated $inc) ''
    Expect-Accepted 'incremental-build-up-to-date-accepted' (Invoke-MSBuildIsolated $inc) ''
    Edit-Json (Join-Path $base 'Info.json') { param($j) $j.ManagerVersion = '0.33.0' }
    Expect-Rejected 'incremental-build-after-manifest-only-change-rejected' (Invoke-MSBuildIsolated $inc) 'ManagerVersion must equal'
    & git -C $base checkout -q -- Info.json
    Edit-Json (Join-Path $base 'scripts\loader-reference-pins.json') { param($j) @($j.references | Where-Object Name -eq '0Harmony')[0].Sha256 = ('0' * 64) }
    Expect-Rejected 'incremental-build-after-pin-only-change-rejected' (Invoke-MSBuildIsolated $inc) 'hash mismatch'
    & git -C $base checkout -q -- scripts/loader-reference-pins.json
    Expect-Accepted 'incremental-build-after-restore-accepted' (Invoke-MSBuildIsolated $inc) ''

    # 4. Csc-input guard (after reference-assembly selection): only the canonical verified files qualify.
    $stray = Join-Path $temp 'stray copy'
    Copy-Pins $references $stray
    Copy-Item -LiteralPath $pinned['0Harmony.dll'] -Destination (Join-Path $references '0Harmony.exe')
    Copy-Item -LiteralPath $pinned['UnityModManager.dll'] -Destination (Join-Path $references 'umm-alias.dll')
    $targets = Join-Path $base 'LoaderReferences.targets'
    function New-ProbeProject([string] $Name, [string] $Umm, [string] $Harmony2, [string] $Extra) {
        $path = Join-Path $temp "probe $Name.csproj"
        $text = '<Project ToolsVersion="15.0" DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">' +
            '<Import Project="$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props" Condition="Exists(''$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props'')" />' +
            '<PropertyGroup><TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion><OutputType>Library</OutputType>' +
            "<OutputPath>$temp\probe bin\$Name\</OutputPath><IntermediateOutputPath>$temp\probe obj\$Name\</IntermediateOutputPath>" +
            '<ResolveAssemblyReferenceIgnoreTargetFrameworkAttributeVersionMismatch>true</ResolveAssemblyReferenceIgnoreTargetFrameworkAttributeVersionMismatch></PropertyGroup>' +
            "<Import Project=`"$targets`" /><ItemGroup>" +
            "<Reference Include=`"UnityModManager`"><HintPath>$Umm</HintPath><Private>False</Private></Reference>" +
            "<Reference Include=`"0Harmony12`"><HintPath>$($pinned['0Harmony12.dll'])</HintPath><Private>False</Private></Reference>" +
            "<Reference Include=`"0Harmony`"><HintPath>$Harmony2</HintPath><Private>False</Private></Reference></ItemGroup>" +
            $Extra + '<Import Project="$(MSBuildToolsPath)\Microsoft.CSharp.targets" /></Project>'
        [IO.File]::WriteAllText($path, $text)
        return $path
    }
    $t = '/t:ResolveReferences;FindReferenceAssembliesForReferences'
    Expect-Accepted 'csc-input-pinned-accepted' (Invoke-MSBuildIsolated @((New-ProbeProject 'pinned' $pinned['UnityModManager.dll'] $pinned['0Harmony.dll'] ''), $t)) ''
    Expect-Rejected 'csc-input-identical-copy-elsewhere-rejected' (Invoke-MSBuildIsolated @((New-ProbeProject 'stray' (Join-Path $stray 'UnityModManager.dll') $pinned['0Harmony.dll'] ''), $t)) 'must be the verified files'
    Expect-Rejected 'csc-input-same-stem-harmony2-in-pinned-dir-rejected' (Invoke-MSBuildIsolated @((New-ProbeProject 'samestem' $pinned['UnityModManager.dll'] (Join-Path $references '0Harmony.exe') ''), $t)) 'must be the verified files'
    Expect-Rejected 'csc-input-renamed-loader-in-pinned-dir-rejected' (Invoke-MSBuildIsolated @((New-ProbeProject 'alias' (Join-Path $references 'umm-alias.dll') $pinned['0Harmony.dll'] ''), $t)) 'must be the verified files'
    Expect-Rejected 'csc-input-umm-0.32.4-fixture-rejected' (Invoke-MSBuildIsolated @((New-ProbeProject 'wrongumm' $wrongUmm $pinned['0Harmony.dll'] ''), $t)) 'must be the verified files'
    Expect-Rejected 'csc-input-harmony2-2.3.6-fixture-rejected' (Invoke-MSBuildIsolated @((New-ProbeProject 'wrongharmony' $pinned['UnityModManager.dll'] $wrongHarmony ''), $t)) 'must be the verified files'
    $substitute = '<Target Name="SubstituteHarmony2ReferenceAssembly" AfterTargets="ResolveAssemblyReferences" BeforeTargets="FindReferenceAssembliesForReferences"><ItemGroup>' +
        "<ReferencePath Condition=`"'%(Filename)' == '0Harmony'`"><ReferenceAssembly>$(Join-Path $stray '0Harmony.dll')</ReferenceAssembly></ReferencePath></ItemGroup></Target>"
    Expect-Rejected 'csc-input-substituted-reference-assembly-rejected' (Invoke-MSBuildIsolated @((New-ProbeProject 'substituted' $pinned['UnityModManager.dll'] $pinned['0Harmony.dll'] $substitute), $t)) 'must be the verified files'
    Remove-Item -LiteralPath (Join-Path $references '0Harmony.exe'), (Join-Path $references 'umm-alias.dll')

    # 5. Standalone validator (run by KwrRuntime.ps1 before every install) and build receipts.
    $v = { param($zip) Invoke-Isolated @('-File', $validator, '-PackagePath', $zip) }
    Expect-Accepted 'package-candidate-with-receipt-accepted' (Invoke-Isolated @('-File', $validator, '-PackagePath', $candidateZip, '-ReceiptPath', $receiptPath)) 'loader references: UnityModManager 0\.31\.1\.0, 0Harmony12 1\.2\.0\.1, 0Harmony 2\.3\.1\.1'
    $repacked = Join-Path $inputs 'repacked.zip'
    $sourceArchive = [IO.Compression.ZipFile]::OpenRead($candidateZip)
    $target = [IO.Compression.ZipFile]::Open($repacked, [IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($entry in @($sourceArchive.Entries | Sort-Object FullName -Descending)) {
            $in = $entry.Open(); $outStream = $target.CreateEntry($entry.FullName, [IO.Compression.CompressionLevel]::NoCompression).Open()
            try { $in.CopyTo($outStream) } finally { $outStream.Dispose(); $in.Dispose() }
        }
    }
    finally { $target.Dispose(); $sourceArchive.Dispose() }
    Expect-Accepted 'package-repacked-same-content-accepted-without-receipt' (& $v $repacked) 'Package validation passed'
    Expect-Rejected 'receipt-for-other-zip-bytes-rejected' (Invoke-Isolated @('-File', $validator, '-PackagePath', $repacked, '-ReceiptPath', $receiptPath)) 'zip SHA-256 differs'
    foreach ($bad in @(
        @{ Name = 'other-dll-digest'; Edit = { param($j) $j.dllSha256 = ('0' * 64) }; Pattern = 'packaged DLL \(SHA-256 differs\)' },
        @{ Name = 'other-loader'; Edit = { param($j) @($j.loaderReferences | Where-Object { $_.name -eq 'UnityModManager' })[0].sha256 = $wrongLoaderDllSha256 }; Pattern = 'does not match pinned reference UnityModManager' },
        @{ Name = 'no-loader-references'; Edit = { param($j) $j.PSObject.Properties.Remove('loaderReferences') }; Pattern = 'Build receipt has no loaderReferences' },
        @{ Name = 'zip-digest-array'; Edit = { param($j) $j.zipSha256 = @($j.zipSha256) }; Pattern = 'must be a lowercase SHA-256 string' },
        @{ Name = 'dll-digest-boolean'; Edit = { param($j) $j.dllSha256 = $true }; Pattern = 'must be a lowercase SHA-256 string' },
        @{ Name = 'loader-version-array'; Edit = { param($j) @($j.loaderReferences)[0].assemblyVersion = @(@($j.loaderReferences)[0].assemblyVersion) }; Pattern = 'must be a string' })) {
        $r = Join-Path $inputs ("receipt-" + $bad.Name + '.json')
        Copy-Item -LiteralPath $receiptPath -Destination $r
        Edit-Json $r $bad.Edit
        Expect-Rejected "receipt-$($bad.Name)-rejected" (Invoke-Isolated @('-File', $validator, '-PackagePath', $candidateZip, '-ReceiptPath', $r)) $bad.Pattern
    }
    $p = Join-Path $inputs 'extra-harmony.zip'; Edit-Zip $candidateZip $p { param($a) Set-ZipEntryFile $a "$modFolder/0Harmony.dll" $pinned['0Harmony.dll'] }
    Expect-Rejected 'package-extra-harmony2-rejected' (& $v $p) 'exactly 2 files'
    $p = Join-Path $inputs 'swap-loader.zip'; Edit-Zip $candidateZip $p { param($a) $a.GetEntry("$modFolder/Info.json").Delete(); Set-ZipEntryFile $a "$modFolder/UnityModManager.dll" $pinned['UnityModManager.dll'] }
    Expect-Rejected 'package-swapped-in-loader-rejected' (& $v $p) 'Unexpected package entry: KingmakerWakingRune/UnityModManager\.dll'
    $p = Join-Path $inputs 'swap-game.zip'; Edit-Zip $candidateZip $p { param($a) $a.GetEntry("$modFolder/Info.json").Delete(); Set-ZipEntryFile $a "$modFolder/Assembly-CSharp-firstpass.dll" (Join-Path $managedDir 'Assembly-CSharp-firstpass.dll') }
    Expect-Rejected 'package-swapped-in-game-assembly-rejected' (& $v $p) 'Unexpected package entry'
    $p = Join-Path $inputs 'directory-with-data.zip'; Edit-Zip $candidateZip $p { param($a) $w = [IO.StreamWriter]::new($a.CreateEntry("$modFolder/").Open()); try { $w.Write('inert marker') } finally { $w.Dispose() } }
    Expect-Rejected 'package-data-bearing-directory-record-rejected' (& $v $p) 'data-bearing package directory record'
    $p = Join-Path $inputs 'deflate-level0-directory.zip'
    $writer = "import sys,zipfile,shutil; shutil.copyfile(sys.argv[1],sys.argv[2]); z=zipfile.ZipFile(sys.argv[2],'a'); i=zipfile.ZipInfo('KingmakerWakingRune/'); z.writestr(i,b'',compress_type=zipfile.ZIP_DEFLATED,compresslevel=0); z.close(); z=zipfile.ZipFile(sys.argv[2]); d=[x for x in z.infolist() if x.filename.endswith('/')]; f=[x for x in z.infolist() if not x.filename.endswith('/')]; print('FIXTURE files=%d dirs=%s' % (len(f), ';'.join('%s,%d,%d,%d' % (x.filename,x.compress_type,x.compress_size,x.file_size) for x in d)))"
    $previous = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
    try { $fixtureOut = (& python -I -c $writer $candidateZip $p 2>&1 | ForEach-Object { "$_" }) -join ' '; $fixtureExit = $LASTEXITCODE } finally { $ErrorActionPreference = $previous }
    $fm = [regex]::Match($fixtureOut, 'FIXTURE files=(\d+) dirs=KingmakerWakingRune/,(\d+),(\d+),(\d+)$')
    $fixtureOk = $fixtureExit -eq 0 -and $fm.Success -and [int]$fm.Groups[1].Value -eq 2 -and [int]$fm.Groups[2].Value -eq 8 -and [int]$fm.Groups[3].Value -gt 2 -and [int]$fm.Groups[4].Value -eq 0
    $r = if ($fixtureOk) { & $v $p } else { $null }
    Add-Case 'package-level0-deflate-empty-directory-accepted' ($fixtureOk -and $r.ExitCode -eq 0 -and $r.Output -match 'Package validation passed') ("writer exit ${fixtureExit}: $($fixtureOut.Trim()); " + $(if (-not $fixtureOk) { 'fixture invalid (case fails)' } elseif ($r.ExitCode -eq 0) { 'accepted' } else { Get-Reason $r.Output }))
    foreach ($bad in @(@{ Name = 'array'; Value = '["0.31.1"]' }, @{ Name = 'boolean'; Value = 'true' })) {
        $p = Join-Path $inputs "manager-$($bad.Name).zip"
        $replacement = '"ManagerVersion": ' + $bad.Value
        Edit-Zip $candidateZip $p { param($a) $text = Get-ZipEntryText $a "$modFolder/Info.json"; Set-ZipEntryText $a "$modFolder/Info.json" ($text -replace '"ManagerVersion":\s*"0\.31\.1"', $replacement) }
        Expect-Rejected "package-manager-version-$($bad.Name)-rejected" (& $v $p) 'ManagerVersion must be a JSON string'
    }
    $legacyRefs = Invoke-Script ("([Reflection.Assembly]::ReflectionOnlyLoad([IO.File]::ReadAllBytes(" + (Quote $legacyDll) + ")).GetReferencedAssemblies() | Where-Object { `$_.Name -eq 'UnityModManager' -or `$_.Name -eq '0Harmony' } | Sort-Object Name | ForEach-Object { `$_.Name + ' ' + `$_.Version }) -join '; '")
    Add-Case 'legacy-release-references-umm-0.33.2-harmony-2.3.6' ($legacyRefs.ExitCode -eq 0 -and $legacyRefs.Output.Trim() -eq '0Harmony 2.3.6.0; UnityModManager 0.33.2.0') ("genuine v0.12.3 ZIP $legacySha256; DLL references " + $legacyRefs.Output.Trim())
    $lowered = Join-Path $inputs 'legacy-lowered-manifest.zip'
    Edit-Zip $legacyZip $lowered { param($a)
        $text = Get-ZipEntryText $a "$modFolder/Info.json"
        $changed = ($text -replace '"ManagerVersion":\s*"[0-9.]+"', '"ManagerVersion": "0.31.1"') -replace '"Version":\s*"[0-9.]+"', ('"Version": "' + $cloneVersion + '"')
        if ($changed -eq $text) { throw 'Legacy manifest was not changed.' }
        Set-ZipEntryText $a "$modFolder/Info.json" $changed }
    Expect-Rejected 'package-legacy-0.33.2-dll-with-0.31.1-manifest-rejected' (& $v $lowered) 'must reference pinned UnityModManager 0\.31\.1\.0'
    foreach ($order in @(@($candidateZip, $lowered), @($lowered, $candidateZip))) {
        $text = "foreach (`$zip in @(" + (Quote $order[0]) + ", " + (Quote $order[1]) + ")) { try { & " + (Quote $validator) + " -PackagePath `$zip | Out-Null; 'ACCEPTED ' + [IO.Path]::GetFileName(`$zip) } catch { 'REJECTED ' + [IO.Path]::GetFileName(`$zip) + ': ' + `$_.Exception.Message } }"
        $r = Invoke-Script $text
        $ok = $r.Output -match ('ACCEPTED ' + [regex]::Escape([IO.Path]::GetFileName($candidateZip))) -and $r.Output -match 'REJECTED legacy-lowered-manifest\.zip: Packaged DLL must reference pinned UnityModManager 0\.31\.1\.0'
        Add-Case "same-host-validation-$([IO.Path]::GetFileNameWithoutExtension($order[0]))-first" $ok ($r.Output.Trim() -replace "`n", ' | ')
    }
    # The validator hashes and inspects one snapshot: a caller file replaced afterwards cannot mix two ZIPs.
    $callerZip = Join-Path $inputs 'caller.zip'
    Copy-Item -LiteralPath $candidateZip -Destination $callerZip
    $text = [IO.File]::ReadAllText($validator)
    $anchor = '[IO.File]::Copy($PackagePath, $inspected)'
    $instrumented = Join-Path $base 'scripts\Validate-Package-instrumented.ps1'
    [IO.File]::WriteAllText($instrumented, $text.Replace($anchor, $anchor + "`r`n[IO.File]::Copy(" + (Quote $repacked) + ', $PackagePath, $true)'))
    $mixReport = Join-Path $inputs 'mix.json'
    $mix = Invoke-Isolated @('-File', $instrumented, '-PackagePath', $callerZip, '-ReportPath', $mixReport)
    $mr = if (Test-Path -LiteralPath $mixReport) { Get-Content -LiteralPath $mixReport -Raw | ConvertFrom-Json } else { $null }
    Add-Case 'validator-report-pairs-zip-digest-with-its-own-dll' ($mix.ExitCode -eq 0 -and $mr -and $mr.package_sha256 -eq $receipt.zipSha256 -and $mr.dll_sha256 -eq $receipt.dllSha256 -and (Get-Sha256 $callerZip) -eq (Get-Sha256 $repacked)) ("report ZIP $($mr.package_sha256) with DLL $($mr.dll_sha256); caller path now holds the repacked ZIP")
    Remove-Item -LiteralPath $instrumented
    # A pin change makes an older package fail validation (no frozen reuse across pins).
    Edit-Json (Join-Path $base 'scripts\loader-reference-pins.json') { param($j) $h = @($j.references | Where-Object Name -eq '0Harmony')[0]; $h.Version = '2.3.6.0'; $h.Sha256 = $wrongHarmonyDllSha256 }
    Expect-Rejected 'package-from-earlier-pins-rejected-after-pin-change' (& $v $candidateZip) 'must reference pinned 0Harmony 2\.3\.6\.0'
    & git -C $base checkout -q -- scripts/loader-reference-pins.json

    # 6. Shared pin contract, product identity and the complete loader-reference set (offline validator).
    $pinsFile = Join-Path $base 'scripts\loader-reference-pins.json'
    Edit-Json $pinsFile { param($j) $j.references = @($j.references | Where-Object Name -ne '0Harmony') }
    Expect-Rejected 'validator-pin-definition-without-harmony2-rejected' (& $v $candidateZip) 'must define exactly'
    & git -C $base checkout -q -- scripts/loader-reference-pins.json
    Edit-Json $pinsFile { param($j) @($j.references | Where-Object Name -eq 'UnityModManager')[0].Version = '0.32.4.0' }
    Expect-Rejected 'validator-umm-pin-disagrees-with-minimum-rejected' (& $v $candidateZip) 'must equal the declared minimum'
    & git -C $base checkout -q -- scripts/loader-reference-pins.json
    $cacheAway = Join-Path $temp 'cache moved away'
    Move-Item -LiteralPath $references -Destination $cacheAway
    Expect-Accepted 'validator-offline-without-reference-cache-accepted' (& $v $candidateZip) 'Package validation passed'
    foreach ($bad in @(
        @{ Name = 'digest-with-trailing-line-feed'; Edit = { param($j) $pin = @($j.references | Where-Object Name -eq 'UnityModManager')[0]; $pin.Sha256 = $pin.Sha256 + "`n" }; Pattern = 'Invalid or duplicate loader reference pin' },
        @{ Name = 'missing-required-archive-digest'; Edit = { param($j) @($j.references | Where-Object Name -eq 'UnityModManager')[0].ArchiveSha256 = $null }; Pattern = 'NuGet package source of UnityModManager\.dll needs ArchiveEntry and ArchiveSha256' },
        @{ Name = 'archive-digest-without-entry'; Edit = { param($j) @($j.references | Where-Object Name -eq '0Harmony12')[0].ArchiveSha256 = ('a' * 64) }; Pattern = 'direct \.dll source of 0Harmony12\.dll must not name archive fields' })) {
        Edit-Json $pinsFile $bad.Edit
        Expect-Rejected "validator-offline-pin-$($bad.Name)-rejected" (& $v $candidateZip) $bad.Pattern
        & git -C $base checkout -q -- scripts/loader-reference-pins.json
    }
    Move-Item -LiteralPath $cacheAway -Destination $references
    # Benign stub assemblies compiled with the Roslyn compiler that ships with MSBuild against the pinned loader files.
    $csc = Join-Path (Split-Path -Parent $msbuild) 'Roslyn\csc.exe'
    $stubs = Join-Path $temp 'stubs'; New-Item -ItemType Directory -Path $stubs | Out-Null
    function New-Stub([string] $Out, [string] $Code, [string[]] $References) {
        $src = Join-Path $stubs ([IO.Path]::GetFileNameWithoutExtension($Out) + '.cs')
        [IO.File]::WriteAllText($src, $Code)
        $cscArgs = @('/nologo', '/target:library', "/out:$Out") + @($References | ForEach-Object { "/reference:$_" }) + @($src)
        $o = & $csc @cscArgs 2>&1 | ForEach-Object { "$_" }
        if ($LASTEXITCODE -ne 0) { throw "Stub compile failed: $($o -join ' ')" }
    }
    $probeTypes = 'typeof(UnityModManagerNet.UnityModManager), typeof(Harmony12.HarmonyInstance), typeof(HarmonyLib.Harmony)'
    $adapter = Join-Path $stubs '0Harmony-1.2.dll'
    New-Stub $adapter 'namespace HarmonyAdapterStub { public class Marker { } }' @()
    $withAdapter = Join-Path $stubs 'adapter\KingmakerWakingRune.dll'; New-Item -ItemType Directory -Path (Split-Path $withAdapter) | Out-Null
    New-Stub $withAdapter ('public static class Probe { public static System.Type[] Types = { ' + $probeTypes + ', typeof(HarmonyAdapterStub.Marker) }; }') @($pinned['UnityModManager.dll'], $pinned['0Harmony12.dll'], $pinned['0Harmony.dll'], $adapter)
    $p = Join-Path $inputs 'extra-harmony-adapter-reference.zip'; Edit-Zip $candidateZip $p { param($a) Set-ZipEntryFile $a "$modFolder/KingmakerWakingRune.dll" $withAdapter }
    Expect-Rejected 'validator-additional-harmony-adapter-reference-rejected' (& $v $p) 'unpinned loader or Harmony assembly: 0Harmony-1\.2'
    $otherName = Join-Path $stubs 'NotWakingRune.dll'
    New-Stub $otherName ('public static class Probe { public static System.Type[] Types = { ' + $probeTypes + ' }; }') @($pinned['UnityModManager.dll'], $pinned['0Harmony12.dll'], $pinned['0Harmony.dll'])
    $p = Join-Path $inputs 'other-assembly-under-product-name.zip'; Edit-Zip $candidateZip $p { param($a) Set-ZipEntryFile $a "$modFolder/KingmakerWakingRune.dll" $otherName }
    Expect-Rejected 'validator-differently-named-assembly-under-product-file-rejected' (& $v $p) "defines assembly 'NotWakingRune'"
    Expect-Accepted 'validator-genuine-candidate-control-accepted' (& $v $candidateZip) 'Package validation passed'

    # 7. Caller paths use PowerShell literal semantics; the report may not overwrite an input.
    $processDir = Join-Path $temp 'process dir'; $psDir = Join-Path $temp 'powershell dir'
    New-Item -ItemType Directory -Path $processDir, $psDir | Out-Null
    Copy-Item -LiteralPath $lowered -Destination (Join-Path $processDir 'pkg [1].zip')
    Copy-Item -LiteralPath $candidateZip -Destination (Join-Path $psDir 'pkg [1].zip')
    $relativeReport = Join-Path $temp 'relative-report.json'
    $rel = Invoke-Script ("[Environment]::CurrentDirectory = $(Quote $processDir); Set-Location -LiteralPath $(Quote $psDir); & $(Quote $validator) -PackagePath 'pkg [1].zip' -ReportPath $(Quote $relativeReport)")
    $rr = if (Test-Path -LiteralPath $relativeReport) { Get-Content -LiteralPath $relativeReport -Raw | ConvertFrom-Json } else { $null }
    Add-Case 'validator-relative-literal-path-from-powershell-location' ($rel.ExitCode -eq 0 -and $rr -and $rr.package_sha256 -eq (Get-Sha256 $candidateZip)) ("relative 'pkg [1].zip' resolved to the PowerShell location: digest $($rr.package_sha256) (candidate $(Get-Sha256 $candidateZip); the process directory holds the lowered legacy ZIP)")
    $zipBefore = Get-Sha256 $candidateZip; $receiptBefore = Get-Sha256 $receiptPath
    Expect-Rejected 'validator-report-path-equal-to-package-rejected' (Invoke-Isolated @('-File', $validator, '-PackagePath', $candidateZip, '-ReceiptPath', $receiptPath, '-ReportPath', $candidateZip)) 'must not be an input'
    Expect-Rejected 'validator-report-path-equal-to-receipt-rejected' (Invoke-Isolated @('-File', $validator, '-PackagePath', $candidateZip, '-ReceiptPath', $receiptPath, '-ReportPath', $receiptPath)) 'must not be an input'
    Add-Case 'validator-report-collisions-leave-inputs-intact' ((Get-Sha256 $candidateZip) -eq $zipBefore -and (Get-Sha256 $receiptPath) -eq $receiptBefore) "package $zipBefore and receipt $receiptBefore unchanged"

    # 8. The runtime installer's copy and staging helper (scripts\KwrPackage.ps1, used by KwrRuntime Begin/Reinstall), on a
    #    temporary destination: captured source and digest, literal names with brackets, a moved caller ZIP, a rejected
    #    receipt, and cleanup of the private copy in every outcome.
    $kwrPackage = Join-Path $base 'scripts\KwrPackage.ps1'
    $installRoot = Join-Path $temp 'install dest'
    $bracketDir = Join-Path $temp 'caller [dir]'; New-Item -ItemType Directory -Path $bracketDir | Out-Null
    $bracketZip = Join-Path $bracketDir 'KingmakerWakingRune [candidate].zip'
    Copy-Item -LiteralPath $candidateZip -Destination $bracketZip; Copy-Item -LiteralPath $receiptPath -Destination ($bracketZip + '.build.json')
    $movedTo = Join-Path $temp 'moved.zip'
    $privBefore = @(Get-ChildItem -LiteralPath ([IO.Path]::GetTempPath()) -Directory -Filter 'KingmakerWakingRune-package-*' | ForEach-Object Name | Sort-Object) -join ';'
    $helper = Invoke-Script (". $(Quote $kwrPackage); `$v = New-KwrValidatedPackageCopy $(Quote $bracketZip); Move-Item -LiteralPath $(Quote $bracketZip) -Destination $(Quote $movedTo); New-Item -ItemType Directory -Path $(Quote $installRoot) | Out-Null; Install-KwrValidatedPackageCopy `$v $(Quote $installRoot); 'RECORDED ' + `$v.Source + ' | ' + `$v.Sha256 + ' | ' + `$v.SourceProvenance; Remove-KwrValidatedPackageCopy `$v; 'PRIVATE-LEFT ' + [IO.Directory]::Exists(`$v.PrivateDirectory)")
    $installedDll = Join-Path $installRoot 'KingmakerWakingRune\KingmakerWakingRune.dll'
    $ok = $helper.ExitCode -eq 0 -and $helper.Output -match ('RECORDED ' + [regex]::Escape($bracketZip) + ' \| ' + $zipBefore + ' \| verified-current') -and $helper.Output -match 'PRIVATE-LEFT False' -and
        (Test-Path -LiteralPath $installedDll) -and (Get-Sha256 $installedDll) -eq $receipt.dllSha256
    Add-Case 'install-helper-records-captured-source-and-digest-after-caller-zip-moved' $ok ((@($helper.Output -split "`n" | Where-Object { $_ -match '^(RECORDED|PRIVATE-LEFT|ERROR)' }) -join ' / ') + "; installed DLL $(if (Test-Path -LiteralPath $installedDll) { Get-Sha256 $installedDll } else { 'missing' })")
    $badReceiptZip = Join-Path $bracketDir 'bad [receipt].zip'
    Copy-Item -LiteralPath $candidateZip -Destination $badReceiptZip; Copy-Item -LiteralPath $receiptPath -Destination ($badReceiptZip + '.build.json')
    Edit-Json ($badReceiptZip + '.build.json') { param($j) $j.dllSha256 = ('0' * 64) }
    $rejected = Invoke-Script (". $(Quote $kwrPackage); New-KwrValidatedPackageCopy $(Quote $badReceiptZip) | Out-Null")
    $privAfter = @(Get-ChildItem -LiteralPath ([IO.Path]::GetTempPath()) -Directory -Filter 'KingmakerWakingRune-package-*' | ForEach-Object Name | Sort-Object) -join ';'
    Add-Case 'install-helper-rejected-receipt-cleans-private-copy' ($rejected.ExitCode -ne 0 -and ($rejected.Output -replace '\s+', ' ') -match 'packaged DLL \(SHA-256 differs\)' -and $privBefore -ceq $privAfter) ('refused: ' + (Get-Reason $rejected.Output) + "; KingmakerWakingRune-package-* directories unchanged: $($privBefore -ceq $privAfter)")
    $runtimeText = [IO.File]::ReadAllText((Join-Path $base 'scripts\KwrRuntime.ps1'))
    $ok = ([regex]::Matches($runtimeText, '\$validated = New-KwrValidatedPackageCopy \$PackagePath')).Count -eq 2 -and ([regex]::Matches($runtimeText, 'Install-KwrValidatedPackageCopy \$validated \$ModsRoot')).Count -eq 1 -and $runtimeText -match 'Expand-KwrValidatedPackageCopy \$validated' -and
        ([regex]::Matches($runtimeText, 'finally \{ Remove-KwrValidatedPackageCopy \$validated \}')).Count -eq 2 -and $runtimeText -notmatch 'Expand-Archive' -and $runtimeText -notmatch 'Resolve-Path \$PackagePath'
    Add-Case 'kwrruntime-begin-and-reinstall-use-the-helper' $ok 'Begin and Reinstall: one New-KwrValidatedPackageCopy each, install from it, cleanup in finally; no Expand-Archive or later Resolve-Path of the caller path (KwrRuntime.ps1 itself is not executed)'

    # 9. Build.ps1 places the ZIP and receipt only after validating the pair in private staging.
    $held = Join-Path $inputs 'held-candidate'; New-Item -ItemType Directory -Path $held | Out-Null
    Move-Item -LiteralPath $candidateZip, $receiptPath -Destination $held
    $buildText = [IO.File]::ReadAllText((Join-Path $base 'scripts\Build.ps1'))
    $anchor = '$receipt | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $pendingReceipt -Encoding UTF8'
    if ($buildText.IndexOf($anchor) -lt 0) { throw 'Build.ps1 instrumentation anchor not found.' }
    $failingBuild = Join-Path $base 'scripts\Build-instrumented.ps1'
    [IO.File]::WriteAllText($failingBuild, $buildText.Replace($anchor, $anchor + "`r`nthrow 'injected failure after the sidecar was written'"))
    $f = Invoke-Isolated @('-File', $failingBuild, '-SkipTests')
    Remove-Item -LiteralPath $failingBuild
    $leftovers = @(Get-ChildItem -LiteralPath (Join-Path $base 'artifacts') -Force | Where-Object { $_.Name -like '.pending-*' -or $_.Name -like ([IO.Path]::GetFileName($candidateZip) + '*') } | ForEach-Object Name)
    Add-Case 'build-failure-after-sidecar-leaves-no-candidate' ($f.ExitCode -ne 0 -and $f.Output -match 'injected failure' -and $leftovers.Count -eq 0) ("exit $($f.ExitCode); artifacts left: $(if ($leftovers.Count) { $leftovers -join ', ' } else { 'none' })")
    $retry = Invoke-Isolated @('-File', (Join-Path $base 'scripts\Build.ps1'), '-SkipTests')
    $retryReceipt = if (Test-Path -LiteralPath $receiptPath) { Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json } else { $null }
    Add-Case 'build-clean-retry-succeeds-without-replace' ($retry.ExitCode -eq 0 -and $retryReceipt -and $retryReceipt.dllSha256 -eq $receipt.dllSha256 -and (Get-Sha256 $candidateZip) -eq $retryReceipt.zipSha256) ("exit $($retry.ExitCode); DLL $($retryReceipt.dllSha256) (same as the first build); ZIP $($retryReceipt.zipSha256)")
    Expect-Rejected 'build-refuses-to-replace-clean-candidate' (Invoke-Isolated @('-File', (Join-Path $base 'scripts\Build.ps1'), '-SkipTests')) 'already exists and may be a tested candidate'
    Copy-Item -LiteralPath (Join-Path $held ([IO.Path]::GetFileName($candidateZip))), (Join-Path $held ([IO.Path]::GetFileName($receiptPath))) -Destination (Join-Path $base 'artifacts') -Force

    # 10. Frozen-candidate provenance: the original ZIP and receipt stay current only while the producing inputs are
    #     unchanged; external content inspection stays available and says provenance was not checked.
    $prov = { Invoke-Isolated @('-File', $validator, '-PackagePath', $candidateZip, '-ReceiptPath', $receiptPath, '-RequireCurrentProvenance') }
    Expect-Accepted 'provenance-clean-candidate-current' (& $prov) 'source provenance: verified-current'
    Expect-Accepted 'external-zip-content-inspection-without-provenance' (& $v $candidateZip) 'source provenance: not-checked'
    [IO.File]::AppendAllText($verifier, "`r`n# loader-baseline test edit`r`n")
    Expect-Rejected 'provenance-after-reference-helper-change-rejected' (& $prov) 'Build inputs changed'
    & git -C $base checkout -q -- scripts/Get-PinnedLoaderReferences.ps1
    $localPaths = Join-Path $base 'LocalGamePaths.props'
    $localOriginal = [IO.File]::ReadAllText($localPaths)
    [IO.File]::WriteAllText($localPaths, $localOriginal.Replace('</Project>', "  <PropertyGroup><CheckForOverflowUnderflow>true</CheckForOverflowUnderflow></PropertyGroup>`r`n</Project>"))
    Expect-Rejected 'provenance-after-resolved-project-setting-change-rejected' (& $prov) 'Build inputs changed'
    [IO.File]::WriteAllText($localPaths, $localOriginal)
    Expect-Accepted 'provenance-current-again-after-restore' (& $prov) 'source provenance: verified-current'
    # A committed helper change (same mod and loader versions): the old pair loses current qualification; the rebuilt
    # candidate passes.
    [IO.File]::AppendAllText($verifier, "`r`n# loader-baseline committed helper change`r`n")
    & git -C $base -c user.name=loader-baseline -c user.email=loader-baseline@invalid commit -q -am 'test: helper change'
    Expect-Rejected 'provenance-old-pair-after-committed-helper-change-rejected' (& $prov) 'Build inputs changed'
    $rebuilt = Invoke-Isolated @('-File', (Join-Path $base 'scripts\Build.ps1'), '-SkipTests', '-ReplacePackage')
    Expect-Accepted 'provenance-rebuilt-candidate-current' (& $prov) 'source provenance: verified-current'

    # 11. Receipt provenance binds the recorded commit to its material inputs and the receipt version to the manifest.
    $savedProv = Join-Path $inputs 'provenance-receipt.json'
    Copy-Item -LiteralPath $receiptPath -Destination $savedProv
    foreach ($bad in @(
        @{ Name = 'older-commit'; Edit = { param($j) $j.commit = $commit }; Pattern = 'material inputs differ' },
        @{ Name = 'unknown-commit'; Edit = { param($j) $j.commit = ('e' * 40) }; Pattern = 'does not have' },
        @{ Name = 'other-version'; Edit = { param($j) $j.version = '9.9.9' }; Pattern = 'version does not match' })) {
        Copy-Item -LiteralPath $savedProv -Destination $receiptPath -Force
        Edit-Json $receiptPath $bad.Edit
        Expect-Rejected "provenance-receipt-$($bad.Name)-rejected" (& $prov) $bad.Pattern
    }
    Copy-Item -LiteralPath $savedProv -Destination $receiptPath -Force
    # A receipt naming a commit that differs from the producing one only in a packaged image (the embedded art is a
    # compiler input, so its tracked path is material). The commit is created without moving HEAD.
    $asset = 'assets/icons/RefugeToken.png'
    $assetVariant = Join-Path $inputs 'RefugeToken variant.png'
    [IO.File]::WriteAllBytes($assetVariant, [byte[]]([IO.File]::ReadAllBytes((Join-Path $base $asset)) + [byte[]](0)))
    $blob = (& git -C $base hash-object -w $assetVariant).Trim()
    $env:GIT_INDEX_FILE = Join-Path $inputs 'image-variant.index'
    try { & git -C $base read-tree HEAD; & git -C $base update-index --cacheinfo "100644,$blob,$asset"; $variantTree = (& git -C $base write-tree).Trim() }
    finally { Remove-Item Env:GIT_INDEX_FILE }
    $imageCommit = (& git -C $base -c user.name=loader-baseline -c user.email=loader-baseline@invalid commit-tree $variantTree -p HEAD -m 'test: image-only variant').Trim()
    $imageDiff = @(& git -C $base diff --name-only HEAD $imageCommit)
    Edit-Json $receiptPath { param($j) $j.commit = $imageCommit }
    $r = & $prov
    Add-Case 'provenance-receipt-commit-differing-only-in-packaged-image-rejected' ($r.ExitCode -ne 0 -and ($r.Output -replace '\s+', ' ') -match 'material inputs differ' -and ($imageDiff -join ',') -ceq $asset) ("commit $imageCommit differs only in $($imageDiff -join ','); " + (Get-Reason $r.Output))
    Copy-Item -LiteralPath $savedProv -Destination $receiptPath -Force
    # Any effective compiler option and file-valued input counts: BaseAddress and an external Win32Resource.
    $localPaths = Join-Path $base 'LocalGamePaths.props'
    $localOriginal = [IO.File]::ReadAllText($localPaths)
    [IO.File]::WriteAllText($localPaths, $localOriginal.Replace('</Project>', "  <PropertyGroup><BaseAddress>0x11000000</BaseAddress></PropertyGroup>`r`n</Project>"))
    Expect-Rejected 'provenance-after-base-address-change-rejected' (& $prov) 'Build inputs changed'
    function New-ResFile([string] $Path, [byte[]] $Payload) {
        $ms = [IO.MemoryStream]::new(); $w = [IO.BinaryWriter]::new($ms)
        foreach ($h in @(@(0, 0), @($Payload.Length, 10))) {
            $w.Write([int]$h[0]); $w.Write([int]32); $w.Write([uint16]0xFFFF); $w.Write([uint16]$h[1]); $w.Write([uint16]0xFFFF); $w.Write([uint16]$(if ($h[1]) { 1 } else { 0 }))
            $w.Write([int]0); $w.Write([uint16]$(if ($h[1]) { 0x30 } else { 0 })); $w.Write([uint16]$(if ($h[1]) { 0x0409 } else { 0 })); $w.Write([int]0); $w.Write([int]0)
            if ($h[1]) { $w.Write($Payload); while ($ms.Length % 4) { $w.Write([byte]0) } }
        }
        $w.Flush(); [IO.File]::WriteAllBytes($Path, $ms.ToArray()); $w.Dispose()
    }
    $res = Join-Path $temp 'external win32.res'
    New-ResFile $res ([Text.Encoding]::ASCII.GetBytes('resource A'))
    [IO.File]::WriteAllText($localPaths, $localOriginal.Replace('</Project>', "  <PropertyGroup><Win32Resource>$([Security.SecurityElement]::Escape($res))</Win32Resource></PropertyGroup>`r`n</Project>"))
    $wr = Invoke-Isolated @('-File', (Join-Path $base 'scripts\Build.ps1'), '-SkipTests', '-ReplacePackage')
    $wrReceipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json
    $resEntry = @($wrReceipt.compilerInputs | Where-Object { $_.kind -eq 'file-option' -and $_.name -like '/win32res:*' })
    Add-Case 'build-with-external-win32-resource-records-its-content' ($wr.ExitCode -eq 0 -and $resEntry.Count -eq 1 -and $resEntry[0].sha256 -eq (Get-Sha256 $res)) ("receipt $($resEntry.name) $($resEntry.sha256); DLL $($wrReceipt.dllSha256)")
    New-ResFile $res ([Text.Encoding]::ASCII.GetBytes('resource B replaced at the same path'))
    Expect-Rejected 'provenance-after-win32-resource-replaced-rejected' (& $prov) 'Build inputs changed'
    [IO.File]::WriteAllText($localPaths, $localOriginal)
    Invoke-Isolated @('-File', (Join-Path $base 'scripts\Build.ps1'), '-SkipTests', '-ReplacePackage') | Out-Null
    $restoredReceipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json
    Add-Case 'rebuild-after-options-removed-reproduces-dll' ($restoredReceipt.dllSha256 -eq $receipt.dllSha256) "DLL $($restoredReceipt.dllSha256)"
    $receipt = $restoredReceipt
    # A response file (CompilerResponseFile) naming a resource: the response file and the resource it names are both
    # identified by content; changing only the resource loses qualification; the rebuilt pair qualifies.
    $rspResource = Join-Path $temp 'rsp resource.bin'
    [IO.File]::WriteAllText($rspResource, 'response-file resource A')
    $rsp = Join-Path $temp 'extra options.rsp'
    [IO.File]::WriteAllText($rsp, "# loader-baseline response file`r`n/resource:`"$rspResource`",KWR/RspProbe.bin`r`n")
    [IO.File]::WriteAllText($localPaths, $localOriginal.Replace('</Project>', "  <PropertyGroup><CompilerResponseFile>$([Security.SecurityElement]::Escape($rsp))</CompilerResponseFile></PropertyGroup>`r`n</Project>"))
    $rb = Invoke-Isolated @('-File', (Join-Path $base 'scripts\Build.ps1'), '-SkipTests', '-ReplacePackage')
    $rbReceipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json
    $rspEntry = @($rbReceipt.compilerInputs | Where-Object { $_.kind -eq 'response-file' -and $_.name -eq '@extra options.rsp' })
    $rspResEntry = @($rbReceipt.compilerInputs | Where-Object { $_.kind -eq 'file-option' -and $_.name -eq '/resource:rsp resource.bin,KWR/RspProbe.bin' })
    Add-Case 'build-with-response-file-records-it-and-its-resource' ($rb.ExitCode -eq 0 -and $rspEntry.Count -eq 1 -and $rspEntry[0].sha256 -eq (Get-Sha256 $rsp) -and $rspResEntry.Count -eq 1 -and $rspResEntry[0].sha256 -eq (Get-Sha256 $rspResource)) ("exit $($rb.ExitCode); $(@($rspEntry | ForEach-Object { $_.name + ' ' + $_.sha256 }) -join ', '); $(@($rspResEntry | ForEach-Object { $_.name + ' ' + $_.sha256 }) -join ', ')")
    Expect-Accepted 'provenance-response-file-build-current' (& $prov) 'source provenance: verified-current'
    $rspBefore = Get-Sha256 $rsp
    [IO.File]::WriteAllText($rspResource, 'response-file resource B (the response file itself is unchanged)')
    $r = & $prov
    Add-Case 'provenance-after-response-file-resource-changed-rejected' ($r.ExitCode -ne 0 -and ($r.Output -replace '\s+', ' ') -match 'Build inputs changed' -and (Get-Sha256 $rsp) -eq $rspBefore) ("response file unchanged ($rspBefore); " + (Get-Reason $r.Output))
    Invoke-Isolated @('-File', (Join-Path $base 'scripts\Build.ps1'), '-SkipTests', '-ReplacePackage') | Out-Null
    Expect-Accepted 'provenance-rebuilt-pair-after-resource-change-current' (& $prov) 'source provenance: verified-current'
    [IO.File]::WriteAllText($localPaths, $localOriginal)
    Invoke-Isolated @('-File', (Join-Path $base 'scripts\Build.ps1'), '-SkipTests', '-ReplacePackage') | Out-Null
    $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json
    # A documentation-only commit keeps the pair current (README is not a material input).
    [IO.File]::AppendAllText((Join-Path $base 'README.md'), "`r`n<!-- loader-baseline documentation-only commit -->`r`n")
    & git -C $base -c user.name=loader-baseline -c user.email=loader-baseline@invalid commit -q -am 'test: documentation-only change'
    $r = & $prov
    Add-Case 'provenance-after-documentation-only-commit-current' ($r.ExitCode -eq 0 -and $r.Output -match 'source provenance: verified-current' -and $receipt.commit -ne (& git -C $base rev-parse HEAD).Trim()) ("receipt commit $($receipt.commit); HEAD $((& git -C $base rev-parse HEAD).Trim()); " + [regex]::Match($r.Output, 'source provenance: [a-z-]+').Value)

    # 12. The report may not overwrite a protected input, by any spelling or link: the package, the given receipt, and the
    #     conventional receipt beside the package even when -ReceiptPath is omitted.
    $zipBefore = Get-Sha256 $candidateZip; $receiptBefore = Get-Sha256 $receiptPath
    Expect-Rejected 'report-path-equal-to-adjacent-receipt-without-receipt-argument-rejected' (Invoke-Isolated @('-File', $validator, '-PackagePath', $candidateZip, '-ReportPath', $receiptPath)) 'must not be an input'
    $link = Join-Path $temp 'hard link to package.zip'
    New-Item -ItemType HardLink -Path $link -Target $candidateZip | Out-Null
    Expect-Rejected 'report-path-hard-link-to-package-rejected' (Invoke-Isolated @('-File', $validator, '-PackagePath', $candidateZip, '-ReportPath', $link)) 'must not be an input'
    Add-Case 'report-identity-refusals-leave-inputs-intact' ((Get-Sha256 $candidateZip) -eq $zipBefore -and (Get-Sha256 $receiptPath) -eq $receiptBefore) "package $zipBefore and receipt $receiptBefore unchanged"
    # The identity the validator uses equals a trusted Windows read: vol (volume serial) and fsutil file queryfileid.
    $idOut = Invoke-Script (". $(Quote (Join-Path $base 'scripts\KwrFileIdentity.ps1')); 'ID ' + [KwrFileIdentityV3]::Of($(Quote $candidateZip))")
    $reported = [regex]::Match($idOut.Output, 'ID ([0-9a-f]{8}:[0-9a-f]{16})').Groups[1].Value
    $previousPreference = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
    try { $fsutilText = (& fsutil file queryfileid $candidateZip 2>&1 | ForEach-Object { "$_" }) -join ' ' }
    finally { $ErrorActionPreference = $previousPreference }
    $fileId = [regex]::Match($fsutilText, '0x([0-9a-fA-F]+)').Groups[1].Value.ToLowerInvariant()
    # The volume serial number from CIM (a hexadecimal string, independent of the display language).
    $volSerial = [string](Get-CimInstance -ClassName Win32_LogicalDisk -Filter "DeviceID='$($candidateZip.Substring(0, 2))'").VolumeSerialNumber
    $trusted = if ($fileId.Length -ge 16 -and $volSerial -match '\A[0-9A-Fa-f]{8}\z') { $volSerial.ToLowerInvariant() + ':' + $fileId.Substring($fileId.Length - 16) } else { '' }
    Add-Case 'file-identity-matches-trusted-windows-read' ($reported -and $reported -ceq $trusted) "validator $reported; fsutil/CIM $trusted ($fsutilText; volume serial $volSerial)"
    $distinctReport = Join-Path $temp 'distinct existing report.json'
    [IO.File]::WriteAllText($distinctReport, 'placeholder')
    $distinctIdBefore = [regex]::Match((Invoke-Script (". $(Quote (Join-Path $base 'scripts\KwrFileIdentity.ps1')); 'ID ' + [KwrFileIdentityV3]::Of($(Quote $distinctReport))")).Output, 'ID (\S+)').Groups[1].Value
    $dr = Invoke-Isolated @('-File', $validator, '-PackagePath', $candidateZip, '-ReceiptPath', $receiptPath, '-ReportPath', $distinctReport)
    $drJson = try { Get-Content -LiteralPath $distinctReport -Raw | ConvertFrom-Json } catch { $null }
    Add-Case 'report-path-distinct-existing-file-written' ($dr.ExitCode -eq 0 -and $drJson -and $drJson.package_sha256 -eq $zipBefore) "exit $($dr.ExitCode); report written in place of the distinct file (identity $distinctIdBefore); package digest $($drJson.package_sha256)"
    $validatorText = [IO.File]::ReadAllText($validator)
    $writeAnchor = '    [void][KwrFileIdentityV3]::WriteChecked($ReportPath, $protectedIds, $reportBytes)'
    if ($validatorText.IndexOf($writeAnchor) -lt 0) { throw 'Validate-Package.ps1 write anchor not found.' }
    $racingValidator = Join-Path $base 'scripts\Validate-Package-instrumented.ps1'
    [IO.File]::WriteAllText($racingValidator, $validatorText.Replace($writeAnchor, "    Remove-Item -LiteralPath `$ReportPath -Force; New-Item -ItemType HardLink -Path `$ReportPath -Target `$PackagePath | Out-Null`r`n" + $writeAnchor))
    $raceReport = Join-Path $temp 'race report.json'
    [IO.File]::WriteAllText($raceReport, 'distinct at check time')
    $race = Invoke-Isolated @('-File', $racingValidator, '-PackagePath', $candidateZip, '-ReceiptPath', $receiptPath, '-ReportPath', $raceReport)
    Remove-Item -LiteralPath $racingValidator
    # An original input moved to an initially distinct report destination right after its snapshot (a disposable
    # instrumented validator): the report publication refuses and the input bytes survive at the destination.
    $snapshotAnchor = '$snapshotIds = @($snapshotIds + @(& $identitiesNow) | Select-Object -Unique)'
    if ($validatorText.IndexOf($snapshotAnchor) -lt 0) { throw 'Validate-Package.ps1 snapshot anchor not found.' }
    $movedInput = Join-Path $temp 'moved input.zip'; Copy-Item -LiteralPath $candidateZip -Destination $movedInput
    $movedSha = Get-Sha256 $movedInput
    $movedReport = Join-Path $temp 'moved destination report.json'
    $movingValidator = Join-Path $base 'scripts\Validate-Package-instrumented.ps1'
    [IO.File]::WriteAllText($movingValidator, $validatorText.Replace($snapshotAnchor, $snapshotAnchor + "`r`nMove-Item -LiteralPath `$PackagePath -Destination `$ReportPath -Force"))
    $moved = Invoke-Isolated @('-File', $movingValidator, '-PackagePath', $movedInput, '-ReportPath', $movedReport)
    Remove-Item -LiteralPath $movingValidator
    $movedNow = if (Test-Path -LiteralPath $movedReport) { Get-Sha256 $movedReport } else { 'missing' }
    Add-Case 'input-moved-to-the-report-destination-after-snapshot-refused' ($moved.ExitCode -ne 0 -and ($moved.Output -replace '\s+', ' ') -match 'must not be an input of the validation' -and $movedNow -eq $movedSha) "exit $($moved.ExitCode); input bytes at the destination unchanged: $($movedNow -eq $movedSha)"
    # R4-F3: between the identity samples the supplied path is replaced; the replacement is the file read into the snapshot
    # and is then moved to the initially distinct report destination (the original is put back). Report publication
    # refuses and the bytes actually read survive at the destination.
    $readAnchor = '$packageBytes = [KwrFileIdentityV3]::ReadAll($PackagePath, [ref]$readId)'
    if ($validatorText.IndexOf($readAnchor) -lt 0) { throw 'Validate-Package.ps1 read anchor not found.' }
    $readInput = Join-Path $temp 'read input.zip'; Copy-Item -LiteralPath $candidateZip -Destination $readInput
    $replacementSource = Join-Path $temp 'replacement source.zip'; Copy-Item -LiteralPath $candidateZip -Destination $replacementSource
    Edit-Zip $candidateZip (Join-Path $temp 'replacement distinct.zip') { param($a) $e = $a.GetEntry("$modFolder/Info.json"); $e.LastWriteTime = $e.LastWriteTime.AddMinutes(3) }
    Move-Item -LiteralPath (Join-Path $temp 'replacement distinct.zip') -Destination $replacementSource -Force
    $replacementSha = Get-Sha256 $replacementSource
    $readReport = Join-Path $temp 'read destination report.json'; [IO.File]::WriteAllText($readReport, 'distinct destination')
    $swapBefore = "Move-Item -LiteralPath `$PackagePath -Destination (`$PackagePath + '.original'); Copy-Item -LiteralPath $(Quote $replacementSource) -Destination `$PackagePath"
    $swapAfter = "Move-Item -LiteralPath `$PackagePath -Destination `$ReportPath -Force; Move-Item -LiteralPath (`$PackagePath + '.original') -Destination `$PackagePath"
    $readingValidator = Join-Path $base 'scripts\Validate-Package-instrumented.ps1'
    [IO.File]::WriteAllText($readingValidator, $validatorText.Replace($readAnchor, $swapBefore + "`r`n" + $readAnchor + "`r`n" + $swapAfter))
    $readRace = Invoke-Isolated @('-File', $readingValidator, '-PackagePath', $readInput, '-ReportPath', $readReport)
    Remove-Item -LiteralPath $readingValidator
    $readNow = if (Test-Path -LiteralPath $readReport) { Get-Sha256 $readReport } else { 'missing' }
    Add-Case 'replacement-read-between-samples-then-moved-to-the-report-destination-refused' ($readRace.ExitCode -ne 0 -and ($readRace.Output -replace '\s+', ' ') -match 'must not be an input of the validation' -and $readNow -eq $replacementSha -and $replacementSha -ne (Get-Sha256 $readInput)) "exit $($readRace.ExitCode); bytes read (replacement $replacementSha) at the destination unchanged: $($readNow -eq $replacementSha); original back at the input path: $((Get-Sha256 $readInput) -eq (Get-Sha256 $candidateZip)); $(Get-Reason $readRace.Output)"
    Add-Case 'report-destination-replaced-during-validation-refused' ($race.ExitCode -ne 0 -and ($race.Output -replace '\s+', ' ') -match 'must not be an input of the validation \(file identity' -and (Get-Sha256 $candidateZip) -eq $zipBefore -and (Get-Sha256 $receiptPath) -eq $receiptBefore) ("exit $($race.ExitCode); " + (Get-Reason $race.Output) + "; package and receipt unchanged: $((Get-Sha256 $candidateZip) -eq $zipBefore -and (Get-Sha256 $receiptPath) -eq $receiptBefore)")
    Remove-Item -LiteralPath $raceReport -Force

    # 13. A failed replacement restores the previous validated ZIP and receipt.
    $buildText = [IO.File]::ReadAllText((Join-Path $base 'scripts\Build.ps1'))
    $placeAnchor = '        [IO.File]::Move($pendingZip, $zip); $done.Add(@($zip, $pendingZip))'
    if ($buildText.IndexOf($placeAnchor) -lt 0) { throw 'Build.ps1 placement anchor not found.' }
    $failingPlace = Join-Path $base 'scripts\Build-instrumented.ps1'
    [IO.File]::WriteAllText($failingPlace, $buildText.Replace($placeAnchor, $placeAnchor + "`r`n        throw 'injected failure after the new ZIP was placed'"))
    $f = Invoke-Isolated @('-File', $failingPlace, '-SkipTests', '-ReplacePackage')
    Remove-Item -LiteralPath $failingPlace
    Add-Case 'failed-replacement-restores-previous-zip-and-receipt' ($f.ExitCode -ne 0 -and $f.Output -match 'injected failure' -and (Get-Sha256 $candidateZip) -eq $zipBefore -and (Get-Sha256 $receiptPath) -eq $receiptBefore) ("exit $($f.ExitCode); ZIP and receipt restored: $((Get-Sha256 $candidateZip) -eq $zipBefore -and (Get-Sha256 $receiptPath) -eq $receiptBefore)")

    # 13b. A receipt held open without delete sharing by another process: the replacement fails, both previous files keep
    #      their bytes and still validate as a pair; after the lock is released the retry succeeds.
    $zipBefore = Get-Sha256 $candidateZip; $receiptBefore = Get-Sha256 $receiptPath
    $receiptLock = [IO.File]::Open($receiptPath, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    try {
        $locked = Invoke-Isolated @('-File', (Join-Path $base 'scripts\Build.ps1'), '-SkipTests', '-ReplacePackage')
        $kept = (Get-Sha256 $candidateZip) -eq $zipBefore -and (Get-Sha256 $receiptPath) -eq $receiptBefore
        Add-Case 'locked-receipt-replacement-fails-and-keeps-previous-pair' ($locked.ExitCode -ne 0 -and $kept) ("exit $($locked.ExitCode); " + (Get-Reason $locked.Output) + "; previous ZIP and receipt unchanged: $kept")
        Expect-Accepted 'locked-receipt-previous-pair-still-validates' (& $prov) 'source provenance: verified-current'
    }
    finally { $receiptLock.Dispose() }
    $retry = Invoke-Isolated @('-File', (Join-Path $base 'scripts\Build.ps1'), '-SkipTests', '-ReplacePackage')
    Add-Case 'retry-after-receipt-lock-released-succeeds' ($retry.ExitCode -eq 0 -and (& $prov).ExitCode -eq 0) "exit $($retry.ExitCode); ZIP $(Get-Sha256 $candidateZip)"

    # 13c. The packaged DLL is this invocation's compile: a valid older DLL at the conventional bin\Release path, a
    #      redirected OutputPath in the local props and a source change; the packaged DLL is the new compile.
    $conventional = Join-Path $base 'bin\Release\KingmakerWakingRune.dll'
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $conventional) | Out-Null
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $za = [IO.Compression.ZipFile]::OpenRead($candidateZip)
    try { [IO.Compression.ZipFileExtensions]::ExtractToFile($za.GetEntry("$modFolder/KingmakerWakingRune.dll"), $conventional, $true) } finally { $za.Dispose() }
    $olderDll = Get-Sha256 $conventional
    [IO.File]::WriteAllText($localPaths, $localOriginal.Replace('</Project>', "  <PropertyGroup><OutputPath>..\..\bin\Redirected\</OutputPath></PropertyGroup>`r`n</Project>"))
    $probeSource = @(& git -C $base ls-files 'src/KingmakerWakingRune/*.cs')[0]
    [IO.File]::AppendAllText((Join-Path $base $probeSource), "`r`nnamespace KwrOwnedOutputProbe { internal static class Marker { internal const int Value = 42; } }`r`n")
    $owned = Invoke-Isolated @('-File', (Join-Path $base 'scripts\Build.ps1'), '-SkipTests')
    $ownedZip = Join-Path $base "artifacts\KingmakerWakingRune-$cloneVersion-dirty.zip"
    $produced = [regex]::Match($owned.Output, "Product DLL SHA-256 \(this invocation's compile\): ([0-9a-f]{64})").Groups[1].Value
    $packaged = if (Test-Path -LiteralPath ($ownedZip + '.build.json')) { [string](Get-Content -LiteralPath ($ownedZip + '.build.json') -Raw | ConvertFrom-Json).dllSha256 } else { '' }
    Add-Case 'packaged-dll-is-this-invocations-compile' ($owned.ExitCode -eq 0 -and $produced -and $packaged -ceq $produced -and $produced -cne $olderDll -and (Get-Sha256 $conventional) -eq $olderDll) "older DLL at bin\Release $olderDll (unchanged); produced $produced; packaged $packaged"
    & git -C $base checkout -q -- $probeSource
    [IO.File]::WriteAllText($localPaths, $localOriginal)
    Remove-Item -LiteralPath $conventional, $ownedZip, ($ownedZip + '.build.json') -Force -ErrorAction SilentlyContinue

    # 14. Diagnostic staging versus frozen-candidate qualification for a receipt-backed dirty build.
    [IO.File]::AppendAllText((Join-Path $base 'README.md'), "`r`n<!-- loader-baseline diagnostic build -->`r`n")
    $diag = Invoke-Isolated @('-File', (Join-Path $base 'scripts\Build.ps1'), '-SkipTests')
    $dirtyZip = Join-Path $base "artifacts\KingmakerWakingRune-$cloneVersion-dirty.zip"
    & git -C $base checkout -q -- README.md
    Expect-Rejected 'install-helper-frozen-mode-refuses-dirty-receipt' (Invoke-Script (". $(Quote $kwrPackage); New-KwrValidatedPackageCopy $(Quote $dirtyZip) | Out-Null")) 'dirty working tree'
    $d = Invoke-Script (". $(Quote $kwrPackage); `$v = New-KwrValidatedPackageCopy $(Quote $dirtyZip) -Diagnostic; 'MODE ' + `$v.InstallMode + ' ' + `$v.SourceProvenance; Remove-KwrValidatedPackageCopy `$v")
    Add-Case 'install-helper-diagnostic-mode-stages-dirty-receipt-build' ($diag.ExitCode -eq 0 -and $d.ExitCode -eq 0 -and $d.Output -match 'MODE diagnostic receipt-matches-package') ((@($d.Output -split "`n" | Where-Object { $_ -match '^(MODE|ERROR)' }) -join ' '))
    $runtimeText = [IO.File]::ReadAllText((Join-Path $base 'scripts\KwrRuntime.ps1'))
    $reinstallAt = $runtimeText.IndexOf('function Invoke-Reinstall')
    $ok = $runtimeText.IndexOf("`$s.phase = 'reinstalling'", $reinstallAt) -gt $runtimeText.IndexOf('Expand-KwrValidatedPackageCopy $validated', $reinstallAt) -and
        $runtimeText.IndexOf('Remove-Item -LiteralPath $ModDir', $reinstallAt) -gt $runtimeText.IndexOf("`$s.phase = 'reinstalling'", $reinstallAt) -and
        $runtimeText -match "phase -notin @\('installed', 'closed'\)\) \{ throw 'No installed transaction ready to launch\.' \}" -and $runtimeText -match 'installMode = \$validated\.InstallMode'
    Add-Case 'kwrruntime-reinstall-verifies-staging-and-is-non-launchable-while-mutating' $ok 'Reinstall stages and verifies the new folder, records phase reinstalling before removing the installed mod (Launch accepts only installed/closed), and records the install mode (KwrRuntime.ps1 itself is not executed)'

    # 14b. The recorded qualification follows the validator: receiptless -> external-content-inspected (also with
    #      -Diagnostic), receipt-backed dirty build with -Diagnostic -> diagnostic, clean current pair -> frozen-candidate.
    $receiptless = Join-Path $inputs 'receiptless [external].zip'
    Copy-Item -LiteralPath $candidateZip -Destination $receiptless
    $modes = @()
    foreach ($spec in @(@($receiptless, ''), @($receiptless, ' -Diagnostic'), @($dirtyZip, ' -Diagnostic'), @($candidateZip, ''))) {
        $m = Invoke-Script (". $(Quote $kwrPackage); `$v = New-KwrValidatedPackageCopy $(Quote $spec[0])$($spec[1]); 'MODE ' + `$v.InstallMode; Remove-KwrValidatedPackageCopy `$v")
        $modes += [regex]::Match($m.Output, 'MODE (\S+)').Groups[1].Value
    }
    Add-Case 'install-helper-modes-follow-the-validator' (($modes -join ',') -ceq 'external-content-inspected,external-content-inspected,diagnostic,frozen-candidate') "receiptless: $($modes[0]); receiptless -Diagnostic: $($modes[1]); dirty receipt -Diagnostic: $($modes[2]); clean current pair: $($modes[3])"

    # 15. KwrRuntime's own Reinstall, Close and Launch functions (parsed from the script) against a temporary journal and
    #     Mods folder: a placement failure after the installed folder was removed leaves 'reinstalling'; Close keeps it;
    #     Launch refuses; Reinstall recovers to the previous phase; Launch is then admitted (a stub stops before Steam).
    $tokens = $null; $parseErrors = $null
    $runtimeAst = [Management.Automation.Language.Parser]::ParseFile((Join-Path $base 'scripts\KwrRuntime.ps1'), [ref]$tokens, [ref]$parseErrors)
    $wanted = @('Write-Step', 'Get-State', 'Save-State', 'Invoke-Reinstall', 'Invoke-Close', 'Invoke-Launch')
    $functionText = @($runtimeAst.FindAll({ param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst] -and $wanted -contains $n.Name }, $false) | ForEach-Object { $_.Extent.Text }) -join "`r`n"
    $runtimeFixture = Join-Path $temp 'runtime fixture'
    $fixtureScript = Join-Path $inputs 'runtime-fixture.ps1'
    [IO.File]::WriteAllText($fixtureScript, @"
`$ErrorActionPreference = 'Stop'
`$StateRoot = $(Quote (Join-Path $runtimeFixture 'state')); `$StatePath = Join-Path `$StateRoot 'transaction.json'
`$ModsRoot = $(Quote (Join-Path $runtimeFixture 'Mods')); `$ModDir = Join-Path `$ModsRoot 'KingmakerWakingRune'
`$PackagePath = $(Quote $candidateZip); `$Diagnostic = `$false; `$LaunchArgs = @(); `$AppId = '0'
New-Item -ItemType Directory -Force -Path `$StateRoot, `$ModDir | Out-Null
Set-Content -LiteralPath (Join-Path `$ModDir 'previous.txt') -Value 'previous install'
Set-Content -LiteralPath (Join-Path `$ModDir 'recon.flag') -Value 'recon'
. $(Quote $kwrPackage)
$functionText
function Get-GameProcesses { @() }
function Get-OwnedProcess(`$s) { `$null }
function Assert-SteamCloudOffline { throw 'LAUNCH-ADMITTED (fixture stub stops before Steam)' }
Save-State ([pscustomobject]@{ runId = 'fixture'; phase = 'closed'; gamePid = `$null; gameStart = `$null; package = 'previous'; packageSha256 = 'previous' })
function Move-Item { throw 'injected placement failure' }
try { Invoke-Reinstall } catch { 'REINSTALL-FAILED ' + `$_.Exception.Message }
Remove-Item -LiteralPath function:Move-Item
'PHASE-AFTER-FAILURE ' + (Get-State).phase + ' MODDIR ' + (Test-Path -LiteralPath `$ModDir)
'RECON-JOURNAL ' + (Get-State).reconEnabled
Invoke-Close
'PHASE-AFTER-CLOSE ' + (Get-State).phase
try { Invoke-Launch } catch { 'LAUNCH-1 ' + `$_.Exception.Message }
Invoke-Reinstall
'PHASE-AFTER-RECOVERY ' + (Get-State).phase + ' DLL ' + (Get-FileHash -LiteralPath (Join-Path `$ModDir 'KingmakerWakingRune.dll') -Algorithm SHA256).Hash.ToLowerInvariant()
'RECON-AFTER-RECOVERY ' + (Test-Path -LiteralPath (Join-Path `$ModDir 'recon.flag')) + ' JOURNAL ' + (Get-State).reconEnabled
try { Invoke-Launch } catch { 'LAUNCH-2 ' + `$_.Exception.Message }
"@)
    $rt = Invoke-Isolated @('-File', $fixtureScript)
    $rtText = $rt.Output -replace '\s+', ' '
    $ok = $rtText -match 'REINSTALL-FAILED injected placement failure' -and $rtText -match 'PHASE-AFTER-FAILURE reinstalling MODDIR False' -and $rtText -match 'PHASE-AFTER-CLOSE reinstalling' -and
        $rtText -match 'LAUNCH-1 No installed transaction ready to launch' -and $rtText -match ('PHASE-AFTER-RECOVERY closed DLL ' + [regex]::Escape([string]$receipt.dllSha256)) -and $rtText -match 'LAUNCH-2 LAUNCH-ADMITTED'
    Add-Case 'kwrruntime-interrupted-reinstall-survives-close-and-recovers' $ok ((@($rt.Output -split "`n" | Where-Object { $_ -match '^(REINSTALL-FAILED|PHASE-|LAUNCH-)' }) -join ' / '))
    # Recon mode enabled before the interrupted reinstall: the journal records it before the installed folder is removed,
    # and the resumed reinstall restores recon.flag from the journal.
    Add-Case 'kwrruntime-interrupted-reinstall-keeps-recon-mode' ($rtText -match 'RECON-JOURNAL True' -and $rtText -match 'RECON-AFTER-RECOVERY True JOURNAL True') ((@($rt.Output -split "`n" | Where-Object { $_ -match '^RECON-' }) -join ' / '))

    # R4-F4: an interrupted reinstall journal written by the previous script version (Begin's recon field, no
    # reconEnabled), phase reinstalling and no installed folder. The resumed Reinstall restores recon.flag from that field
    # and records reconEnabled before Launch admission; the same journal with recon=false is the control.
    foreach ($reconValue in @($true, $false)) {
        $legacyFixture = Join-Path $temp ("legacy journal fixture " + $reconValue)
        $legacyScript = Join-Path $inputs ("legacy-journal-fixture-$reconValue.ps1")
        [IO.File]::WriteAllText($legacyScript, @"
`$ErrorActionPreference = 'Stop'
`$StateRoot = $(Quote (Join-Path $legacyFixture 'state')); `$StatePath = Join-Path `$StateRoot 'transaction.json'
`$ModsRoot = $(Quote (Join-Path $legacyFixture 'Mods')); `$ModDir = Join-Path `$ModsRoot 'KingmakerWakingRune'
`$PackagePath = $(Quote $candidateZip); `$Diagnostic = `$false; `$LaunchArgs = @(); `$AppId = '0'
New-Item -ItemType Directory -Force -Path `$StateRoot, `$ModsRoot | Out-Null
. $(Quote $kwrPackage)
$functionText
function Get-GameProcesses { @() }
function Get-OwnedProcess(`$s) { `$null }
function Assert-SteamCloudOffline { throw 'LAUNCH-ADMITTED (fixture stub stops before Steam)' }
Save-State ([pscustomobject]@{ runId = 'legacy'; phase = 'reinstalling'; gamePid = `$null; gameStart = `$null; package = 'previous'; packageSha256 = 'previous'; recon = `$$reconValue })
'MODDIR-BEFORE ' + (Test-Path -LiteralPath `$ModDir)
Invoke-Reinstall
'RECON-FLAG ' + (Test-Path -LiteralPath (Join-Path `$ModDir 'recon.flag')) + ' JOURNAL ' + (Get-State).reconEnabled + ' PHASE ' + (Get-State).phase
try { Invoke-Launch } catch { 'LAUNCH ' + `$_.Exception.Message }
"@)
        $legacy = Invoke-Isolated @('-File', $legacyScript)
        $legacyText = $legacy.Output -replace '\s+', ' '
        $flagExpected = if ($reconValue) { 'True' } else { 'False' }
        $ok = $legacyText -match 'MODDIR-BEFORE False' -and $legacyText -match "RECON-FLAG $flagExpected JOURNAL $flagExpected PHASE closed" -and $legacyText -match 'LAUNCH LAUNCH-ADMITTED'
        Add-Case "kwrruntime-previous-format-journal-recon-$($flagExpected.ToLowerInvariant())-restored" $ok ((@($legacy.Output -split "`n" | Where-Object { $_ -match '^(MODDIR-|RECON-|LAUNCH)' }) -join ' / '))
    }

    # 15b. Round-5 boundary cases.
    # A receipt naming a commit with one extra icon under assets/ (the embedded-art tree is material as a whole, so an
    # added image is detected even though no receipt entry names it); the commit is created without moving HEAD.
    $savedRound5 = Join-Path $inputs 'round5-receipt.json'; Copy-Item -LiteralPath $receiptPath -Destination $savedRound5 -Force
    $extraIcon = 'assets/icons/LoaderBaselineExtra.png'
    $extraBlob = (& git -C $base hash-object -w (Join-Path $base 'assets\icons\RefugeToken.png')).Trim()
    $env:GIT_INDEX_FILE = Join-Path $inputs 'extra-icon.index'
    try { & git -C $base read-tree HEAD; & git -C $base update-index --add --cacheinfo "100644,$extraBlob,$extraIcon"; $extraTree = (& git -C $base write-tree).Trim() }
    finally { Remove-Item Env:GIT_INDEX_FILE }
    $extraCommit = (& git -C $base -c user.name=loader-baseline -c user.email=loader-baseline@invalid commit-tree $extraTree -p HEAD -m 'test: one extra icon').Trim()
    $extraDiff = @(& git -C $base diff --name-only HEAD $extraCommit)
    Edit-Json $receiptPath { param($j) $j.commit = $extraCommit }
    $r = & $prov
    Add-Case 'provenance-receipt-commit-with-an-extra-icon-rejected' ($r.ExitCode -ne 0 -and ($r.Output -replace '\s+', ' ') -match 'material inputs differ' -and ($extraDiff -join ',') -ceq $extraIcon) ("commit $extraCommit adds $($extraDiff -join ', '); " + (Get-Reason $r.Output))
    Copy-Item -LiteralPath $savedRound5 -Destination $receiptPath -Force
    Expect-Accepted 'provenance-current-again-after-extra-icon-case' (& $prov) 'source provenance: verified-current'
    # R1-F1: a valid PNG at an ignored path that the EmbeddedResource pattern assets/portraits/*/*.png embeds
    # (assets/portraits/obj/LocalOnly.png, ignored by obj/): the build cannot qualify it as a clean frozen candidate and the
    # existing pair's current provenance is declined; removed, qualification returns. A documentation file in the art tree
    # does not affect qualification.
    $ignoredArt = Join-Path $base 'assets\portraits\obj\LocalOnly.png'
    New-Item -ItemType Directory -Path (Split-Path -Parent $ignoredArt) -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $base 'assets\icons\RefugeToken.png') -Destination $ignoredArt
    $ignoredStatus = @(& git -C $base status --porcelain).Count
    $ignoredBuild = Invoke-Isolated @('-File', (Join-Path $base 'scripts\Build.ps1'), '-SkipTests', '-ReplacePackage')
    $embeddedIgnored = $ignoredBuild.Output -match 'Untracked or ignored files match the embedded artwork \(assets/portraits/obj/LocalOnly\.png\)'
    Add-Case 'build-with-ignored-embedded-image-not-qualified' ($ignoredStatus -eq 0 -and $ignoredBuild.ExitCode -ne 0 -and $embeddedIgnored -and (Get-Sha256 $receiptPath) -eq (Get-Sha256 $savedRound5)) ("git status entries $ignoredStatus (ignored); build exit $($ignoredBuild.ExitCode); " + (Get-Reason $ignoredBuild.Output) + "; previous pair kept: $((Get-Sha256 $receiptPath) -eq (Get-Sha256 $savedRound5))")
    Expect-Rejected 'provenance-with-ignored-embedded-image-rejected' (& $prov) 'Untracked or ignored files match the embedded artwork'
    Remove-Item -LiteralPath (Split-Path -Parent $ignoredArt) -Recurse -Force
    Expect-Accepted 'provenance-current-after-ignored-image-removed' (& $prov) 'source provenance: verified-current'
    $artNote = Join-Path $base 'assets\README.md'
    [IO.File]::WriteAllText($artNote, "Artwork notes (documentation only).`r`n")
    Expect-Accepted 'provenance-current-with-art-tree-documentation-change' (& $prov) 'source provenance: verified-current'
    Remove-Item -LiteralPath $artNote
    # An unchanged response file whose /recurse names external sources: wildcard recursion cannot be bound to the receipt,
    # so the build refuses it and current qualification is declined while it is configured.
    $recurseDir = Join-Path $temp 'recurse sources'; New-Item -ItemType Directory -Path $recurseDir -Force | Out-Null
    [IO.File]::WriteAllText((Join-Path $recurseDir 'RecurseProbe.cs'), 'namespace KwrRecurseProbe { internal static class Marker { } }')
    $recurseRsp = Join-Path $temp 'recurse options.rsp'
    [IO.File]::WriteAllText($recurseRsp, "/recurse:`"$recurseDir\*.cs`"`r`n")
    [IO.File]::WriteAllText($localPaths, $localOriginal.Replace('</Project>', "  <PropertyGroup><CompilerResponseFile>$([Security.SecurityElement]::Escape($recurseRsp))</CompilerResponseFile></PropertyGroup>`r`n</Project>"))
    Expect-Rejected 'build-with-response-file-recursion-refused' (Invoke-Isolated @('-File', (Join-Path $base 'scripts\Build.ps1'), '-SkipTests', '-ReplacePackage')) 'Unsupported compiler input: /recurse'
    Expect-Rejected 'provenance-with-response-file-recursion-declined' (& $prov) 'Unsupported compiler input: /recurse'
    [IO.File]::WriteAllText($localPaths, $localOriginal)
    Expect-Accepted 'provenance-current-again-after-recursion-removed' (& $prov) 'source provenance: verified-current'
    # Offline pin definitions agree with acquisition: the UnityModManager NuGet source with both archive fields cleared is
    # refused offline; the raw 0Harmony12 source with both archive keys omitted is accepted offline and acquired cold.
    $cacheAside = Join-Path $temp 'cache aside round5'
    Move-Item -LiteralPath $references -Destination $cacheAside
    Edit-Json $pinsFile { param($j) $p = @($j.references | Where-Object Name -eq 'UnityModManager')[0]; $p.ArchiveEntry = $null; $p.ArchiveSha256 = $null }
    Expect-Rejected 'validator-offline-nuget-pin-without-archive-fields-rejected' (& $v $candidateZip) 'NuGet package source of UnityModManager\.dll needs ArchiveEntry and ArchiveSha256'
    & git -C $base checkout -q -- scripts/loader-reference-pins.json
    Edit-Json $pinsFile { param($j) $p = @($j.references | Where-Object Name -eq '0Harmony12')[0]; [void]$p.PSObject.Properties.Remove('ArchiveEntry'); [void]$p.PSObject.Properties.Remove('ArchiveSha256') }
    Expect-Accepted 'validator-offline-raw-pin-with-omitted-archive-keys-accepted' (& $v $candidateZip) 'Package validation passed'
    $coldCache = Join-Path $temp 'cold cache round5'
    $cold = Invoke-Isolated @('-File', (Join-Path $base 'scripts\Get-PinnedLoaderReferences.ps1'), '-ReferenceDirectory', $coldCache)
    $harmonyPin = @((Get-Content -LiteralPath $pinsFile -Raw | ConvertFrom-Json).references | Where-Object Name -eq '0Harmony12')[0]
    Add-Case 'cold-acquisition-of-the-raw-pin-with-omitted-archive-keys' ($cold.ExitCode -eq 0 -and (Test-Path -LiteralPath (Join-Path $coldCache '0Harmony12.dll')) -and (Get-Sha256 (Join-Path $coldCache '0Harmony12.dll')) -eq [string]$harmonyPin.Sha256) "exit $($cold.ExitCode); 0Harmony12.dll $(if (Test-Path -LiteralPath (Join-Path $coldCache '0Harmony12.dll')) { Get-Sha256 (Join-Path $coldCache '0Harmony12.dll') })"
    & git -C $base checkout -q -- scripts/loader-reference-pins.json
    Move-Item -LiteralPath $cacheAside -Destination $references
    # R4-F2: one test-enabled Build.ps1 invocation with the test project's OutputPath redirected in the local props
    # (active: MSBuild evaluates it to the redirected folder) and a stale passing runner (prints STALE TEST RUNNER, exits 0)
    # both at bin\Tests and at the redirected folder. The packaging gate is the runner this invocation built: its own
    # result line appears, the stale runners never answer and keep their digests, and the package names this compile.
    $staleSource = Join-Path $inputs 'stale-runner.cs'
    [IO.File]::WriteAllText($staleSource, 'public static class P { public static int Main() { System.Console.WriteLine("STALE TEST RUNNER"); return 0; } }')
    $frameworkCsc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
    $staleDirs = @((Join-Path $base 'bin\Tests'), (Join-Path $base 'bin\Redirected'))
    foreach ($dir in $staleDirs) { New-Item -ItemType Directory -Path $dir -Force | Out-Null; $null = & $frameworkCsc /nologo /target:exe "/out:$(Join-Path $dir 'KingmakerWakingRune.Tests.exe')" $staleSource }
    $staleBefore = @($staleDirs | ForEach-Object { Get-Sha256 (Join-Path $_ 'KingmakerWakingRune.Tests.exe') }) -join '/'
    [IO.File]::WriteAllText($localPaths, $localOriginal.Replace('</Project>', "  <PropertyGroup><OutputPath>..\bin\Redirected\</OutputPath></PropertyGroup>`r`n</Project>"))
    $evaluated = Invoke-MSBuildIsolated @((Join-Path $base 'tests\KingmakerWakingRune.Tests.csproj'), '-getProperty:OutputPath', '-p:Configuration=Release')
    $redirectActive = $evaluated.ExitCode -eq 0 -and $evaluated.Output.Trim() -ceq '..\bin\Redirected\'
    $withTests = Invoke-Isolated @('-File', (Join-Path $base 'scripts\Build.ps1'), '-ReplacePackage')
    [IO.File]::WriteAllText($localPaths, $localOriginal)
    $staleAfter = @($staleDirs | ForEach-Object { Get-Sha256 (Join-Path $_ 'KingmakerWakingRune.Tests.exe') }) -join '/'
    $ownRunner = [regex]::Match($withTests.Output, 'Tests: \d+ passed, 0 failed').Value
    $producedWithTests = [regex]::Match($withTests.Output, "Product DLL SHA-256 \(this invocation's compile\): ([0-9a-f]{64})").Groups[1].Value
    $placedWithTests = [string](Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json).dllSha256
    Add-Case 'packaging-gated-by-this-invocations-tests' ($redirectActive -and $withTests.ExitCode -eq 0 -and $withTests.Output -notmatch 'STALE TEST RUNNER' -and $ownRunner -and $staleAfter -ceq $staleBefore -and $producedWithTests -and $placedWithTests -ceq $producedWithTests) ("evaluated test OutputPath '$($evaluated.Output.Trim())' (redirect active: $redirectActive); exit $($withTests.ExitCode); own runner: '$ownRunner'; stale runners answered: $($withTests.Output -match 'STALE TEST RUNNER'); stale digests unchanged: $($staleAfter -ceq $staleBefore); packaged DLL is this compile: $($placedWithTests -ceq $producedWithTests)")
    foreach ($dir in $staleDirs) { Remove-Item -LiteralPath $dir -Recurse -Force }
    # One publisher per destination. While another holder has the destination's publish lock a build refuses at once and
    # places nothing (no queued publication).
    $pairBefore = (Get-Sha256 $candidateZip) + '/' + (Get-Sha256 $receiptPath)
    $holder = [IO.FileStream]::new(($candidateZip + '.publish.lock'), [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None, 1, [IO.FileOptions]::DeleteOnClose)
    try { $held = Invoke-Isolated @('-File', (Join-Path $base 'scripts\Build.ps1'), '-SkipTests', '-ReplacePackage') } finally { $holder.Dispose() }
    Add-Case 'build-refuses-while-the-destination-is-being-published' ($held.ExitCode -ne 0 -and ($held.Output -replace '\s+', ' ') -match 'Another build is publishing' -and ((Get-Sha256 $candidateZip) + '/' + (Get-Sha256 $receiptPath)) -ceq $pairBefore) ("exit $($held.ExitCode); " + (Get-Reason $held.Output) + "; previous pair unchanged: $(((Get-Sha256 $candidateZip) + '/' + (Get-Sha256 $receiptPath)) -ceq $pairBefore)")
    $buildText = [IO.File]::ReadAllText((Join-Path $base 'scripts\Build.ps1'))
    $underLock = '        # Under the lock, before any file is moved: the source and material inputs must still be the ones this build used.'
    if ($buildText.IndexOf($underLock) -lt 0) { throw 'Build.ps1 under-lock anchor not found.' }
    # R5-F2: a material input changes after this build's private validation and before placement (a disposable
    # instrumented Build edits a tracked source just before taking the lock): the final check under the lock declines
    # placement and the previous pair keeps its hashes.
    $lateSource = @(& git -C $base ls-files 'src/KingmakerWakingRune/*.cs')[0]
    $lockAnchor = "    try { `$publishLock = [IO.FileStream]::new((`$zip + '.publish.lock'), [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None, 1, [IO.FileOptions]::DeleteOnClose) }"
    if ($buildText.IndexOf($lockAnchor) -lt 0) { throw 'Build.ps1 lock anchor not found.' }
    $lateBuild = Join-Path $base 'scripts\Build-instrumented.ps1'
    [IO.File]::WriteAllText($lateBuild, $buildText.Replace('$PSScriptRoot', (Quote (Join-Path $base 'scripts'))).Replace($lockAnchor, "    [IO.File]::AppendAllText($(Quote (Join-Path $base $lateSource)), ""``r``n// changed after validation``r``n"")`r`n" + $lockAnchor))
    $late = Invoke-Isolated @('-File', $lateBuild, '-SkipTests', '-ReplacePackage')
    Remove-Item -LiteralPath $lateBuild
    $lateChanged = [bool](& git -C $base status --porcelain -- $lateSource)
    & git -C $base checkout -q -- $lateSource
    Add-Case 'build-declines-placement-when-an-input-changed-after-validation' ($late.ExitCode -ne 0 -and ($late.Output -replace '\s+', ' ') -match 'HEAD or a build input changed after this build was validated; nothing was placed' -and $lateChanged -and ((Get-Sha256 $candidateZip) + '/' + (Get-Sha256 $receiptPath)) -ceq $pairBefore) ("source edited after validation: $lateChanged; exit $($late.ExitCode); " + (Get-Reason $late.Output) + "; previous pair unchanged: $(((Get-Sha256 $candidateZip) + '/' + (Get-Sha256 $receiptPath)) -ceq $pairBefore)")
    # R4-F1: two real overlapping publishers of distinguishable compiles. Publisher A (default settings; a disposable
    # instrumented Build that pauses right after taking the destination lock) and publisher B (the real Build.ps1 with the
    # process-local compiler setting CheckForOverflowUnderflow=true, so its DLL differs). B refuses while A holds the
    # lock; A then places its pair. The surviving ZIP and receipt are A's and validate under A's settings.
    $signal = Join-Path $temp 'publisher signals'; New-Item -ItemType Directory -Path $signal -Force | Out-Null
    $holdInsert = "`r`n        [IO.File]::WriteAllText($(Quote (Join-Path $signal 'A-holds-lock')), 'held'); `$waitUntil = [DateTime]::UtcNow.AddMinutes(10); while (-not [IO.File]::Exists($(Quote (Join-Path $signal 'A-continue'))) -and [DateTime]::UtcNow -lt `$waitUntil) { Start-Sleep -Milliseconds 200 }"
    $publisherA = Join-Path $base 'scripts\Build-publisherA-instrumented.ps1'
    [IO.File]::WriteAllText($publisherA, $buildText.Replace('$PSScriptRoot', (Quote (Join-Path $base 'scripts'))).Replace($underLock, $underLock + $holdInsert))
    $logA = Join-Path $inputs 'publisher-a.txt'
    $procA = Start-Process -FilePath powershell.exe -ArgumentList @('-NoLogo', '-NoProfile', '-NonInteractive', '-ExecutionPolicy', 'Bypass', '-File', ('"' + $publisherA + '"'), '-SkipTests', '-ReplacePackage') -RedirectStandardOutput $logA -RedirectStandardError ($logA + '.err') -PassThru -WindowStyle Hidden
    $null = $procA.Handle
    $deadline = (Get-Date).AddMinutes(10)
    while (-not $procA.HasExited -and -not (Test-Path -LiteralPath (Join-Path $signal 'A-holds-lock')) -and (Get-Date) -lt $deadline) { Start-Sleep -Milliseconds 500 }
    $aHeld = Test-Path -LiteralPath (Join-Path $signal 'A-holds-lock')
    $env:CheckForOverflowUnderflow = 'true'
    try { $publisherB = Invoke-Isolated @('-File', (Join-Path $base 'scripts\Build.ps1'), '-SkipTests', '-ReplacePackage') } finally { Remove-Item Env:\CheckForOverflowUnderflow }
    $pairWhileAHeld = (Get-Sha256 $candidateZip) + '/' + (Get-Sha256 $receiptPath)
    [IO.File]::WriteAllText((Join-Path $signal 'A-continue'), 'go')
    $procA.WaitForExit()
    Remove-Item -LiteralPath $publisherA
    $textA = Get-Content -LiteralPath $logA -Raw
    $dllA = [regex]::Match($textA, "Product DLL SHA-256 \(this invocation's compile\): ([0-9a-f]{64})").Groups[1].Value
    $dllB = [regex]::Match($publisherB.Output, "Product DLL SHA-256 \(this invocation's compile\): ([0-9a-f]{64})").Groups[1].Value
    $survivor = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json
    $winnerValid = & $prov
    $ok = $aHeld -and $publisherB.ExitCode -ne 0 -and ($publisherB.Output -replace '\s+', ' ') -match 'Another build is publishing' -and $pairWhileAHeld -ceq $pairBefore -and
        $procA.ExitCode -eq 0 -and $dllA -and $dllB -and $dllA -cne $dllB -and [string]$survivor.dllSha256 -ceq $dllA -and [string]$survivor.zipSha256 -ceq (Get-Sha256 $candidateZip) -and
        $winnerValid.ExitCode -eq 0 -and $winnerValid.Output -match 'source provenance: verified-current'
    Add-Case 'overlapping-publishers-the-lock-holder-owns-the-surviving-pair' $ok ("A held the lock: $aHeld; compiles A $dllA / B $dllB; B exit $($publisherB.ExitCode) (" + (Get-Reason $publisherB.Output) + "); pair while A held unchanged: $($pairWhileAHeld -ceq $pairBefore); A exit $($procA.ExitCode); surviving receipt DLL $($survivor.dllSha256), ZIP $($survivor.zipSha256); winner validates: $($winnerValid.ExitCode -eq 0)")
    # R5-F1: an installer read during publication. A dirty pair exists; a second dirty build (disposable instrumented
    # Build) pauses between placing its ZIP and its receipt, then fails the receipt placement and rolls back. During the
    # pause the real copy helper declines (no default-mode admission of the half-published pair); after the rollback the
    # restored dirty pair is refused in default mode and admitted only as -Diagnostic. A receiptless ZIP outside the
    # build-owned folder keeps content-only inspection.
    [IO.File]::AppendAllText((Join-Path $base 'README.md'), "`r`n<!-- loader-baseline publication read -->`r`n")
    $firstDirty = Invoke-Isolated @('-File', (Join-Path $base 'scripts\Build.ps1'), '-SkipTests')
    $dirtyZipR5 = Join-Path $base "artifacts\KingmakerWakingRune-$cloneVersion-dirty.zip"
    $dirtyPairBefore = if ((Test-Path -LiteralPath $dirtyZipR5) -and (Test-Path -LiteralPath ($dirtyZipR5 + '.build.json'))) { (Get-Sha256 $dirtyZipR5) + '/' + (Get-Sha256 ($dirtyZipR5 + '.build.json')) } else { 'missing' }
    $zipPlace = '        [IO.File]::Move($pendingZip, $zip); $done.Add(@($zip, $pendingZip))'
    if ($buildText.IndexOf($zipPlace) -lt 0) { throw 'Build.ps1 ZIP placement anchor not found.' }
    $pauseInsert = "`r`n        [IO.File]::WriteAllText($(Quote (Join-Path $signal 'zip-placed')), 'placed'); `$waitUntil = [DateTime]::UtcNow.AddMinutes(10); while (-not [IO.File]::Exists($(Quote (Join-Path $signal 'receipt-go'))) -and [DateTime]::UtcNow -lt `$waitUntil) { Start-Sleep -Milliseconds 200 }; throw 'injected receipt placement failure'"
    $pausing = Join-Path $base 'scripts\Build-pausing-instrumented.ps1'
    [IO.File]::WriteAllText($pausing, $buildText.Replace('$PSScriptRoot', (Quote (Join-Path $base 'scripts'))).Replace($zipPlace, $zipPlace + $pauseInsert))
    $logP = Join-Path $inputs 'pausing-publisher.txt'
    $procP = Start-Process -FilePath powershell.exe -ArgumentList @('-NoLogo', '-NoProfile', '-NonInteractive', '-ExecutionPolicy', 'Bypass', '-File', ('"' + $pausing + '"'), '-SkipTests') -RedirectStandardOutput $logP -RedirectStandardError ($logP + '.err') -PassThru -WindowStyle Hidden
    $null = $procP.Handle
    $deadline = (Get-Date).AddMinutes(10)
    while (-not $procP.HasExited -and -not (Test-Path -LiteralPath (Join-Path $signal 'zip-placed')) -and (Get-Date) -lt $deadline) { Start-Sleep -Milliseconds 500 }
    $paused = Test-Path -LiteralPath (Join-Path $signal 'zip-placed')
    $receiptVisible = Test-Path -LiteralPath ($dirtyZipR5 + '.build.json')
    $duringPause = Invoke-Script (". $(Quote $kwrPackage); `$v = New-KwrValidatedPackageCopy $(Quote $dirtyZipR5); 'MODE ' + `$v.InstallMode; Remove-KwrValidatedPackageCopy `$v")
    [IO.File]::WriteAllText((Join-Path $signal 'receipt-go'), 'go')
    $procP.WaitForExit()
    Remove-Item -LiteralPath $pausing
    $textP = (Get-Content -LiteralPath $logP -Raw) + "`n" + (Get-Content -LiteralPath ($logP + '.err') -Raw)
    $dirtyPairAfter = if ((Test-Path -LiteralPath $dirtyZipR5) -and (Test-Path -LiteralPath ($dirtyZipR5 + '.build.json'))) { (Get-Sha256 $dirtyZipR5) + '/' + (Get-Sha256 ($dirtyZipR5 + '.build.json')) } else { 'missing' }
    $afterDefault = Invoke-Script (". $(Quote $kwrPackage); `$v = New-KwrValidatedPackageCopy $(Quote $dirtyZipR5); 'MODE ' + `$v.InstallMode; Remove-KwrValidatedPackageCopy `$v")
    $afterDiagnostic = Invoke-Script (". $(Quote $kwrPackage); `$v = New-KwrValidatedPackageCopy $(Quote $dirtyZipR5) -Diagnostic; 'MODE ' + `$v.InstallMode; Remove-KwrValidatedPackageCopy `$v")
    $externalZip = Join-Path $temp 'external package.zip'; Copy-Item -LiteralPath $candidateZip -Destination $externalZip
    $external = Invoke-Script (". $(Quote $kwrPackage); `$v = New-KwrValidatedPackageCopy $(Quote $externalZip); 'MODE ' + `$v.InstallMode; Remove-KwrValidatedPackageCopy `$v")
    $ok = $firstDirty.ExitCode -eq 0 -and $dirtyPairBefore -ne 'missing' -and $paused -and -not $receiptVisible -and $duringPause.ExitCode -ne 0 -and ($duringPause.Output -replace '\s+', ' ') -match 'A build is publishing' -and $duringPause.Output -notmatch 'MODE ' -and
        $procP.ExitCode -ne 0 -and $textP -match 'injected receipt placement failure' -and $dirtyPairAfter -ceq $dirtyPairBefore -and
        $afterDefault.ExitCode -ne 0 -and ($afterDefault.Output -replace '\s+', ' ') -match 'dirty working tree|not verified as current' -and $afterDiagnostic.ExitCode -eq 0 -and $afterDiagnostic.Output -match 'MODE diagnostic' -and
        $external.ExitCode -eq 0 -and $external.Output -match 'MODE external-content-inspected'
    Add-Case 'installer-read-during-publication-declined-and-rollback-exposes-the-restored-pair' $ok ("paused between placements: $paused (receipt visible: $receiptVisible); during the pause: " + (Get-Reason $duringPause.Output) + "; publisher exit $($procP.ExitCode); restored dirty pair equals the previous: $($dirtyPairAfter -ceq $dirtyPairBefore); after rollback default: " + (Get-Reason $afterDefault.Output) + "; -Diagnostic: " + ([regex]::Match($afterDiagnostic.Output, 'MODE \S+').Value) + "; external receiptless ZIP: " + ([regex]::Match($external.Output, 'MODE \S+').Value))
    & git -C $base checkout -q -- README.md
    Remove-Item -LiteralPath $dirtyZipR5, ($dirtyZipR5 + '.build.json'), $externalZip -Force -ErrorAction SilentlyContinue

    # 16. Shipping version 0.12.99: the alternate fixture version differs, and both pairs are built and validated without
    #     replacing the shipping pair.
    #     The regression runs in its own temporary checkout of the clone (warm reference cache copied), so its artifacts
    #     never meet the pairs created at suite entry.
    $base99 = Join-Path $temp 'clone 0.12.99'
    & git clone -q --no-hardlinks $base $base99
    if ($LASTEXITCODE -ne 0) { throw 'git clone of the clone failed.' }
    Copy-Item -LiteralPath (Join-Path $base 'LocalGamePaths.props') -Destination $base99
    New-Item -ItemType Directory -Path (Join-Path $base99 'obj') -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $base 'obj\loader-references') -Destination (Join-Path $base99 'obj\loader-references') -Recurse
    $validator99 = Join-Path $base99 'scripts\Validate-Package.ps1'
    Edit-Json (Join-Path $base99 'Info.json') { param($j) $j.Version = '0.12.99' }
    & git -C $base99 -c user.name=loader-baseline -c user.email=loader-baseline@invalid commit -q -am 'test: shipping version 0.12.99'
    $b99 = Invoke-Isolated @('-File', (Join-Path $base99 'scripts\Build.ps1'), '-SkipTests')
    $zip99 = Join-Path $base99 'artifacts\KingmakerWakingRune-0.12.99.zip'
    $pair99 = if (Test-Path -LiteralPath ($zip99 + '.build.json')) { (Get-Sha256 $zip99) + '/' + (Get-Sha256 ($zip99 + '.build.json')) } else { 'missing' }
    # Validated while 0.12.99 is the repository's version (the validator requires the packaged manifest to match it).
    $v99 = Invoke-Isolated @('-File', $validator99, '-PackagePath', $zip99, '-ReceiptPath', ($zip99 + '.build.json'), '-RequireCurrentProvenance')
    $alt99 = Get-AlternateFixtureVersion '0.12.99'
    Edit-Json (Join-Path $base99 'Info.json') { param($j) $j.Version = $alt99 }
    & git -C $base99 -c user.name=loader-baseline -c user.email=loader-baseline@invalid commit -q -am 'test: alternate fixture version'
    $bAlt = Invoke-Isolated @('-File', (Join-Path $base99 'scripts\Build.ps1'), '-SkipTests')
    $zipAlt = Join-Path $base99 "artifacts\KingmakerWakingRune-$alt99.zip"
    $vAlt = Invoke-Isolated @('-File', $validator99, '-PackagePath', $zipAlt, '-ReceiptPath', ($zipAlt + '.build.json'), '-RequireCurrentProvenance')
    $pair99After = if (Test-Path -LiteralPath ($zip99 + '.build.json')) { (Get-Sha256 $zip99) + '/' + (Get-Sha256 ($zip99 + '.build.json')) } else { 'missing' }
    $ok = $alt99 -cne '0.12.99' -and $b99.ExitCode -eq 0 -and $bAlt.ExitCode -eq 0 -and $v99.ExitCode -eq 0 -and $vAlt.ExitCode -eq 0 -and $vAlt.Output -match 'verified-current' -and
        $pair99 -ne 'missing' -and $pair99 -ceq $pair99After -and (Get-Sha256 $zipAlt) -ne (Get-Sha256 $zip99)
    Add-Case 'shipping-0.12.99-and-alternate-fixture-are-distinct-validated-packages' $ok "builds exit $($b99.ExitCode)/$($bAlt.ExitCode); validation (current provenance) 0.12.99 exit $($v99.ExitCode) before the version change, $alt99 exit $($vAlt.ExitCode); alternate $alt99; 0.12.99 pair $pair99 (after alternate build: $pair99After); alternate ZIP $(if (Test-Path -LiteralPath $zipAlt) { Get-Sha256 $zipAlt } else { 'missing' })"

    $artifactsAfter = @(Get-ChildItem -LiteralPath (Join-Path $source 'artifacts') -File -ErrorAction SilentlyContinue | ForEach-Object { $_.Name + '=' + (Get-Sha256 $_.FullName) }) -join ';'
    Add-Case 'repository-artifacts-untouched' ($sourceArtifactsBefore -eq $artifactsAfter) 'this checkout''s artifacts folder is unchanged'
    Add-Case 'legacy-input-preserved' ((Get-Sha256 $legacyZip) -eq $legacySha256) "private copy still $legacySha256"

    $failed = @($cases | Where-Object { -not $_.passed })
    $summary = [ordered]@{
        schema = 'kwr.loader-baseline-tests.v3'
        test_clone_commit = $testCommit
        source_commit = $commit
        clone_build_receipt = $cloneReceipt
        rebuild_receipt = [ordered]@{ note = 'receipt of the rebuild after restoring the section-11 compiler settings, recorded separately from the clone build'; receipt = $receipt }
        legacy_release_zip_sha256 = $legacySha256
        wrong_fixtures = @("UnityModManager 0.32.4.0 $wrongLoaderDllSha256", "0Harmony 2.3.6.0 net48 $wrongHarmonyDllSha256")
        owner_install_measured = $owner
        passed = $cases.Count - $failed.Count
        total = $cases.Count
        cases = @($cases)
    }
    if ($ReportPath) { $summary | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $ReportPath -Encoding UTF8 }
    Write-Host ("RESULT {0}/{1} loader-baseline checks passed" -f $summary.passed, $summary.total)
    if ($failed.Count -gt 0) { throw "$($failed.Count) loader-baseline check(s) failed." }
}
finally {
    Remove-Item -LiteralPath $temp -Recurse -Force -ErrorAction SilentlyContinue
}
