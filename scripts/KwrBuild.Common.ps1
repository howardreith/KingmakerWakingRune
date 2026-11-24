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

# Reads a compiler-input list written by the WriteCompilerReferenceList target (the intermediate directory, then every Csc
# argument) into path-free identities in a canonical order: kind reference/compile/file-option with the file's name
# (repository-relative, obj:<name> for generated files in the intermediate directory, otherwise the file name) and
# SHA-256; kind option for every other argument verbatim; kind output for output locations (file name only).
function ConvertFrom-KwrCompilerInputList([string] $ListPath) {
    $rootPrefix = [IO.Path]::GetFullPath($script:KwrRoot).TrimEnd('\') + '\'
    $lines = @(Get-Content -LiteralPath $ListPath -Encoding UTF8 | Where-Object { $_ })
    if ($lines.Count -lt 4 -or -not $lines[0].StartsWith('intermediate|') -or -not $lines[1].StartsWith('project|') -or -not $lines[2].StartsWith('compiler|')) { throw "Compiler input list is malformed: $ListPath" }
    $objPrefix = [IO.Path]::GetFullPath($lines[0].Substring('intermediate|'.Length)).TrimEnd('\') + '\'
    $projectDirectory = $lines[1].Substring('project|'.Length)
    # Csc arguments name files relative to the project directory.
    function Resolve-InputPath([string] $Path) { return [IO.Path]::GetFullPath([IO.Path]::Combine($projectDirectory, $Path.Trim('"'))) }
    function Get-InputName([string] $Path) {
        $full = Resolve-InputPath $Path
        # MSBuild's generated target-framework attribute file lives in the intermediate or the temporary directory.
        if ([IO.Path]::GetFileName($full) -like '*.AssemblyAttributes.cs') { return 'generated:' + [IO.Path]::GetFileName($full) }
        if ($full.StartsWith($objPrefix, [StringComparison]::OrdinalIgnoreCase)) { return 'obj:' + $full.Substring($objPrefix.Length).Replace('\', '/') }
        if ($full.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) { return $full.Substring($rootPrefix.Length).Replace('\', '/') }
        return [IO.Path]::GetFileName($full)
    }
    function Get-InputDigest([string] $Path) { $full = Resolve-InputPath $Path; if (Test-Path -LiteralPath $full -PathType Leaf) { return Get-KwrSha256 $full } else { return 'absent' } }
    $outputSwitches = @('out', 'pdb', 'refout', 'doc', 'errorlog', 'generatedfilesout')
    $fileSwitches = @('win32res', 'win32icon', 'win32manifest', 'keyfile', 'ruleset', 'appconfig', 'analyzer', 'a', 'additionalfile', 'analyzerconfig', 'embed', 'sourcelink', 'addmodule')
    $resourceSwitches = @('resource', 'res', 'linkresource', 'linkres')
    # The effective compiler binary (and, without /noconfig, the implicit csc.rsp beside it) is an input.
    $compiler = $lines[2].Substring('compiler|'.Length)
    $compilerEntry = [ordered]@{ kind = 'compiler'; name = [IO.Path]::GetFileName($compiler); sha256 = $(if (Test-Path -LiteralPath $compiler -PathType Leaf) { Get-KwrSha256 $compiler } else { 'absent' }) }
    $cscArgs = @($lines[3..($lines.Count - 1)] | ForEach-Object { if (-not $_.StartsWith('arg|')) { throw "Compiler input list is malformed: $_" }; $_.Substring(4) })
    $implicit = @()
    if ($cscArgs -notcontains '/noconfig') {
        $rsp = Join-Path (Split-Path -Parent $compiler) 'csc.rsp'
        $implicit += [ordered]@{ kind = 'response-file'; name = 'implicit:csc.rsp'; sha256 = $(if (Test-Path -LiteralPath $rsp -PathType Leaf) { Get-KwrSha256 $rsp } else { 'absent' }) }
    }
    $entries = foreach ($line in @($cscArgs | ForEach-Object { 'arg|' + $_ })) {
        if (-not $line.StartsWith('arg|')) { throw "Compiler input list is malformed: $line" }
        $arg = $line.Substring(4)
        if ($arg.StartsWith('@')) { [ordered]@{ kind = 'response-file'; name = '@' + (Get-InputName $arg.Substring(1)); sha256 = Get-InputDigest $arg.Substring(1) }; continue }
        $m = [regex]::Match($arg, '^[/-]([A-Za-z0-9_+-]+?)(?::(.*))?$')
        if (-not $m.Success) { [ordered]@{ kind = 'compile'; name = Get-InputName $arg; sha256 = Get-InputDigest $arg }; continue }
        $switch = $m.Groups[1].Value.ToLowerInvariant(); $value = $m.Groups[2].Value
        if ($outputSwitches -ccontains $switch) { [ordered]@{ kind = 'output'; name = "/$switch" + ':' + [IO.Path]::GetFileName($value.Trim('"')); sha256 = '' } }
        elseif ($switch -ceq 'reference' -or $switch -ceq 'r') {
            $alias, $path = if ($value -match '^[A-Za-z_][A-Za-z0-9_]*=') { $value -split '=', 2 } else { $null, $value }
            [ordered]@{ kind = 'reference'; name = $(if ($alias) { "$alias=" } else { '' }) + (Get-InputName $path); sha256 = Get-InputDigest $path }
        }
        elseif ($resourceSwitches -ccontains $switch) {
            $path, $rest = $value -split ',', 2
            [ordered]@{ kind = 'file-option'; name = "/$switch" + ':' + (Get-InputName $path) + $(if ($rest) { ',' + $rest } else { '' }); sha256 = Get-InputDigest $path }
        }
        elseif ($fileSwitches -ccontains $switch -and $value) { [ordered]@{ kind = 'file-option'; name = "/$switch" + ':' + (Get-InputName $value); sha256 = Get-InputDigest $value } }
        else { [ordered]@{ kind = 'option'; name = $arg.Replace($objPrefix.TrimEnd('\'), 'obj:'); sha256 = '' } }
    }
    # Argument order is kept: the inventory is the compiler, any implicit response file, then every argument as passed.
    return @(@($compilerEntry) + $implicit + @($entries))
}


function ConvertTo-KwrCompilerInventoryText($Inventory) {
    return (@($Inventory | ForEach-Object { [string]$_.kind + '|' + [string]$_.name + '|' + [string]$_.sha256 }) -join "`n")
}

# The compiler inputs of the production project as MSBuild evaluates them (LocalGamePaths.props and every other import
# included): the exact Csc command line from a SkipCompilerExecution pass through PrepareResources and CoreCompile in a
# fresh intermediate directory - the effective compiler, then every argument in order (options verbatim; references,
# sources including generated ones, response files and file-valued options by content).
function Get-KwrCompilerInputs {
    $id = [Guid]::NewGuid().ToString('N')
    $list = Join-Path ([IO.Path]::GetTempPath()) ("kwr-compiler-inputs-$id.txt")
    $obj = Join-Path ([IO.Path]::GetTempPath()) ("kwr-compiler-inputs-$id\")
    $project = Join-Path $script:KwrRoot 'src\KingmakerWakingRune\KingmakerWakingRune.csproj'
    $msbuild = Get-KwrMsBuild
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $output = $null | & $msbuild $project /nologo /v:q /nr:false '/t:PrepareResources;Compile' /p:SkipCompilerExecution=true `
            /p:Configuration=Release "/p:CompilerReferenceListPath=$list" "/p:IntermediateOutputPath=$obj" "/p:OutputPath=$($obj)bin" 2>&1 | ForEach-Object { "$_" }
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
