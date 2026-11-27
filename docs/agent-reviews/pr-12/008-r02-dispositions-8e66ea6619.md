<!-- agent-review:v1 enrollment=kwr-codex-umm-0311-baseline-20261123-20261123181024-1057 key=r02-dispositions-8e66ea6619 part=1/1 sha256=8e66ea6619bc6d5a8870c97eab974e6d4b5eed52e374a1097c2ca516489dec1a -->
### Claude Opus — finding dispositions — round 2

Candidate after corrections: `dbc715ced16b00aa6ff01727d06d6108402b94ac`

- **R1-F1** — `fixed` — commits: `3e331cf`, `68815b0`, `3c9f79e`, `be93a26`, `dbc715c`
  docs/evidence/umm-0311/loader-baseline.json (88/88 on a temporary clone of 3c9f79e): provenance-receipt-older-commit-rejected (material inputs differ from the recorded commit), provenance-receipt-unknown-commit-rejected, provenance-receipt-other-version-rejected, provenance-after-base-address-change-rejected, build-with-external-win32-resource-records-its-content, provenance-after-win32-resource-replaced-rejected, rebuild-after-options-removed-reproduces-dll, provenance-clean-candidate-current.
- **R2-F1** — `fixed` — commits: `3e331cf`, `68815b0`, `3c9f79e`, `be93a26`, `dbc715c`
  Build.ps1 compiles the product with -t:Rebuild and requires the production command line (written by that compile) to equal the evaluated inventory; docs/evidence/umm-0311/loader-baseline.json (88/88 on a temporary clone of 3c9f79e): unmodified-candidate-cold-cache-build-and-receipt-check, build-clean-retry-succeeds-without-replace, rebuild-after-options-removed-reproduces-dll.
- **R1-F2** — `fixed` — commits: `3e331cf`, `68815b0`, `3c9f79e`, `be93a26`, `dbc715c`
  scripts/Get-PinnedLoaderReferences.ps1 validates every pin field (file name, digest, version, name, https URL, duplicates, archive digest) before the -DefinitionOnly return. docs/evidence/umm-0311/loader-baseline.json (88/88 on a temporary clone of 3c9f79e): validator-pin-definition-without-harmony2-rejected, validator-umm-pin-disagrees-with-minimum-rejected, validator-offline-without-reference-cache-accepted.
- **R1-F4** — `fixed` — commits: `3e331cf`, `68815b0`, `3c9f79e`, `be93a26`, `dbc715c`
  docs/evidence/umm-0311/loader-baseline.json (88/88 on a temporary clone of 3c9f79e): report-path-equal-to-adjacent-receipt-without-receipt-argument-rejected, report-path-hard-link-to-package-rejected (existing destination compared by volume and file index), report-identity-refusals-leave-inputs-intact, validator-report-path-equal-to-package-rejected, validator-report-path-equal-to-receipt-rejected.
- **R2-F2** — `fixed` — commits: `3e331cf`, `68815b0`, `3c9f79e`, `be93a26`, `dbc715c`
  docs/evidence/umm-0311/loader-baseline.json (88/88 on a temporary clone of 3c9f79e): failed-replacement-restores-previous-zip-and-receipt (instrumented disposable Build.ps1 fails after placing the new ZIP under -ReplacePackage; the previous ZIP and receipt are byte-identical afterwards), build-failure-after-sidecar-leaves-no-candidate, build-refuses-to-replace-clean-candidate.
- **R2-F3** — `fixed` — commits: `3e331cf`, `68815b0`, `3c9f79e`, `be93a26`, `dbc715c`
  docs/evidence/umm-0311/loader-baseline.json (88/88 on a temporary clone of 3c9f79e): unmodified-candidate-cold-cache-build-and-receipt-check (shipping version at the unmodified commit, empty cache before the build, standalone receipt check with current provenance) runs before clone-build-at-different-version and the legacy regression.
- **R2-F4** — `fixed` — commits: `3e331cf`, `68815b0`, `3c9f79e`, `be93a26`, `dbc715c`
  docs/evidence/umm-0311/loader-baseline.json (88/88 on a temporary clone of 3c9f79e): install-helper-frozen-mode-refuses-dirty-receipt, install-helper-diagnostic-mode-stages-dirty-receipt-build (MODE diagnostic receipt-matches-package); the mode is recorded in the transaction (installMode).
- **R2-F5** — `fixed` — commits: `3e331cf`, `68815b0`, `3c9f79e`, `be93a26`, `dbc715c`
  docs/evidence/umm-0311/loader-baseline.json (88/88 on a temporary clone of 3c9f79e): kwrruntime-reinstall-verifies-staging-and-is-non-launchable-while-mutating (staging verified before the phase changes to 'reinstalling', which is recorded before the installed folder is removed; Launch accepts only installed/closed); install-helper-records-captured-source-and-digest-after-caller-zip-moved exercises the staging helper on a temporary destination.
- **R1-F8** — `fixed` — commits: `68815b0`
  README.md: creates the net8.0 project from loader-api-audit-Program.cs.txt and loader-api-audit-csproj.txt, builds it and invokes the explicit output path; lists the three component-assembled profiles and the measured owner copy separately. docs-link-check.json: 0 broken.
- **R1-F9** — `fixed` — commits: `dbc715c`
  docs/evidence/umm-0311/onboarding-push-policy.json: the installed controller (package 85dece1c) defines no path-level protected list and the profile sets none; effective protection is GitHub branch protection; guarded-push rules, remotes, deny list and runtime locks listed. It states that the pre-enrollment check-project run of 2026-11-23 was not retained and cannot be confirmed; the dated 2026-11-24 run (onboarding-check-project.json, problems []) and the 2026-11-23 preflight are retained.
- **R2-F6** — `fixed` — commits: `dbc715c`
  The draft PR #12 description was updated through gh pr edit (review comments and status untouched): Validation now records the Windows build, 290 tests, the boundary suite, the controller gate and the static audit, with in-game runtime as the only Not run item; docs/evidence/umm-0311/pr-12-description.json is the read-back of the current body.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/agent_review.py verify --enrollment kwr-codex-umm-0311-baseline-20261123-20261123181024-1057 --gate baseline | yes | pass | 196.3s | Controller receipt on dbc715c (the gate runs Test-LoaderBaseline.ps1 on that commit). |
| scripts/Test-LoaderBaseline.ps1 (temporary clone of 3c9f79e) | no | pass | ~8 min | 88/88; docs/evidence/umm-0311/loader-baseline.json (88/88 on a temporary clone of 3c9f79e). |
| scripts/Build.ps1 + Validate-Package.ps1 -RequireCurrentProvenance on a clean clone of 3c9f79e | no | pass | ~3 min | build-receipt.json, package-validation.json (verified-current), direct-build-record.json. |
| LoaderApiAudit on 7cd3cb09; profile inventory; docs link check | no | pass | ~2s | api-audit.json (14 types/39 members resolved on four profiles), loader-profile-inventory.json, docs-link-check.json (0 broken). |
| Native checks on UMM 0.31.1 / 0.32.4 / 0.33.0 / 0.33.4 | no | not_run | 0 | No disposable game copy; KwrRuntime transacts against the live game. |

_Model-generated developer response (Claude opus / high), posted by the agent-review controller through the owner's GitHub account; not a human review._
