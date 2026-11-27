# Shared by Build.ps1 and Validate-Package.ps1: the compiler inputs MSBuild evaluates for the production project and the
# build-input fingerprint a frozen candidate's receipt records.
Set-StrictMode -Version Latest

$script:KwrRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
. (Join-Path $PSScriptRoot 'KwrFileIdentity.ps1')

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
    $lines = @(Get-Content -LiteralPath $ListPath -Encoding UTF8 | Where-Object { $_ })
    if ($lines.Count -lt 4 -or -not $lines[0].StartsWith('intermediate|') -or -not $lines[1].StartsWith('project|') -or -not $lines[2].StartsWith('compiler|')) { throw "Compiler input list is malformed: $ListPath" }
    # Every location is compared physically (R1-F1): the repository root, the intermediate folder and each existing input
    # are resolved to the path the file system stores (Get-KwrPhysicalPath), so a junction, symbolic link, short name or
    # other letter case cannot make a file inside the checkout look external, or the reverse.
    $rootPrefix = (Get-KwrPhysicalPath $script:KwrRoot).TrimEnd('\') + '\'
    $objPrefix = (Get-KwrPhysicalPath $lines[0].Substring('intermediate|'.Length)).TrimEnd('\') + '\'
    $projectDirectory = $lines[1].Substring('project|'.Length)
    # Csc arguments name files relative to the project directory; an existing file is named by its physical location.
    function Resolve-InputPath([string] $Path) { return Get-KwrPhysicalPath ([IO.Path]::Combine($projectDirectory, $Path.Trim('"'))) }
    function Get-InputName([string] $Path) {
        $full = Resolve-InputPath $Path
        # MSBuild's generated target-framework attribute file lives in the intermediate or the temporary directory; a file of
        # that name anywhere else in the repository is an authored source and keeps its repository path.
        $inRepository = $full.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase) -and -not $full.StartsWith($objPrefix, [StringComparison]::OrdinalIgnoreCase)
        if ([IO.Path]::GetFileName($full) -like '*.AssemblyAttributes.cs' -and -not $inRepository) { return 'generated:' + [IO.Path]::GetFileName($full) }
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
    # One argument; a response file contributes its own entry and then, in place, every argument it contains (Csc reads
    # them as if they were on the command line; '#' lines are comments), so files it names are identified by content
    # too. A named file that cannot be read is recorded as 'absent', which current qualification refuses.
    function ConvertFrom-CscArgument([string] $arg, [int] $Depth) {
        if ($arg.StartsWith('@')) {
            $rspPath = $arg.Substring(1)
            [ordered]@{ kind = 'response-file'; name = '@' + (Get-InputName $rspPath); sha256 = Get-InputDigest $rspPath }
            $rspFull = Resolve-InputPath $rspPath
            if ($Depth -ge 4) { throw "Response files are nested too deeply: $rspFull" }
            if (Test-Path -LiteralPath $rspFull -PathType Leaf) {
                foreach ($rspLine in [IO.File]::ReadAllLines($rspFull)) {
                    if ($rspLine.TrimStart().StartsWith('#')) { continue }
                    foreach ($token in [regex]::Matches($rspLine, '(?:"[^"]*"|[^\s"])+')) { ConvertFrom-CscArgument $token.Value.Replace('"', '') ($Depth + 1) }
                }
            }
            return
        }
        $m = [regex]::Match($arg, '^[/-]([A-Za-z0-9_+-]+?)(?::(.*))?$')
        if (-not $m.Success) {
            # repositoryPath: the source's physical location inside the repository (outside the intermediate folder), so the
            # Git membership check never depends on the reduced name or on the spelling used to select it; null for generated
            # and external sources. An external source with more than one hard-link name may be another name of a file in
            # the checkout, whose ownership a path cannot establish: it is refused rather than exempted.
            $full = Resolve-InputPath $arg
            $repositoryPath = if ($full.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase) -and -not $full.StartsWith($objPrefix, [StringComparison]::OrdinalIgnoreCase)) { $full.Substring($rootPrefix.Length).Replace('\', '/') } else { $null }
            if ($null -eq $repositoryPath -and -not $full.StartsWith($objPrefix, [StringComparison]::OrdinalIgnoreCase) -and [IO.File]::Exists($full) -and [KwrFileIdentityV9]::LinkCountOf($full) -gt 1) {
                throw "Compiler source $full is a hard link outside the repository; whether it is a repository file cannot be established, so it is refused. Use an independent copy or a tracked source."
            }
            [ordered]@{ kind = 'compile'; name = Get-InputName $arg; sha256 = Get-InputDigest $arg; repositoryPath = $repositoryPath }; return
        }
        $switch = $m.Groups[1].Value.ToLowerInvariant(); $value = $m.Groups[2].Value
        # Inputs the inventory cannot name file by file are refused, also inside a response file: wildcard source
        # recursion and linked (embedded-interop) references.
        if (@('recurse', 'link', 'l') -ccontains $switch) { throw "Unsupported compiler input: $arg (wildcard recursion and linked references cannot be bound to the receipt); remove it from the project or local configuration." }
        if ($outputSwitches -ccontains $switch) { [ordered]@{ kind = 'output'; name = "/$switch" + ':' + [IO.Path]::GetFileName($value.Trim('"')); sha256 = '' } }
        elseif ($switch -ceq 'reference' -or $switch -ceq 'r') {
            $alias, $path = if ($value -match '^[A-Za-z_][A-Za-z0-9_]*=') { $value -split '=', 2 } else { $null, $value }
            # referencePath: the referenced file's name without the alias, kept from the parsed argument (R9-F4, round 12), so
            # the producing-commit comparison never infers an alias from a path such as Rules=Local/Helper.dll.
            $referencePath = Get-InputName $path
            [ordered]@{ kind = 'reference'; name = $(if ($alias) { "$alias=" } else { '' }) + $referencePath; sha256 = Get-InputDigest $path; referencePath = $referencePath }
        }
        elseif ($resourceSwitches -ccontains $switch) {
            $path, $rest = $value -split ',', 2
            [ordered]@{ kind = 'file-option'; name = "/$switch" + ':' + (Get-InputName $path) + $(if ($rest) { ',' + $rest } else { '' }); sha256 = Get-InputDigest $path }
        }
        elseif ($fileSwitches -ccontains $switch -and $value) { [ordered]@{ kind = 'file-option'; name = "/$switch" + ':' + (Get-InputName $value); sha256 = Get-InputDigest $value } }
        else { [ordered]@{ kind = 'option'; name = $arg.Replace($objPrefix.TrimEnd('\'), 'obj:'); sha256 = '' } }
    }
    $entries = foreach ($arg in $cscArgs) { ConvertFrom-CscArgument $arg 0 }
    # Argument order is kept: the inventory is the compiler, any implicit response file, then every argument as passed.
    return @(@($compilerEntry) + $implicit + @($entries))
}


# The repository files an inventory names (sources, resources such as the embedded art, references, response files and
# other file-valued options under the repository), as tracked paths: current provenance compares them with the receipt's
# commit in addition to the fixed material paths.
function Get-KwrInventoryRepositoryPaths($Inventory) {
    $candidates = @(foreach ($entry in @($Inventory)) {
        if ([string]$entry.kind -notin @('compile', 'reference', 'file-option', 'response-file')) { continue }
        if ([string]$entry.kind -ceq 'compile') {
            # A compiler source is identified by its complete literal repository path (R9-F4): the entry's physical
            # repositoryPath when the inventory records one (null for generated and external sources), otherwise its
            # literal name; no option or reference-alias decoding applies to a source file name (Rules=Root.cs stays whole).
            $recorded = if ($entry -is [Collections.IDictionary]) { $entry.Contains('repositoryPath') } else { $null -ne $entry.PSObject.Properties['repositoryPath'] }
            $source = if ($recorded) { [string]$entry.repositoryPath } else { [string]$entry.name }
            if ([string]::IsNullOrWhiteSpace($source) -or $source -match '^(obj|generated|implicit):') { continue }
            $source
            continue
        }
        # Each entry is decoded according to its kind (R9-F4, round 11): only a reference loses its alias (round 12: by using
        # the path parsed from the compiler argument),
        # a response file only its '@', a file-valued option only its '/switch:' prefix and, for the resource switches
        # alone, the ',logical name' suffix Csc reads there. A literal equals sign in a response-file or option file name
        # (Rules=Base.rsp) is part of the name.
        $name = [string]$entry.name
        switch -CaseSensitive ([string]$entry.kind) {
            'reference' {
                # The parsed path when the inventory records it (every inventory evaluated now does); an older receipt's entry
                # without it falls back to removing a leading alias (its paths are only ever added to the comparison).
                $binding = Get-KwrReferenceBinding $entry
                if ($binding) { $name = $binding } else { $name = $name -replace '^[A-Za-z_][A-Za-z0-9_]*=', '' }
            }
            'response-file' { if ($name.StartsWith('@')) { $name = $name.Substring(1) } }
            'file-option' {
                $option = [regex]::Match($name, '^/([A-Za-z0-9_+-]+):(.*)$')
                if ($option.Success) {
                    $name = $option.Groups[2].Value
                    if (@('resource', 'res', 'linkresource', 'linkres') -ccontains $option.Groups[1].Value.ToLowerInvariant()) { $name = ($name -split ',', 2)[0] }
                }
            }
        }
        if ($name -match '^(obj|generated|implicit):' -or [string]::IsNullOrWhiteSpace($name)) { continue }
        $name
    })
    if ($candidates.Count -eq 0) { return @() }
    # Each name resolves to Git's tracked spelling with Windows-compatible (case-insensitive, literal) matching (R8-F1), so a
    # source selected as localroot.cs is compared as the tracked LocalRoot.cs; names Git does not track are not paths of
    # the producing commit.
    $spellings = Get-KwrTrackedSpellings
    $tracked = [Collections.Generic.List[string]]::new()
    foreach ($candidate in $candidates) {
        $canonical = Resolve-KwrTrackedSpelling $spellings $candidate
        if ($canonical -and -not $tracked.Contains($canonical)) { $tracked.Add($canonical) }
    }
    return @($tracked)
}

# The physical location of an existing file or directory (junctions, symbolic links, short names and letter case
# resolved); a path that does not exist is returned in full form, unchanged.
function Get-KwrPhysicalPath([string] $Path) {
    $full = [IO.Path]::GetFullPath($Path)
    if ([IO.File]::Exists($full) -or [IO.Directory]::Exists($full)) { return [KwrFileIdentityV9]::FinalPathOf($full) }
    return $full
}

# Git's tracked paths (lossless listing) grouped by their case-insensitive spelling.
function Get-KwrTrackedSpellings {
    $spellings = [Collections.Generic.Dictionary[string, Collections.Generic.List[string]]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($path in @(Get-KwrGitPathList @('--cached') @())) {
        if (-not $spellings.ContainsKey($path)) { $spellings[$path] = [Collections.Generic.List[string]]::new() }
        $spellings[$path].Add($path)
    }
    return , $spellings
}

# Git's spelling of one repository-relative path: the tracked path equal to it without regard to letter case (Windows
# opens either), or $null when Git does not track it. Two tracked paths differing only in case are refused as ambiguous.
function Resolve-KwrTrackedSpelling($Spellings, [string] $Path) {
    $key = $Path.Replace('\', '/')
    if (-not $Spellings.ContainsKey($key)) { return $null }
    $found = $Spellings[$key]
    if ($found.Count -gt 1) { throw "The repository path $Path matches more than one tracked spelling ($($found -join ', ')); it cannot be attributed to one tracked file." }
    return $found[0]
}

# Git pathnames without C-style quoting: NUL-separated output read as UTF-8 through the process API (Windows PowerShell
# would decode native output with the console code page), so a non-ASCII tracked name is compared and read exactly.
function Get-KwrGitPathList([string[]] $Options, [string[]] $Paths) {
    $quote = { param([string] $a) '"' + ($a -replace '(\\*)"', '$1$1\"' -replace '(\\+)$', '$1$1') + '"' }
    $arguments = @('-C', $script:KwrRoot, '-c', 'core.quotepath=off', 'ls-files') + @($Options) + @('-z', '--') + @($Paths)
    $psi = [Diagnostics.ProcessStartInfo]::new('git', ((@($arguments) | ForEach-Object { & $quote $_ }) -join ' '))
    $psi.UseShellExecute = $false; $psi.RedirectStandardOutput = $true; $psi.RedirectStandardError = $true; $psi.CreateNoWindow = $true
    $psi.StandardOutputEncoding = [Text.UTF8Encoding]::new($false)
    $process = [Diagnostics.Process]::Start($psi)
    $stderrTask = $process.StandardError.ReadToEndAsync()
    $output = $process.StandardOutput.ReadToEnd()
    $process.WaitForExit()
    if ($process.ExitCode -ne 0) { throw "git ls-files failed: $($stderrTask.Result)" }
    return @($output.Split([char]0) | Where-Object { $_ })
}

# The parsed reference binding of an inventory entry (R12-F2, round 13): for a reference, the referenced file's path
# without the alias ('' when an older receipt's entry does not record it); '' for every other kind. The display name keeps
# the alias, so name plus binding distinguishes alias Rules for Local/Accessibility.dll from the literal path
# Rules=Local/Accessibility.dll. The inventory comparison and the build fingerprint both include it.
function Get-KwrReferenceBinding($Entry) {
    if ([string]$Entry.kind -cne 'reference') { return '' }
    $has = if ($Entry -is [Collections.IDictionary]) { $Entry.Contains('referencePath') } else { $null -ne $Entry.PSObject.Properties['referencePath'] }
    if ($has) { return [string]$Entry.referencePath } else { return '' }
}

function ConvertTo-KwrCompilerInventoryText($Inventory) {
    return (@($Inventory | ForEach-Object { [string]$_.kind + '|' + [string]$_.name + '|' + [string]$_.sha256 + $(if ([string]$_.kind -ceq 'reference') { '|' + (Get-KwrReferenceBinding $_) } else { '' }) }) -join "`n")
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
        'scripts/Build.ps1', 'scripts/KwrBuild.Common.ps1', 'scripts/KwrFileIdentity.ps1')
    $files = @(Get-KwrGitPathList @('--cached', '--others', '--exclude-standard') $paths)
    $lines = @(foreach ($file in ($files | Sort-Object -Unique)) {
        $full = Join-Path $script:KwrRoot $file
        if (Test-Path -LiteralPath $full -PathType Leaf) { $file + "`t" + (Get-KwrSha256 $full) }
    })
    if ($null -eq $CompilerInputs) { $CompilerInputs = @(Get-KwrCompilerInputs) }
    foreach ($entry in @($CompilerInputs)) { $lines += 'compiler-' + [string]$entry.kind + ':' + [string]$entry.name + "`t" + [string]$entry.sha256 + $(if ([string]$entry.kind -ceq 'reference') { "`t" + (Get-KwrReferenceBinding $entry) } else { '' }) }
    $hasher = [Security.Cryptography.SHA256]::Create()
    try { $digest = $hasher.ComputeHash([Text.Encoding]::UTF8.GetBytes(($lines -join "`n"))) }
    finally { $hasher.Dispose() }
    return ([BitConverter]::ToString($digest)).Replace('-', '').ToLowerInvariant()
}
