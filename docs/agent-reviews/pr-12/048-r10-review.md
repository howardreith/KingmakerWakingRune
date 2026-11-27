<!-- agent-review:v1 enrollment=kwr-codex-umm-0311-baseline-20261123-20261123181024-1057 key=r10-review part=1/1 sha256=f8586487c0dd4d43e1e34644b9de6af9521d128a299b32a434f35f8239e85090 -->
### Codex GPT-6.2 Astra — review — round 10 — candidate `10ac278808`

**Decision:** `changes_required` — The configured gate passed, and the retained production DLL matches the four-loader static audit. The installer can still obstruct rollback after an input alias changes, the report writer can overwrite a moved build receipt, and the provenance check can accept the wrong producing commit. One gate fixture also reports a source deletion it never performs.

Continuation for Claude

Round 10 reaches the authorized review cap. Record the open findings and preserve the retained 6869e55 production pair, its receipt, the gate log and the focused results. Any further submission requires owner authorization. For an authorized continuation, correct the existing helpers together: allow deletion or rename while an unlocked reader establishes ownership, capture protected receipt identities with the ZIP snapshot, and derive material Git paths from verified compiler inputs with decoding specific to each input kind. Make each producer fixture prove its exact Git change before counting a refusal. The next review needs the rollback and receipt-preservation observations, isolated producer comparisons, and the configured gate on the corrected candidate. Preserve the file-symbolic-link NOT RUN disposition under OWNER AMENDMENT 6 and the existing native-testing limits.

#### Findings

**R9-F2** · P1 · defect · blocks readiness — An input reclassified from external can still obstruct publisher rollback
- Location: `scripts/KwrPackage.ps1:82`; acceptance: mission §6, OWNER AMENDMENT 2, rollback integrity, OWNER AMENDMENT 5, Waking package ownership
- Trigger: A directory junction initially selects an independent external ZIP, then points to the same-named final publication between classification and the external-branch open.
- Expected: A reader that has not acquired the canonical lock must allow the publisher to move its ZIP through placement and rollback.
- Actual: At line 82 the external branch reopens the requested path with FileShare.Read. If the junction now resolves to the canonical ZIP, this holds that ZIP without delete sharing. The identity recheck rejects the reader afterward, but Build.ps1 can already have failed its rollback move and left the previous pair in the recovery folder.
- Why it blocks / does not: A reader that ultimately refuses the package can prevent automatic restoration of the previous publication.
- Requested correction: Use delete sharing for a ZIP handle opened before ownership is confirmed. If that handle identifies a build-owned file, refuse it or acquire the canonical lock before admitting the pair.
- Requested evidence: In the existing disposable rollback fixture, redirect a directory junction after external classification and overlap the reopened reader with receipt-placement failure. Show reader refusal and both previous hashes restored at their final paths.

**R10-F1** · P1 · defect · blocks readiness — A linked ZIP's target receipt can move before its identity is protected
- Location: `scripts/Validate-Package.ps1:96`; acceptance: mission §6, OWNER AMENDMENT 6, protection of the actual ZIP and its receipt
- Trigger: Validate a ZIP through a hard link while its target's conventional receipt moves to an initially distinct ReportPath during the ZIP read.
- Expected: The checked report write protects the identity of the target's existing conventional receipt across a move during snapshot acquisition.
- Actual: The initial identity set contains the requested ZIP and its alias-side receipt. ReadAllBound returns the ZIP identity and physical names, but no receipt identities. If the target's receipt moves to the report destination before the first identity sample for those names, its old pathname is absent. WriteChecked then accepts the moved receipt's unrecorded identity and truncates it.
- Why it blocks / does not: A successful content-only validation can destroy the receipt belonging to the ZIP it inspected.
- Requested correction: Capture the identities of existing target receipts when establishing the ZIP's protected names, before copying its bytes, and retain them through WriteChecked. Declining an ambiguous linked-input report is also sufficient.
- Requested evidence: Extend the existing moved-input fixture to a hard-linked ZIP with ReceiptPath omitted. Move the target's conventional receipt to the report destination during the snapshot. Show refusal and unchanged ZIP and receipt hashes.

**R10-F2** · P2 · defect · blocks readiness — An unchecked receipt field controls which compiled sources Git compares
- Location: `scripts/Validate-Package.ps1:257`; acceptance: mission §5, material build inputs, OWNER AMENDMENT 5, compiler-source ownership and producing-commit comparison
- Trigger: A receipt records a tracked root compiler source as external while retaining its correct display name and content digest.
- Expected: Every repository-authored source selected by the current compiler inventory participates in the literal Git comparison with the claimed producer.
- Actual: The Git comparison takes repositoryPath from the supplied receipt. A null value drops a root source from materialPaths. The later live membership check only establishes that the source is tracked now. Both the inventory-text comparison and the fingerprint omit repositoryPath, so neither detects the discrepancy. A commit differing only in that source can receive verified-current.
- Why it blocks / does not: The standalone validator can attribute packaged code to a producing tree that lacks a compiled source.
- Requested correction: Derive the material source paths from the freshly evaluated compiler inventory, or verify the receipt's repository paths against that inventory before relying on them. Preserve generated and genuine external inputs.
- Requested evidence: Use the tracked root-source fixture with a receipt whose source classification disagrees with the live inventory and whose claimed producer differs only in that source. Show refusal and successful qualification of the genuine pair.

**R9-F4** · P2 · defect · blocks readiness — Reference-alias decoding still strips literal compiler input names
- Location: `scripts/KwrBuild.Common.ps1:126`; acceptance: mission §5, material build inputs, R9-F4, restrict reference-alias decoding to reference aliases
- Trigger: A supported compiler response file or file-valued option selects a tracked root filename beginning with an identifier and an equals sign.
- Expected: Only an actual reference alias is decoded. Literal response-file and file-option names retain their complete tracked spelling.
- Actual: The new compile branch preserves Rules=Root.cs. The remaining branch still removes an identifier followed by '=' from every response-file and file-option name. For a tracked root response file named Rules=Base.rsp, the lookup uses Base.rsp. The actual file can consequently be omitted from the producing-commit comparison.
- Why it blocks / does not: A receipt can name a producer missing a tracked response file that supplied compiler arguments.
- Requested correction: Decode each inventory entry according to its kind. Preserve literal equals signs in response-file and file-option paths when resolving Git's tracked spelling.
- Requested evidence: Use the existing compiler response-file route with a tracked root filename containing '='. Show that the genuine pair qualifies and a producer differing only in that file is refused.

**R10-F3** · P2 · defect · blocks readiness — The producer fixtures do not establish their advertised Git changes
- Location: `scripts/Test-LoaderBaseline.ps1:106`; acceptance: mission §6, realistic failure-boundary checks, OWNER AMENDMENT 2, fixtures must reach their claimed behavior, R9-F4 evidence
- Trigger: Run the missing Rules=Root.cs row or the two casing controls in the corrected candidate gate.
- Expected: Each negative producer fixture differs in exactly its named path, with the missing variant absent from the Git tree and the modified variant present.
- Actual: Windows PowerShell coerces $null to an empty string for New-VariantCommit's typed Content parameter. The helper therefore writes a blob at Rules=Root.cs in the row labeled missing. Both drivers accept the validator refusal without asserting absence. The revised gate also adds Rules=Root.cs after taking both CaseRoot.cs comparison commits, so each casing control differs in a second material file.
- Why it blocks / does not: The gate reports a deletion it did not exercise, and the casing rows can pass on an unrelated material difference.
- Requested correction: Represent deletion explicitly in the variant helper. Make the gate and focused checks verify the exact path, change type and tree membership before counting the validator result. Isolate the CaseRoot.cs variants from the equals-sign source.
- Requested evidence: Record the one-path Git differences for absent and modified variants alongside the validator refusals and genuine-pair success. Preserve the original run counts with an accurate explanation of the old rows.

#### Prior finding assessments

- **R5-F1** — `fix_verified` — The classifier distinguishes a final publication from its private staging and backup names. Gate rows 177–178 show refusal of the pre-existing hard link while the previous ZIP was in backup, followed by diagnostic qualification through the restored canonical pair with matching hashes.
- **R8-F2** — `rebuttal_accepted` — OWNER AMENDMENT 6 withdrew the requirement to execute the real Windows file-symbolic-link fixture before handoff. That was the outstanding observation in R8-F2. The test remains available and gate row 103 records the privilege error. For its specified ReportPath, the read handle supplies the physical target name, the validator protects its conventional receipt path, and WriteChecked checks the destination handle. R10-F1 covers the distinct case where the receipt moves to another pathname.
- **R9-F1** — `fix_verified` — The gate now copies the pending receipt for the newly placed ZIP and requires digest equality in the pass predicate. Rows 180–181 record a true match, refusal on the canonical lock, and refusal after lock release while the canonical receipt is absent.
- **R9-F2** — `fix_rejected` — The canonical-path control in row 179 restored both original hashes and refused the waiting reader. The external branch can still take the same rename-blocking handle before lock admission if the requested path changes classification.
- **R9-F3** — `fix_verified` — The validator obtains the physical ZIP name from the handle that supplies the snapshot bytes. Row 111 switches the input hard link before that read, refuses the second ZIP's receipt as ReportPath and confirms both second-pair hashes survived. The receipt-move ordering is recorded separately under R10-F1.
- **R9-F4** — `fix_rejected` — The literal Rules=Root.cs source now reaches the Git comparison, and the genuinely modified-source control was refused. The requested restriction of alias decoding to actual reference aliases remains incomplete for response files and file-valued options. R10-F3 records the separate fixture defect.
- **R9-F5** — `fix_verified` — Both the fingerprint and the validator's fixed material paths now include scripts/KwrFileIdentity.ps1. Gate row 198 records refusal of a producer differing only in that helper. The genuine pair qualified.
- **R9-F6** — `fix_verified` — The installer and report validator use the physical name directly for a single-link file. With enumeration deliberately unavailable, row 106 validated and reported the independent copy, admitted it for content-only staging, and refused report generation for the multi-link control.
- **R9-F7** — `fix_verified` — The focused record identifies worktrees\umm-0311-final-pair-6869e55\producer and records verified-current after disposable cleanup. The clean 6869e55 receipt agrees on ZIP 3d7804a9 and DLL 7cd3cb09. The recorded raw receipt hash begins a79b0d29, and the DLL MVID ccc59117-f992-44ce-9355-540d35f181f0 matches the static audit.

#### Evidence assessment

- **Inspected:** Verified the candidate, merge base, mission and amendment digests. The full 13,171-line supplied feature diff matches Git. Read the repository instructions, pending ledger, developer dispositions and controller gate receipt and log.; Compared the round-10 changes with 855a86a and traced their consumers in Build.ps1, KwrPackage.ps1, KwrFileIdentity.ps1, KwrBuild.Common.ps1, Validate-Package.ps1 and Test-LoaderBaseline.ps1. The changes from production commit 6869e55 to review head 10ac278 are documentation and evidence.; Inspected the focused driver and export, retained receipt, compiler-reference pins and guards, package checks, and four-profile static-audit records.
- **Executed verification:** The controller's baseline gate at 10ac278 exited 0 in 639.9 seconds. Its log hash matches the receipt. I counted 166 PASS rows and one NOT RUN row. The cold-cache shipping build passed standalone current-provenance validation, and the alternate-version build recorded 290 tests passed.; The committed focused record reports 10/10 at 6869e55 and successful current-provenance validation after disposable-clone cleanup. I cross-checked its producer, ZIP and DLL digests against the exported receipt, its DLL digest and MVID against the static audit, and all 29 indexed evidence-blob hashes against Git.; A read-only Windows PowerShell 5.1 parameter-binding check confirmed that the fixture's [AllowNull()][string] parameter receives an empty string for $null and takes the blob-writing branch. A separate string check confirmed that the current decoder reduces @Rules=Base.rsp to Base.rsp.
- **Source inspection only:** The unlocked external read, moved-receipt overwrite and incorrect producer attribution follow from the candidate's control flow. The recorded gate exercised different file orderings and receipt contents.; For the original file-symbolic-link scenario, physical target resolution and the checked destination write were inspected in source. The executed hard-link test establishes its own result.
- **Required gaps:** Show successful rollback with both previous hashes intact when an initially external package path changes to the final publication, and preservation of a linked target's receipt when it moves during the ZIP snapshot.; Show that current compiler ownership and literal response-file paths participate in the producing-commit comparison. Record the exact Git tree difference for each missing-source and changed-source control.
- **Limitations:** The foreground session transcript was withheld. The controller receipt establishes the candidate gate run. The retained pair is outside the review snapshot, so its post-cleanup observation comes from the indexed focused record and receipt.; The real Windows file-symbolic-link case remains in the suite and was recorded as NOT RUN with “Administrator privilege required for this operation.” OWNER AMENDMENT 6 accepts that execution limitation.; Native gameplay and combined-stack testing were NOT RUN. The compatibility evidence for 0.31.1, 0.32.4, owner-installed 0.33.0 and 0.33.4 is static.

#### Summary

- **Delivered behavior:** The shipping manifest declares UMM 0.31.1. The verified production build references UnityModManager 0.31.1.0, Harmony12 1.2.0.1 and Harmony2 2.3.1.1. The retained package and four-profile static audit identify the same DLL.
- **Scope reviewed:** Waking Rune PR #12 at the final checkpoint, including all nine pending findings, the round-10 corrections and their directly affected build, validation and installation paths under owner amendment revision 6.
- **Pending later-phase work:** none
- **Unresolved questions:** none
- **Remaining owner review:** Decide whether to authorize a further bounded correction and review for PR #12 after the tenth submission.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: revision 6, sha256 `d54c6068e1a449dd169858d8981a6070abc050ed2b18249dd1b1055044e3413d`.

Codex CLI `0.170.0`, thread `01a21b80-7c6c-7f11-b19b-d5588b9f8df6`, duration 763.3s, response sha256 `bec105143e6b9e4f0d53ca8e252eecf1e0ab7020449d96dc35bbd6c206cf667f`, request `kwr-codex-umm-0311-baseline-20261123-20261123181024-1057-r10-52a1bd`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "10ac278808e71e2138110519364358f13ab9d62b",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The configured gate passed, and the retained production DLL matches the four-loader static audit. The installer can still obstruct rollback after an input alias changes, the report writer can overwrite a moved build receipt, and the provenance check can accept the wrong producing commit. One gate fixture also reports a source deletion it never performs.\n\nContinuation for Claude\n\nRound 10 reaches the authorized review cap. Record the open findings and preserve the retained 6869e55 production pair, its receipt, the gate log and the focused results. Any further submission requires owner authorization. For an authorized continuation, correct the existing helpers together: allow deletion or rename while an unlocked reader establishes ownership, capture protected receipt identities with the ZIP snapshot, and derive material Git paths from verified compiler inputs with decoding specific to each input kind. Make each producer fixture prove its exact Git change before counting a refusal. The next review needs the rollback and receipt-preservation observations, isolated producer comparisons, and the configured gate on the corrected candidate. Preserve the file-symbolic-link NOT RUN disposition under OWNER AMENDMENT 6 and the existing native-testing limits.",
  "evidence_assessment": {
    "executed_verification": [
      "The controller's baseline gate at 10ac278 exited 0 in 639.9 seconds. Its log hash matches the receipt. I counted 166 PASS rows and one NOT RUN row. The cold-cache shipping build passed standalone current-provenance validation, and the alternate-version build recorded 290 tests passed.",
      "The committed focused record reports 10/10 at 6869e55 and successful current-provenance validation after disposable-clone cleanup. I cross-checked its producer, ZIP and DLL digests against the exported receipt, its DLL digest and MVID against the static audit, and all 29 indexed evidence-blob hashes against Git.",
      "A read-only Windows PowerShell 5.1 parameter-binding check confirmed that the fixture's [AllowNull()][string] parameter receives an empty string for $null and takes the blob-writing branch. A separate string check confirmed that the current decoder reduces @Rules=Base.rsp to Base.rsp."
    ],
    "inspected": [
      "Verified the candidate, merge base, mission and amendment digests. The full 13,171-line supplied feature diff matches Git. Read the repository instructions, pending ledger, developer dispositions and controller gate receipt and log.",
      "Compared the round-10 changes with 855a86a and traced their consumers in Build.ps1, KwrPackage.ps1, KwrFileIdentity.ps1, KwrBuild.Common.ps1, Validate-Package.ps1 and Test-LoaderBaseline.ps1. The changes from production commit 6869e55 to review head 10ac278 are documentation and evidence.",
      "Inspected the focused driver and export, retained receipt, compiler-reference pins and guards, package checks, and four-profile static-audit records."
    ],
    "limitations": [
      "The foreground session transcript was withheld. The controller receipt establishes the candidate gate run. The retained pair is outside the review snapshot, so its post-cleanup observation comes from the indexed focused record and receipt.",
      "The real Windows file-symbolic-link case remains in the suite and was recorded as NOT RUN with \u201cAdministrator privilege required for this operation.\u201d OWNER AMENDMENT 6 accepts that execution limitation.",
      "Native gameplay and combined-stack testing were NOT RUN. The compatibility evidence for 0.31.1, 0.32.4, owner-installed 0.33.0 and 0.33.4 is static."
    ],
    "required_gaps": [
      "Show successful rollback with both previous hashes intact when an initially external package path changes to the final publication, and preservation of a linked target's receipt when it moves during the ZIP snapshot.",
      "Show that current compiler ownership and literal response-file paths participate in the producing-commit comparison. Record the exact Git tree difference for each missing-source and changed-source control."
    ],
    "source_inspection_only": [
      "The unlocked external read, moved-receipt overwrite and incorrect producer attribution follow from the candidate's control flow. The recorded gate exercised different file orderings and receipt contents.",
      "For the original file-symbolic-link scenario, physical target resolution and the checked destination write were inspected in source. The executed hard-link test establishes its own result."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "mission \u00a76",
        "OWNER AMENDMENT 2, rollback integrity",
        "OWNER AMENDMENT 5, Waking package ownership"
      ],
      "actual": "At line 82 the external branch reopens the requested path with FileShare.Read. If the junction now resolves to the canonical ZIP, this holds that ZIP without delete sharing. The identity recheck rejects the reader afterward, but Build.ps1 can already have failed its rollback move and left the previous pair in the recovery folder.",
      "blocking_rationale": "A reader that ultimately refuses the package can prevent automatic restoration of the previous publication.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "A reader that has not acquired the canonical lock must allow the publisher to move its ZIP through placement and rollback.",
      "id": "R9-F2",
      "location": {
        "file": "scripts/KwrPackage.ps1",
        "line": 82
      },
      "priority": "P1",
      "requested_correction": "Use delete sharing for a ZIP handle opened before ownership is confirmed. If that handle identifies a build-owned file, refuse it or acquire the canonical lock before admitting the pair.",
      "requested_evidence": "In the existing disposable rollback fixture, redirect a directory junction after external classification and overlap the reopened reader with receipt-placement failure. Show reader refusal and both previous hashes restored at their final paths.",
      "title": "An input reclassified from external can still obstruct publisher rollback",
      "trigger": "A directory junction initially selects an independent external ZIP, then points to the same-named final publication between classification and the external-branch open."
    },
    {
      "acceptance_ids": [
        "mission \u00a76",
        "OWNER AMENDMENT 6, protection of the actual ZIP and its receipt"
      ],
      "actual": "The initial identity set contains the requested ZIP and its alias-side receipt. ReadAllBound returns the ZIP identity and physical names, but no receipt identities. If the target's receipt moves to the report destination before the first identity sample for those names, its old pathname is absent. WriteChecked then accepts the moved receipt's unrecorded identity and truncates it.",
      "blocking_rationale": "A successful content-only validation can destroy the receipt belonging to the ZIP it inspected.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The checked report write protects the identity of the target's existing conventional receipt across a move during snapshot acquisition.",
      "id": "R10-F1",
      "location": {
        "file": "scripts/Validate-Package.ps1",
        "line": 96
      },
      "priority": "P1",
      "requested_correction": "Capture the identities of existing target receipts when establishing the ZIP's protected names, before copying its bytes, and retain them through WriteChecked. Declining an ambiguous linked-input report is also sufficient.",
      "requested_evidence": "Extend the existing moved-input fixture to a hard-linked ZIP with ReceiptPath omitted. Move the target's conventional receipt to the report destination during the snapshot. Show refusal and unchanged ZIP and receipt hashes.",
      "title": "A linked ZIP's target receipt can move before its identity is protected",
      "trigger": "Validate a ZIP through a hard link while its target's conventional receipt moves to an initially distinct ReportPath during the ZIP read."
    },
    {
      "acceptance_ids": [
        "mission \u00a75, material build inputs",
        "OWNER AMENDMENT 5, compiler-source ownership and producing-commit comparison"
      ],
      "actual": "The Git comparison takes repositoryPath from the supplied receipt. A null value drops a root source from materialPaths. The later live membership check only establishes that the source is tracked now. Both the inventory-text comparison and the fingerprint omit repositoryPath, so neither detects the discrepancy. A commit differing only in that source can receive verified-current.",
      "blocking_rationale": "The standalone validator can attribute packaged code to a producing tree that lacks a compiled source.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Every repository-authored source selected by the current compiler inventory participates in the literal Git comparison with the claimed producer.",
      "id": "R10-F2",
      "location": {
        "file": "scripts/Validate-Package.ps1",
        "line": 257
      },
      "priority": "P2",
      "requested_correction": "Derive the material source paths from the freshly evaluated compiler inventory, or verify the receipt's repository paths against that inventory before relying on them. Preserve generated and genuine external inputs.",
      "requested_evidence": "Use the tracked root-source fixture with a receipt whose source classification disagrees with the live inventory and whose claimed producer differs only in that source. Show refusal and successful qualification of the genuine pair.",
      "title": "An unchecked receipt field controls which compiled sources Git compares",
      "trigger": "A receipt records a tracked root compiler source as external while retaining its correct display name and content digest."
    },
    {
      "acceptance_ids": [
        "mission \u00a75, material build inputs",
        "R9-F4, restrict reference-alias decoding to reference aliases"
      ],
      "actual": "The new compile branch preserves Rules=Root.cs. The remaining branch still removes an identifier followed by '=' from every response-file and file-option name. For a tracked root response file named Rules=Base.rsp, the lookup uses Base.rsp. The actual file can consequently be omitted from the producing-commit comparison.",
      "blocking_rationale": "A receipt can name a producer missing a tracked response file that supplied compiler arguments.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Only an actual reference alias is decoded. Literal response-file and file-option names retain their complete tracked spelling.",
      "id": "R9-F4",
      "location": {
        "file": "scripts/KwrBuild.Common.ps1",
        "line": 126
      },
      "priority": "P2",
      "requested_correction": "Decode each inventory entry according to its kind. Preserve literal equals signs in response-file and file-option paths when resolving Git's tracked spelling.",
      "requested_evidence": "Use the existing compiler response-file route with a tracked root filename containing '='. Show that the genuine pair qualifies and a producer differing only in that file is refused.",
      "title": "Reference-alias decoding still strips literal compiler input names",
      "trigger": "A supported compiler response file or file-valued option selects a tracked root filename beginning with an identifier and an equals sign."
    },
    {
      "acceptance_ids": [
        "mission \u00a76, realistic failure-boundary checks",
        "OWNER AMENDMENT 2, fixtures must reach their claimed behavior",
        "R9-F4 evidence"
      ],
      "actual": "Windows PowerShell coerces $null to an empty string for New-VariantCommit's typed Content parameter. The helper therefore writes a blob at Rules=Root.cs in the row labeled missing. Both drivers accept the validator refusal without asserting absence. The revised gate also adds Rules=Root.cs after taking both CaseRoot.cs comparison commits, so each casing control differs in a second material file.",
      "blocking_rationale": "The gate reports a deletion it did not exercise, and the casing rows can pass on an unrelated material difference.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Each negative producer fixture differs in exactly its named path, with the missing variant absent from the Git tree and the modified variant present.",
      "id": "R10-F3",
      "location": {
        "file": "scripts/Test-LoaderBaseline.ps1",
        "line": 106
      },
      "priority": "P2",
      "requested_correction": "Represent deletion explicitly in the variant helper. Make the gate and focused checks verify the exact path, change type and tree membership before counting the validator result. Isolate the CaseRoot.cs variants from the equals-sign source.",
      "requested_evidence": "Record the one-path Git differences for absent and modified variants alongside the validator refusals and genuine-pair success. Preserve the original run counts with an accurate explanation of the old rows.",
      "title": "The producer fixtures do not establish their advertised Git changes",
      "trigger": "Run the missing Rules=Root.cs row or the two casing controls in the corrected candidate gate."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_verified",
      "finding_id": "R5-F1",
      "rationale": "The classifier distinguishes a final publication from its private staging and backup names. Gate rows 177\u2013178 show refusal of the pre-existing hard link while the previous ZIP was in backup, followed by diagnostic qualification through the restored canonical pair with matching hashes."
    },
    {
      "assessment": "rebuttal_accepted",
      "finding_id": "R8-F2",
      "rationale": "OWNER AMENDMENT 6 withdrew the requirement to execute the real Windows file-symbolic-link fixture before handoff. That was the outstanding observation in R8-F2. The test remains available and gate row 103 records the privilege error. For its specified ReportPath, the read handle supplies the physical target name, the validator protects its conventional receipt path, and WriteChecked checks the destination handle. R10-F1 covers the distinct case where the receipt moves to another pathname."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R9-F1",
      "rationale": "The gate now copies the pending receipt for the newly placed ZIP and requires digest equality in the pass predicate. Rows 180\u2013181 record a true match, refusal on the canonical lock, and refusal after lock release while the canonical receipt is absent."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R9-F2",
      "rationale": "The canonical-path control in row 179 restored both original hashes and refused the waiting reader. The external branch can still take the same rename-blocking handle before lock admission if the requested path changes classification."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R9-F3",
      "rationale": "The validator obtains the physical ZIP name from the handle that supplies the snapshot bytes. Row 111 switches the input hard link before that read, refuses the second ZIP's receipt as ReportPath and confirms both second-pair hashes survived. The receipt-move ordering is recorded separately under R10-F1."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R9-F4",
      "rationale": "The literal Rules=Root.cs source now reaches the Git comparison, and the genuinely modified-source control was refused. The requested restriction of alias decoding to actual reference aliases remains incomplete for response files and file-valued options. R10-F3 records the separate fixture defect."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R9-F5",
      "rationale": "Both the fingerprint and the validator's fixed material paths now include scripts/KwrFileIdentity.ps1. Gate row 198 records refusal of a producer differing only in that helper. The genuine pair qualified."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R9-F6",
      "rationale": "The installer and report validator use the physical name directly for a single-link file. With enumeration deliberately unavailable, row 106 validated and reported the independent copy, admitted it for content-only staging, and refused report generation for the multi-link control."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R9-F7",
      "rationale": "The focused record identifies worktrees\\umm-0311-final-pair-6869e55\\producer and records verified-current after disposable cleanup. The clean 6869e55 receipt agrees on ZIP 3d7804a9 and DLL 7cd3cb09. The recorded raw receipt hash begins a79b0d29, and the DLL MVID ccc59117-f992-44ce-9355-540d35f181f0 matches the static audit."
    }
  ],
  "request_id": "kwr-codex-umm-0311-baseline-20261123-20261123181024-1057-r10-52a1bd",
  "round": 10,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "The shipping manifest declares UMM 0.31.1. The verified production build references UnityModManager 0.31.1.0, Harmony12 1.2.0.1 and Harmony2 2.3.1.1. The retained package and four-profile static audit identify the same DLL.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "Decide whether to authorize a further bounded correction and review for PR #12 after the tenth submission."
    ],
    "scope_reviewed": "Waking Rune PR #12 at the final checkpoint, including all nine pending findings, the round-10 corrections and their directly affected build, validation and installation paths under owner amendment revision 6.",
    "unresolved_questions": []
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
