<!-- agent-review:v1 enrollment=kwr-codex-umm-0311-baseline-20261123-20261123181024-1057 key=r01-dispositions-929cec8dea part=1/1 sha256=929cec8dea7e78de67c3fbf6632bc7802315b27299d304971f83a59b85a264dc -->
### Claude Opus — finding dispositions — round 1

Candidate after corrections: `c328c65e00f8aa15d016f0637a1174b4ee007dd4`

- **R1-F1** — `fixed` — commits: `c0a6732`, `0db1d88`, `1ab548e`, `c328c65`
  docs/evidence/umm-0311/loader-baseline.json (73/73 on a temporary clone of c0a6732: empty reference cache, product version 0.12.99 committed in the clone): provenance-clean-candidate-current, external-zip-content-inspection-without-provenance (source provenance: not-checked), provenance-after-reference-helper-change-rejected, provenance-after-resolved-project-setting-change-rejected (CheckForOverflowUnderflow in LocalGamePaths.props), provenance-current-again-after-restore, provenance-old-pair-after-committed-helper-change-rejected (same mod and loader versions), provenance-rebuilt-candidate-current.
- **R1-F2** — `fixed` — commits: `c0a6732`, `0db1d88`, `1ab548e`, `c328c65`
  docs/evidence/umm-0311/loader-baseline.json (73/73 on a temporary clone of c0a6732: empty reference cache, product version 0.12.99 committed in the clone): validator-pin-definition-without-harmony2-rejected, validator-umm-pin-disagrees-with-minimum-rejected, validator-additional-harmony-adapter-reference-rejected (stub DLL compiled against the three pins plus a 0Harmony-1.2 adapter), validator-offline-without-reference-cache-accepted, validator-genuine-candidate-control-accepted.
- **R1-F3** — `fixed` — commits: `c0a6732`, `0db1d88`, `1ab548e`, `c328c65`
  docs/evidence/umm-0311/loader-baseline.json (73/73 on a temporary clone of c0a6732: empty reference cache, product version 0.12.99 committed in the clone): validator-differently-named-assembly-under-product-file-rejected (benign 'NotWakingRune' with the three correct loader references placed under KingmakerWakingRune.dll), validator-genuine-candidate-control-accepted.
- **R1-F4** — `fixed` — commits: `c0a6732`, `0db1d88`, `1ab548e`, `c328c65`
  docs/evidence/umm-0311/loader-baseline.json (73/73 on a temporary clone of c0a6732: empty reference cache, product version 0.12.99 committed in the clone): validator-relative-literal-path-from-powershell-location (relative 'pkg [1].zip' with the lowered legacy ZIP of the same name in the process directory resolves to the candidate digest), validator-report-path-equal-to-package-rejected, validator-report-path-equal-to-receipt-rejected, validator-report-collisions-leave-inputs-intact.
- **R1-F5** — `fixed` — commits: `c0a6732`, `0db1d88`, `1ab548e`, `c328c65`
  docs/evidence/umm-0311/loader-baseline.json (73/73 on a temporary clone of c0a6732: empty reference cache, product version 0.12.99 committed in the clone): install-helper-records-captured-source-and-digest-after-caller-zip-moved (bracketed folder and file names; caller ZIP moved after validation; recorded source path, validated digest and verified-current provenance; installed DLL = receipt DLL; private copy removed), install-helper-rejected-receipt-cleans-private-copy, kwrruntime-begin-and-reinstall-use-the-helper.
- **R1-F6** — `fixed` — commits: `c0a6732`, `0db1d88`, `1ab548e`, `c328c65`
  docs/evidence/umm-0311/loader-baseline.json (73/73 on a temporary clone of c0a6732: empty reference cache, product version 0.12.99 committed in the clone): build-failure-after-sidecar-leaves-no-candidate (instrumented disposable Build.ps1 throws after writing the receipt in staging: no ZIP, receipt or .pending folder left), build-clean-retry-succeeds-without-replace, build-refuses-to-replace-clean-candidate.
- **R1-F7** — `fixed` — commits: `c0a6732`, `0db1d88`, `1ab548e`, `c328c65`
  docs/evidence/umm-0311/loader-baseline.json (73/73 on a temporary clone of c0a6732: empty reference cache, product version 0.12.99 committed in the clone): clone-cold-cache-build-at-different-version (no seeded cache; pins downloaded and verified by the clone; version 0.12.99; artifact name from the manifest), legacy-release-references-umm-0.33.2-harmony-2.3.6 and package-legacy-0.33.2-dll-with-0.31.1-manifest-rejected (lowered fixture also carries the clone's version, so only the UMM reference differs).
- **R1-F8** — `fixed` — commits: `0db1d88`
  README.md Install: the reviewed build's artifacts/KingmakerWakingRune-<version>.zip and its receipt, distinguished from the published pre-baseline v0.12.3 ZIP; two-file layout; Validate-Package command with -RequireCurrentProvenance; Build section: runnable LoaderApiAudit command with the four profiles and their components. docs-link-check.json: 0 broken links. Package digest and audit output for all four profiles: package-validation.json, api-audit.json.
- **R1-F9** — `fixed` — commits: `c328c65`
  docs/evidence/umm-0311/onboarding-check-project.json (2026-11-24: problems [], origin, actor, permission, branch pattern, guarded push, gate baseline), onboarding-preflight.json (onboarding preflight 2026-11-23T23:09Z: developer/reviewer models observed, permission isolation), onboarding-profile-summary.json (user-local profile: remotes, gates, limits, push policy, developer allow/deny lists, runtime lock paths; the profile itself stays user-local), pr-12-description.json (read-only record of draft PR #12).
- **R1-F10** — `fixed` — commits: `c328c65`
  docs/evidence/umm-0311/INDEX.md labels the exported column as Git-blob SHA-256 and gives git cat-file blob ... | sha256sum and a Python equivalent.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/agent_review.py verify --enrollment kwr-codex-umm-0311-baseline-20261123-20261123181024-1057 --gate baseline | yes | pass | 144.5s | Controller receipt on c328c65 (the gate runs Test-LoaderBaseline.ps1 on that commit). |
| scripts/Test-LoaderBaseline.ps1 (temporary clone of c0a6732) | no | pass | ~6 min | 73/73; docs/evidence/umm-0311/loader-baseline.json (73/73 on a temporary clone of c0a6732: empty reference cache, product version 0.12.99 committed in the clone). |
| scripts/Build.ps1 + Validate-Package.ps1 -RequireCurrentProvenance on a clean clone of c0a6732 | no | pass | ~3 min | build-receipt.json, package-validation.json (verified-current), direct-build-record.json (Csc /reference list). |
| LoaderApiAudit on 7cd3cb09; profile inventory; docs link check | no | pass | ~2s | api-audit.json (14 types/39 members resolved on all four profiles), loader-profile-inventory.json, docs-link-check.json (0 broken). Static only. |
| Native checks on UMM 0.31.1 / 0.32.4 / 0.33.0 / 0.33.4 | no | not_run | 0 | No disposable game copy; KwrRuntime transacts against the live game. |

_Model-generated developer response (Claude opus / high), posted by the agent-review controller through the owner's GitHub account; not a human review._
