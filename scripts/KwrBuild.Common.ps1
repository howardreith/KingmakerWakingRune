# Shared by Build.ps1 and Validate-Package.ps1: the compiler inputs MSBuild evaluates for the production project and the
# build-input fingerprint a frozen candidate's receipt records.
Set-StrictMode -Version Latest

$script:KwrRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))

function Get-KwrMsBuild {
    $vswhere = 'C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe'
    $found = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
    if (-not $found) { throw 'MSBuild not found (Visual Studio Build Tools required).' }
    return $found
}

function Get-KwrSha256([string] $Path) { return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant() }

# Reads a list written by the WriteCompilerReferenceList target (kind|path or setting|Name=value) into path-free
# identities in a canonical order.
function ConvertFrom-KwrCompilerInputList([string] $ListPath) {
    $rootPrefix = $script:KwrRoot.TrimEnd('\') + '\'
    return @(Get-Content -LiteralPath $ListPath -Encoding UTF8 | Where-Object { $_ } | ForEach-Object {
        $kind, $value = $_ -split '\|', 2
        if ($kind -ceq 'setting') { [ordered]@{ kind = $kind; name = $value; sha256 = '' } }
        else {
            $name = if ($value.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) { $value.Substring($rootPrefix.Length).Replace('\', '/') } else { [IO.Path]::GetFileName($value) }
            $digest = if (Test-Path -LiteralPath $value -PathType Leaf) { Get-KwrSha256 $value } else { 'absent' }
            [ordered]@{ kind = $kind; name = $name; sha256 = $digest }
        }
    } | Sort-Object { $_.kind + '|' + $_.name + '|' + $_.sha256 })
}

function ConvertTo-KwrCompilerInventoryText($Inventory) {
    return (@($Inventory | ForEach-Object { [string]$_.kind + '|' + [string]$_.name + '|' + [string]$_.sha256 }) -join "`n")
}

# The compiler inputs of the production project as MSBuild evaluates them (LocalGamePaths.props and every other import
# included): the exact references Csc receives after reference-assembly selection, the source/resource/analyzer items and
# the compile-affecting properties.
function Get-KwrCompilerInputs {
    $id = [Guid]::NewGuid().ToString('N')
    $list = Join-Path ([IO.Path]::GetTempPath()) ("kwr-compiler-inputs-$id.txt")
    $obj = Join-Path ([IO.Path]::GetTempPath()) ("kwr-compiler-inputs-$id\")
    $project = Join-Path $script:KwrRoot 'src\KingmakerWakingRune\KingmakerWakingRune.csproj'
    $msbuild = Get-KwrMsBuild
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $output = $null | & $msbuild $project /nologo /v:q /nr:false '/t:ResolveReferences;FindReferenceAssembliesForReferences' `
            /p:Configuration=Release "/p:CompilerReferenceListPath=$list" "/p:IntermediateOutputPath=$obj" 2>&1 | ForEach-Object { "$_" }
        if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $list)) { throw "Could not resolve the compiler inputs: $($output -join ' ')" }
        return @(ConvertFrom-KwrCompilerInputList $list)
    }
    finally {
        $ErrorActionPreference = $previous
        Remove-Item -LiteralPath $list -Force -ErrorAction SilentlyContinue
        if ([IO.Directory]::Exists($obj)) { [IO.Directory]::Delete($obj, $true) }
    }
}

# Material inputs of the product DLL: tracked or new files under src, the manifest, the loader-pin definition, verifier
# and MSBuild guard, the build scripts, and the compiler inventory (which covers ignored local references and settings).
function Get-KwrBuildInputFingerprint($CompilerInputs = $null) {
    $paths = @('src', 'Info.json', 'LoaderReferences.targets', 'scripts/loader-reference-pins.json', 'scripts/Get-PinnedLoaderReferences.ps1',
        'scripts/Build.ps1', 'scripts/KwrBuild.Common.ps1')
    $files = @(& git -C $script:KwrRoot ls-files --cached --others --exclude-standard -- @paths)
    if ($LASTEXITCODE -ne 0) { throw 'git ls-files failed while fingerprinting build inputs.' }
    $lines = @(foreach ($file in ($files | Sort-Object -Unique)) {
        $full = Join-Path $script:KwrRoot $file
        if (Test-Path -LiteralPath $full -PathType Leaf) { $file + "`t" + (Get-KwrSha256 $full) }
    })
    if ($null -eq $CompilerInputs) { $CompilerInputs = @(Get-KwrCompilerInputs) }
    foreach ($entry in @($CompilerInputs)) { $lines += 'compiler-' + [string]$entry.kind + ':' + [string]$entry.name + "`t" + [string]$entry.sha256 }
    $hasher = [Security.Cryptography.SHA256]::Create()
    try { $digest = $hasher.ComputeHash([Text.Encoding]::UTF8.GetBytes(($lines -join "`n"))) }
    finally { $hasher.Dispose() }
    return ([BitConverter]::ToString($digest)).Replace('-', '').ToLowerInvariant()
}
