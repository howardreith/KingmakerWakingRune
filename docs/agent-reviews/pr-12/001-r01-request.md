<!-- agent-review:v1 enrollment=kwr-codex-umm-0311-baseline-20261123-20261123181024-1057 key=r01-request part=1/1 sha256=57e87e3faeafa05ed9771fd62505b101fdf1f59726284500b0f3ff4ea4d32a57 -->
### Claude Opus — review request — round 1 — candidate `8ae782f6ac`

| Field | Value |
|---|---|
| Request ID | `kwr-codex-umm-0311-baseline-20261123-20261123181024-1057-r01-13aac7` |
| Checkpoint | `final` (final) |
| Candidate H | `8ae782f6acbe53553e37dcb5f7a9d9c216a69981` |
| Target B (`main`) | `480cdbd31425c8855f5856936aeaff60ae236d88` |
| Merge base M | `480cdbd31425c8855f5856936aeaff60ae236d88` |
| Mission | `kingmaker-umm-0311-mission-20261123` sha256 `f7d990f396` |
| Workflow | `6d613defe7` |
| Owner guidance | none |
| Owner amendments | none |
| Developer observed | foreground session `e178a93b-ca53-4b6b-a4d2-a357d48eaa77`: models ['claude-opus-5-5'], effort ['high'], permission modes ['auto'] (CLI-recorded transcript; the headless permission profile does not apply to a foreground session) |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `max`, read-only sandbox |
| Round budget | 1 of 5 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

Waking Rune UMM 0.31.1 baseline, adopting PR #12 (codex/umm-0311-baseline-20261123; its first commit 27e94ac was prepared offline and never built). This repository only; a user-local agent-review profile (kingmaker-waking-rune) was created through init-project because none existed.

Contract: Info.json ManagerVersion 0.31.1; the production csproj compiles against UnityModManager 0.31.1.0 (author NuGet, DLL 2e038ff2...), 0Harmony12 1.2.0.1 (aa1cd483...) and, because Waking Rune calls Harmony2 directly (dialogue exception finalizers), 0Harmony 2.3.1.1 (official Lib.Harmony net48, 5f7ad0df...; the Harmony2 that UMM 0.31.1 itself references). All three are pinned in scripts/loader-reference-pins.json, fetched into ignored obj/loader-references and verified; no fallback to the installed loader.

Added here (3ca53fc, 8ae782f): canonical three-pin set and string-typed ManagerVersion in the verifier; LoaderReferences.targets checks the exact Csc list (@(ReferencePathWithRefAssemblies) after FindReferenceAssembliesForReferences) for all three assemblies by full path (file name or FusionName), with DisableFastUpToDateCheck and loader inputs as compile inputs; new standalone scripts/Validate-Package.ps1 (private snapshot, two-file allow-list, decoded-empty directory records, string manifest fields, actual AssemblyRefs = all three pins, out-of-process metadata read under Windows PowerShell, optional typed receipt binding ZIP/DLL bytes and pin identities); Build.ps1 writes <zip>.build.json and runs the validator; KwrRuntime.ps1 validates a private copy and installs that same copy (Begin, Reinstall); scripts/Test-LoaderBaseline.ps1 (51 checks in a temporary clone with a space in its path, private hash-checked fixtures: genuine v0.12.3 release compiled against UMM 0.33.2.0/Harmony 2.3.6.0, author UMM 0.32.4, Lib.Harmony 2.3.6 net48); docs/UMM-COMPATIBILITY.md and README updated; structured evidence committed in docs/evidence/umm-0311/ (no logs; local path prefixes replaced).

Gate: the configured baseline gate IS the boundary suite; the controller receipt on 8ae782f shows 51/51. Candidate DLL 7cd3cb09f12f75a7247c1b401673528a0773a139e59dc3a940da860c28f24904 (MVID ccc59117-f992-44ce-9355-540d35f181f0), AssemblyRefs UnityModManager 0.31.1.0, 0Harmony12 1.2.0.1, 0Harmony 2.3.1.1; 290/290 unit tests; 19 embedded images verified. Static audit (docs/evidence/umm-0311/api-audit.json): 14 types/39 members incl. the Harmony2 finalizer surface resolved on 0.31.1, 0.32.4, owner 0.33.0 and 0.33.4 (component-assembled profiles labelled). Native runtime: NOT RUN on all loaders (KwrRuntime transacts against the live game and running another loader would replace the owner's live UMM; no disposable game copy).

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/agent_review.py verify --enrollment kwr-codex-umm-0311-baseline-20261123-20261123181024-1057 --gate baseline | yes | pass | 60.5s | Controller receipt on 8ae782f: Test-LoaderBaseline.ps1, 51/51, built and packaged in a temporary clone of 8ae782f (Build.ps1: 290 tests, art check, package, receipt, standalone validation). KwrRuntime.ps1 is not executed (live game); its install source is checked statically. |
| scripts/Build.ps1 on clean 3ca53fc + direct normal-verbosity MSBuild | no | pass | ~30s | docs/evidence/umm-0311/build-receipt.json, build-summary.json, direct-build-record.json (Csc /reference to <repo>/obj/loader-references/UnityModManager.dll, 0Harmony12.dll, 0Harmony.dll). 8ae782f only changes the suite's legacy-fixture download (private repo -> gh), so the product and its build inputs are unchanged. |
| LoaderApiAudit (source in docs/evidence/umm-0311/loader-api-audit-*.txt) | no | pass | ~1s | Static only; no binding/reflection/Harmony behaviour/Unity/save claim. |
| Native in-game checks on UMM 0.31.1 / 0.32.4 / 0.33.0 / 0.33.4 | no | not_run | 0 | No disposable game copy; live installation must not be modified. |

**Evidence references:**
- docs/evidence/umm-0311/INDEX.md
- docs/evidence/umm-0311/loader-baseline.json
- docs/evidence/umm-0311/direct-build-record.json
- docs/evidence/umm-0311/api-audit.json
- docs/evidence/umm-0311/loader-profile-inventory.json
- docs/UMM-COMPATIBILITY.md

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
