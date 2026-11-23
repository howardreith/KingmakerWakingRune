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
    $legacyZip = Get-Fixture $LegacyReleaseZip $legacyUrl 'legacy-v0.12.3.zip' $legacySha256
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
    $references = Join-Path $base 'obj\loader-references'
    Copy-Pins (Join-Path $source 'obj\loader-references') $references
    $build = Invoke-Isolated @('-File', (Join-Path $base 'scripts\Build.ps1'))
    $candidateZip = Join-Path $base 'artifacts\KingmakerWakingRune-0.12.3.zip'
    $receiptPath = $candidateZip + '.build.json'
    if ($build.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $receiptPath)) { throw "Clone build failed: $($build.Output)" }
    $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json
    Add-Case 'clone-build-and-package-from-detached-commit' ($receipt.commit -eq $commit -and -not $receipt.dirty -and $build.Output -match 'Tests: \d+ passed, 0 failed') ("receipt commit $($receipt.commit) dirty $($receipt.dirty); DLL $($receipt.dllSha256); ZIP $($receipt.zipSha256); " + [regex]::Match($build.Output, 'Tests: \d+ passed, 0 failed').Value)
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
    Copy-Item -LiteralPath $candidateZip -Destination $p
    & python -I -c "import sys,zipfile; z=zipfile.ZipFile(sys.argv[1],'a'); i=zipfile.ZipInfo('KingmakerWakingRune/'); z.writestr(i,b'',compress_type=zipfile.ZIP_DEFLATED,compresslevel=0); z.close()" $p
    Expect-Accepted 'package-level0-deflate-empty-directory-accepted' (& $v $p) 'Package validation passed'
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
        $changed = $text -replace '"ManagerVersion":\s*"[0-9.]+"', '"ManagerVersion": "0.31.1"'
        if ($changed -eq $text) { throw 'Legacy manifest was not changed.' }
        Set-ZipEntryText $a "$modFolder/Info.json" $changed }
    Expect-Rejected 'package-legacy-0.33.2-dll-with-0.31.1-manifest-rejected' (& $v $lowered) 'must reference pinned UnityModManager 0\.31\.1\.0'
    foreach ($order in @(@($candidateZip, $lowered), @($lowered, $candidateZip))) {
        $text = "foreach (`$zip in @(" + (Quote $order[0]) + ", " + (Quote $order[1]) + ")) { try { & " + (Quote $validator) + " -PackagePath `$zip | Out-Null; 'ACCEPTED ' + [IO.Path]::GetFileName(`$zip) } catch { 'REJECTED ' + [IO.Path]::GetFileName(`$zip) + ': ' + `$_.Exception.Message } }"
        $r = Invoke-Script $text
        $ok = $r.Output -match 'ACCEPTED KingmakerWakingRune-0\.12\.3\.zip' -and $r.Output -match 'REJECTED legacy-lowered-manifest\.zip: Packaged DLL must reference pinned UnityModManager 0\.31\.1\.0'
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

    # 6. KwrRuntime.ps1 installs only the validated private copy.
    $runtime = [IO.File]::ReadAllText((Join-Path $base 'scripts\KwrRuntime.ps1'))
    $expands = @([regex]::Matches($runtime, 'Expand-Archive -LiteralPath (\S+)') | ForEach-Object { $_.Groups[1].Value })
    $copies = @([regex]::Matches($runtime, '\$validatedCopy = Get-ValidatedPackageCopy \$PackagePath')).Count
    Add-Case 'kwrruntime-installs-only-validated-copies' ($expands.Count -eq 2 -and @($expands | Where-Object { $_ -ne '$validatedCopy' }).Count -eq 0 -and $copies -eq 2) ("Expand-Archive sources: $($expands -join ', '); validated copies: $copies (KwrRuntime.ps1 itself is not run: it transacts against the live game)")

    $artifactsAfter = @(Get-ChildItem -LiteralPath (Join-Path $source 'artifacts') -File -ErrorAction SilentlyContinue | ForEach-Object { $_.Name + '=' + (Get-Sha256 $_.FullName) }) -join ';'
    Add-Case 'repository-artifacts-untouched' ($sourceArtifactsBefore -eq $artifactsAfter) 'this checkout''s artifacts folder is unchanged'
    Add-Case 'legacy-input-preserved' ((Get-Sha256 $legacyZip) -eq $legacySha256) "private copy still $legacySha256"

    $failed = @($cases | Where-Object { -not $_.passed })
    $summary = [ordered]@{
        schema = 'kwr.loader-baseline-tests.v2'
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
