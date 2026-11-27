<!-- agent-review:v1 enrollment=kwr-codex-umm-0311-baseline-20261123-20261123181024-1057 key=r11-review part=1/1 sha256=08a2eede14994741a4efb8e36a81e9b2194e85bcfa66681f37f6141d5f729b7a -->
### Codex GPT-6.2 Astra — review — round 11 — candidate `861f2e6a0e`

**Decision:** `changes_required` — The configured gate passed. Source inspection still identifies paths that can overwrite a receipt, obstruct publisher rollback, skip an external package’s receipt, or attribute a DLL to the wrong producing commit. The supplied evidence also lacks the required post-cleanup validation record for the retained pair.

## Continuation for Claude

Coordinate the fixes in the existing helpers. Bind report protection to the first destination observation. Let the publisher rename a ZIP throughout an unlocked read, while keeping the selected receipt with that ZIP or declining a changed pair. Carry the parsed reference path into the Git comparison so literal equals signs survive normalization.

Use the existing disposable fixtures for the timing and producer comparisons described in the findings. Retain one final production pair, run the configured gate, and make its post-cleanup current-provenance result available in the review evidence. Reuse the four-loader audit when the full DLL hash and MVID match. Keep the accepted controls, historical outputs, documented NOT RUN cases, and OWNER AMENDMENT 7 limits. The corrections fit the existing tooling and require no native campaign.

#### Findings

**R10-F1** · P1 · defect · blocks readiness — A moved receipt can become the permitted report destination
- Location: `scripts/Validate-Package.ps1:68`; acceptance: OWNER AMENDMENT 7, R10-F1, OWNER AMENDMENT 6, protection of the actual ZIP and receipt
- Trigger: Validate a hard-linked ZIP while its target receipt moves to the initially absent ReportPath between the preliminary report check and the destination identity sample.
- Expected: Once validation observes the report destination, a receipt moved there remains protected through the write.
- Actual: The preliminary check at line 64 can observe an absent report. Line 68 then samples the destination again. If the target receipt moves there between those observations, its identity becomes the permitted reportStartId. Its former name is absent when ReadAllBound collects receipt identities, and WriteChecked can truncate it. Both gate moves occur after line 68.
- Why it blocks / does not: Content-only validation can overwrite the build receipt belonging to the ZIP it inspected.
- Requested correction: Use the first destination observation throughout report admission and the checked write. Decline a linked-input report when the target receipt’s identity cannot be established safely.
- Requested evidence: In the existing hard-linked ZIP fixture, omit ReceiptPath and move the target receipt after the first destination check but before the current reportStartId assignment. Record refusal and unchanged ZIP and receipt hashes. Preserve the independent-copy control.

**R11-F1** · P1 · defect · blocks readiness — The standalone validator can still obstruct rollback
- Location: `scripts/KwrFileIdentity.ps1:133`; acceptance: OWNER AMENDMENT 7, R9-F2 unlocked ZIP handles, mission §6, standalone pre-install validation
- Trigger: Run content validation directly on a newly placed ZIP while its publisher fails to place the receipt.
- Expected: A repository reader holding a published ZIP outside its canonical lock allows the publisher to rename it during rollback.
- Actual: Validate-Package.ps1 calls ReadAllBound directly on the published ZIP. That method holds FileShare.Read across receipt discovery and the byte copy without acquiring the publication lock. The first rollback move in Build.ps1 then fails if it overlaps that handle, leaving the previous pair in the recovery folder.
- Why it blocks / does not: The documented standalone validation command can prevent automatic restoration of the previous publication.
- Requested correction: Make the standalone snapshot compatible with publisher renames, or decline the read under the canonical publication guard. Preserve the binding between the bytes read, the ZIP identity, and its protected receipts.
- Requested evidence: Use the paused-publisher fixture with the standalone validator reading the final ZIP. Overlap its snapshot with receipt-placement failure and verify both previous hashes at their final paths.

**R11-F2** · P2 · defect · blocks readiness — Moving an external pair can silently skip its receipt
- Location: `scripts/KwrPackage.ps1:105`; acceptance: OWNER AMENDMENT 7, R9-F2 direct regressions, OWNER AMENDMENT 5, independent external copies
- Trigger: An independent external ZIP and its adjacent receipt move after the helper rechecks the ZIP and before it probes the old sidecar path.
- Expected: The helper validates the receipt belonging to the selected external pair or refuses the changed input.
- Actual: OpenReadShared now permits the external ZIP to move after its identity recheck. The helper copies bytes through that handle, then probes the receipt at the old pathname. If the ZIP and its receipt have moved in that order, the probe returns false and default staging admits the held ZIP as external-content-inspected.
- Why it blocks / does not: A selected external pair with a dirty or mismatching receipt can pass default staging without its required provenance check.
- Requested correction: Bind external receipt selection to the ZIP snapshot, or recheck the source before granting receiptless admission. Keep delete sharing for handles opened before ownership is established.
- Requested evidence: Move a receipt-backed independent ZIP and then its sidecar during the held read. Show that default staging enforces the original receipt or refuses the change. Keep the stable receiptless-copy control.

**R9-F4** · P2 · defect · blocks readiness — A literal reference path can still be shortened as an alias
- Location: `scripts/KwrBuild.Common.ps1:129`; acceptance: OWNER AMENDMENT 7, R9-F4, mission §5, material compiler references
- Trigger: An ordinary unaliased compiler reference normalizes to a repository path beginning with an alias-shaped prefix.
- Expected: Only an alias present in the original compiler argument is removed. A literal equals sign in the normalized repository path survives.
- Actual: The inventory stores an optional alias and normalized path in one string. The decoder removes a leading identifier and equals sign from every reference entry. An unaliased reference resolving to the tracked path Rules=Local/Helper.dll therefore becomes Local/Helper.dll. Both the live and recorded inventories can omit the actual file from the Git comparison.
- Why it blocks / does not: A receipt can receive verified-current for a claimed producer that differs in a tracked compiler reference.
- Requested correction: Retain the parsed reference path or explicit alias information when constructing the inventory. Use that information for tracked-path lookup.
- Requested evidence: Use an unaliased reference whose physical tracked path begins with an identifier and equals sign. Show the complete Git path, genuine-pair qualification, and refusal of a producer differing only in that reference. Preserve the real-alias, response-file, and file-option controls.

**R11-F3** · P2 · evidence_gap · blocks readiness — The retained pair’s post-cleanup result is missing from the review packet
- Location: `docs/evidence/umm-0311/retained-r11.json:3`; acceptance: OWNER AMENDMENT 7, retained candidate after fixture cleanup
- Trigger: Trace the claimed post-cleanup qualification of the corrected 859674e pair.
- Expected: The review packet contains the measured identities and validator result for the retained pair after cleanup.
- Actual: retained-r11.json records Retained mode at 13:25 UTC. The gate ran from 13:29 to 13:42 UTC and used a different ZIP in its temporary clone. The developer report names postcleanup-r11.json in the fleet folder, but the supplied evidence and transcript contain no output from that run.
- Why it blocks / does not: The owner explicitly requires successful current-provenance validation of the retained pair after fixture cleanup.
- Requested correction: Export the existing post-cleanup machine output through a review-visible evidence path. If material corrections produce a new final pair, include the corresponding result for those bytes.
- Requested evidence: Provide the durable location, producing commit, ZIP and receipt SHA-256, packaged DLL SHA-256 and MVID, validator exit status, and verified-current result measured after cleanup.

**R11-F4** · P3 · optional_polish · non-blocking — An unchanged locked report is described as replaced
- Location: `scripts/KwrFileIdentity.ps1:182`; acceptance: OWNER AMENDMENT 7, R10-F1 checked report write
- Trigger: Another process holds an existing, distinct report file open without write sharing.
- Expected: A sharing violation on an unchanged report destination retains its underlying cause.
- Actual: WriteChecked converts every IOException from opening the output into a destination-changed error, even when its second identity check finds the same file.
- Why it blocks / does not: The operation fails safely and preserves the file. The diagnostic can misdirect troubleshooting.
- Requested correction: Preserve the original I/O error when the destination still has its expected identity.
- Requested evidence: A locked, distinct report destination should retain its bytes and report the sharing failure.

**R11-F5** · P3 · optional_polish · non-blocking — Retained modes skip the driver’s failed-case exit
- Location: `docs/evidence/umm-0311/focused-r11-driver.ps1.txt:100`; acceptance: OWNER AMENDMENT 7, validation reporting
- Trigger: A retained validation returns a failed case without throwing an exception.
- Expected: A failed retained-pair check produces a failing driver exit status as well as a failed JSON case.
- Actual: Retained and PostCleanup return from inside the try block. The finally block writes the JSON, then control leaves the script before the failed-case exit check at line 274.
- Why it blocks / does not: The JSON exposes the actual case result and validator status. Those fields can substantiate a successful observation.
- Requested correction: For future use of the driver, propagate failed case counts from all modes after writing their output. Preserve the archived driver’s attribution.
- Requested evidence: A failed retained validation should yield a failed JSON case and a nonzero process exit.

#### Prior finding assessments

- **R10-F1** — `fix_rejected` — Gate row 112 confirms protection on both sides of ReadAllBound once reportStartId exists. The preliminary destination check and the later identity sample still allow the moved receipt itself to become the permitted output.
- **R10-F2** — `fix_verified` — The comparison includes paths from the freshly evaluated compiler inventory. Gate row 202 establishes that the receipt omitted CaseRoot.cs, the live inventory retained it, and the claimed producer differed only there. Validation refused that pair and qualified the genuine one.
- **R10-F3** — `fix_verified` — Both drivers use explicit deletion and a common base. Gate rows 196–201 record the exact single-path change and tree presence for each variant before counting its refusal. The documentation explains the old empty-file and two-path cases while preserving their recorded counts.
- **R9-F2** — `fix_verified` — The installer’s external-branch handle shares delete and checks the held identity before admission. Gate row 181 confirms the redirected junction reached the placed ZIP, rollback restored both hashes while the handle was open, and the installer refused the read. R11-F1 concerns the separate standalone reader.
- **R9-F4** — `fix_rejected` — Gate rows 195, 200, and 203 verify literal response-file and file-option names, including the one-file producer refusal. The changed reference branch still infers an alias from the normalized display name and can remove a literal prefix that was never an alias in the compiler argument.

#### Evidence assessment

- **Inspected:** The mission, all seven owner amendments, pending ledger entries, developer report, transcript notice, README, compatibility guide, and controller gate evidence.; The full feature diff and the round-11 delta from 10ac278, including the package reader, checked report writer, compiler inventory, producing-commit comparison, gate fixtures, focused driver, and retained records.
- **Executed verification:** Confirmed that the controller’s 15,075-line feature diff matches Git’s M..H diff and that all 34 indexed evidence files match their published Git-blob SHA-256 values.; Verified the controller gate receipt and log digest for 861f2e6. The Windows baseline gate exited 0 in 780.1 seconds with 171 executed passes and one separately recorded NOT RUN.; The gate recorded a cold-cache build of the unmodified shipping candidate, standalone current-provenance success, the three pinned direct compiler inputs, and 290 passing tests on the alternate-version build.; Gate row 181 records external classification, junction redirection, a ZIP handle held during receipt-placement failure, reader refusal, and restoration of both previous hashes at their final paths. Rows 195–203 record the genuine producer, six isolated Git variants, the live-versus-receipt source classifications, and the literal response-file and file-option controls.; The focused exports record 12/13 on c4340df and 13/13 on 859674e. The retained build record and receipt agree on ZIP 78b9106e979a3e1e4032022e05c292ef869959ea40e980dea78277a93cb7380b. Its DLL SHA-256 and MVID match the existing four-loader audit.
- **Source inspection only:** The additional file-move orderings and the unaliased-reference normalization case below follow from the candidate code. The supplied gate did not execute those cases.; The focused driver contains a PostCleanup implementation. Its source establishes what it would measure. The supplied outputs establish the earlier build-time measurement.
- **Required gaps:** The remaining receipt-protection, reader-sharing, external-pair, and literal-reference cases identified in the blocking findings.; A review-visible post-cleanup current-provenance result for the retained production pair.
- **Limitations:** The reviewer used the read-only snapshot. The controller withheld the mixed foreground transcript. The focused and retained results are attributed to their committed machine-output exports.; The real Windows file-symbolic-link case remains NOT RUN with the recorded error “Administrator privilege required for this operation.” OWNER AMENDMENT 6 withdrew execution of that fixture before handoff, and OWNER AMENDMENT 7 preserved the exception.; Native gameplay and combined-stack testing remain NOT RUN. The reused audit establishes static API resolution on 0.31.1, 0.32.4, the measured owner 0.33.0 installation, and 0.33.4.

#### Summary

- **Delivered behavior:** The shipping manifest declares UMM 0.31.1. The Windows build and packaged DLL use UnityModManager 0.31.1.0, Harmony12 1.2.0.1, and Harmony2 2.3.1.1. The retained DLL matches the four-loader static audit. The installer rollback fixture, authoritative source classification, and corrected producer variants have executed evidence.
- **Scope reviewed:** Waking Rune PR #12 at the fixed final candidate, with emphasis on the five pending findings, their affected consumers, and the retained-candidate evidence required by OWNER AMENDMENT 7.
- **Pending later-phase work:** none
- **Unresolved questions:** none
- **Remaining owner review:** Final disposition of draft PR #12 after the controller presents a current handoff.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: revision 7, sha256 `cf3ca150f22d96316318c752858a3382b4c3e1108ae225d5b485f8b4c7406226`.

Codex CLI `0.170.0`, thread `01a21d3e-ed1e-72f1-b2d0-7303c225f970`, duration 924.7s, response sha256 `d6ae0531081a2fb18963bba75a23797e7a0b0749999603a50fe706b1ba96dd6a`, request `kwr-codex-umm-0311-baseline-20261123-20261123181024-1057-r11-0b145c`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "861f2e6a0e1a0113583d279f1e14daa234702f2f",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The configured gate passed. Source inspection still identifies paths that can overwrite a receipt, obstruct publisher rollback, skip an external package\u2019s receipt, or attribute a DLL to the wrong producing commit. The supplied evidence also lacks the required post-cleanup validation record for the retained pair.\n\n## Continuation for Claude\n\nCoordinate the fixes in the existing helpers. Bind report protection to the first destination observation. Let the publisher rename a ZIP throughout an unlocked read, while keeping the selected receipt with that ZIP or declining a changed pair. Carry the parsed reference path into the Git comparison so literal equals signs survive normalization.\n\nUse the existing disposable fixtures for the timing and producer comparisons described in the findings. Retain one final production pair, run the configured gate, and make its post-cleanup current-provenance result available in the review evidence. Reuse the four-loader audit when the full DLL hash and MVID match. Keep the accepted controls, historical outputs, documented NOT RUN cases, and OWNER AMENDMENT 7 limits. The corrections fit the existing tooling and require no native campaign.",
  "evidence_assessment": {
    "executed_verification": [
      "Confirmed that the controller\u2019s 15,075-line feature diff matches Git\u2019s M..H diff and that all 34 indexed evidence files match their published Git-blob SHA-256 values.",
      "Verified the controller gate receipt and log digest for 861f2e6. The Windows baseline gate exited 0 in 780.1 seconds with 171 executed passes and one separately recorded NOT RUN.",
      "The gate recorded a cold-cache build of the unmodified shipping candidate, standalone current-provenance success, the three pinned direct compiler inputs, and 290 passing tests on the alternate-version build.",
      "Gate row 181 records external classification, junction redirection, a ZIP handle held during receipt-placement failure, reader refusal, and restoration of both previous hashes at their final paths. Rows 195\u2013203 record the genuine producer, six isolated Git variants, the live-versus-receipt source classifications, and the literal response-file and file-option controls.",
      "The focused exports record 12/13 on c4340df and 13/13 on 859674e. The retained build record and receipt agree on ZIP 78b9106e979a3e1e4032022e05c292ef869959ea40e980dea78277a93cb7380b. Its DLL SHA-256 and MVID match the existing four-loader audit."
    ],
    "inspected": [
      "The mission, all seven owner amendments, pending ledger entries, developer report, transcript notice, README, compatibility guide, and controller gate evidence.",
      "The full feature diff and the round-11 delta from 10ac278, including the package reader, checked report writer, compiler inventory, producing-commit comparison, gate fixtures, focused driver, and retained records."
    ],
    "limitations": [
      "The reviewer used the read-only snapshot. The controller withheld the mixed foreground transcript. The focused and retained results are attributed to their committed machine-output exports.",
      "The real Windows file-symbolic-link case remains NOT RUN with the recorded error \u201cAdministrator privilege required for this operation.\u201d OWNER AMENDMENT 6 withdrew execution of that fixture before handoff, and OWNER AMENDMENT 7 preserved the exception.",
      "Native gameplay and combined-stack testing remain NOT RUN. The reused audit establishes static API resolution on 0.31.1, 0.32.4, the measured owner 0.33.0 installation, and 0.33.4."
    ],
    "required_gaps": [
      "The remaining receipt-protection, reader-sharing, external-pair, and literal-reference cases identified in the blocking findings.",
      "A review-visible post-cleanup current-provenance result for the retained production pair."
    ],
    "source_inspection_only": [
      "The additional file-move orderings and the unaliased-reference normalization case below follow from the candidate code. The supplied gate did not execute those cases.",
      "The focused driver contains a PostCleanup implementation. Its source establishes what it would measure. The supplied outputs establish the earlier build-time measurement."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "OWNER AMENDMENT 7, R10-F1",
        "OWNER AMENDMENT 6, protection of the actual ZIP and receipt"
      ],
      "actual": "The preliminary check at line 64 can observe an absent report. Line 68 then samples the destination again. If the target receipt moves there between those observations, its identity becomes the permitted reportStartId. Its former name is absent when ReadAllBound collects receipt identities, and WriteChecked can truncate it. Both gate moves occur after line 68.",
      "blocking_rationale": "Content-only validation can overwrite the build receipt belonging to the ZIP it inspected.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Once validation observes the report destination, a receipt moved there remains protected through the write.",
      "id": "R10-F1",
      "location": {
        "file": "scripts/Validate-Package.ps1",
        "line": 68
      },
      "priority": "P1",
      "requested_correction": "Use the first destination observation throughout report admission and the checked write. Decline a linked-input report when the target receipt\u2019s identity cannot be established safely.",
      "requested_evidence": "In the existing hard-linked ZIP fixture, omit ReceiptPath and move the target receipt after the first destination check but before the current reportStartId assignment. Record refusal and unchanged ZIP and receipt hashes. Preserve the independent-copy control.",
      "title": "A moved receipt can become the permitted report destination",
      "trigger": "Validate a hard-linked ZIP while its target receipt moves to the initially absent ReportPath between the preliminary report check and the destination identity sample."
    },
    {
      "acceptance_ids": [
        "OWNER AMENDMENT 7, R9-F2 unlocked ZIP handles",
        "mission \u00a76, standalone pre-install validation"
      ],
      "actual": "Validate-Package.ps1 calls ReadAllBound directly on the published ZIP. That method holds FileShare.Read across receipt discovery and the byte copy without acquiring the publication lock. The first rollback move in Build.ps1 then fails if it overlaps that handle, leaving the previous pair in the recovery folder.",
      "blocking_rationale": "The documented standalone validation command can prevent automatic restoration of the previous publication.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "A repository reader holding a published ZIP outside its canonical lock allows the publisher to rename it during rollback.",
      "id": "R11-F1",
      "location": {
        "file": "scripts/KwrFileIdentity.ps1",
        "line": 133
      },
      "priority": "P1",
      "requested_correction": "Make the standalone snapshot compatible with publisher renames, or decline the read under the canonical publication guard. Preserve the binding between the bytes read, the ZIP identity, and its protected receipts.",
      "requested_evidence": "Use the paused-publisher fixture with the standalone validator reading the final ZIP. Overlap its snapshot with receipt-placement failure and verify both previous hashes at their final paths.",
      "title": "The standalone validator can still obstruct rollback",
      "trigger": "Run content validation directly on a newly placed ZIP while its publisher fails to place the receipt."
    },
    {
      "acceptance_ids": [
        "OWNER AMENDMENT 7, R9-F2 direct regressions",
        "OWNER AMENDMENT 5, independent external copies"
      ],
      "actual": "OpenReadShared now permits the external ZIP to move after its identity recheck. The helper copies bytes through that handle, then probes the receipt at the old pathname. If the ZIP and its receipt have moved in that order, the probe returns false and default staging admits the held ZIP as external-content-inspected.",
      "blocking_rationale": "A selected external pair with a dirty or mismatching receipt can pass default staging without its required provenance check.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The helper validates the receipt belonging to the selected external pair or refuses the changed input.",
      "id": "R11-F2",
      "location": {
        "file": "scripts/KwrPackage.ps1",
        "line": 105
      },
      "priority": "P2",
      "requested_correction": "Bind external receipt selection to the ZIP snapshot, or recheck the source before granting receiptless admission. Keep delete sharing for handles opened before ownership is established.",
      "requested_evidence": "Move a receipt-backed independent ZIP and then its sidecar during the held read. Show that default staging enforces the original receipt or refuses the change. Keep the stable receiptless-copy control.",
      "title": "Moving an external pair can silently skip its receipt",
      "trigger": "An independent external ZIP and its adjacent receipt move after the helper rechecks the ZIP and before it probes the old sidecar path."
    },
    {
      "acceptance_ids": [
        "OWNER AMENDMENT 7, R9-F4",
        "mission \u00a75, material compiler references"
      ],
      "actual": "The inventory stores an optional alias and normalized path in one string. The decoder removes a leading identifier and equals sign from every reference entry. An unaliased reference resolving to the tracked path Rules=Local/Helper.dll therefore becomes Local/Helper.dll. Both the live and recorded inventories can omit the actual file from the Git comparison.",
      "blocking_rationale": "A receipt can receive verified-current for a claimed producer that differs in a tracked compiler reference.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Only an alias present in the original compiler argument is removed. A literal equals sign in the normalized repository path survives.",
      "id": "R9-F4",
      "location": {
        "file": "scripts/KwrBuild.Common.ps1",
        "line": 129
      },
      "priority": "P2",
      "requested_correction": "Retain the parsed reference path or explicit alias information when constructing the inventory. Use that information for tracked-path lookup.",
      "requested_evidence": "Use an unaliased reference whose physical tracked path begins with an identifier and equals sign. Show the complete Git path, genuine-pair qualification, and refusal of a producer differing only in that reference. Preserve the real-alias, response-file, and file-option controls.",
      "title": "A literal reference path can still be shortened as an alias",
      "trigger": "An ordinary unaliased compiler reference normalizes to a repository path beginning with an alias-shaped prefix."
    },
    {
      "acceptance_ids": [
        "OWNER AMENDMENT 7, retained candidate after fixture cleanup"
      ],
      "actual": "retained-r11.json records Retained mode at 13:25 UTC. The gate ran from 13:29 to 13:42 UTC and used a different ZIP in its temporary clone. The developer report names postcleanup-r11.json in the fleet folder, but the supplied evidence and transcript contain no output from that run.",
      "blocking_rationale": "The owner explicitly requires successful current-provenance validation of the retained pair after fixture cleanup.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "The review packet contains the measured identities and validator result for the retained pair after cleanup.",
      "id": "R11-F3",
      "location": {
        "file": "docs/evidence/umm-0311/retained-r11.json",
        "line": 3
      },
      "priority": "P2",
      "requested_correction": "Export the existing post-cleanup machine output through a review-visible evidence path. If material corrections produce a new final pair, include the corresponding result for those bytes.",
      "requested_evidence": "Provide the durable location, producing commit, ZIP and receipt SHA-256, packaged DLL SHA-256 and MVID, validator exit status, and verified-current result measured after cleanup.",
      "title": "The retained pair\u2019s post-cleanup result is missing from the review packet",
      "trigger": "Trace the claimed post-cleanup qualification of the corrected 859674e pair."
    },
    {
      "acceptance_ids": [
        "OWNER AMENDMENT 7, R10-F1 checked report write"
      ],
      "actual": "WriteChecked converts every IOException from opening the output into a destination-changed error, even when its second identity check finds the same file.",
      "blocking_rationale": "The operation fails safely and preserves the file. The diagnostic can misdirect troubleshooting.",
      "blocks_readiness": false,
      "category": "optional_polish",
      "expected": "A sharing violation on an unchanged report destination retains its underlying cause.",
      "id": "R11-F4",
      "location": {
        "file": "scripts/KwrFileIdentity.ps1",
        "line": 182
      },
      "priority": "P3",
      "requested_correction": "Preserve the original I/O error when the destination still has its expected identity.",
      "requested_evidence": "A locked, distinct report destination should retain its bytes and report the sharing failure.",
      "title": "An unchanged locked report is described as replaced",
      "trigger": "Another process holds an existing, distinct report file open without write sharing."
    },
    {
      "acceptance_ids": [
        "OWNER AMENDMENT 7, validation reporting"
      ],
      "actual": "Retained and PostCleanup return from inside the try block. The finally block writes the JSON, then control leaves the script before the failed-case exit check at line 274.",
      "blocking_rationale": "The JSON exposes the actual case result and validator status. Those fields can substantiate a successful observation.",
      "blocks_readiness": false,
      "category": "optional_polish",
      "expected": "A failed retained-pair check produces a failing driver exit status as well as a failed JSON case.",
      "id": "R11-F5",
      "location": {
        "file": "docs/evidence/umm-0311/focused-r11-driver.ps1.txt",
        "line": 100
      },
      "priority": "P3",
      "requested_correction": "For future use of the driver, propagate failed case counts from all modes after writing their output. Preserve the archived driver\u2019s attribution.",
      "requested_evidence": "A failed retained validation should yield a failed JSON case and a nonzero process exit.",
      "title": "Retained modes skip the driver\u2019s failed-case exit",
      "trigger": "A retained validation returns a failed case without throwing an exception."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_rejected",
      "finding_id": "R10-F1",
      "rationale": "Gate row 112 confirms protection on both sides of ReadAllBound once reportStartId exists. The preliminary destination check and the later identity sample still allow the moved receipt itself to become the permitted output."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R10-F2",
      "rationale": "The comparison includes paths from the freshly evaluated compiler inventory. Gate row 202 establishes that the receipt omitted CaseRoot.cs, the live inventory retained it, and the claimed producer differed only there. Validation refused that pair and qualified the genuine one."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R10-F3",
      "rationale": "Both drivers use explicit deletion and a common base. Gate rows 196\u2013201 record the exact single-path change and tree presence for each variant before counting its refusal. The documentation explains the old empty-file and two-path cases while preserving their recorded counts."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R9-F2",
      "rationale": "The installer\u2019s external-branch handle shares delete and checks the held identity before admission. Gate row 181 confirms the redirected junction reached the placed ZIP, rollback restored both hashes while the handle was open, and the installer refused the read. R11-F1 concerns the separate standalone reader."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R9-F4",
      "rationale": "Gate rows 195, 200, and 203 verify literal response-file and file-option names, including the one-file producer refusal. The changed reference branch still infers an alias from the normalized display name and can remove a literal prefix that was never an alias in the compiler argument."
    }
  ],
  "request_id": "kwr-codex-umm-0311-baseline-20261123-20261123181024-1057-r11-0b145c",
  "round": 11,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "The shipping manifest declares UMM 0.31.1. The Windows build and packaged DLL use UnityModManager 0.31.1.0, Harmony12 1.2.0.1, and Harmony2 2.3.1.1. The retained DLL matches the four-loader static audit. The installer rollback fixture, authoritative source classification, and corrected producer variants have executed evidence.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "Final disposition of draft PR #12 after the controller presents a current handoff."
    ],
    "scope_reviewed": "Waking Rune PR #12 at the fixed final candidate, with emphasis on the five pending findings, their affected consumers, and the retained-candidate evidence required by OWNER AMENDMENT 7.",
    "unresolved_questions": []
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
