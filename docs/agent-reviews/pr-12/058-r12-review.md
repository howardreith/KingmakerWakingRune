<!-- agent-review:v1 enrollment=kwr-codex-umm-0311-baseline-20261123-20261123181024-1057 key=r12-review part=1/1 sha256=f45016e8b00005cadce89281f2dd631c9f85a1eeb8e99cc0f360807dd03dfc0b -->
### Codex GPT-6.2 Astra — review — round 12 — candidate `15590edb52`

**Decision:** `changes_required` — The pinned build and retained package are evidenced. Four defects still block readiness. Two report-write paths can overwrite a build receipt, an external package can lose its receipt check during a move and restoration, and a change in compiler reference alias can leave an old package marked verified-current.

Continuation for Claude

Round 12 reaches the review cap in OWNER AMENDMENT 7. Preserve draft PR #12, the retained producer and the review evidence at this stop. Further correction and review require owner direction.

If authorized, address the two report findings together. Capture the initial destination identity in one open operation and decline a linked-input report when the protected receipt names cannot be tied to the ZIP being read. Preserve delete sharing during publisher rollback. Bind external receipt selection to the selected package, and include the parsed reference binding in both compiler-inventory equality and the build fingerprint. The locked-report diagnostic is optional.

Use the existing disposable fixtures to establish each reported condition, verify receipt preservation or correct refusal, and retain the stable-copy controls. Any corrected handoff needs a retained production pair, post-cleanup validation and the configured candidate gate. The existing static audit remains reusable on a full DLL hash and MVID match. The native-testing exclusions and file-symbolic-link evidence exception remain in force.

#### Findings

**R10-F1** · P1 · defect · blocks readiness — A receipt can still become the permitted identity of an existing report
- Location: `scripts/Validate-Package.ps1:45`; acceptance: OWNER AMENDMENT 7, R10-F1, OWNER AMENDMENT 6, protection of the actual ZIP and receipt
- Trigger: A hard-linked ZIP is validated without ReceiptPath while its target receipt replaces an existing ReportPath during the initial lookup.
- Expected: The first lookup establishes the destination identity or absence. A receipt moved there afterward survives the report write.
- Actual: Line 45 checks File.Exists and then opens the path again with Of. When an existing report is replaced by the target receipt between those calls, reportStartId becomes the receipt's ID. Its conventional name is already vacant when ReadAllBound looks for it. WriteChecked consequently accepts that receipt as the original report and truncates it.
- Why it blocks / does not: A supported content-only validation can destroy the receipt of the ZIP it inspected.
- Requested correction: Attempt the identity-bearing open directly and treat a missing-file result as absence. Carry that result through WriteChecked and refuse a linked-input report when the target receipt cannot be protected.
- Requested evidence: Extend the moved-receipt fixture to start with a distinct existing report. Cover replacement during initial destination lookup and record refusal plus unchanged ZIP and receipt hashes. Retain the successful ordinary existing-report control.

**R12-F1** · P1 · defect · blocks readiness — Hard-link enumeration can protect a replacement ZIP's receipts
- Location: `scripts/KwrFileIdentity.ps1:145`; acceptance: OWNER AMENDMENT 7, R9-F2 rollback correction and preservation of R9-F3, OWNER AMENDMENT 6, protection of the actual ZIP and receipt
- Trigger: The opened name of a hard-linked ZIP changes during ReadAllBound while ReportPath selects the receipt beside another name of the original ZIP.
- Expected: The protected receipt names belong to the ZIP handle that supplies the snapshot bytes, or the validator declines the report.
- Actual: ReadAllBound gets the ZIP identity and link count from its handle, then calls LinkNamesOf(finalPath) by pathname. The new delete sharing allows that name to be replaced in between. A replacement with the same link count passes the count check. Its names omit the original ZIP's other name and conventional receipt, so WriteChecked can overwrite that receipt even when the receipt itself never moved.
- Why it blocks / does not: The rollback correction can separate the inspected ZIP bytes from the receipt identities protected against report writes.
- Requested correction: Bind link-name discovery to the held file identity or decline the report when that correspondence is uncertain. Preserve the delete sharing that allows rollback.
- Requested evidence: Extend the existing two-ZIP hard-link fixture to change the opened name after physical-path capture and before enumeration. Use equal link counts, select the original ZIP's receipt as ReportPath, and verify refusal and both original hashes.

**R11-F2** · P2 · defect · blocks readiness — A restored external pair can be admitted without its receipt
- Location: `scripts/KwrPackage.ps1:115`; acceptance: OWNER AMENDMENT 7, R9-F2 direct regressions, OWNER AMENDMENT 5, independent external copies
- Trigger: An independent pair moves away after the first ZIP identity check and returns to the same paths before the second check.
- Expected: Staging checks the receipt belonging to the selected external pair or refuses an ambiguous read.
- Actual: The two stillInPlace calls compare only the ZIP identity. If the pair is away when File.Exists probes the receipt and both original files are restored before the second comparison, both identity checks pass and hasReceipt stays false. Line 128 then performs content-only validation despite the restored adjacent receipt.
- Why it blocks / does not: Default staging can admit a receipt-backed package without enforcing a dirty or mismatching receipt.
- Requested correction: Bind receipt selection to the external ZIP snapshot. A transiently missing sidecar must not downgrade a selected pair to receiptless admission. Preserve delete sharing before ownership is confirmed.
- Requested evidence: Use the existing external-pair fixture with a receipt that default staging would reject. Cover temporary removal and restoration across the receipt probe, and retain the stable receiptless-copy control.

**R12-F2** · P2 · defect · blocks readiness — The fingerprint discards the distinction between an alias and a literal path
- Location: `scripts/KwrBuild.Common.ps1:209`; acceptance: Mission section 5, material build inputs invalidate frozen reuse, OWNER AMENDMENT 7, R9-F4 compiler reference handling
- Trigger: LocalGamePaths.props switches from an unaliased Rules=Local/Accessibility.dll to alias Rules for a byte-identical Local/Accessibility.dll.
- Expected: Compiler-inventory equality and the build fingerprint distinguish a literal reference path from an actual alias.
- Actual: Both selections produce the display name Rules=Local/Accessibility.dll and the same digest. The new referencePath distinguishes them, but the inventory comparison at line 209 and fingerprint at line 248 serialize only kind, name and sha256. With both paths already tracked at the producing commit, the Git comparison also passes. The old package can receive verified-current after Csc's alias binding changes.
- Why it blocks / does not: A material compiler configuration change can leave a frozen binary qualified. The alias changes whether source code can access the reference through the global namespace.
- Requested correction: Include the parsed reference binding in both the compile comparison and the build-input fingerprint. Use the same representation in Build.ps1 and current-provenance validation.
- Requested evidence: In the existing Accessibility fixture, switch the ignored local props between the two bindings with identical DLL bytes. Record the actual compiler arguments, refusal of the stale pair, and qualification after restoring the original selection.

**R11-F4** · P3 · optional_polish · non-blocking — An exclusive lock can still be reported as a replaced destination
- Location: `scripts/KwrFileIdentity.ps1:184`; acceptance: OWNER AMENDMENT 7, checked report write diagnostics
- Trigger: A distinct report becomes exclusively locked after its initial identity was captured and before WriteChecked opens it.
- Expected: A sharing failure remains visible when replacement has not been established.
- Actual: An exclusive lock prevents both the write open and the fallback Of(path) read. The catch leaves now null and reports that the destination changed. The new rethrow works only when the second open can read the identity.
- Why it blocks / does not: The report bytes survive. Only the diagnostic is incorrect.
- Requested correction: Retain the original I/O error when the identity probe is itself blocked. Reserve the replacement diagnostic for an observed change.
- Requested evidence: If addressed, show that an unchanged, exclusively locked report retains its bytes and produces a sharing error.

#### Prior finding assessments

- **R10-F1** — `fix_rejected` — Gate row 112 verifies the initially absent destination at three points. For an existing destination, File.Exists and Of remain separate observations and the moved receipt can still become the permitted output.
- **R11-F1** — `fix_verified` — ReadAllBound and ReadAll share delete access. Gate row 182 held the standalone snapshot open across receipt-placement failure and confirmed restoration of the previous ZIP and receipt at their final paths.
- **R11-F2** — `fix_rejected` — Gate row 175 confirms refusal when the pair remains at its new location. Restoration of the same files across the receipt probe satisfies both ZIP identity checks while leaving the receipt omitted.
- **R11-F3** — `fix_verified` — Both post-cleanup exports are present. The round-12 record identifies the durable d93c56e producer, both file hashes, DLL hash and MVID, zero remaining focused clones, clean producer status, validator exit 0 and verified-current.
- **R11-F4** — `fix_rejected` — The original error is preserved when the second identity read succeeds. An exclusive lock can prevent that read and still produce the incorrect replacement diagnostic. This remains optional.
- **R11-F5** — `fix_verified` — All three round-12 driver modes reach the failed-case exit check after the finally block writes the result. The archived round-11 driver retains its original attribution.
- **R9-F4** — `fix_verified` — Gate rows 197 and 203 show the complete tracked Rules=Local/Accessibility.dll path, genuine-producer qualification and refusal of a producer differing only in that file. R12-F2 concerns the separate comparison of two compiler configurations.

#### Evidence assessment

- **Inspected:** Controller mission, all seven owner amendments, pending findings ledger, developer report, transcript notice, gate receipt and log. The mission and amendment hashes match the request.; The M..H feature diff, README.md, docs/UMM-COMPATIBILITY.md, production project, pin definitions, MSBuild targets, build and package helpers, validator and runtime staging callers.; The round-12 changes against 861f2e6, their gate and focused fixtures, both post-cleanup exports, the retained build receipt and the static-audit identities.
- **Executed verification:** The controller's baseline gate ran on 15590edb523716aebef880203959af735a69660a and exited 0 in 786.8 seconds. The log SHA-256 matches the receipt. I counted 174 PASS rows, zero FAIL rows and one separately recorded NOT RUN.; The gate built shipping version 0.12.3 from an empty reference cache and validated its receipt. It rejected corrupt references, substituted compiler inputs, mismatched receipts and the genuine 0.33.2 release DLL packaged with a lowered manifest.; The exported d93c56e driver output records the retained build passing, four focused checks passing, then post-cleanup validation exiting 0 with verified-current. The retained ZIP SHA-256 is 36a3c26364a011b8de698de124c8ea10c552e58fdd05255b7270d31bd46707de and its receipt SHA-256 is b8d5102bcceb513bfa67fb02bf10120d8db2034dc4cd4d5097717f74ec1af37e.; The retained DLL SHA-256 is 7cd3cb09f12f75a7247c1b401673528a0773a139e59dc3a940da860c28f24904 and its MVID is ccc59117-f992-44ce-9355-540d35f181f0. Both match the archived audit that resolved 14 type references and 39 member references on all four loader profiles.; Read-only Git checks confirmed HEAD, the merge base, and that only documentation and evidence changed between producer d93c56e and the reviewed commit. The feature diff passed git diff --check.
- **Source inspection only:** The four blocking failure paths follow from the candidate's file opens, path lookups and inventory projections. The recorded gate exercised different event orderings and a one-file Git change.; The remaining exclusive-lock diagnostic and the corrected driver exit flow were assessed from source. No deliberate failing driver run was supplied.
- **Required gaps:** Demonstrate that the target receipt survives replacement of an existing report during initial identity capture and replacement of a hard-link name during snapshot acquisition.; Demonstrate that a temporarily moved and restored external pair receives its receipt check or is refused, while a stable receiptless copy still qualifies for content inspection.; Demonstrate that changing between a literal equals-sign reference path and an actual compiler alias invalidates frozen provenance even when the DLL bytes and Git tree stay the same.
- **Limitations:** The reviewer used a read-only snapshot. The controller supplied the candidate gate. The focused and retained results come from committed machine-output exports. The mixed foreground transcript was withheld.; The retained build preceded the focused run so the fixtures could use copies of that pair. The first two driver JSON summaries were overwritten. Their console outputs and the post-cleanup JSON survive.; The real Windows file-symbolic-link fixture remains available and recorded NOT RUN with “Administrator privilege required for this operation.” OWNER AMENDMENT 6 withdrew execution of that fixture as a handoff prerequisite.; Native gameplay and combined-stack checks were NOT RUN. The four-loader results establish static API resolution.

#### Summary

- **Delivered behavior:** Waking Rune declares UMM 0.31.1 and the packaged DLL references UnityModManager 0.31.1.0, Harmony12 1.2.0.1 and Harmony2 2.3.1.1. The retained DLL matches the static audit for 0.31.1, 0.32.4, the measured owner 0.33.0 profile and 0.33.4. The standalone rollback fix and literal tracked-reference comparison are verified.
- **Scope reviewed:** Final correction review of Waking Rune PR #12 under mission revision 7, including all seven pending findings and the callers directly affected by the round-12 changes.
- **Pending later-phase work:** none
- **Unresolved questions:** none
- **Remaining owner review:** Decide whether to authorize a further bounded correction and review of PR #12 after the twelve-review cap.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: revision 7, sha256 `cf3ca150f22d96316318c752858a3382b4c3e1108ae225d5b485f8b4c7406226`.

Codex CLI `0.170.0`, thread `01a21d6c-84b9-7ed2-a3f1-ce02f06d7a2f`, duration 788.4s, response sha256 `3e563f331080a05c80d4aae0040cc49e674bc718cc1eae3d37f007644d6d66ad`, request `kwr-codex-umm-0311-baseline-20261123-20261123181024-1057-r12-460e58`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "15590edb523716aebef880203959af735a69660a",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The pinned build and retained package are evidenced. Four defects still block readiness. Two report-write paths can overwrite a build receipt, an external package can lose its receipt check during a move and restoration, and a change in compiler reference alias can leave an old package marked verified-current.\n\nContinuation for Claude\n\nRound 12 reaches the review cap in OWNER AMENDMENT 7. Preserve draft PR #12, the retained producer and the review evidence at this stop. Further correction and review require owner direction.\n\nIf authorized, address the two report findings together. Capture the initial destination identity in one open operation and decline a linked-input report when the protected receipt names cannot be tied to the ZIP being read. Preserve delete sharing during publisher rollback. Bind external receipt selection to the selected package, and include the parsed reference binding in both compiler-inventory equality and the build fingerprint. The locked-report diagnostic is optional.\n\nUse the existing disposable fixtures to establish each reported condition, verify receipt preservation or correct refusal, and retain the stable-copy controls. Any corrected handoff needs a retained production pair, post-cleanup validation and the configured candidate gate. The existing static audit remains reusable on a full DLL hash and MVID match. The native-testing exclusions and file-symbolic-link evidence exception remain in force.",
  "evidence_assessment": {
    "executed_verification": [
      "The controller's baseline gate ran on 15590edb523716aebef880203959af735a69660a and exited 0 in 786.8 seconds. The log SHA-256 matches the receipt. I counted 174 PASS rows, zero FAIL rows and one separately recorded NOT RUN.",
      "The gate built shipping version 0.12.3 from an empty reference cache and validated its receipt. It rejected corrupt references, substituted compiler inputs, mismatched receipts and the genuine 0.33.2 release DLL packaged with a lowered manifest.",
      "The exported d93c56e driver output records the retained build passing, four focused checks passing, then post-cleanup validation exiting 0 with verified-current. The retained ZIP SHA-256 is 36a3c26364a011b8de698de124c8ea10c552e58fdd05255b7270d31bd46707de and its receipt SHA-256 is b8d5102bcceb513bfa67fb02bf10120d8db2034dc4cd4d5097717f74ec1af37e.",
      "The retained DLL SHA-256 is 7cd3cb09f12f75a7247c1b401673528a0773a139e59dc3a940da860c28f24904 and its MVID is ccc59117-f992-44ce-9355-540d35f181f0. Both match the archived audit that resolved 14 type references and 39 member references on all four loader profiles.",
      "Read-only Git checks confirmed HEAD, the merge base, and that only documentation and evidence changed between producer d93c56e and the reviewed commit. The feature diff passed git diff --check."
    ],
    "inspected": [
      "Controller mission, all seven owner amendments, pending findings ledger, developer report, transcript notice, gate receipt and log. The mission and amendment hashes match the request.",
      "The M..H feature diff, README.md, docs/UMM-COMPATIBILITY.md, production project, pin definitions, MSBuild targets, build and package helpers, validator and runtime staging callers.",
      "The round-12 changes against 861f2e6, their gate and focused fixtures, both post-cleanup exports, the retained build receipt and the static-audit identities."
    ],
    "limitations": [
      "The reviewer used a read-only snapshot. The controller supplied the candidate gate. The focused and retained results come from committed machine-output exports. The mixed foreground transcript was withheld.",
      "The retained build preceded the focused run so the fixtures could use copies of that pair. The first two driver JSON summaries were overwritten. Their console outputs and the post-cleanup JSON survive.",
      "The real Windows file-symbolic-link fixture remains available and recorded NOT RUN with \u201cAdministrator privilege required for this operation.\u201d OWNER AMENDMENT 6 withdrew execution of that fixture as a handoff prerequisite.",
      "Native gameplay and combined-stack checks were NOT RUN. The four-loader results establish static API resolution."
    ],
    "required_gaps": [
      "Demonstrate that the target receipt survives replacement of an existing report during initial identity capture and replacement of a hard-link name during snapshot acquisition.",
      "Demonstrate that a temporarily moved and restored external pair receives its receipt check or is refused, while a stable receiptless copy still qualifies for content inspection.",
      "Demonstrate that changing between a literal equals-sign reference path and an actual compiler alias invalidates frozen provenance even when the DLL bytes and Git tree stay the same."
    ],
    "source_inspection_only": [
      "The four blocking failure paths follow from the candidate's file opens, path lookups and inventory projections. The recorded gate exercised different event orderings and a one-file Git change.",
      "The remaining exclusive-lock diagnostic and the corrected driver exit flow were assessed from source. No deliberate failing driver run was supplied."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "OWNER AMENDMENT 7, R10-F1",
        "OWNER AMENDMENT 6, protection of the actual ZIP and receipt"
      ],
      "actual": "Line 45 checks File.Exists and then opens the path again with Of. When an existing report is replaced by the target receipt between those calls, reportStartId becomes the receipt's ID. Its conventional name is already vacant when ReadAllBound looks for it. WriteChecked consequently accepts that receipt as the original report and truncates it.",
      "blocking_rationale": "A supported content-only validation can destroy the receipt of the ZIP it inspected.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The first lookup establishes the destination identity or absence. A receipt moved there afterward survives the report write.",
      "id": "R10-F1",
      "location": {
        "file": "scripts/Validate-Package.ps1",
        "line": 45
      },
      "priority": "P1",
      "requested_correction": "Attempt the identity-bearing open directly and treat a missing-file result as absence. Carry that result through WriteChecked and refuse a linked-input report when the target receipt cannot be protected.",
      "requested_evidence": "Extend the moved-receipt fixture to start with a distinct existing report. Cover replacement during initial destination lookup and record refusal plus unchanged ZIP and receipt hashes. Retain the successful ordinary existing-report control.",
      "title": "A receipt can still become the permitted identity of an existing report",
      "trigger": "A hard-linked ZIP is validated without ReceiptPath while its target receipt replaces an existing ReportPath during the initial lookup."
    },
    {
      "acceptance_ids": [
        "OWNER AMENDMENT 7, R9-F2 rollback correction and preservation of R9-F3",
        "OWNER AMENDMENT 6, protection of the actual ZIP and receipt"
      ],
      "actual": "ReadAllBound gets the ZIP identity and link count from its handle, then calls LinkNamesOf(finalPath) by pathname. The new delete sharing allows that name to be replaced in between. A replacement with the same link count passes the count check. Its names omit the original ZIP's other name and conventional receipt, so WriteChecked can overwrite that receipt even when the receipt itself never moved.",
      "blocking_rationale": "The rollback correction can separate the inspected ZIP bytes from the receipt identities protected against report writes.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The protected receipt names belong to the ZIP handle that supplies the snapshot bytes, or the validator declines the report.",
      "id": "R12-F1",
      "location": {
        "file": "scripts/KwrFileIdentity.ps1",
        "line": 145
      },
      "priority": "P1",
      "requested_correction": "Bind link-name discovery to the held file identity or decline the report when that correspondence is uncertain. Preserve the delete sharing that allows rollback.",
      "requested_evidence": "Extend the existing two-ZIP hard-link fixture to change the opened name after physical-path capture and before enumeration. Use equal link counts, select the original ZIP's receipt as ReportPath, and verify refusal and both original hashes.",
      "title": "Hard-link enumeration can protect a replacement ZIP's receipts",
      "trigger": "The opened name of a hard-linked ZIP changes during ReadAllBound while ReportPath selects the receipt beside another name of the original ZIP."
    },
    {
      "acceptance_ids": [
        "OWNER AMENDMENT 7, R9-F2 direct regressions",
        "OWNER AMENDMENT 5, independent external copies"
      ],
      "actual": "The two stillInPlace calls compare only the ZIP identity. If the pair is away when File.Exists probes the receipt and both original files are restored before the second comparison, both identity checks pass and hasReceipt stays false. Line 128 then performs content-only validation despite the restored adjacent receipt.",
      "blocking_rationale": "Default staging can admit a receipt-backed package without enforcing a dirty or mismatching receipt.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Staging checks the receipt belonging to the selected external pair or refuses an ambiguous read.",
      "id": "R11-F2",
      "location": {
        "file": "scripts/KwrPackage.ps1",
        "line": 115
      },
      "priority": "P2",
      "requested_correction": "Bind receipt selection to the external ZIP snapshot. A transiently missing sidecar must not downgrade a selected pair to receiptless admission. Preserve delete sharing before ownership is confirmed.",
      "requested_evidence": "Use the existing external-pair fixture with a receipt that default staging would reject. Cover temporary removal and restoration across the receipt probe, and retain the stable receiptless-copy control.",
      "title": "A restored external pair can be admitted without its receipt",
      "trigger": "An independent pair moves away after the first ZIP identity check and returns to the same paths before the second check."
    },
    {
      "acceptance_ids": [
        "Mission section 5, material build inputs invalidate frozen reuse",
        "OWNER AMENDMENT 7, R9-F4 compiler reference handling"
      ],
      "actual": "Both selections produce the display name Rules=Local/Accessibility.dll and the same digest. The new referencePath distinguishes them, but the inventory comparison at line 209 and fingerprint at line 248 serialize only kind, name and sha256. With both paths already tracked at the producing commit, the Git comparison also passes. The old package can receive verified-current after Csc's alias binding changes.",
      "blocking_rationale": "A material compiler configuration change can leave a frozen binary qualified. The alias changes whether source code can access the reference through the global namespace.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Compiler-inventory equality and the build fingerprint distinguish a literal reference path from an actual alias.",
      "id": "R12-F2",
      "location": {
        "file": "scripts/KwrBuild.Common.ps1",
        "line": 209
      },
      "priority": "P2",
      "requested_correction": "Include the parsed reference binding in both the compile comparison and the build-input fingerprint. Use the same representation in Build.ps1 and current-provenance validation.",
      "requested_evidence": "In the existing Accessibility fixture, switch the ignored local props between the two bindings with identical DLL bytes. Record the actual compiler arguments, refusal of the stale pair, and qualification after restoring the original selection.",
      "title": "The fingerprint discards the distinction between an alias and a literal path",
      "trigger": "LocalGamePaths.props switches from an unaliased Rules=Local/Accessibility.dll to alias Rules for a byte-identical Local/Accessibility.dll."
    },
    {
      "acceptance_ids": [
        "OWNER AMENDMENT 7, checked report write diagnostics"
      ],
      "actual": "An exclusive lock prevents both the write open and the fallback Of(path) read. The catch leaves now null and reports that the destination changed. The new rethrow works only when the second open can read the identity.",
      "blocking_rationale": "The report bytes survive. Only the diagnostic is incorrect.",
      "blocks_readiness": false,
      "category": "optional_polish",
      "expected": "A sharing failure remains visible when replacement has not been established.",
      "id": "R11-F4",
      "location": {
        "file": "scripts/KwrFileIdentity.ps1",
        "line": 184
      },
      "priority": "P3",
      "requested_correction": "Retain the original I/O error when the identity probe is itself blocked. Reserve the replacement diagnostic for an observed change.",
      "requested_evidence": "If addressed, show that an unchanged, exclusively locked report retains its bytes and produces a sharing error.",
      "title": "An exclusive lock can still be reported as a replaced destination",
      "trigger": "A distinct report becomes exclusively locked after its initial identity was captured and before WriteChecked opens it."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_rejected",
      "finding_id": "R10-F1",
      "rationale": "Gate row 112 verifies the initially absent destination at three points. For an existing destination, File.Exists and Of remain separate observations and the moved receipt can still become the permitted output."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R11-F1",
      "rationale": "ReadAllBound and ReadAll share delete access. Gate row 182 held the standalone snapshot open across receipt-placement failure and confirmed restoration of the previous ZIP and receipt at their final paths."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R11-F2",
      "rationale": "Gate row 175 confirms refusal when the pair remains at its new location. Restoration of the same files across the receipt probe satisfies both ZIP identity checks while leaving the receipt omitted."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R11-F3",
      "rationale": "Both post-cleanup exports are present. The round-12 record identifies the durable d93c56e producer, both file hashes, DLL hash and MVID, zero remaining focused clones, clean producer status, validator exit 0 and verified-current."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R11-F4",
      "rationale": "The original error is preserved when the second identity read succeeds. An exclusive lock can prevent that read and still produce the incorrect replacement diagnostic. This remains optional."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R11-F5",
      "rationale": "All three round-12 driver modes reach the failed-case exit check after the finally block writes the result. The archived round-11 driver retains its original attribution."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R9-F4",
      "rationale": "Gate rows 197 and 203 show the complete tracked Rules=Local/Accessibility.dll path, genuine-producer qualification and refusal of a producer differing only in that file. R12-F2 concerns the separate comparison of two compiler configurations."
    }
  ],
  "request_id": "kwr-codex-umm-0311-baseline-20261123-20261123181024-1057-r12-460e58",
  "round": 12,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "Waking Rune declares UMM 0.31.1 and the packaged DLL references UnityModManager 0.31.1.0, Harmony12 1.2.0.1 and Harmony2 2.3.1.1. The retained DLL matches the static audit for 0.31.1, 0.32.4, the measured owner 0.33.0 profile and 0.33.4. The standalone rollback fix and literal tracked-reference comparison are verified.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "Decide whether to authorize a further bounded correction and review of PR #12 after the twelve-review cap."
    ],
    "scope_reviewed": "Final correction review of Waking Rune PR #12 under mission revision 7, including all seven pending findings and the callers directly affected by the round-12 changes.",
    "unresolved_questions": []
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
