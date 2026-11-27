<!-- agent-review:v1 enrollment=kwr-codex-umm-0311-baseline-20261123-20261123181024-1057 key=r03-dispositions-982fc89d85 part=1/1 sha256=982fc89d85ca2d0f1428457b2481341cb923434d97f54f312edb536e38c09aac -->
### Claude Opus — finding dispositions — round 3

Candidate after corrections: `35dc80db3d3e173d4369a65aad97bf1f803c693d`

- **R2-F1** — `fixed` — commits: `97a8659`, `afd8c2b`, `5b91fa1`, `258978c`, `efc32d8`, `35dc80d`
  docs/evidence/umm-0311/loader-baseline.json (108/108 on a temporary clone of 5b91fa1): packaged-dll-is-this-invocations-compile (OutputPath redirected in the local props, a source change alters the compiled bytes, an older valid DLL left at bin\Release: older 7cd3cb09 unchanged, produced 21ec80e3 = packaged 21ec80e3).
- **R2-F2** — `fixed` — commits: `97a8659`, `afd8c2b`, `5b91fa1`, `258978c`, `efc32d8`, `35dc80d`
  docs/evidence/umm-0311/loader-baseline.json (108/108 on a temporary clone of 5b91fa1): locked-receipt-replacement-fails-and-keeps-previous-pair (existing receipt held open without delete sharing; exit 1; previous ZIP and receipt hashes unchanged), locked-receipt-previous-pair-still-validates (verified-current), retry-after-receipt-lock-released-succeeds, failed-replacement-restores-previous-zip-and-receipt.
- **R1-F1** — `fixed` — commits: `97a8659`, `afd8c2b`, `5b91fa1`, `258978c`, `efc32d8`, `35dc80d`
  docs/evidence/umm-0311/loader-baseline.json (108/108 on a temporary clone of 5b91fa1): provenance-receipt-commit-differing-only-in-packaged-image-rejected (commit differing only in assets/icons/RefugeToken.png), build-with-response-file-records-it-and-its-resource, provenance-response-file-build-current, provenance-after-response-file-resource-changed-rejected (response file bytes unchanged), provenance-rebuilt-pair-after-resource-change-current, provenance-after-documentation-only-commit-current.
- **R2-F5** — `fixed` — commits: `97a8659`, `afd8c2b`, `5b91fa1`, `258978c`, `efc32d8`, `35dc80d`
  docs/evidence/umm-0311/loader-baseline.json (108/108 on a temporary clone of 5b91fa1): kwrruntime-interrupted-reinstall-survives-close-and-recovers (temporary directories and a real temporary journal: injected placement failure leaves phase 'reinstalling'; Close keeps it; launch refused; recovery restores 'closed' with DLL 7cd3cb09; launch then admitted by the fixture stub, which stops before Steam), kwrruntime-reinstall-verifies-staging-and-is-non-launchable-while-mutating. The live game is outside the test.
- **R3-F1** — `fixed` — commits: `97a8659`, `afd8c2b`, `5b91fa1`, `258978c`, `efc32d8`, `35dc80d`
  docs/evidence/umm-0311/loader-baseline.json (108/108 on a temporary clone of 5b91fa1): install-helper-modes-follow-the-validator (receiptless: external-content-inspected; receiptless -Diagnostic: external-content-inspected; dirty receipt -Diagnostic: diagnostic; clean current pair: frozen-candidate), install-helper-frozen-mode-refuses-dirty-receipt, external-zip-content-inspection-without-provenance.
- **R1-F2** — `fixed` — commits: `97a8659`, `afd8c2b`, `5b91fa1`, `258978c`, `efc32d8`, `35dc80d`
  docs/evidence/umm-0311/loader-baseline.json (108/108 on a temporary clone of 5b91fa1) with the reference cache absent: validator-offline-without-reference-cache-accepted (genuine definition), validator-offline-pin-digest-with-trailing-line-feed-rejected, validator-offline-pin-missing-required-archive-digest-rejected, validator-offline-pin-archive-digest-without-entry-rejected.
- **R3-F2** — `fixed` — commits: `97a8659`, `afd8c2b`, `5b91fa1`, `258978c`, `efc32d8`, `35dc80d`
  docs/evidence/umm-0311/loader-baseline.json (108/108 on a temporary clone of 5b91fa1): file-identity-matches-trusted-windows-read (validator 6cfdfe4e:003000000029345e = fsutil file queryfileid + vol), report-path-distinct-existing-file-written, report-path-hard-link-to-package-rejected, report-destination-replaced-during-validation-refused (package and receipt unchanged), report-identity-refusals-leave-inputs-intact.
- **R3-F3** — `fixed` — commits: `97a8659`, `afd8c2b`, `5b91fa1`, `258978c`, `efc32d8`, `35dc80d`
  docs/evidence/umm-0311/loader-baseline.json: clone_build_receipt.commit 5609b015 = test_clone_commit, its zipSha256 6abb3957 = the clone-build-at-different-version case's ZIP; the later rebuild is a separate rebuild_receipt record (commit 186f3448) with its own note.
- **R3-F4** — `fixed` — commits: `97a8659`, `afd8c2b`, `5b91fa1`, `258978c`, `efc32d8`, `35dc80d`
  docs/evidence/umm-0311/loader-baseline.json (108/108 on a temporary clone of 5b91fa1): shipping-0.12.99-and-alternate-fixture-are-distinct-validated-packages (shipping 0.12.99 pair 33ee0cfc/c78b7305 validated with current provenance and unchanged after the alternate build; alternate fixture 0.12.100, ZIP 27e8692b, validated), alternate-version-build-keeps-shipping-pair.
- **R3-F5** — `fixed` — commits: `efc32d8`
  PR #12 description (updated with gh pr edit, read back; docs/evidence/umm-0311/pr-12-description.json): '-ReceiptPath alone: byte matching only' and '-ReceiptPath with -RequireCurrentProvenance' as separate items, with the install modes KwrRuntime records.
- **R1-F8** — `fixed` — commits: `258978c`, `efc32d8`
  docs/evidence/umm-0311/readme-audit-run.json (run 2026-11-24T03:01:57Z on a clean checkout of 5b91fa1, dotnet SDK 8.0.424): every README step with its exit code and output, the audit project built from loader-api-audit-Program.cs.txt and loader-api-audit-csproj.txt, invocation from audit\bin\Release\net8.0, four profile results, mod digest equal to package-validation.json.
- **R1-F9** — `rebutted` — commits: `35dc80d`
  Owner amendment recorded by the controller on 2026-11-24 (resume --amend-mission; see the effective task): after the current effective profile and preflight are verified against the enrollment's configuration, the absence of contemporaneous pre-enrollment check-project evidence is accepted as a documented historical process gap; it waives no current defect. Verified 2026-11-24 and exported: effective-profile-verification-20261124.json (profile sha256 d67f653b equals the enrollment snapshot), onboarding-check-project-20261124.json (problems []), onboarding-preflight-20261124.json (ok). Chronology preserved: pre-enrollment-check-project.json (transcript summary, 2026-11-23T23:09:33Z) and onboarding-check-project.json (2026-11-24T01:01:16Z) are unchanged; INDEX.md states that the new records are not pre-enrollment evidence.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/agent_review.py verify --enrollment kwr-codex-umm-0311-baseline-20261123-20261123181024-1057 --gate baseline | yes | pass | 322.4s | Controller receipt on 35dc80d: the gate runs Test-LoaderBaseline.ps1 on that commit; RESULT 108/108 loader-baseline checks passed. |
| scripts/Test-LoaderBaseline.ps1 (temporary clone of 5b91fa1) | no | pass | ~12 min | 108/108; docs/evidence/umm-0311/loader-baseline.json (108/108 on a temporary clone of 5b91fa1). Later commits change documentation and evidence only. |
| README audit procedure from a clean checkout of 5b91fa1 | no | pass | ~1 min | docs/evidence/umm-0311/readme-audit-run.json: audit project built from the two committed files, invoked from its documented output path, four profile rows, mod digest matching package-validation.json. |
| agent_review.py check-project / preflight --project kingmaker-waking-rune (2026-11-24) | no | pass | ~2 min | onboarding-check-project-20261124.json (problems []), onboarding-preflight-20261124.json (ok), effective-profile-verification-20261124.json (profile sha256 d67f653b equals the enrollment snapshot). Current-configuration evidence, not pre-enrollment evidence. |
| Native checks on UMM 0.31.1 / 0.32.4 / 0.33.0 / 0.33.4 | no | not_run | 0 | No approved disposable game copy; KwrRuntime transacts against the live game, which this mission does not authorize. |

_Model-generated developer response (Claude opus / high), posted by the agent-review controller through the owner's GitHub account; not a human review._
