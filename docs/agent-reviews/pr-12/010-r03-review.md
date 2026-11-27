<!-- agent-review:v1 enrollment=kwr-codex-umm-0311-baseline-20261123-20261123181024-1057 key=r03-review part=1/1 sha256=1137dbad1631a6d23fb316cbd6bcb0f3dc8f0c75c5f8a471b6733fcda1862c86 -->
### Codex GPT-6.2 Astra — review — round 3 — candidate `dbc715ced1`

**Decision:** `changes_required` — The controller gate passed 88/88 checks at the submitted commit. Three untested paths still affect acceptance. A redirected build can certify an older DLL, a locked receipt can cause replacement to delete the previous validated ZIP, and Close can make a failed reinstall launchable under the previous package identity.

## Continuation for Claude

Bind packaging to the DLL produced by the measured MSBuild invocation. Carry material inputs through both the compiler fingerprint and the producing-commit comparison. Protect the previous ZIP and receipt throughout backup, placement and rollback. Keep a failed reinstall non-launchable until recovery, and reserve frozen-candidate status for a receipt that passed current-provenance validation.

Exercise these paths with real files, redirected output, a locked sidecar and a temporary transaction journal. Correct the offline pin checks, report-file handling, alternate-version fixture and exported receipt pairing identified in the findings. Run the revised README audit procedure, correct the PR’s receipt-only validation claim, and rerun the configured gate at the next candidate. Preserve the historical onboarding gap if a contemporaneous record cannot be recovered.

Keep the corrections within Waking Rune’s existing build, validation, staging and documentation workflows. Preserve the enrollment, historical artifacts and owner installation. The four native rows remain NOT RUN under the documented fixture limitation.

#### Findings

**R2-F1** · P1 · defect · blocks readiness — A fresh compile can still package the previous DLL
- Location: `scripts/Build.ps1:62`; acceptance: mission §5, mission §6
- Trigger: Redirect the production output in LocalGamePaths.props while an older baseline DLL remains in bin/Release.
- Expected: The ZIP contains the DLL produced by this invocation, or packaging refuses before issuing a receipt.
- Actual: Rebuild compiles the product, but line 62 still copies bin/Release/KingmakerWakingRune.dll. The inventory comparison records the output filename without its directory and never compares the copied DLL to the newly produced TargetPath. An older DLL with the expected art and references can receive the new receipt.
- Why it blocks / does not: The wrapper can qualify different product bytes from those compiled for the recorded inputs.
- Requested correction: Package from an invocation-owned output or capture the evaluated production TargetPath and verify its digest through staging.
- Requested evidence: Leave a valid older DLL at the conventional path, redirect the product output, and make a source change that alters the compiled bytes. Record the produced and packaged digests or the explicit refusal.

**R2-F2** · P1 · defect · blocks readiness — A locked sidecar still causes loss of the previous ZIP
- Location: `scripts/Build.ps1:141`; acceptance: mission §5, mission §6
- Trigger: Run an authorized replacement while the existing receipt permits reading but prevents renaming.
- Expected: The previous ZIP and receipt survive a failure at either backup or placement step.
- Actual: The backup loop moves the old ZIP into pending/previous before the rollback try block. If moving the old receipt then fails, the outer finally deletes pending recursively, including the only saved copy of the old ZIP.
- Why it blocks / does not: An authorized replacement can permanently destroy the previously validated artifact.
- Requested correction: Include backup moves in the rollback scope. Track completed moves and retain recoverable backups if restoration itself fails.
- Requested evidence: Hold the existing receipt open without delete sharing in a temporary checkout. Verify both original hashes after the failed replacement, validate the pair, release the lock and demonstrate a successful retry.

**R1-F1** · P1 · defect · blocks readiness — Current provenance omits material source and compiler inputs
- Location: `scripts/Validate-Package.ps1:234`; acceptance: mission §5, mission §6
- Trigger: Change the claimed commit across an embedded-art change, or change a resource named inside a compiler response file.
- Expected: Qualification checks tracked material inputs against the producing commit and detects changes to files consumed through compiler arguments.
- Actual: The Git comparison omits assets/, although the real receipt lists 19 embedded images from that directory. Changing only receipt.commit to a commit with different artwork can therefore pass. Separately, KwrBuild.Common.ps1 line 52 hashes an @response file and stops there. Changing an external resource named inside an unchanged response file leaves that inventory unchanged.
- Why it blocks / does not: Current-provenance validation can attribute a DLL to the wrong source tree or retain qualification after a material compiler input changes.
- Requested correction: Include tracked compiler inputs such as embedded resources in the commit comparison. Account for files referenced through response files, or refuse current qualification when their dependencies cannot be identified.
- Requested evidence: Reject a receipt naming a commit that differs only in a packaged image. Show an unchanged response file losing qualification after its resource changes, a rebuilt pair qualifying, and a documentation-only commit retaining qualification.

**R2-F5** · P2 · defect · blocks readiness — Close makes an interrupted reinstall launchable
- Location: `scripts/KwrRuntime.ps1:524`; acceptance: mission §6, mission §7
- Trigger: Call Close after Reinstall has removed the previous mod and failed to finish installing its replacement.
- Expected: Only recovery or successful verified installation can make an incomplete reinstall launchable.
- Actual: Reinstall saves the reinstalling phase before deleting the old mod. If placement fails, Invoke-Close accepts that journal with no owned process and unconditionally saves closed. Invoke-Launch accepts closed, while the journal still names the previous package.
- Why it blocks / does not: A failed installation can be launched and evaluated under a stale package identity.
- Requested correction: Preserve the recovery phase across Close and other ordinary phase transitions. Keep End available to restore the transaction.
- Requested evidence: Induce a post-mutation placement or digest failure using temporary directories and a real temporary journal. Exercise Close and launch admission, then demonstrate recovery. Keep the live game outside the test.

**R3-F1** · P2 · defect · blocks readiness — Receiptless packages are recorded as frozen candidates
- Location: `scripts/KwrPackage.ps1:36`; acceptance: mission §5, mission §6, mission §7
- Trigger: Pass a valid ZIP without its sidecar to the default staging helper.
- Expected: The frozen-candidate label requires verified-current provenance. A supported external ZIP receives an accurate content-only classification.
- Actual: Without an adjacent receipt, line 26 performs content-only validation and returns source provenance not-checked. Line 36 nevertheless assigns frozen-candidate whenever Diagnostic is false. Begin and Reinstall persist that label.
- Why it blocks / does not: A transaction can identify an unqualified external or dirty ZIP as a frozen candidate.
- Requested correction: Derive the recorded qualification from the validator result. Preserve external content inspection and explicit diagnostic staging with distinct, accurate labels.
- Requested evidence: Exercise a receiptless ZIP, a receipt-backed diagnostic ZIP and a clean current pair through the temporary staging path. Show that only the last records frozen-candidate.

**R1-F2** · P2 · defect · blocks readiness — Offline validation still accepts malformed pin definitions
- Location: `scripts/Get-PinnedLoaderReferences.ps1:40`; acceptance: mission §5, mission §6
- Trigger: Inspect a receiptless ZIP using a pin with a trailing line feed in its digest or an incomplete archive definition.
- Expected: The offline path requires an exact digest and a consistent archive-entry and archive-digest pair.
- Actual: The digest expression ends in $, which accepts a final line feed in PowerShell. DefinitionOnly accepts that malformed digest while the byte comparison rejects it. A NuGet pin with an archive digest and a null ArchiveEntry also skips the archive check and passes definition validation, although cold acquisition treats the whole package as the DLL.
- Why it blocks / does not: Standalone content validation still accepts pin definitions that cannot produce the declared compiler reference.
- Requested correction: Validate exact string length and characters through the absolute end of each digest. Validate archive fields together before the offline return.
- Requested evidence: With the cache absent, run the standalone validator against a genuine definition, a malformed digest including a trailing line feed, a missing required archive digest and an archive digest with no entry.

**R3-F2** · P2 · defect · blocks readiness — The report writer can act on a different file identity from the one checked
- Location: `scripts/Validate-Package.ps1:47`; acceptance: mission §5, mission §6
- Trigger: Publish to an existing report on another volume or change the report destination while package validation is in progress.
- Expected: Report publication compares the actual volume and full file index and preserves protected inputs through the write.
- Actual: The Win32 structure uses three aligned C# long fields for native FILETIME pairs. On 64-bit Windows this places Volume at offset 32 instead of 28 and misreads the file index. Also, the handles used for comparison are closed before Set-Content reopens ReportPath at line 261. A destination changed to an input alias during validation can then overwrite that input.
- Why it blocks / does not: The result can reject a distinct existing report or write through a different file identity from the one checked.
- Requested correction: Use the native field layout and bind the write to the checked destination identity before truncating an existing file, or use an equivalent safe publication operation.
- Requested evidence: Compare the reported identity with a trusted Windows file-ID read. Exercise distinct existing files, a hard link and a destination changed during validation in a temporary directory. Verify the ZIP and receipt hashes survive.

**R3-F3** · P2 · defect · blocks readiness — The boundary report attaches the wrong receipt to its clone build
- Location: `scripts/Test-LoaderBaseline.ps1:520`; acceptance: mission §6, mission §8
- Trigger: Read the structured suite report after the helper-change and compiler-option rebuilds.
- Expected: The named clone commit, ZIP digest and corresponding build receipt identify one build.
- Actual: Line 520 replaces the initial fixture receipt with a later rebuild receipt. The committed report names test_clone_commit c123b343 and gives ZIP 75ba2942 for that case, while clone_build_receipt names commit e74e13c0 and ZIP c55c1c1e.
- Why it blocks / does not: The structured evidence pairs a fixture commit with the receipt for a different build.
- Requested correction: Preserve the original alternate-version receipt for the summary. Give any later rebuild receipt its own explicitly identified record.
- Requested evidence: Regenerate the suite report and verify that clone_build_receipt.commit and its ZIP digest match test_clone_commit and the corresponding positive case.

**R3-F4** · P2 · defect · blocks readiness — Shipping version 0.12.99 collides with the gate’s alternate-version fixture
- Location: `scripts/Test-LoaderBaseline.ps1:170`; acceptance: mission §6
- Trigger: Run the gate on a clean commit whose valid shipping Version is 0.12.99.
- Expected: The alternate-version fixture differs from the submitted version and preserves the shipping artifact.
- Actual: The first build now occupies the shipping ZIP path. The suite then unconditionally sets Version to 0.12.99. If that is already the shipping version, the fixture has no version change and its second clean build selects the occupied path.
- Why it blocks / does not: The configured gate rejects a valid product version because its own fixture collides with the shipping build.
- Requested correction: Choose a valid fixture version distinct from the shipping version and use it consistently in assertions and artifact names.
- Requested evidence: Exercise the fixture selection with shipping Version 0.12.99 and show two distinct validated packages without replacing the shipping pair.

**R3-F5** · P2 · required_gap · blocks readiness — The revised PR description overstates receipt-only validation
- Location: `docs/evidence/umm-0311/pr-12-description.json:7`; acceptance: mission §8
- Trigger: Follow the draft’s description of Validate-Package.ps1 with a receipt.
- Expected: The draft describes the flag required for current-source qualification.
- Actual: The draft body says supplying a receipt checks the producing source and inputs. ReceiptPath alone returns receipt-matches-package. The commit and current-input checks run only with RequireCurrentProvenance.
- Why it blocks / does not: The owner-facing description overstates what a successful pre-install command establishes.
- Requested correction: Clarify the two receipt modes in the draft description through the permitted PR workflow and refresh the readback.
- Requested evidence: A dated PR body that distinguishes receipt byte matching from verification with -RequireCurrentProvenance.

**R1-F8** · P2 · evidence_gap · blocks readiness — The corrected audit instructions lack their requested execution record
- Location: `README.md:61`; acceptance: mission §8
- Trigger: Verify that the revised README procedure works without a preinstalled audit executable.
- Expected: A recorded run builds the audit tool from the two committed files and uses the documented invocation on the validated product.
- Actual: The README now supplies the project setup, output path and measured owner row. The retained four-row audit predates the revised instructions and lists the profiles in a different order. The cited link check verifies Markdown links only.
- Why it blocks / does not: The requested clean-checkout reproduction of the corrected procedure remains unverified.
- Requested correction: Run the revised procedure from a clean checkout with the documented prerequisites and retain its command output.
- Requested evidence: Show the audit project build, invocation from its documented output path, four profile results and a mod digest matching the standalone package report.

**R1-F9** · P2 · evidence_gap · blocks readiness — The pre-enrollment project check remains unconfirmed
- Location: `docs/evidence/umm-0311/onboarding-push-policy.json:43`; acceptance: mission §4
- Trigger: Verify the required onboarding sequence using the supplied historical records.
- Expected: A contemporaneous record establishes successful project validation before Waking Rune enrollment.
- Actual: The new policy export describes the effective push protection and explicitly says the historical check-project result cannot be confirmed. Preflight is dated 2026-11-23T23:09:42Z. The retained successful check-project result is dated 2026-11-24T01:01:16Z, after enrollment.
- Why it blocks / does not: The unamended mission requires both onboarding checks before enrollment. The later success establishes the later profile state only.
- Requested correction: Recover a dated, read-only controller or enrollment record if one exists. If the record is irretrievable, preserve the gap for explicit owner disposition.
- Requested evidence: The Waking Rune pre-enrollment check-project result or a contemporaneous enrollment record showing that validation and its outcome.

#### Prior finding assessments

- **R1-F1** — `fix_rejected` — The gate now rejects unknown commits, wrong receipt versions, BaseAddress drift and changed direct Win32Resource bytes. The producing-commit comparison still omits embedded artwork, and response-file dependencies escape the current-input check.
- **R1-F2** — `fix_rejected` — Field validation moved before DefinitionOnly. A digest with a final line feed and a half-configured archive still demonstrate the reported disagreement between offline acceptance and acquisition.
- **R1-F4** — `fix_verified` — The gate rejected the conventional adjacent receipt without ReceiptPath and an existing hard link to the package. Both original hashes survived. R3-F2 covers the new identity implementation and the interval before report publication.
- **R1-F8** — `fix_rejected` — The documentation changes address the missing setup and executable path. The earlier matching-DLL audit and the link check do not establish the requested execution of the revised clean-checkout commands.
- **R1-F9** — `fix_rejected` — The effective path policy is now described. The export expressly identifies the missing pre-enrollment check-project record, and the supplied successful check occurred later.
- **R2-F1** — `fix_rejected` — Rebuild forces compilation. Packaging still reads the conventional bin/Release file independently of the production target, and the gate lacks the redirected-output regression.
- **R2-F2** — `fix_rejected` — The injected failure after new ZIP placement restored both hashes. Failure while moving the old receipt occurs before that rollback handler and can delete the previous ZIP.
- **R2-F3** — `fix_verified` — The controller log identifies a cold build and standalone current-provenance check of unmodified H at shipping version 0.12.3 before the separate 0.12.99 case. R3-F4 covers a collision introduced by the two-build sequence.
- **R2-F4** — `fix_verified` — The real helper accepted a receipt-backed dirty build in diagnostic mode and rejected it in frozen mode. Both paths use the same byte checks and staging helper. R3-F1 covers the newly assigned label when the receipt is absent.
- **R2-F5** — `fix_rejected` — Reinstall stages first and records reinstalling before mutation. Close can still promote that journal to closed after failure. The passing source-text assertion does not exercise this transition.
- **R2-F6** — `fix_verified` — The dated draft readback now identifies the Windows build, 290 tests, package validation, boundary gate, four static rows and outstanding native checks. R3-F5 addresses a separate claim introduced in the revised body.

#### Evidence assessment

- **Inspected:** The mission, developer report, transcript notice, prior ledger, applicable repository instructions, all 5,696 feature-diff lines and the correction delta from c328c65.; The production project, MSBuild targets, pin resolver, compiler inventory, packaging and standalone validation scripts, staging helper, transaction phase consumers and boundary-suite assertions.; The frozen build and package records from 3c9f79e28acf473c919d21ab4e46deb323ff3902. Their ZIP digest is 6a49a3fd4aa1800b61565bbb5f2647046449feca733f2830cc51e4fafa617109. The recorded DLL matches the gate build, the API audit and the profile inventory, including MVID ccc59117-f992-44ce-9355-540d35f181f0. Only documentation and evidence changed between that producing commit and H.; The audit source and four-profile result, measured loader inventories, newest-stable selection, onboarding records and dated draft PR #12 readback.
- **Executed verification:** I verified HEAD, the mission SHA-256, all 16 indexed evidence blobs and the controller gate-log SHA-256. The feature diff passed git diff --check.; The controller ran scripts/Test-LoaderBaseline.ps1 at dbc715ced16b00aa6ff01727d06d6108402b94ac. The receipt records exit 0 in 196.3 seconds, and its bound log contains 88 passing cases.; The gate built the unmodified shipping version 0.12.3 from an empty reference cache and ran standalone current-provenance validation. It recorded ZIP ab78697e9d0724adeb45f4970396b28433ef875ac830922a1fc6f6dfd4a7ff0b and DLL 7cd3cb09f12f75a7247c1b401673528a0773a139e59dc3a940da860c28f24904. The separate 0.12.99 build reported 290 tests passed.; The gate observed the three pinned direct compiler references, rejection of corrupt cache bytes, dependency-bearing ZIPs and the genuine older Waking Rune DLL under a lowered manifest. It also exercised an existing hard link, dirty-receipt validation and a placement failure after the new ZIP had moved.; A read-only PowerShell comparison confirmed that the pin digest predicate accepts 64 hex characters followed by a line feed and that this value differs from the canonical digest.
- **Source inspection only:** The redirected-output, locked-sidecar, response-file dependency, receiptless install-mode and failed-reinstall Close paths were traced in source. The gate did not execute those failure cases.; The gate’s reinstall-order case searches script text. Its diagnostic-mode case validates a private copy. Neither case exercises a failed reinstall against a temporary transaction journal.; The committed static audit reports resolution of 14 types and 39 members on four profiles for the matching DLL. The revised README command sequence has no corresponding execution record.
- **Required gaps:** Prove that the receipt binds the actual production output, all material inputs and the recorded source tree, including embedded assets.; Demonstrate recovery when backup of the previous pair fails and when a reinstall fails after destination mutation. Exercise the actual temporary journal transitions.; Close the pin-definition, report-destination and frozen-candidate classification gaps. Keep fixture versions, commits and receipts paired in the gate output.; Supply the clean-checkout audit run and accurate PR wording. The required pre-enrollment check-project timing remains unconfirmed.
- **Limitations:** The whole developer transcript was withheld. The candidate-bound gate receipt and log establish the recorded execution. The PR and static-audit records were inspected locally.; The read-only review performed no builds, installations, network access or game runs. Local PowerShell execution policy also prevented an independent invocation of the read-only DefinitionOnly script.; Native behavior remains NOT RUN for UMM 0.31.1, 0.32.4, owner-installed 0.33.0 and 0.33.4. The repository reports no approved disposable game copy and no combined-stack run.

#### Summary

- **Delivered behavior:** The shipping manifest declares UMM 0.31.1. The normal Windows build and packaged DLL use UnityModManager 0.31.1.0, 0Harmony12 1.2.0.1 and 0Harmony 2.3.1.1. The gate verifies the shipping package, and the static records cover four loader profiles on the same DLL. Artifact provenance, failure recovery and the evidence gaps above prevent final readiness.
- **Scope reviewed:** Final checkpoint for KingmakerWakingRune draft PR #12 at dbc715ced16b00aa6ff01727d06d6108402b94ac, including all eleven pending findings and the consumers affected by their corrections.
- **Pending later-phase work:** none
- **Unresolved questions:** Does a contemporaneous Waking Rune enrollment record establish the pre-enrollment check-project result?
- **Remaining owner review:** Disposition of the historical onboarding prerequisite if contemporaneous evidence cannot be recovered.; Native gameplay acceptance on a separately approved disposable fixture before making runtime-support claims.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: none.

Codex CLI `0.170.0`, thread `01a20b5b-4a0c-7330-9e39-7f6f7b6b11f2`, duration 854.5s, response sha256 `c6a3ee9b36ba516749f6a1fb684fd1421a07b0c587015ca972d62608b13927d8`, request `kwr-codex-umm-0311-baseline-20261123-20261123181024-1057-r03-63075a`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "dbc715ced16b00aa6ff01727d06d6108402b94ac",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The controller gate passed 88/88 checks at the submitted commit. Three untested paths still affect acceptance. A redirected build can certify an older DLL, a locked receipt can cause replacement to delete the previous validated ZIP, and Close can make a failed reinstall launchable under the previous package identity.\n\n## Continuation for Claude\n\nBind packaging to the DLL produced by the measured MSBuild invocation. Carry material inputs through both the compiler fingerprint and the producing-commit comparison. Protect the previous ZIP and receipt throughout backup, placement and rollback. Keep a failed reinstall non-launchable until recovery, and reserve frozen-candidate status for a receipt that passed current-provenance validation.\n\nExercise these paths with real files, redirected output, a locked sidecar and a temporary transaction journal. Correct the offline pin checks, report-file handling, alternate-version fixture and exported receipt pairing identified in the findings. Run the revised README audit procedure, correct the PR\u2019s receipt-only validation claim, and rerun the configured gate at the next candidate. Preserve the historical onboarding gap if a contemporaneous record cannot be recovered.\n\nKeep the corrections within Waking Rune\u2019s existing build, validation, staging and documentation workflows. Preserve the enrollment, historical artifacts and owner installation. The four native rows remain NOT RUN under the documented fixture limitation.",
  "evidence_assessment": {
    "executed_verification": [
      "I verified HEAD, the mission SHA-256, all 16 indexed evidence blobs and the controller gate-log SHA-256. The feature diff passed git diff --check.",
      "The controller ran scripts/Test-LoaderBaseline.ps1 at dbc715ced16b00aa6ff01727d06d6108402b94ac. The receipt records exit 0 in 196.3 seconds, and its bound log contains 88 passing cases.",
      "The gate built the unmodified shipping version 0.12.3 from an empty reference cache and ran standalone current-provenance validation. It recorded ZIP ab78697e9d0724adeb45f4970396b28433ef875ac830922a1fc6f6dfd4a7ff0b and DLL 7cd3cb09f12f75a7247c1b401673528a0773a139e59dc3a940da860c28f24904. The separate 0.12.99 build reported 290 tests passed.",
      "The gate observed the three pinned direct compiler references, rejection of corrupt cache bytes, dependency-bearing ZIPs and the genuine older Waking Rune DLL under a lowered manifest. It also exercised an existing hard link, dirty-receipt validation and a placement failure after the new ZIP had moved.",
      "A read-only PowerShell comparison confirmed that the pin digest predicate accepts 64 hex characters followed by a line feed and that this value differs from the canonical digest."
    ],
    "inspected": [
      "The mission, developer report, transcript notice, prior ledger, applicable repository instructions, all 5,696 feature-diff lines and the correction delta from c328c65.",
      "The production project, MSBuild targets, pin resolver, compiler inventory, packaging and standalone validation scripts, staging helper, transaction phase consumers and boundary-suite assertions.",
      "The frozen build and package records from 3c9f79e28acf473c919d21ab4e46deb323ff3902. Their ZIP digest is 6a49a3fd4aa1800b61565bbb5f2647046449feca733f2830cc51e4fafa617109. The recorded DLL matches the gate build, the API audit and the profile inventory, including MVID ccc59117-f992-44ce-9355-540d35f181f0. Only documentation and evidence changed between that producing commit and H.",
      "The audit source and four-profile result, measured loader inventories, newest-stable selection, onboarding records and dated draft PR #12 readback."
    ],
    "limitations": [
      "The whole developer transcript was withheld. The candidate-bound gate receipt and log establish the recorded execution. The PR and static-audit records were inspected locally.",
      "The read-only review performed no builds, installations, network access or game runs. Local PowerShell execution policy also prevented an independent invocation of the read-only DefinitionOnly script.",
      "Native behavior remains NOT RUN for UMM 0.31.1, 0.32.4, owner-installed 0.33.0 and 0.33.4. The repository reports no approved disposable game copy and no combined-stack run."
    ],
    "required_gaps": [
      "Prove that the receipt binds the actual production output, all material inputs and the recorded source tree, including embedded assets.",
      "Demonstrate recovery when backup of the previous pair fails and when a reinstall fails after destination mutation. Exercise the actual temporary journal transitions.",
      "Close the pin-definition, report-destination and frozen-candidate classification gaps. Keep fixture versions, commits and receipts paired in the gate output.",
      "Supply the clean-checkout audit run and accurate PR wording. The required pre-enrollment check-project timing remains unconfirmed."
    ],
    "source_inspection_only": [
      "The redirected-output, locked-sidecar, response-file dependency, receiptless install-mode and failed-reinstall Close paths were traced in source. The gate did not execute those failure cases.",
      "The gate\u2019s reinstall-order case searches script text. Its diagnostic-mode case validates a private copy. Neither case exercises a failed reinstall against a temporary transaction journal.",
      "The committed static audit reports resolution of 14 types and 39 members on four profiles for the matching DLL. The revised README command sequence has no corresponding execution record."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "mission \u00a75",
        "mission \u00a76"
      ],
      "actual": "Rebuild compiles the product, but line 62 still copies bin/Release/KingmakerWakingRune.dll. The inventory comparison records the output filename without its directory and never compares the copied DLL to the newly produced TargetPath. An older DLL with the expected art and references can receive the new receipt.",
      "blocking_rationale": "The wrapper can qualify different product bytes from those compiled for the recorded inputs.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The ZIP contains the DLL produced by this invocation, or packaging refuses before issuing a receipt.",
      "id": "R2-F1",
      "location": {
        "file": "scripts/Build.ps1",
        "line": 62
      },
      "priority": "P1",
      "requested_correction": "Package from an invocation-owned output or capture the evaluated production TargetPath and verify its digest through staging.",
      "requested_evidence": "Leave a valid older DLL at the conventional path, redirect the product output, and make a source change that alters the compiled bytes. Record the produced and packaged digests or the explicit refusal.",
      "title": "A fresh compile can still package the previous DLL",
      "trigger": "Redirect the production output in LocalGamePaths.props while an older baseline DLL remains in bin/Release."
    },
    {
      "acceptance_ids": [
        "mission \u00a75",
        "mission \u00a76"
      ],
      "actual": "The backup loop moves the old ZIP into pending/previous before the rollback try block. If moving the old receipt then fails, the outer finally deletes pending recursively, including the only saved copy of the old ZIP.",
      "blocking_rationale": "An authorized replacement can permanently destroy the previously validated artifact.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The previous ZIP and receipt survive a failure at either backup or placement step.",
      "id": "R2-F2",
      "location": {
        "file": "scripts/Build.ps1",
        "line": 141
      },
      "priority": "P1",
      "requested_correction": "Include backup moves in the rollback scope. Track completed moves and retain recoverable backups if restoration itself fails.",
      "requested_evidence": "Hold the existing receipt open without delete sharing in a temporary checkout. Verify both original hashes after the failed replacement, validate the pair, release the lock and demonstrate a successful retry.",
      "title": "A locked sidecar still causes loss of the previous ZIP",
      "trigger": "Run an authorized replacement while the existing receipt permits reading but prevents renaming."
    },
    {
      "acceptance_ids": [
        "mission \u00a75",
        "mission \u00a76"
      ],
      "actual": "The Git comparison omits assets/, although the real receipt lists 19 embedded images from that directory. Changing only receipt.commit to a commit with different artwork can therefore pass. Separately, KwrBuild.Common.ps1 line 52 hashes an @response file and stops there. Changing an external resource named inside an unchanged response file leaves that inventory unchanged.",
      "blocking_rationale": "Current-provenance validation can attribute a DLL to the wrong source tree or retain qualification after a material compiler input changes.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Qualification checks tracked material inputs against the producing commit and detects changes to files consumed through compiler arguments.",
      "id": "R1-F1",
      "location": {
        "file": "scripts/Validate-Package.ps1",
        "line": 234
      },
      "priority": "P1",
      "requested_correction": "Include tracked compiler inputs such as embedded resources in the commit comparison. Account for files referenced through response files, or refuse current qualification when their dependencies cannot be identified.",
      "requested_evidence": "Reject a receipt naming a commit that differs only in a packaged image. Show an unchanged response file losing qualification after its resource changes, a rebuilt pair qualifying, and a documentation-only commit retaining qualification.",
      "title": "Current provenance omits material source and compiler inputs",
      "trigger": "Change the claimed commit across an embedded-art change, or change a resource named inside a compiler response file."
    },
    {
      "acceptance_ids": [
        "mission \u00a76",
        "mission \u00a77"
      ],
      "actual": "Reinstall saves the reinstalling phase before deleting the old mod. If placement fails, Invoke-Close accepts that journal with no owned process and unconditionally saves closed. Invoke-Launch accepts closed, while the journal still names the previous package.",
      "blocking_rationale": "A failed installation can be launched and evaluated under a stale package identity.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Only recovery or successful verified installation can make an incomplete reinstall launchable.",
      "id": "R2-F5",
      "location": {
        "file": "scripts/KwrRuntime.ps1",
        "line": 524
      },
      "priority": "P2",
      "requested_correction": "Preserve the recovery phase across Close and other ordinary phase transitions. Keep End available to restore the transaction.",
      "requested_evidence": "Induce a post-mutation placement or digest failure using temporary directories and a real temporary journal. Exercise Close and launch admission, then demonstrate recovery. Keep the live game outside the test.",
      "title": "Close makes an interrupted reinstall launchable",
      "trigger": "Call Close after Reinstall has removed the previous mod and failed to finish installing its replacement."
    },
    {
      "acceptance_ids": [
        "mission \u00a75",
        "mission \u00a76",
        "mission \u00a77"
      ],
      "actual": "Without an adjacent receipt, line 26 performs content-only validation and returns source provenance not-checked. Line 36 nevertheless assigns frozen-candidate whenever Diagnostic is false. Begin and Reinstall persist that label.",
      "blocking_rationale": "A transaction can identify an unqualified external or dirty ZIP as a frozen candidate.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The frozen-candidate label requires verified-current provenance. A supported external ZIP receives an accurate content-only classification.",
      "id": "R3-F1",
      "location": {
        "file": "scripts/KwrPackage.ps1",
        "line": 36
      },
      "priority": "P2",
      "requested_correction": "Derive the recorded qualification from the validator result. Preserve external content inspection and explicit diagnostic staging with distinct, accurate labels.",
      "requested_evidence": "Exercise a receiptless ZIP, a receipt-backed diagnostic ZIP and a clean current pair through the temporary staging path. Show that only the last records frozen-candidate.",
      "title": "Receiptless packages are recorded as frozen candidates",
      "trigger": "Pass a valid ZIP without its sidecar to the default staging helper."
    },
    {
      "acceptance_ids": [
        "mission \u00a75",
        "mission \u00a76"
      ],
      "actual": "The digest expression ends in $, which accepts a final line feed in PowerShell. DefinitionOnly accepts that malformed digest while the byte comparison rejects it. A NuGet pin with an archive digest and a null ArchiveEntry also skips the archive check and passes definition validation, although cold acquisition treats the whole package as the DLL.",
      "blocking_rationale": "Standalone content validation still accepts pin definitions that cannot produce the declared compiler reference.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The offline path requires an exact digest and a consistent archive-entry and archive-digest pair.",
      "id": "R1-F2",
      "location": {
        "file": "scripts/Get-PinnedLoaderReferences.ps1",
        "line": 40
      },
      "priority": "P2",
      "requested_correction": "Validate exact string length and characters through the absolute end of each digest. Validate archive fields together before the offline return.",
      "requested_evidence": "With the cache absent, run the standalone validator against a genuine definition, a malformed digest including a trailing line feed, a missing required archive digest and an archive digest with no entry.",
      "title": "Offline validation still accepts malformed pin definitions",
      "trigger": "Inspect a receiptless ZIP using a pin with a trailing line feed in its digest or an incomplete archive definition."
    },
    {
      "acceptance_ids": [
        "mission \u00a75",
        "mission \u00a76"
      ],
      "actual": "The Win32 structure uses three aligned C# long fields for native FILETIME pairs. On 64-bit Windows this places Volume at offset 32 instead of 28 and misreads the file index. Also, the handles used for comparison are closed before Set-Content reopens ReportPath at line 261. A destination changed to an input alias during validation can then overwrite that input.",
      "blocking_rationale": "The result can reject a distinct existing report or write through a different file identity from the one checked.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Report publication compares the actual volume and full file index and preserves protected inputs through the write.",
      "id": "R3-F2",
      "location": {
        "file": "scripts/Validate-Package.ps1",
        "line": 47
      },
      "priority": "P2",
      "requested_correction": "Use the native field layout and bind the write to the checked destination identity before truncating an existing file, or use an equivalent safe publication operation.",
      "requested_evidence": "Compare the reported identity with a trusted Windows file-ID read. Exercise distinct existing files, a hard link and a destination changed during validation in a temporary directory. Verify the ZIP and receipt hashes survive.",
      "title": "The report writer can act on a different file identity from the one checked",
      "trigger": "Publish to an existing report on another volume or change the report destination while package validation is in progress."
    },
    {
      "acceptance_ids": [
        "mission \u00a76",
        "mission \u00a78"
      ],
      "actual": "Line 520 replaces the initial fixture receipt with a later rebuild receipt. The committed report names test_clone_commit c123b343 and gives ZIP 75ba2942 for that case, while clone_build_receipt names commit e74e13c0 and ZIP c55c1c1e.",
      "blocking_rationale": "The structured evidence pairs a fixture commit with the receipt for a different build.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The named clone commit, ZIP digest and corresponding build receipt identify one build.",
      "id": "R3-F3",
      "location": {
        "file": "scripts/Test-LoaderBaseline.ps1",
        "line": 520
      },
      "priority": "P2",
      "requested_correction": "Preserve the original alternate-version receipt for the summary. Give any later rebuild receipt its own explicitly identified record.",
      "requested_evidence": "Regenerate the suite report and verify that clone_build_receipt.commit and its ZIP digest match test_clone_commit and the corresponding positive case.",
      "title": "The boundary report attaches the wrong receipt to its clone build",
      "trigger": "Read the structured suite report after the helper-change and compiler-option rebuilds."
    },
    {
      "acceptance_ids": [
        "mission \u00a76"
      ],
      "actual": "The first build now occupies the shipping ZIP path. The suite then unconditionally sets Version to 0.12.99. If that is already the shipping version, the fixture has no version change and its second clean build selects the occupied path.",
      "blocking_rationale": "The configured gate rejects a valid product version because its own fixture collides with the shipping build.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The alternate-version fixture differs from the submitted version and preserves the shipping artifact.",
      "id": "R3-F4",
      "location": {
        "file": "scripts/Test-LoaderBaseline.ps1",
        "line": 170
      },
      "priority": "P2",
      "requested_correction": "Choose a valid fixture version distinct from the shipping version and use it consistently in assertions and artifact names.",
      "requested_evidence": "Exercise the fixture selection with shipping Version 0.12.99 and show two distinct validated packages without replacing the shipping pair.",
      "title": "Shipping version 0.12.99 collides with the gate\u2019s alternate-version fixture",
      "trigger": "Run the gate on a clean commit whose valid shipping Version is 0.12.99."
    },
    {
      "acceptance_ids": [
        "mission \u00a78"
      ],
      "actual": "The draft body says supplying a receipt checks the producing source and inputs. ReceiptPath alone returns receipt-matches-package. The commit and current-input checks run only with RequireCurrentProvenance.",
      "blocking_rationale": "The owner-facing description overstates what a successful pre-install command establishes.",
      "blocks_readiness": true,
      "category": "required_gap",
      "expected": "The draft describes the flag required for current-source qualification.",
      "id": "R3-F5",
      "location": {
        "file": "docs/evidence/umm-0311/pr-12-description.json",
        "line": 7
      },
      "priority": "P2",
      "requested_correction": "Clarify the two receipt modes in the draft description through the permitted PR workflow and refresh the readback.",
      "requested_evidence": "A dated PR body that distinguishes receipt byte matching from verification with -RequireCurrentProvenance.",
      "title": "The revised PR description overstates receipt-only validation",
      "trigger": "Follow the draft\u2019s description of Validate-Package.ps1 with a receipt."
    },
    {
      "acceptance_ids": [
        "mission \u00a78"
      ],
      "actual": "The README now supplies the project setup, output path and measured owner row. The retained four-row audit predates the revised instructions and lists the profiles in a different order. The cited link check verifies Markdown links only.",
      "blocking_rationale": "The requested clean-checkout reproduction of the corrected procedure remains unverified.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "A recorded run builds the audit tool from the two committed files and uses the documented invocation on the validated product.",
      "id": "R1-F8",
      "location": {
        "file": "README.md",
        "line": 61
      },
      "priority": "P2",
      "requested_correction": "Run the revised procedure from a clean checkout with the documented prerequisites and retain its command output.",
      "requested_evidence": "Show the audit project build, invocation from its documented output path, four profile results and a mod digest matching the standalone package report.",
      "title": "The corrected audit instructions lack their requested execution record",
      "trigger": "Verify that the revised README procedure works without a preinstalled audit executable."
    },
    {
      "acceptance_ids": [
        "mission \u00a74"
      ],
      "actual": "The new policy export describes the effective push protection and explicitly says the historical check-project result cannot be confirmed. Preflight is dated 2026-11-23T23:09:42Z. The retained successful check-project result is dated 2026-11-24T01:01:16Z, after enrollment.",
      "blocking_rationale": "The unamended mission requires both onboarding checks before enrollment. The later success establishes the later profile state only.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "A contemporaneous record establishes successful project validation before Waking Rune enrollment.",
      "id": "R1-F9",
      "location": {
        "file": "docs/evidence/umm-0311/onboarding-push-policy.json",
        "line": 43
      },
      "priority": "P2",
      "requested_correction": "Recover a dated, read-only controller or enrollment record if one exists. If the record is irretrievable, preserve the gap for explicit owner disposition.",
      "requested_evidence": "The Waking Rune pre-enrollment check-project result or a contemporaneous enrollment record showing that validation and its outcome.",
      "title": "The pre-enrollment project check remains unconfirmed",
      "trigger": "Verify the required onboarding sequence using the supplied historical records."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F1",
      "rationale": "The gate now rejects unknown commits, wrong receipt versions, BaseAddress drift and changed direct Win32Resource bytes. The producing-commit comparison still omits embedded artwork, and response-file dependencies escape the current-input check."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F2",
      "rationale": "Field validation moved before DefinitionOnly. A digest with a final line feed and a half-configured archive still demonstrate the reported disagreement between offline acceptance and acquisition."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F4",
      "rationale": "The gate rejected the conventional adjacent receipt without ReceiptPath and an existing hard link to the package. Both original hashes survived. R3-F2 covers the new identity implementation and the interval before report publication."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F8",
      "rationale": "The documentation changes address the missing setup and executable path. The earlier matching-DLL audit and the link check do not establish the requested execution of the revised clean-checkout commands."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F9",
      "rationale": "The effective path policy is now described. The export expressly identifies the missing pre-enrollment check-project record, and the supplied successful check occurred later."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R2-F1",
      "rationale": "Rebuild forces compilation. Packaging still reads the conventional bin/Release file independently of the production target, and the gate lacks the redirected-output regression."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R2-F2",
      "rationale": "The injected failure after new ZIP placement restored both hashes. Failure while moving the old receipt occurs before that rollback handler and can delete the previous ZIP."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R2-F3",
      "rationale": "The controller log identifies a cold build and standalone current-provenance check of unmodified H at shipping version 0.12.3 before the separate 0.12.99 case. R3-F4 covers a collision introduced by the two-build sequence."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R2-F4",
      "rationale": "The real helper accepted a receipt-backed dirty build in diagnostic mode and rejected it in frozen mode. Both paths use the same byte checks and staging helper. R3-F1 covers the newly assigned label when the receipt is absent."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R2-F5",
      "rationale": "Reinstall stages first and records reinstalling before mutation. Close can still promote that journal to closed after failure. The passing source-text assertion does not exercise this transition."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R2-F6",
      "rationale": "The dated draft readback now identifies the Windows build, 290 tests, package validation, boundary gate, four static rows and outstanding native checks. R3-F5 addresses a separate claim introduced in the revised body."
    }
  ],
  "request_id": "kwr-codex-umm-0311-baseline-20261123-20261123181024-1057-r03-63075a",
  "round": 3,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "The shipping manifest declares UMM 0.31.1. The normal Windows build and packaged DLL use UnityModManager 0.31.1.0, 0Harmony12 1.2.0.1 and 0Harmony 2.3.1.1. The gate verifies the shipping package, and the static records cover four loader profiles on the same DLL. Artifact provenance, failure recovery and the evidence gaps above prevent final readiness.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "Disposition of the historical onboarding prerequisite if contemporaneous evidence cannot be recovered.",
      "Native gameplay acceptance on a separately approved disposable fixture before making runtime-support claims."
    ],
    "scope_reviewed": "Final checkpoint for KingmakerWakingRune draft PR #12 at dbc715ced16b00aa6ff01727d06d6108402b94ac, including all eleven pending findings and the consumers affected by their corrections.",
    "unresolved_questions": [
      "Does a contemporaneous Waking Rune enrollment record establish the pre-enrollment check-project result?"
    ]
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
