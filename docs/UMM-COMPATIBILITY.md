# UMM compatibility baseline

The common baseline is Unity Mod Manager **0.31.1**. `Info.json` declares it, and
the production DLL is compiled against **UnityModManager 0.31.1.0**, the Harmony 1.2
adapter **0Harmony12 1.2.0.1** and **0Harmony (Harmony2) 2.3.1.1**, whatever loader
is installed on the build machine. Waking Rune calls Harmony2 directly (dialogue
exception finalizers), so Harmony2 is an explicit, pinned compiler dependency; the
Harmony12 adapter alone does not cover that API surface.

## Build identity

`scripts/loader-reference-pins.json` pins exact upstream bytes:

| Reference | Version | Source | SHA-256 |
| --- | --- | --- | --- |
| UnityModManager.dll | 0.31.1.0 | NuGet `unitymodmanager` 0.31.1 (package `05a82b66…bb53c9`), `lib/net35/UnityModManager.dll` | `2e038ff23ab01d538dfef792096e1f5c04802d1edac3660db20d0d54c167c2c4` |
| 0Harmony12.dll | 1.2.0.1 | newman55/unity-mod-manager `10cfb1cc` `lib/Harmony/1.2/0Harmony12.dll` | `aa1cd48317254985d8b700cc74953477d1b40c3022ce9aa4c95ed2b8327e1292` |
| 0Harmony.dll | 2.3.1.1 | NuGet `Lib.Harmony` 2.3.1.1 (package `b9096d3e…6798aa`), `lib/net48/0Harmony.dll` (the variant UMM installs; UMM 0.31.1 itself references 0Harmony 2.3.1.1) | `5f7ad0df1eb08e81f66d4006d3a9e5cb40fc05e381f36d7bd0513f334f6a251f` |

- `scripts/Get-PinnedLoaderReferences.ps1` fetches them into the ignored
  `obj/loader-references` cache and verifies archive hash, DLL hash and assembly
  identity. A missing, corrupt or different file (including the installed newer
  loader or Harmony) fails clearly; nothing falls back to the installed UMM. It also
  requires `Info.json` `ManagerVersion` to be a JSON string equal to the pinned
  UnityModManager version, and the pin file to define exactly these three references
  under their canonical file names (a dropped or renamed pin is refused).
- Direct MSBuild builds import `LoaderReferences.targets`: on every build, before
  reference resolution, it re-verifies the cache and manifest; after reference-assembly
  selection, immediately before compilation, the list Csc receives
  (`@(ReferencePathWithRefAssemblies)`; each loader assembly found by file name or by
  assembly identity) must contain exactly the canonical UnityModManager, 0Harmony12 and
  0Harmony files by full path. A stray HintPath, a substituted reference assembly, a
  same-stem or renamed file, or the installed loader is refused even when its bytes equal
  the pin. The supported routes are `scripts/Build.ps1` and command-line MSBuild; IDE
  builds are not a supported or verified route. The fast up-to-date check is disabled and
  the pin, verifier, guard and manifest are compile inputs, so an incremental build refuses
  a pin-only or manifest-only change.
- `scripts/Build.ps1` reads the commit and dirty state, the compiler inventory - the exact Csc command line
  MSBuild produces for the production project (an opt-in `WriteCompilerReferenceList` target with
  `ProvideCommandLineArgs`, evaluated by a `SkipCompilerExecution` pass through `PrepareResources` and
  `CoreCompile` in a fresh intermediate directory, so anything `LocalGamePaths.props` sets is included): the
  effective compiler binary, then every argument in order, options verbatim, references, sources (generated
  ones included), response files and file-valued options (`/win32res`, `/resource`, `/keyfile`, ...) by the
  content of the files they name; a response file is also expanded in place, so the files it names are identified
  by content too, and a named file that cannot be read is recorded as unidentified - and a build-input fingerprint (tracked or new
  files under `src/`, `Info.json`, `LoaderReferences.targets`, the pins, verifier and build scripts, plus
  that inventory) before building; the production compile writes its own inventory, which must equal the
  evaluated one, and neither HEAD nor the inputs may change while building. The product is always a fresh
  compile (`Rebuild`) into a folder the invocation owns (`OutDir` and `IntermediateOutputPath` are global
  properties, so no project or local setting redirects them): the copied DLL must equal Csc's `/out:` file there,
  and those bytes are staged and packaged, never a DLL left at `bin\Release`. It then builds, runs the
  tests, checks the embedded art and the DLL's reference versions, writes the ZIP
  (`KingmakerWakingRune/{Info.json,KingmakerWakingRune.dll}`) and its receipt (`<zip>.build.json`:
  commit, dirty state, build inputs, compiler inventory, DLL and ZIP SHA-256, pinned reference identities)
  in a private staging folder, validates the pair (with current provenance for a clean build) and only
  then moves both to their final paths. A failure leaves no new file at the candidate paths; when a package
  is replaced (owner-authorized `-ReplacePackage`, or a `-dirty` build), moving the previous ZIP and receipt
  aside and placing the new pair are one rollback scope: every completed move is undone in reverse if any step
  fails (for example a receipt held open by another process), and if the rollback itself fails the staging
  folder holding the previous files is kept and named. A clean candidate is never replaced for the same version
  without `-ReplacePackage`.
- `scripts/Validate-Package.ps1` is the standalone pre-install check. It resolves its paths with
  PowerShell literal semantics (relative to the current PowerShell location, no wildcards), refuses a
  report path that is one of its inputs - the package, the given receipt, or the conventional receipt beside
  the package even when none is given - compared by file identity (volume serial number and file index from
  `GetFileInformationByHandle`, `scripts/KwrFileIdentity.ps1`) when the destination exists, so another spelling,
  a short name or a hard link is refused too; the report itself is written through one handle whose identity is
  checked against the inputs immediately before it is truncated, so a destination replaced during validation is
  refused as well. It applies the same pin-definition contract as the build offline
  (`Get-PinnedLoaderReferences.ps1 -DefinitionOnly`: every pin string matched through its absolute end, so a
  trailing line feed is refused, and the archive entry and archive SHA-256 given together or not at all), and inspects and hashes one private copy of the ZIP
  (and of the receipt) created inside its cleanup scope. It accepts only the two package files (no loader,
  Harmony, game or Unity DLL; a directory record only for the package folder and only when its decoded
  content is empty), requires the packaged manifest's `ManagerVersion` to equal the baseline, requires the
  packaged DLL to define the assembly `KingmakerWakingRune`, and reads its AssemblyRef table: it must name
  exactly UnityModManager 0.31.1.0, 0Harmony12 1.2.0.1 and 0Harmony 2.3.1.1 and no other UnityModManager
  or Harmony assembly (an extra Harmony adapter reference is refused). With a receipt, the receipt must
  describe the same ZIP and DLL bytes and the pinned identities (fields are type-checked first). With
  `-RequireCurrentProvenance` (the frozen-candidate path) the receipt must also name a clean producing
  commit that exists in the repository and whose material inputs (the fixed build paths plus every tracked file
  the recorded inventory names, such as the embedded art) equal the files present now, its version
  must equal the packaged manifest, it may contain no unidentified compiler input, and its recorded compiler
  inventory and build inputs must equal what MSBuild and the repository give now; otherwise the old ZIP and receipt lose current-candidate status. The report states
  `source_provenance` (`not-checked` for an external ZIP inspected by content only,
  `receipt-matches-package`, or `verified-current`).
- `KwrRuntime.ps1` (`Begin`, `Reinstall`) uses `scripts/KwrPackage.ps1`: it resolves the caller's ZIP once,
  validates a private copy (frozen-candidate mode requires current provenance when a receipt is beside the
  ZIP; `-Diagnostic` stages a receipt-backed diagnostic build with the ZIP, DLL, pin and receipt-match checks
  only) and records the qualification the validator established - `frozen-candidate` (current provenance),
  `diagnostic` (receipt byte matching under `-Diagnostic`) or `external-content-inspected` (no receipt) - stages and verifies that copy (the staged DLL must equal the validated DLL) before installing it,
  records the source path, validated digest, provenance and install mode captured before installing (never
  re-resolved from the caller's path), and removes the private copy in every outcome. `Reinstall` marks the
  transaction `reinstalling` (which `Launch` refuses) before replacing the installed folder, so a failure
  leaves a non-launchable transaction; `Close` keeps that phase (only `running` becomes `closed`), and either a
  repeated `Reinstall` (which resumes it and returns to the phase before the interrupted reinstall) or `End`
  restores it.

Earlier releases were compiled against the installed loader: the published v0.12.3
prerelease DLL references UnityModManager 0.33.2.0 and 0Harmony 2.3.6.0 while its
manifest declares 0.28.2. Those historical packages and their records are unchanged.

## Reproduce the checks

```powershell
.\scripts\Build.ps1                      # pins, build, 290 tests, embedded art, package, receipt, standalone validation
.\scripts\Test-LoaderBaseline.ps1        # failure-boundary tests (needs network once for the v0.12.3 ZIP,
                                         # or -LegacyReleaseZip <path>; ZIP SHA-256 4dec0487...4593b402)
.\scripts\Validate-Package.ps1 -PackagePath <zip> [-ReceiptPath <zip>.build.json [-RequireCurrentProvenance]] [-ReportPath <file>]
```

`Test-LoaderBaseline.ps1` clones the committed HEAD into a temporary directory whose path
contains a space, builds and packages it there with `Build.ps1`, and runs each check in its
own PowerShell or MSBuild process; this checkout's artifacts and the installed game are not
written. It downloads (or accepts) three hash-checked fixtures and reads them through private
copies: the genuine v0.12.3 release (SHA-256 `4dec0487…4593b402`; its DLL references
UnityModManager 0.33.2.0 and 0Harmony 2.3.6.0), the author's UnityModManager 0.32.4 package and
the official Lib.Harmony 2.3.6 package (net48), as known distinct wrong fixtures; the owner's
installation is only measured. The suite starts the clone with an empty reference cache (the clone's own
resolver downloads and verifies the pins) and a different product version committed in the clone (the
positive artifact name is derived from the manifest). Its 108 checks cover cache bytes; dropped/misnamed pins and raised
or array-typed manifests; direct and incremental builds (the three pinned paths on the Csc
command line; manifest-only and pin-only changes refused); Csc-input probes (identical copy
elsewhere, same-stem or renamed files, both fixtures, a substituted reference assembly); the
validator with and without receipts, typed and mismatched receipt fields, smuggled or renamed
DLLs, data-bearing and level-0 DEFLATE empty directory records, non-string `ManagerVersion`, the
v0.12.3 DLL with only its manifest set to 0.31.1 (refused), both packages in one Windows
PowerShell session, a caller ZIP replaced after the validator's snapshot, an older package after
a pin change; and, from round 2: offline validation without a cache, a dropped Harmony2 pin and a UMM pin
disagreeing with the minimum (refused), an extra Harmony adapter reference and a differently named assembly
under the product file name (benign stubs compiled against the pinned loader files; refused); a relative
literal ZIP path with brackets resolved from the PowerShell location (the process directory holds another
ZIP of the same name); report paths equal to the package or receipt (refused, inputs intact); the install
helper on a temporary destination (captured source and digest after the caller ZIP is moved, brackets in
names, a rejected receipt with its private copy removed) and its use by `KwrRuntime.ps1` (which is not run:
it transacts against the live game); a `Build.ps1` failure after the receipt is written (no candidate left;
clean retry without `-ReplacePackage`; replacement of a clean candidate refused); and current provenance
(clean candidate verified; external content inspection marked not checked; a reference-helper change and an
overflow-checking setting in `LocalGamePaths.props` refused and accepted after restore; after a committed
helper change the old pair is refused and the rebuilt candidate verified); and, from round 3: the unmodified
candidate built from an empty cache and checked with its receipt before the version change; receipts naming an
older commit, an unknown commit or another version (refused); `BaseAddress` and an external `Win32Resource`
(refused until rebuilt; the receipt records the resource's digest; removing the options reproduces the DLL);
report destinations equal to the adjacent receipt without `-ReceiptPath` or a hard link to the package
(refused, inputs intact); a failed replacement (previous ZIP and receipt restored); a receipt-backed dirty build
refused in frozen mode and staged in diagnostic mode; and the reinstall ordering in `KwrRuntime.ps1`; and, from
round 4: the alternate fixture version derived from the shipping one (also with shipping version 0.12.99) without
replacing the shipping pair; offline pin definitions with a trailing line feed, a missing archive digest and an
archive digest without an entry (refused, cache absent); a receipt naming a commit that differs only in a packaged
image (refused); a response file whose resource changes (refused until rebuilt) and a documentation-only commit
(still current); the file identity compared with `fsutil file queryfileid` and `vol`, a distinct existing report
file written, and a report destination replaced by a hard link to the package during validation (refused, inputs
intact); a receipt held open by another process during replacement (previous pair kept and still valid; retry
succeeds after release); an older DLL at `bin\Release` with a redirected `OutputPath` and a source change (the
packaged DLL is the new compile); the recorded install mode for receiptless, diagnostic and frozen packages; and
`KwrRuntime.ps1`'s own `Reinstall`, `Close` and `Launch` functions against a temporary journal (an interrupted
reinstall stays non-launchable through `Close` and is recovered).

## Evidence for this change (2026-11-24, round 4)

The build and validation tooling is at `5b91fa1`; the product sources are unchanged, so the candidate DLL is
byte-identical to round 1. Candidate (clean `Build.ps1` of `5b91fa1` on Windows against the pinned cache and
the Kingmaker 2.1.7b managed assemblies): DLL SHA-256
`7cd3cb09f12f75a7247c1b401673528a0773a139e59dc3a940da860c28f24904`, MVID
`ccc59117-f992-44ce-9355-540d35f181f0`; AssemblyRefs UnityModManager 0.31.1.0,
0Harmony12 1.2.0.1, 0Harmony 2.3.1.1; `KingmakerWakingRune-0.12.3.zip` SHA-256
`6f13d111b22b9158ac5f4adad08e990a2f363443ac43c74e24cdb49d4cb8de94` (ZIP digests depend on file times; the
receipt and validator report name the exact bytes). 290/290 tests, 19 embedded images verified, standalone
validation with current provenance passed. `Test-LoaderBaseline.ps1` on `5b91fa1`: 108/108 (the clone at the
alternate version 0.12.4 produced the same DLL).

Machine-generated records for the candidate are committed as structured files (no logs) in
[`docs/evidence/umm-0311/`](evidence/umm-0311/INDEX.md): the `Build.ps1` receipt and summary, a
direct normal-verbosity build record (the Csc inputs), the boundary-suite results, the static
audit, a full loader-profile inventory (every component's identity, SHA-256, MVID and
references), the newest-stable selection record and the audit tool's source. Local path
prefixes are replaced by placeholders; the index names the producing commit.

| Observation | Result |
| --- | --- |
| 1. Declared minimum | UMM 0.31.1 (`Info.json`) |
| 2. Verified compile baseline | UnityModManager 0.31.1.0 + 0Harmony12 1.2.0.1 + 0Harmony 2.3.1.1, pinned bytes above, confirmed on the `csc` command line and in the DLL's AssemblyRefs |
| 3. Static/API compatibility | See the table below (same candidate DLL) |
| 4. Observed native behaviour | **NOT RUN** for every loader version (see below) |

Static/API audit: every type and member reference the candidate DLL makes into
UnityModManager, 0Harmony12 and 0Harmony (14 types, 39 members, including the Harmony2
finalizer surface) was resolved by name and decoded signature against each profile's
real assemblies, following base types and type forwarders. Profiles are
component-assembled, not official UMM distribution ZIPs: the author's NuGet
`unitymodmanager` DLL plus the Harmony2 that DLL itself references, from the official
`Lib.Harmony` package's `lib/net48` build, plus the pinned 0Harmony12. The owner row is a
read-only copy of the live installation.

| Loader | UnityModManager (SHA-256) | 0Harmony (Harmony2) | 0Harmony12 | Static result |
| --- | --- | --- | --- | --- |
| 0.31.1 (baseline) | 0.31.1.0 `2e038ff2…` | 2.3.1.1 net48 `5f7ad0df…` | 1.2.0.1 `aa1cd483…` | all resolved |
| 0.32.4 | 0.32.4.0 `1387468b…` | 2.3.6.0 net48 `df043f05…` | 1.2.0.1 `aa1cd483…` | all resolved |
| 0.33.0 (owner install, measured 2026-11-23) | 0.33.0.0 `63e5baf7…` | 2.3.6.0 `df043f05…` | 1.2.0.1 `aa1cd483…` | all resolved |
| 0.33.4 (newest stable: author's NuGet 2026-11-19, GitHub `3d14bf0d`) | 0.33.4.0 `c5ed5617…` | 2.4.3.0 net48 `ddc070b5…` | 1.2.0.1 `aa1cd483…` | all resolved |

This shows that the referenced names and signatures exist. It does not prove assembly
binding (the DLL references 0Harmony 2.3.1.1; newer loaders ship 2.3.6.0 or 2.4.3.0),
reflection by name, Harmony patch or finalizer behaviour, Unity integration, dialogue
behaviour or save compatibility, and it is not a compatibility claim.

Native runtime: **NOT RUN** on 0.31.1, 0.32.4, 0.33.0 and 0.33.4. `KwrRuntime.ps1`
transacts against the live installation (snapshot, install, restore); running a
different loader would mean replacing the owner's live UMM, which this change must not
do, and no disposable game copy exists for this repository. The candidate is therefore
not runtime-verified on any loader, including the 0.31.1 minimum. No combined mod-stack
check was run.

## Required native qualification

| Profile | Historical evidence (bound to its original DLLs) | Required for this candidate |
| --- | --- | --- |
| UMM 0.31.1 / Harmony 2.3.1.1 | none | Mandatory: startup, New Game entry, lodge and Refuge Token travel, Kurshu dialogue finalizers, fresh-process save/reload |
| UMM 0.32.4 / Harmony 2.3.6 | none for Waking Rune | Same candidate DLL and scenarios, exact identities recorded |
| UMM 0.33.0 / Harmony 2.3.6 | owner profile (earlier builds) | Same candidate DLL and scenarios |
| UMM 0.33.2 / Harmony 2.3.6 | `docs/RECON.md` environment (earlier builds) | Requalify the 0.31.1-built candidate |
| UMM 0.33.4 / Harmony 2.4.3 | none | Same candidate DLL and scenarios before any support claim |

Record the candidate commit, DLL hash and MVID, exact UMM/Harmony hashes, game version,
OS, results and fresh-process save/reload evidence for each run. Use a disposable game
copy and copied test saves. This policy does not downgrade the owner's live
installation or rewrite historical environment records. Neither startup alone nor a
manifest edit proves gameplay compatibility.

## Sources

- Baseline UMM: https://www.nuget.org/packages/UnityModManager/0.31.1
- Baseline adapter: https://github.com/newman55/unity-mod-manager/tree/10cfb1cce51914a5114928c5bc6a06d1d2c31fe2/lib/Harmony/1.2
- Baseline Harmony: https://www.nuget.org/packages/Lib.Harmony/2.3.1.1
- Newest stable UMM (2026-11-23): https://www.nuget.org/packages/UnityModManager/0.33.4 ; upstream page https://www.nexusmods.com/site/mods/21
