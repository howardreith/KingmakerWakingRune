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
    Edit-Json (Join-Path $base 'Info.json') { param($j) $j.Version = '0.12.99' }
    & git -C $base -c user.name=loader-baseline -c user.email=loader-baseline@invalid commit -q -am 'test: disposable product version for the boundary suite'
    if ($LASTEXITCODE -ne 0) { throw 'Version commit in the clone failed.' }
    $testCommit = (& git -C $base rev-parse HEAD).Trim()
    $cloneVersion = [string](Get-Content -LiteralPath (Join-Path $base 'Info.json') -Raw | ConvertFrom-Json).Version
    $build = Invoke-Isolated @('-File', (Join-Path $base 'scripts\Build.ps1'))
    $candidateZip = Join-Path $base "artifacts\KingmakerWakingRune-$cloneVersion.zip"
    $receiptPath = $candidateZip + '.build.json'
    if ($build.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $receiptPath)) { throw "Clone build failed: $($build.Output)" }
    $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json
    Add-Case 'clone-build-at-different-version' ((Test-Path -LiteralPath (Join-Path $references 'UnityModManager.dll')) -and $receipt.version -eq '0.12.99' -and $receipt.commit -eq $testCommit -and -not $receipt.dirty -and $build.Output -match 'Tests: \d+ passed, 0 failed') ("receipt commit $($receipt.commit) dirty $($receipt.dirty); DLL $($receipt.dllSha256); ZIP $($receipt.zipSha256); " + [regex]::Match($build.Output, 'Tests: \d+ passed, 0 failed').Value)
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
    $leftovers = @(Get-ChildItem -LiteralPath (Join-Path $base 'artifacts') -Force | Where-Object { $_.Name -like '.pending-*' -or $_.Name -like 'KingmakerWakingRune-*.zip*' } | ForEach-Object Name)
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

    # 12. The report may not overwrite a protected input, by any spelling or link: the package, the given receipt, and the
    #     conventional receipt beside the package even when -ReceiptPath is omitted.
    $zipBefore = Get-Sha256 $candidateZip; $receiptBefore = Get-Sha256 $receiptPath
    Expect-Rejected 'report-path-equal-to-adjacent-receipt-without-receipt-argument-rejected' (Invoke-Isolated @('-File', $validator, '-PackagePath', $candidateZip, '-ReportPath', $receiptPath)) 'must not be an input'
    $link = Join-Path $temp 'hard link to package.zip'
    New-Item -ItemType HardLink -Path $link -Target $candidateZip | Out-Null
    Expect-Rejected 'report-path-hard-link-to-package-rejected' (Invoke-Isolated @('-File', $validator, '-PackagePath', $candidateZip, '-ReportPath', $link)) 'must not be an input'
    Add-Case 'report-identity-refusals-leave-inputs-intact' ((Get-Sha256 $candidateZip) -eq $zipBefore -and (Get-Sha256 $receiptPath) -eq $receiptBefore) "package $zipBefore and receipt $receiptBefore unchanged"

    # 13. A failed replacement restores the previous validated ZIP and receipt.
    $buildText = [IO.File]::ReadAllText((Join-Path $base 'scripts\Build.ps1'))
    $placeAnchor = '        [IO.File]::Move($pendingZip, $zip)'
    if ($buildText.IndexOf($placeAnchor) -lt 0) { throw 'Build.ps1 placement anchor not found.' }
    $failingPlace = Join-Path $base 'scripts\Build-instrumented.ps1'
    [IO.File]::WriteAllText($failingPlace, $buildText.Replace($placeAnchor, $placeAnchor + "`r`n        throw 'injected failure after the new ZIP was placed'"))
    $f = Invoke-Isolated @('-File', $failingPlace, '-SkipTests', '-ReplacePackage')
    Remove-Item -LiteralPath $failingPlace
    Add-Case 'failed-replacement-restores-previous-zip-and-receipt' ($f.ExitCode -ne 0 -and $f.Output -match 'injected failure' -and (Get-Sha256 $candidateZip) -eq $zipBefore -and (Get-Sha256 $receiptPath) -eq $receiptBefore) ("exit $($f.ExitCode); ZIP and receipt restored: $((Get-Sha256 $candidateZip) -eq $zipBefore -and (Get-Sha256 $receiptPath) -eq $receiptBefore)")

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

    $artifactsAfter = @(Get-ChildItem -LiteralPath (Join-Path $source 'artifacts') -File -ErrorAction SilentlyContinue | ForEach-Object { $_.Name + '=' + (Get-Sha256 $_.FullName) }) -join ';'
    Add-Case 'repository-artifacts-untouched' ($sourceArtifactsBefore -eq $artifactsAfter) 'this checkout''s artifacts folder is unchanged'
    Add-Case 'legacy-input-preserved' ((Get-Sha256 $legacyZip) -eq $legacySha256) "private copy still $legacySha256"

    $failed = @($cases | Where-Object { -not $_.passed })
    $summary = [ordered]@{
        schema = 'kwr.loader-baseline-tests.v3'
        test_clone_commit = $testCommit
        source_commit = $commit
        clone_build_receipt = $receipt
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
