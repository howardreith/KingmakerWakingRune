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
  requires `Info.json` `ManagerVersion` to equal the pinned UnityModManager version.
- Direct MSBuild and IDE builds import `LoaderReferences.targets`: before reference
  resolution it re-verifies the cache; after resolution it requires that the
  compiler's actual inputs (`@(ReferencePath)`) contain exactly one UnityModManager,
  one 0Harmony12 and one 0Harmony, all inside the pinned directory. A stray HintPath
  or the installed loader is refused even when its bytes equal the pin.
- `scripts/Build.ps1` builds, runs the tests, checks the embedded art and the DLL's
  reference versions, packages `KingmakerWakingRune/{Info.json,KingmakerWakingRune.dll}`,
  writes a build receipt beside the ZIP (`<zip>.build.json`: commit, dirty state, DLL
  and ZIP SHA-256, pinned reference identities) and runs the standalone validator on
  the result.
- `scripts/Validate-Package.ps1` is the standalone pre-install check. `KwrRuntime.ps1`
  runs it before every install (`Begin`, `Reinstall`); run it by hand on any supplied
  ZIP. It accepts only the two package files (no loader, Harmony, game or Unity DLL),
  requires the packaged manifest's `ManagerVersion` to equal the baseline, and reads the
  packaged DLL's AssemblyRef table: it must name exactly UnityModManager 0.31.1.0,
  0Harmony12 1.2.0.1 and 0Harmony 2.3.1.1. With a receipt (KwrRuntime passes one when it
  exists beside the ZIP), the receipt must describe the same ZIP and DLL bytes and the
  pinned identities, so a stale receipt cannot vouch for different bytes. A clean
  package is never overwritten for the same version (existing `Build.ps1` rule), so a
  changed pin or build input produces a new `-dirty` or new-version package rather than
  silently reusing a frozen one.

Earlier releases were compiled against the installed loader: the published v0.12.3
prerelease DLL references UnityModManager 0.33.2.0 and 0Harmony 2.3.6.0 while its
manifest declares 0.28.2. Those historical packages and their records are unchanged.

## Reproduce the checks

```powershell
.\scripts\Build.ps1                      # pins, build, 290 tests, embedded art, package, receipt, standalone validation
.\scripts\Test-LoaderBaseline.ps1        # failure-boundary tests (needs network once for the v0.12.3 ZIP,
                                         # or -LegacyReleaseZip <path>; ZIP SHA-256 4dec0487...4593b402)
.\scripts\Validate-Package.ps1 -PackagePath <zip> [-ReceiptPath <zip>.build.json]
```

`Test-LoaderBaseline.ps1` runs each check in its own process with real files: genuine,
corrupt (Harmony2), missing and installed-loader cache bytes; direct MSBuild of the
production project with a corrupt cache and with the genuine cache (the `csc` command
line must name exactly the three pinned files); probe projects whose UnityModManager or
0Harmony resolves outside the pinned directory; manifest/pin disagreement on copies of
the real scripts; the candidate with its receipt (accepted), a re-packed ZIP of the same
content (accepted alone, refused with the old receipt), receipts naming other DLL bytes,
another loader or no loader references; packages with an added Harmony2 or a swapped-in
UnityModManager or game DLL; the genuine v0.12.3 release with only its manifest set to
0.31.1 (refused by the standalone validator); and that every `Expand-Archive` in
`KwrRuntime.ps1` follows a validator call.

## Evidence for this change (2026-11-23)

Candidate DLL (Release, `Build.ps1` on Windows against the pinned cache and the
Kingmaker 2.1.7b managed assemblies): SHA-256
`7cd3cb09f12f75a7247c1b401673528a0773a139e59dc3a940da860c28f24904`, MVID
`ccc59117-f992-44ce-9355-540d35f181f0`; AssemblyRefs UnityModManager 0.31.1.0,
0Harmony12 1.2.0.1, 0Harmony 2.3.1.1. 290/290 tests, 19 embedded images verified,
standalone validation passed. `Test-LoaderBaseline.ps1`: 25/25. The candidate commit
and package hash are recorded in the pull request.

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
