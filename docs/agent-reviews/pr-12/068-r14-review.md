<!-- agent-review:v1 enrollment=kwr-codex-umm-0311-baseline-20261123-20261123181024-1057 key=r14-review part=1/1 sha256=1e7b893b61ee5227774c3f8b1a756c66b42a5a2ee62ef1e0c33ef68178e660fa -->
### Codex GPT-6.2 Astra — review — round 14 — candidate `166fbb909d`

**Decision:** `changes_required` — Both pending findings are fixed. A separate defect remains in the branch that finds a receipt. During an external package replacement, staging can use another copy’s clean receipt and record the selected dirty pair as a frozen candidate.

Continuation for Claude

Round 14 reaches the review cap in OWNER AMENDMENT 8. Preserve draft PR #12, the retained production pair and the evidence at this stop. Any owner-authorized continuation should stay within the external staging helper and its existing fixture. Bind the receipt to the ZIP when taking the snapshot, preserve delete sharing and publication locking, and demonstrate refusal when a temporary replacement supplies a valid receipt for identical ZIP bytes. The next review would need that observation, the ordinary staging and rollback controls, and a configured gate receipt for the corrected commit.

#### Findings

**R14-F1** · P2 · defect · blocks readiness — External staging can qualify a ZIP with another pair’s receipt
- Location: `scripts/KwrPackage.ps1:120`; acceptance: OWNER AMENDMENT 8, R11-F2 external ZIP and receipt selection, Mission §5, bind the packaged DLL to its source/build receipt
- Trigger: A dirty external pair moves away after the ZIP identity check. A second pair with byte-identical ZIP contents and a clean receipt occupies the paths during the receipt read. The original pair returns before the next ZIP identity check.
- Expected: Default staging validates the receipt belonging to the selected external ZIP or refuses the ambiguous pair.
- Actual: The helper copies the ZIP before its first receipt observation. ReadAll returns the temporary receipt’s bytes and file identity, but the helper never compares that identity. The check after the read examines only the ZIP, whose original identity has returned. The final receipt check runs only for an initially absent receipt. A genuine clean receipt for identical ZIP bytes therefore passes current-provenance validation, and Begin or Reinstall records frozen-candidate for the restored source path.
- Why it blocks / does not: Default staging can grant frozen-candidate provenance to a selected pair that is refused when read undisturbed. Removing the folder-wide check exposed this case on the receipt-present path.
- Requested correction: Capture the adjacent receipt’s identity and bytes when selecting the external ZIP and carry that observation through the ZIP copy. Check that the receipt read belongs to the selected pair before accepting the snapshot. Keep the stability checks specific to those two files.
- Requested evidence: Extend the existing external-pair fixture with two independent copies of the same ZIP. Establish that the clean pair qualifies and the dirty pair is refused. Record the distinct receipt identities and show that restoring the dirty pair across a successful read of the other receipt results in refusal.

#### Prior finding assessments

- **R11-F2** — `fix_verified` — Gate line 177 observed hasReceipt=False, restored the original dirty pair with the directory timestamp preserved, and recorded refusal because the receipt had reappeared. The helper now rechecks an initial absence before returning content-only staging.
- **R13-F1** — `fix_verified` — The staging decision no longer uses the directory timestamp. Gate line 178 created and renamed unrelated siblings while the copy was paused. The helper returned external-content-inspected with the expected ZIP digest.

#### Evidence assessment

- **Inspected:** Verified the mission and amendment hashes and matched the complete 18,946-line feature diff to Git. Reviewed README.md, docs/UMM-COMPATIBILITY.md, the findings ledger and the round-14 changes.; Traced receipt selection through KwrPackage.ps1, KwrFileIdentity.ps1, Validate-Package.ps1 and the Begin and Reinstall journal writes. Compared the changed fixture with the controller log.; Compared the retained producer with the candidate. The production sources, compiler inputs and provenance validator are unchanged. All 40 checked evidence exports matched their indexed Git-blob digests.
- **Executed verification:** The controller ran the baseline gate at 166fbb9. It exited 0 in 844.1 seconds with 179 of 179 executed checks passing. I verified the log SHA-256 against the controller receipt.; The gate built shipping version 0.12.3 from an empty reference cache and validated its package with current provenance. The direct MSBuild check recorded all three pinned compiler paths. The test-enabled alternate-version build reported 290 tests passed.; The focused exports record both old failures on 12bf7d5 and both corrections on 5fca018. Candidate gate lines 177–179 independently confirm the restored-receipt refusal, sibling-change success and moved-pair refusal. Lines 185–187 confirm publisher rollback restored both previous files.; The post-cleanup record identifies the retained clean producer as 12bf7d5, ZIP 8f757331b42ad157939108702fb59c8c9aae031d0416ee271ac792a0aaa4d344 and receipt ba8e64d33d79ff474f8675d31c696d41c6da33f79111e6c6e208587a6994e2c8. Validation returned verified-current. Its DLL SHA-256 and MVID match the four-loader static audit.
- **Source inspection only:** R14-F1 follows from the receipt-present branch and its journal consumers. The supplied gate exercises an absent receipt during the move. It does not exercise a different receipt present at that point.
- **Required gaps:** R14-F1 requires a correction and evidence that a successful receipt read from a temporary replacement cannot qualify the original external pair.
- **Limitations:** The real Windows file-symbolic-link fixture was NOT RUN. The host reported “Administrator privilege required for this operation.” OWNER AMENDMENT 6 accepts that execution limitation.; Native gameplay and combined-stack testing were NOT RUN. The compatibility evidence for UMM 0.31.1, 0.32.4, owner-installed 0.33.0 and 0.33.4 is static.; The controller withheld the mixed foreground transcript. The focused and retained-pair results come from indexed exports. The candidate gate has a separate controller receipt and log. R11-F4 remains deferred and nonblocking.

#### Summary

- **Delivered behavior:** Waking Rune declares UMM 0.31.1 and compiles against UnityModManager 0.31.1.0, Harmony12 1.2.0.1 and Harmony2 2.3.1.1. The retained DLL matches the audited SHA-256 and MVID. External receipt selection during replacement remains the blocking issue.
- **Scope reviewed:** Waking Rune PR #12 at the final checkpoint under owner amendment revision 8, including the two pending findings and the staging paths directly affected by their correction.
- **Pending later-phase work:** none
- **Unresolved questions:** none
- **Remaining owner review:** Only the owner can authorize further correction and review beyond the 14-submission cap. PR #12 remains a draft.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: revision 8, sha256 `bd67d1a2b5deb1f3d77926609d7401488ccf79c3426bbb36468b8caf9807f010`.

Codex CLI `0.170.0`, thread `01a21dc4-a3ef-75c1-9eae-c8b4273a2653`, duration 726.8s, response sha256 `3c2cbf68040a451b8c7fa542cebebbf245291f8bef44ff09f1d9362b25530ca3`, request `kwr-codex-umm-0311-baseline-20261123-20261123181024-1057-r14-e6482b`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "166fbb909da03d1ea98b02868cca53b37eed9191",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "Both pending findings are fixed. A separate defect remains in the branch that finds a receipt. During an external package replacement, staging can use another copy\u2019s clean receipt and record the selected dirty pair as a frozen candidate.\n\nContinuation for Claude\n\nRound 14 reaches the review cap in OWNER AMENDMENT 8. Preserve draft PR #12, the retained production pair and the evidence at this stop. Any owner-authorized continuation should stay within the external staging helper and its existing fixture. Bind the receipt to the ZIP when taking the snapshot, preserve delete sharing and publication locking, and demonstrate refusal when a temporary replacement supplies a valid receipt for identical ZIP bytes. The next review would need that observation, the ordinary staging and rollback controls, and a configured gate receipt for the corrected commit.",
  "evidence_assessment": {
    "executed_verification": [
      "The controller ran the baseline gate at 166fbb9. It exited 0 in 844.1 seconds with 179 of 179 executed checks passing. I verified the log SHA-256 against the controller receipt.",
      "The gate built shipping version 0.12.3 from an empty reference cache and validated its package with current provenance. The direct MSBuild check recorded all three pinned compiler paths. The test-enabled alternate-version build reported 290 tests passed.",
      "The focused exports record both old failures on 12bf7d5 and both corrections on 5fca018. Candidate gate lines 177\u2013179 independently confirm the restored-receipt refusal, sibling-change success and moved-pair refusal. Lines 185\u2013187 confirm publisher rollback restored both previous files.",
      "The post-cleanup record identifies the retained clean producer as 12bf7d5, ZIP 8f757331b42ad157939108702fb59c8c9aae031d0416ee271ac792a0aaa4d344 and receipt ba8e64d33d79ff474f8675d31c696d41c6da33f79111e6c6e208587a6994e2c8. Validation returned verified-current. Its DLL SHA-256 and MVID match the four-loader static audit."
    ],
    "inspected": [
      "Verified the mission and amendment hashes and matched the complete 18,946-line feature diff to Git. Reviewed README.md, docs/UMM-COMPATIBILITY.md, the findings ledger and the round-14 changes.",
      "Traced receipt selection through KwrPackage.ps1, KwrFileIdentity.ps1, Validate-Package.ps1 and the Begin and Reinstall journal writes. Compared the changed fixture with the controller log.",
      "Compared the retained producer with the candidate. The production sources, compiler inputs and provenance validator are unchanged. All 40 checked evidence exports matched their indexed Git-blob digests."
    ],
    "limitations": [
      "The real Windows file-symbolic-link fixture was NOT RUN. The host reported \u201cAdministrator privilege required for this operation.\u201d OWNER AMENDMENT 6 accepts that execution limitation.",
      "Native gameplay and combined-stack testing were NOT RUN. The compatibility evidence for UMM 0.31.1, 0.32.4, owner-installed 0.33.0 and 0.33.4 is static.",
      "The controller withheld the mixed foreground transcript. The focused and retained-pair results come from indexed exports. The candidate gate has a separate controller receipt and log. R11-F4 remains deferred and nonblocking."
    ],
    "required_gaps": [
      "R14-F1 requires a correction and evidence that a successful receipt read from a temporary replacement cannot qualify the original external pair."
    ],
    "source_inspection_only": [
      "R14-F1 follows from the receipt-present branch and its journal consumers. The supplied gate exercises an absent receipt during the move. It does not exercise a different receipt present at that point."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "OWNER AMENDMENT 8, R11-F2 external ZIP and receipt selection",
        "Mission \u00a75, bind the packaged DLL to its source/build receipt"
      ],
      "actual": "The helper copies the ZIP before its first receipt observation. ReadAll returns the temporary receipt\u2019s bytes and file identity, but the helper never compares that identity. The check after the read examines only the ZIP, whose original identity has returned. The final receipt check runs only for an initially absent receipt. A genuine clean receipt for identical ZIP bytes therefore passes current-provenance validation, and Begin or Reinstall records frozen-candidate for the restored source path.",
      "blocking_rationale": "Default staging can grant frozen-candidate provenance to a selected pair that is refused when read undisturbed. Removing the folder-wide check exposed this case on the receipt-present path.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Default staging validates the receipt belonging to the selected external ZIP or refuses the ambiguous pair.",
      "id": "R14-F1",
      "location": {
        "file": "scripts/KwrPackage.ps1",
        "line": 120
      },
      "priority": "P2",
      "requested_correction": "Capture the adjacent receipt\u2019s identity and bytes when selecting the external ZIP and carry that observation through the ZIP copy. Check that the receipt read belongs to the selected pair before accepting the snapshot. Keep the stability checks specific to those two files.",
      "requested_evidence": "Extend the existing external-pair fixture with two independent copies of the same ZIP. Establish that the clean pair qualifies and the dirty pair is refused. Record the distinct receipt identities and show that restoring the dirty pair across a successful read of the other receipt results in refusal.",
      "title": "External staging can qualify a ZIP with another pair\u2019s receipt",
      "trigger": "A dirty external pair moves away after the ZIP identity check. A second pair with byte-identical ZIP contents and a clean receipt occupies the paths during the receipt read. The original pair returns before the next ZIP identity check."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_verified",
      "finding_id": "R11-F2",
      "rationale": "Gate line 177 observed hasReceipt=False, restored the original dirty pair with the directory timestamp preserved, and recorded refusal because the receipt had reappeared. The helper now rechecks an initial absence before returning content-only staging."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R13-F1",
      "rationale": "The staging decision no longer uses the directory timestamp. Gate line 178 created and renamed unrelated siblings while the copy was paused. The helper returned external-content-inspected with the expected ZIP digest."
    }
  ],
  "request_id": "kwr-codex-umm-0311-baseline-20261123-20261123181024-1057-r14-e6482b",
  "round": 14,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "Waking Rune declares UMM 0.31.1 and compiles against UnityModManager 0.31.1.0, Harmony12 1.2.0.1 and Harmony2 2.3.1.1. The retained DLL matches the audited SHA-256 and MVID. External receipt selection during replacement remains the blocking issue.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "Only the owner can authorize further correction and review beyond the 14-submission cap. PR #12 remains a draft."
    ],
    "scope_reviewed": "Waking Rune PR #12 at the final checkpoint under owner amendment revision 8, including the two pending findings and the staging paths directly affected by their correction.",
    "unresolved_questions": []
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
