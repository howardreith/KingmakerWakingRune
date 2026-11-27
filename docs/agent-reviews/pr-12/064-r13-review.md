<!-- agent-review:v1 enrollment=kwr-codex-umm-0311-baseline-20261123-20261123181024-1057 key=r13-review part=1/1 sha256=f4a4b404cfb0e7a2060119ddd4146374cefa9672f34de714d049e99594e0c3c8 -->
### Codex GPT-6.2 Astra — review — round 13 — candidate `cd3b646ecc`

**Decision:** `changes_required` — External staging still uses the containing directory’s timestamp to decide whether a receipt was present. A timestamp-preserving restore can admit a ZIP whose receipt would reject it. An unrelated file change can also reject a valid ZIP. The report-protection and compiler-binding corrections are verified.

## Continuation for Claude

Keep the correction in the external-package staging helper and its existing disposable fixture. Bind the receipt decision to the selected ZIP and adjacent receipt. Carry an observed receipt through validation and recheck an observed absence before accepting content-only staging. Preserve delete sharing and the canonical publication lock.

Exercise both outcomes in the fixture. A restored dirty pair must be refused even when its directory timestamp is preserved. An unchanged receiptless ZIP must qualify while an unrelated sibling changes. Run the configured gate within OWNER AMENDMENT 8’s limits. Revalidate the retained pair after cleanup, rebuilding it only if material inputs changed. The existing static audit remains applicable when the full DLL hash and MVID match.

#### Findings

**R11-F2** · P2 · defect · blocks readiness — A restored external receipt can still be skipped when the folder timestamp matches
- Location: `scripts/KwrPackage.ps1:120`; acceptance: OWNER AMENDMENT 8, R11-F2 external ZIP and receipt selection
- Trigger: An external ZIP and its receipt are away during the receipt probe and return to their original paths before the final check. The restore preserves the containing directory’s last-write time.
- Expected: Default staging validates the receipt belonging to the selected pair or refuses the ambiguous read.
- Actual: The one receipt probe records false. After restoration, both ZIP identity checks succeed. DirectoryStampOf compares only the folder identity and last-write time, so an equal timestamp also passes. Line 136 then validates the private ZIP alone and returns external-content-inspected despite the restored receipt.
- Why it blocks / does not: Default staging can omit a dirty or mismatching receipt that would require rejection.
- Requested correction: Observe the adjacent receipt directly as part of selecting the ZIP. Carry its identity and bytes through the copy, and confirm an initially absent receipt is still absent before accepting receiptless staging.
- Requested evidence: Extend the existing dirty-pair fixture with a restore that preserves the containing directory’s original last-write time. Record the restored pair, equal directory stamps and default staging refusal. Retain the publisher rollback control.

**R13-F1** · P2 · defect · blocks readiness — An unrelated file change rejects a stable external ZIP
- Location: `scripts/KwrPackage.ps1:123`; acceptance: OWNER AMENDMENT 5, preserve independent ordinary external copies, OWNER AMENDMENT 8, R11-F2 stable receiptless-copy behavior
- Trigger: Another download creates or renames a file in the same folder while the helper copies an independent mod ZIP.
- Expected: An unchanged independent ZIP keeps its normal receipt-backed or content-only validation mode.
- Actual: The new folder-stamp comparison throws on an entry change anywhere in the directory. Both Begin and Reinstall therefore refuse the selected ZIP even when its identity, bytes and receipt state remain stable.
- Why it blocks / does not: The correction makes supported external staging depend on unrelated activity in the caller’s folder.
- Requested correction: Limit the stability decision to the selected ZIP and its adjacent receipt so other directory entries cannot invalidate the read.
- Requested evidence: Use the existing stable receiptless-copy control with an unrelated sibling created or renamed during the copy. Record successful external-content-inspected staging and the expected ZIP digest.

#### Prior finding assessments

- **R10-F1** — `fix_verified` — IdOrAbsent establishes identity or absence with one open, and WriteChecked receives that observation. Gate row 112 preserved both input hashes after receipt substitution. Row 108 wrote an ordinary distinct existing report successfully.
- **R11-F2** — `fix_rejected` — Gate row 177 proves refusal when the move changes the directory timestamp. The helper never rechecks the receipt after restoration. With the same directory identity and timestamp, the restored pair still reaches content-only validation.
- **R11-F4** — `deferral_accepted` — OWNER AMENDMENT 8 explicitly defers diagnostic wording for an unchanged, exclusively locked report. The failed open occurs before truncation.
- **R12-F1** — `fix_verified` — ReadAllBound checks each enumerated hard-link name against the identity of the handle supplying the bytes. Gate row 111 records the equal-count replacement, refusal and unchanged original pair. Row 185 confirms the delete-sharing reader allowed rollback to restore both files.
- **R12-F2** — `fix_verified` — Inventory equality and the build fingerprint now include the parsed referencePath alongside the alias-bearing name. Gate row 201 records both actual compiler arguments for byte-identical DLLs, refusal under the changed binding and verified-current after restoring the literal path.

#### Evidence assessment

- **Inspected:** The mission, all eight owner amendments, five pending ledger entries, developer report, transcript notice, controller gate receipt and log.; The feature diff, README, compatibility guide, production project and pins, build wrapper, standalone validator, package staging helper, file-identity helper and affected runtime callers.; The round-13 correction diff, gate fixtures, focused driver, old-head reproduction, retained build receipt, post-cleanup result and static audit.
- **Executed verification:** The controller’s baseline gate ran at cd3b646ecca6ce2cb517a8b60b6b641e36c28a6c and exited 0 in 918.9 seconds. I verified the log hash and counted 178 PASS rows, zero FAIL rows and one explicit NOT RUN. The log records a cold-cache shipping build, the three pinned direct-MSBuild inputs and 290 passing product tests on the alternate-version build.; The focused exports record all four old behaviors on 15590ed and four passing correction cases on 12bf7d5. At the review commit, gate rows 111–112 record preservation of the original ZIP and receipt, row 177 records refusal when the moved pair changed the folder timestamp, and row 201 records refusal after the compiler alias change and qualification after restoration.; The post-cleanup export records verified-current for clean producer 12bf7d51b3129fa40cff9ca1471eda647ef7bb70, ZIP SHA-256 8f757331b42ad157939108702fb59c8c9aae031d0416ee271ac792a0aaa4d344 and receipt SHA-256 ba8e64d33d79ff474f8675d31c696d41c6da33f79111e6c6e208587a6994e2c8. I matched the recorded DLL SHA-256 7cd3cb09f12f75a7247c1b401673528a0773a139e59dc3a940da860c28f24904 and MVID ccc59117-f992-44ce-9355-540d35f181f0 to the four-loader static audit.; Read-only checks confirmed the mission and amendment hashes, the detached candidate commit, and 46 evidence-file hashes listed in the index. Only documentation and evidence changed between the retained producer and the review commit.
- **Source inspection only:** The two remaining staging outcomes follow from the single receipt probe and the directory-wide comparison in KwrPackage.ps1. The candidate gate exercised a restore that changed the timestamp and a receiptless control in a quiet folder. I did not execute the two additional cases in the read-only snapshot.
- **Required gaps:** Show receipt-backed refusal after a timestamp-preserving move and restore, and successful staging of a stable independent copy during unrelated sibling activity.
- **Limitations:** The whole foreground transcript was withheld. The controller receipt establishes gate execution at the candidate. The separate producer and focused runs are documented by their committed machine outputs. The retained ZIP itself is outside the snapshot.; The real Windows file-symbolic-link case is recorded as NOT RUN because Windows required administrator privilege. OWNER AMENDMENTS 6 and 8 accept that execution limitation.; Native gameplay and combined-stack testing are recorded as NOT RUN. The audit covers static API resolution on 0.31.1, 0.32.4, the owner’s 0.33.0 installation and 0.33.4.; The earlier round-12 retained and focused JSON summaries remain unavailable. Their surviving console exports are identified in the evidence index.

#### Summary

- **Delivered behavior:** Waking Rune declares UMM 0.31.1 and compiles against UnityModManager 0.31.1.0, Harmony12 1.2.0.1 and Harmony2 2.3.1.1. The packaged DLL matches the static audit. The report writer preserves the tested inputs, and frozen provenance detects the compiler alias change. External receipt selection remains the readiness blocker.
- **Scope reviewed:** Final review of Waking Rune PR #12 under mission revision 8, focused on the round-13 corrections and their build, validation and staging consumers.
- **Pending later-phase work:** none
- **Unresolved questions:** none
- **Remaining owner review:** Owner acceptance of draft PR #12 after the staging correction clears review.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: revision 8, sha256 `bd67d1a2b5deb1f3d77926609d7401488ccf79c3426bbb36468b8caf9807f010`.

Codex CLI `0.170.0`, thread `01a21da5-df34-7040-bc13-c7dfcb3ace5e`, duration 659.9s, response sha256 `5a7256b85a853b2af11585bbf4deabcc25865737ceaf414b543ba025dfdc9e43`, request `kwr-codex-umm-0311-baseline-20261123-20261123181024-1057-r13-15b837`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "cd3b646ecca6ce2cb517a8b60b6b641e36c28a6c",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "External staging still uses the containing directory\u2019s timestamp to decide whether a receipt was present. A timestamp-preserving restore can admit a ZIP whose receipt would reject it. An unrelated file change can also reject a valid ZIP. The report-protection and compiler-binding corrections are verified.\n\n## Continuation for Claude\n\nKeep the correction in the external-package staging helper and its existing disposable fixture. Bind the receipt decision to the selected ZIP and adjacent receipt. Carry an observed receipt through validation and recheck an observed absence before accepting content-only staging. Preserve delete sharing and the canonical publication lock.\n\nExercise both outcomes in the fixture. A restored dirty pair must be refused even when its directory timestamp is preserved. An unchanged receiptless ZIP must qualify while an unrelated sibling changes. Run the configured gate within OWNER AMENDMENT 8\u2019s limits. Revalidate the retained pair after cleanup, rebuilding it only if material inputs changed. The existing static audit remains applicable when the full DLL hash and MVID match.",
  "evidence_assessment": {
    "executed_verification": [
      "The controller\u2019s baseline gate ran at cd3b646ecca6ce2cb517a8b60b6b641e36c28a6c and exited 0 in 918.9 seconds. I verified the log hash and counted 178 PASS rows, zero FAIL rows and one explicit NOT RUN. The log records a cold-cache shipping build, the three pinned direct-MSBuild inputs and 290 passing product tests on the alternate-version build.",
      "The focused exports record all four old behaviors on 15590ed and four passing correction cases on 12bf7d5. At the review commit, gate rows 111\u2013112 record preservation of the original ZIP and receipt, row 177 records refusal when the moved pair changed the folder timestamp, and row 201 records refusal after the compiler alias change and qualification after restoration.",
      "The post-cleanup export records verified-current for clean producer 12bf7d51b3129fa40cff9ca1471eda647ef7bb70, ZIP SHA-256 8f757331b42ad157939108702fb59c8c9aae031d0416ee271ac792a0aaa4d344 and receipt SHA-256 ba8e64d33d79ff474f8675d31c696d41c6da33f79111e6c6e208587a6994e2c8. I matched the recorded DLL SHA-256 7cd3cb09f12f75a7247c1b401673528a0773a139e59dc3a940da860c28f24904 and MVID ccc59117-f992-44ce-9355-540d35f181f0 to the four-loader static audit.",
      "Read-only checks confirmed the mission and amendment hashes, the detached candidate commit, and 46 evidence-file hashes listed in the index. Only documentation and evidence changed between the retained producer and the review commit."
    ],
    "inspected": [
      "The mission, all eight owner amendments, five pending ledger entries, developer report, transcript notice, controller gate receipt and log.",
      "The feature diff, README, compatibility guide, production project and pins, build wrapper, standalone validator, package staging helper, file-identity helper and affected runtime callers.",
      "The round-13 correction diff, gate fixtures, focused driver, old-head reproduction, retained build receipt, post-cleanup result and static audit."
    ],
    "limitations": [
      "The whole foreground transcript was withheld. The controller receipt establishes gate execution at the candidate. The separate producer and focused runs are documented by their committed machine outputs. The retained ZIP itself is outside the snapshot.",
      "The real Windows file-symbolic-link case is recorded as NOT RUN because Windows required administrator privilege. OWNER AMENDMENTS 6 and 8 accept that execution limitation.",
      "Native gameplay and combined-stack testing are recorded as NOT RUN. The audit covers static API resolution on 0.31.1, 0.32.4, the owner\u2019s 0.33.0 installation and 0.33.4.",
      "The earlier round-12 retained and focused JSON summaries remain unavailable. Their surviving console exports are identified in the evidence index."
    ],
    "required_gaps": [
      "Show receipt-backed refusal after a timestamp-preserving move and restore, and successful staging of a stable independent copy during unrelated sibling activity."
    ],
    "source_inspection_only": [
      "The two remaining staging outcomes follow from the single receipt probe and the directory-wide comparison in KwrPackage.ps1. The candidate gate exercised a restore that changed the timestamp and a receiptless control in a quiet folder. I did not execute the two additional cases in the read-only snapshot."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "OWNER AMENDMENT 8, R11-F2 external ZIP and receipt selection"
      ],
      "actual": "The one receipt probe records false. After restoration, both ZIP identity checks succeed. DirectoryStampOf compares only the folder identity and last-write time, so an equal timestamp also passes. Line 136 then validates the private ZIP alone and returns external-content-inspected despite the restored receipt.",
      "blocking_rationale": "Default staging can omit a dirty or mismatching receipt that would require rejection.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Default staging validates the receipt belonging to the selected pair or refuses the ambiguous read.",
      "id": "R11-F2",
      "location": {
        "file": "scripts/KwrPackage.ps1",
        "line": 120
      },
      "priority": "P2",
      "requested_correction": "Observe the adjacent receipt directly as part of selecting the ZIP. Carry its identity and bytes through the copy, and confirm an initially absent receipt is still absent before accepting receiptless staging.",
      "requested_evidence": "Extend the existing dirty-pair fixture with a restore that preserves the containing directory\u2019s original last-write time. Record the restored pair, equal directory stamps and default staging refusal. Retain the publisher rollback control.",
      "title": "A restored external receipt can still be skipped when the folder timestamp matches",
      "trigger": "An external ZIP and its receipt are away during the receipt probe and return to their original paths before the final check. The restore preserves the containing directory\u2019s last-write time."
    },
    {
      "acceptance_ids": [
        "OWNER AMENDMENT 5, preserve independent ordinary external copies",
        "OWNER AMENDMENT 8, R11-F2 stable receiptless-copy behavior"
      ],
      "actual": "The new folder-stamp comparison throws on an entry change anywhere in the directory. Both Begin and Reinstall therefore refuse the selected ZIP even when its identity, bytes and receipt state remain stable.",
      "blocking_rationale": "The correction makes supported external staging depend on unrelated activity in the caller\u2019s folder.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "An unchanged independent ZIP keeps its normal receipt-backed or content-only validation mode.",
      "id": "R13-F1",
      "location": {
        "file": "scripts/KwrPackage.ps1",
        "line": 123
      },
      "priority": "P2",
      "requested_correction": "Limit the stability decision to the selected ZIP and its adjacent receipt so other directory entries cannot invalidate the read.",
      "requested_evidence": "Use the existing stable receiptless-copy control with an unrelated sibling created or renamed during the copy. Record successful external-content-inspected staging and the expected ZIP digest.",
      "title": "An unrelated file change rejects a stable external ZIP",
      "trigger": "Another download creates or renames a file in the same folder while the helper copies an independent mod ZIP."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_verified",
      "finding_id": "R10-F1",
      "rationale": "IdOrAbsent establishes identity or absence with one open, and WriteChecked receives that observation. Gate row 112 preserved both input hashes after receipt substitution. Row 108 wrote an ordinary distinct existing report successfully."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R11-F2",
      "rationale": "Gate row 177 proves refusal when the move changes the directory timestamp. The helper never rechecks the receipt after restoration. With the same directory identity and timestamp, the restored pair still reaches content-only validation."
    },
    {
      "assessment": "deferral_accepted",
      "finding_id": "R11-F4",
      "rationale": "OWNER AMENDMENT 8 explicitly defers diagnostic wording for an unchanged, exclusively locked report. The failed open occurs before truncation."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R12-F1",
      "rationale": "ReadAllBound checks each enumerated hard-link name against the identity of the handle supplying the bytes. Gate row 111 records the equal-count replacement, refusal and unchanged original pair. Row 185 confirms the delete-sharing reader allowed rollback to restore both files."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R12-F2",
      "rationale": "Inventory equality and the build fingerprint now include the parsed referencePath alongside the alias-bearing name. Gate row 201 records both actual compiler arguments for byte-identical DLLs, refusal under the changed binding and verified-current after restoring the literal path."
    }
  ],
  "request_id": "kwr-codex-umm-0311-baseline-20261123-20261123181024-1057-r13-15b837",
  "round": 13,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "Waking Rune declares UMM 0.31.1 and compiles against UnityModManager 0.31.1.0, Harmony12 1.2.0.1 and Harmony2 2.3.1.1. The packaged DLL matches the static audit. The report writer preserves the tested inputs, and frozen provenance detects the compiler alias change. External receipt selection remains the readiness blocker.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "Owner acceptance of draft PR #12 after the staging correction clears review."
    ],
    "scope_reviewed": "Final review of Waking Rune PR #12 under mission revision 8, focused on the round-13 corrections and their build, validation and staging consumers.",
    "unresolved_questions": []
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
