<!-- agent-review:v1 enrollment=kwr-codex-umm-0311-baseline-20261123-20261123181024-1057 key=r06-review part=1/1 sha256=346b1e40b6337a025f70d9925802877890d5fd09420a3a531d7a0276c7709a12 -->
### Codex GPT-6.2 Astra — review — round 6 — candidate `495635d7e5`

**Decision:** `changes_required` — The candidate passed all 130 baseline checks. Three prior defects remain. Ignored compiler inputs can receive clean provenance, a build-owned ZIP can enter installation without its receipt, and recovery can disable recon for a previous-format journal.

Continuation for Claude

Bind authored compiler inputs to the producing Git tree, including ignored sources and artwork matched with Windows filename casing. At installation, recognize the physical build destination across alternate path spellings and require a matching receipt for a build-owned ZIP. During recovery, use the legacy journal’s recorded recon value when the replacement folder has already been placed. Extend the existing temporary fixtures with the cases in R1-F1, R5-F1 and R4-F4, then run the configured baseline gate at the corrected commit within OWNER AMENDMENT 2’s allowance. The corrections fit the existing validation, package-copy and journal-recovery code.

#### Findings

**R1-F1** · P1 · defect · blocks readiness — Ignored Windows compiler inputs can still receive clean provenance
- Location: `scripts/Validate-Package.ps1:238`; acceptance: mission §5, mission §6
- Trigger: Build with an ignored uppercase-extension portrait or an ignored C# file selected by the production Compile wildcard.
- Expected: Clean qualification binds all authored sources and embedded images selected by the production project to the recorded Git tree.
- Actual: The new Git globs match .png case-sensitively, while the Windows project also embeds .PNG. The ignored-source check still uses --exclude-standard, and Get-KwrInventoryRepositoryPaths retains only tracked files. A local source under Blueprints/obj matches the project’s recursive Compile item because its obj exclusion is relative to the project root. Both compiler inventories can therefore agree on bytes absent from the recorded commit.
- Why it blocks / does not: A fresh checkout of a commit identified as clean can produce a different DLL from the frozen candidate.
- Requested correction: Match the artwork with Windows case semantics in both the tree comparison and untracked-file check. Check repository-authored Compile inputs against Git independently of ignore rules, while preserving the existing handling of generated files and external references.
- Requested evidence: In the temporary checkout, exercise an ignored assets/portraits/obj/LocalOnly.PNG and an ignored src/KingmakerWakingRune/Blueprints/obj/LocalOnly.cs. Show that neither can enter a clean frozen package. Retain the restored-build, committed-icon and documentation-only controls.

**R5-F1** · P1 · defect · blocks readiness — A build-owned ZIP can still enter receiptless install mode
- Location: `scripts/KwrPackage.ps1:19`; acceptance: mission §5, mission §6
- Trigger: Read a dirty ZIP through an alternate spelling of its publication directory between placements, or read it after the publisher exits before placing the receipt.
- Expected: A reader of a build-owned destination obtains a complete matching pair or declines the installation.
- Actual: The installer decides whether to take the publish lock using a string prefix. An 8.3 spelling of the same artifacts directory misses that prefix. Between ZIP and receipt placement, the helper can then copy the dirty ZIP and return external-content-inspected. Even on the ordinary path, an interrupted publisher can leave a ZIP without its receipt, and the helper takes the same content-only branch once the lock closes.
- Why it blocks / does not: Begin and Reinstall can admit a partially published dirty package in default mode.
- Requested correction: Recognize the actual publication directory across alternate path spellings and decline a build-owned output whose receipt is missing. Preserve content-only inspection for a genuinely external receiptless ZIP.
- Requested evidence: Use the paused publication fixture to call the real helper through a second spelling of the same directory. Also test the receiptless dirty output with the lock released. Both must decline installation, while the restored pair and genuine external ZIP retain their recorded modes.

**R4-F4** · P2 · defect · blocks readiness — Legacy recovery loses recon after the replacement folder is placed
- Location: `scripts/KwrRuntime.ps1:259`; acceptance: mission §6, mission §7
- Trigger: Resume a previous-format reinstall interrupted after moving the replacement folder and before restoring its recon flag.
- Expected: Recovery preserves the recon value recorded in the interrupted transaction through replacement and launch admission.
- Actual: The previous writer moves the staged folder into place, verifies the DLL, then writes recon.flag. An interruption during verification leaves the new directory present without the flag. The fallback at line 259 reads legacy recon only when the entire directory is absent. Recovery therefore persists reconEnabled=false and can admit launch with recon disabled.
- Why it blocks / does not: Recovery can silently change the recorded diagnostic mode before launch.
- Requested correction: For a reinstalling journal, prefer reconEnabled and fall back to the previous format’s recon field. Use the installed flag for an ordinary reinstall where it describes the current installation.
- Requested evidence: Extend the existing legacy-journal fixture with the replacement directory present and recon.flag absent. Verify the true and false journal values, resulting flag and persisted reconEnabled before the fixture admits launch.

**R6-F1** · P3 · optional_polish · non-blocking — The reproduction instructions retain the round-five check count
- Location: `docs/UMM-COMPATIBILITY.md:137`; acceptance: mission §8
- Trigger: Read the reproduction section for the round-six candidate.
- Expected: Readers can distinguish the current gate count from the historical result.
- Actual: The reproduction section describes the suite as having 120 checks. That count belongs to round five. The candidate receipt records 130.
- Why it blocks / does not: The controller receipt and dated historical record establish the respective results.
- Requested correction: Qualify 120 as the round-five count or omit a fixed count from the general reproduction instructions.
- Requested evidence: A documentation comparison with the dated gate records is sufficient.

#### Prior finding assessments

- **R1-F1** — `fix_rejected` — The gate now rejects the lowercase ignored portrait and accepts an artwork-directory documentation change. Mixed-case artwork and ignored authored sources still escape the recorded-commit comparison.
- **R4-F1** — `fix_verified` — Two overlapping builds produced distinct DLL digests, 7cd3cb09… and 4e16f623…. The second publisher was refused while the first held the lock. The surviving receipt bound the first compile to the surviving ZIP, and standalone validation verified it under the winner’s settings.
- **R4-F2** — `fix_verified` — One test-enabled build exercised the active OutputPath redirect with stale passing runners in both locations. Their digests survived, the invocation-owned runner reported 290 passes, and the receipt identified that invocation’s compiled DLL.
- **R4-F3** — `fix_verified` — ReadAll obtains each supplied input’s bytes and identity through one handle. The gate moved the actual replacement snapshot source onto the report destination between samples. The checked write refused it and preserved its bytes.
- **R4-F4** — `fix_rejected` — Both missing-directory controls pass. The previous writer can also leave a replacement directory without recon.flag. In that state the new fallback still loses a recorded true value.
- **R5-F1** — `fix_rejected` — The gate proves refusal during canonical-path publication and correct admission after rollback. The same physical destination can still skip the lock through an alternate spelling, and a missing sidecar at a build-owned output still selects external mode.
- **R5-F2** — `fix_verified` — A competing lock now causes immediate refusal. The publisher rechecks HEAD and the material fingerprint under the acquired lock before moving either file. The gate changed a tracked source after validation and confirmed refusal with both previous hashes preserved.
- **R5-F3** — `deferral_accepted` — OWNER AMENDMENT 2 explicitly keeps the singleton-array pin-field issue optional. The checked-in archive fields have the intended scalar types and the archive and DLL byte checks remain effective.

#### Evidence assessment

- **Inspected:** The effective mission, both owner amendments, all eight pending ledger entries, developer report, transcript notice, controller receipt and gate log.; The feature diff, round-six changes since 5432227, production project, pin definitions, build and validation scripts, installer callers, previous journal writer, regression fixtures, README and compatibility instructions.; The historical static audit records 14 type references and 39 member references resolved on UMM 0.31.1, 0.32.4, owner-installed 0.33.0 and 0.33.4. Its DLL digest matches the round-six shipping DLL.
- **Executed verification:** Verified HEAD and the mission and amendment hashes. The baseline gate receipt names the exact candidate, records exit 0 in 515 seconds, and matches the supplied log’s SHA-256. The log reports 130/130 checks passed.; The gate cold-built shipping version 0.12.3 and validated ZIP 78b0230fca0de5da691a145c54296eb378a2b39bcfa8c5ca8c7aa074ba5da4f1 with its receipt. The packaged DLL SHA-256 was 7cd3cb09f12f75a7247c1b401673528a0773a139e59dc3a940da860c28f24904. Direct MSBuild resolved the three pinned paths, and the redirected test run reported 290 passed.; Read-only probes confirmed that the Git artwork glob was case-sensitive on this checkout despite core.ignorecase=true. Windows enumeration found the existing icon using *.PNG. Git also confirmed that both example local inputs in R1-F1 are ignored.; Read the same manifest through long and 8.3 paths and verified equal digests. The provider retained the short spelling, which failed the installer’s ownership-prefix test. Separately, evaluating the recon selection against an existing directory without a flag returned false for a legacy journal recording true.
- **Source inspection only:** Traced the ignored inputs through the compiler inventory, tracked-file filter and commit comparison. The candidate gate exercised the lowercase PNG case.; Traced the installer’s receiptless branch and the previous writer’s placement order. The read-only probes exercised the relevant decisions without performing an installation or recovery transaction.
- **Required gaps:** R1-F1 needs frozen-build refusal for ignored inputs that Windows actually compiles, including mixed-case artwork and an authored source.; R5-F1 needs installer refusal for an incomplete build-owned pair reached through an alternate path and for a receiptless build output after the lock closes.; R4-F4 needs recovery evidence for a previous-format journal with the replacement directory already present and its recon flag still unwritten.
- **Limitations:** The reviewer used read-only inspection and probes. The controller receipt supplies the candidate build and gate evidence. The developer’s two earlier failed runs are reported separately and their whole-session transcript was withheld.; Native gameplay, fresh-process save/reload and combined-stack qualification remain NOT RUN. The journal fixtures stop before Steam.; OWNER AMENDMENT 1 accepts the documented historical gap in pre-enrollment check-project evidence. The retained later profile comparison and preflight establish the effective configuration.

#### Summary

- **Delivered behavior:** The shipping manifest declares UMM 0.31.1. The packaged DLL references UnityModManager 0.31.1.0, 0Harmony12 1.2.0.1 and 0Harmony 2.3.1.1. The gate verifies the competing-publisher, redirected-runner, handle-bound snapshot and late-source-change corrections.
- **Scope reviewed:** Final correction review of Waking Rune draft PR #12 at 495635d7e514cd99125a8b98d21250c75bd3e786 under both owner amendments, covering all eight pending findings and the affected build, validation, installer and recovery paths.
- **Pending later-phase work:** none
- **Unresolved questions:** none
- **Remaining owner review:** Once the three blocking findings clear, the owner can judge the draft with static compatibility evidence and native qualification marked NOT RUN.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: revision 2, sha256 `caabe8443864f7dd5aea0f2812b035a2ffd98a20a5834c8769281cf6f3268700`.

Codex CLI `0.170.0`, thread `01a21850-698e-7641-8214-25d870d8e4f1`, duration 782.1s, response sha256 `106682c0ce77155e909f5d5a1eca1b46181b15dddbd10fd7a553837a8faf17d0`, request `kwr-codex-umm-0311-baseline-20261123-20261123181024-1057-r06-a54f87`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "495635d7e514cd99125a8b98d21250c75bd3e786",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The candidate passed all 130 baseline checks. Three prior defects remain. Ignored compiler inputs can receive clean provenance, a build-owned ZIP can enter installation without its receipt, and recovery can disable recon for a previous-format journal.\n\nContinuation for Claude\n\nBind authored compiler inputs to the producing Git tree, including ignored sources and artwork matched with Windows filename casing. At installation, recognize the physical build destination across alternate path spellings and require a matching receipt for a build-owned ZIP. During recovery, use the legacy journal\u2019s recorded recon value when the replacement folder has already been placed. Extend the existing temporary fixtures with the cases in R1-F1, R5-F1 and R4-F4, then run the configured baseline gate at the corrected commit within OWNER AMENDMENT 2\u2019s allowance. The corrections fit the existing validation, package-copy and journal-recovery code.",
  "evidence_assessment": {
    "executed_verification": [
      "Verified HEAD and the mission and amendment hashes. The baseline gate receipt names the exact candidate, records exit 0 in 515 seconds, and matches the supplied log\u2019s SHA-256. The log reports 130/130 checks passed.",
      "The gate cold-built shipping version 0.12.3 and validated ZIP 78b0230fca0de5da691a145c54296eb378a2b39bcfa8c5ca8c7aa074ba5da4f1 with its receipt. The packaged DLL SHA-256 was 7cd3cb09f12f75a7247c1b401673528a0773a139e59dc3a940da860c28f24904. Direct MSBuild resolved the three pinned paths, and the redirected test run reported 290 passed.",
      "Read-only probes confirmed that the Git artwork glob was case-sensitive on this checkout despite core.ignorecase=true. Windows enumeration found the existing icon using *.PNG. Git also confirmed that both example local inputs in R1-F1 are ignored.",
      "Read the same manifest through long and 8.3 paths and verified equal digests. The provider retained the short spelling, which failed the installer\u2019s ownership-prefix test. Separately, evaluating the recon selection against an existing directory without a flag returned false for a legacy journal recording true."
    ],
    "inspected": [
      "The effective mission, both owner amendments, all eight pending ledger entries, developer report, transcript notice, controller receipt and gate log.",
      "The feature diff, round-six changes since 5432227, production project, pin definitions, build and validation scripts, installer callers, previous journal writer, regression fixtures, README and compatibility instructions.",
      "The historical static audit records 14 type references and 39 member references resolved on UMM 0.31.1, 0.32.4, owner-installed 0.33.0 and 0.33.4. Its DLL digest matches the round-six shipping DLL."
    ],
    "limitations": [
      "The reviewer used read-only inspection and probes. The controller receipt supplies the candidate build and gate evidence. The developer\u2019s two earlier failed runs are reported separately and their whole-session transcript was withheld.",
      "Native gameplay, fresh-process save/reload and combined-stack qualification remain NOT RUN. The journal fixtures stop before Steam.",
      "OWNER AMENDMENT 1 accepts the documented historical gap in pre-enrollment check-project evidence. The retained later profile comparison and preflight establish the effective configuration."
    ],
    "required_gaps": [
      "R1-F1 needs frozen-build refusal for ignored inputs that Windows actually compiles, including mixed-case artwork and an authored source.",
      "R5-F1 needs installer refusal for an incomplete build-owned pair reached through an alternate path and for a receiptless build output after the lock closes.",
      "R4-F4 needs recovery evidence for a previous-format journal with the replacement directory already present and its recon flag still unwritten."
    ],
    "source_inspection_only": [
      "Traced the ignored inputs through the compiler inventory, tracked-file filter and commit comparison. The candidate gate exercised the lowercase PNG case.",
      "Traced the installer\u2019s receiptless branch and the previous writer\u2019s placement order. The read-only probes exercised the relevant decisions without performing an installation or recovery transaction."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "mission \u00a75",
        "mission \u00a76"
      ],
      "actual": "The new Git globs match .png case-sensitively, while the Windows project also embeds .PNG. The ignored-source check still uses --exclude-standard, and Get-KwrInventoryRepositoryPaths retains only tracked files. A local source under Blueprints/obj matches the project\u2019s recursive Compile item because its obj exclusion is relative to the project root. Both compiler inventories can therefore agree on bytes absent from the recorded commit.",
      "blocking_rationale": "A fresh checkout of a commit identified as clean can produce a different DLL from the frozen candidate.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Clean qualification binds all authored sources and embedded images selected by the production project to the recorded Git tree.",
      "id": "R1-F1",
      "location": {
        "file": "scripts/Validate-Package.ps1",
        "line": 238
      },
      "priority": "P1",
      "requested_correction": "Match the artwork with Windows case semantics in both the tree comparison and untracked-file check. Check repository-authored Compile inputs against Git independently of ignore rules, while preserving the existing handling of generated files and external references.",
      "requested_evidence": "In the temporary checkout, exercise an ignored assets/portraits/obj/LocalOnly.PNG and an ignored src/KingmakerWakingRune/Blueprints/obj/LocalOnly.cs. Show that neither can enter a clean frozen package. Retain the restored-build, committed-icon and documentation-only controls.",
      "title": "Ignored Windows compiler inputs can still receive clean provenance",
      "trigger": "Build with an ignored uppercase-extension portrait or an ignored C# file selected by the production Compile wildcard."
    },
    {
      "acceptance_ids": [
        "mission \u00a75",
        "mission \u00a76"
      ],
      "actual": "The installer decides whether to take the publish lock using a string prefix. An 8.3 spelling of the same artifacts directory misses that prefix. Between ZIP and receipt placement, the helper can then copy the dirty ZIP and return external-content-inspected. Even on the ordinary path, an interrupted publisher can leave a ZIP without its receipt, and the helper takes the same content-only branch once the lock closes.",
      "blocking_rationale": "Begin and Reinstall can admit a partially published dirty package in default mode.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "A reader of a build-owned destination obtains a complete matching pair or declines the installation.",
      "id": "R5-F1",
      "location": {
        "file": "scripts/KwrPackage.ps1",
        "line": 19
      },
      "priority": "P1",
      "requested_correction": "Recognize the actual publication directory across alternate path spellings and decline a build-owned output whose receipt is missing. Preserve content-only inspection for a genuinely external receiptless ZIP.",
      "requested_evidence": "Use the paused publication fixture to call the real helper through a second spelling of the same directory. Also test the receiptless dirty output with the lock released. Both must decline installation, while the restored pair and genuine external ZIP retain their recorded modes.",
      "title": "A build-owned ZIP can still enter receiptless install mode",
      "trigger": "Read a dirty ZIP through an alternate spelling of its publication directory between placements, or read it after the publisher exits before placing the receipt."
    },
    {
      "acceptance_ids": [
        "mission \u00a76",
        "mission \u00a77"
      ],
      "actual": "The previous writer moves the staged folder into place, verifies the DLL, then writes recon.flag. An interruption during verification leaves the new directory present without the flag. The fallback at line 259 reads legacy recon only when the entire directory is absent. Recovery therefore persists reconEnabled=false and can admit launch with recon disabled.",
      "blocking_rationale": "Recovery can silently change the recorded diagnostic mode before launch.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Recovery preserves the recon value recorded in the interrupted transaction through replacement and launch admission.",
      "id": "R4-F4",
      "location": {
        "file": "scripts/KwrRuntime.ps1",
        "line": 259
      },
      "priority": "P2",
      "requested_correction": "For a reinstalling journal, prefer reconEnabled and fall back to the previous format\u2019s recon field. Use the installed flag for an ordinary reinstall where it describes the current installation.",
      "requested_evidence": "Extend the existing legacy-journal fixture with the replacement directory present and recon.flag absent. Verify the true and false journal values, resulting flag and persisted reconEnabled before the fixture admits launch.",
      "title": "Legacy recovery loses recon after the replacement folder is placed",
      "trigger": "Resume a previous-format reinstall interrupted after moving the replacement folder and before restoring its recon flag."
    },
    {
      "acceptance_ids": [
        "mission \u00a78"
      ],
      "actual": "The reproduction section describes the suite as having 120 checks. That count belongs to round five. The candidate receipt records 130.",
      "blocking_rationale": "The controller receipt and dated historical record establish the respective results.",
      "blocks_readiness": false,
      "category": "optional_polish",
      "expected": "Readers can distinguish the current gate count from the historical result.",
      "id": "R6-F1",
      "location": {
        "file": "docs/UMM-COMPATIBILITY.md",
        "line": 137
      },
      "priority": "P3",
      "requested_correction": "Qualify 120 as the round-five count or omit a fixed count from the general reproduction instructions.",
      "requested_evidence": "A documentation comparison with the dated gate records is sufficient.",
      "title": "The reproduction instructions retain the round-five check count",
      "trigger": "Read the reproduction section for the round-six candidate."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F1",
      "rationale": "The gate now rejects the lowercase ignored portrait and accepts an artwork-directory documentation change. Mixed-case artwork and ignored authored sources still escape the recorded-commit comparison."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R4-F1",
      "rationale": "Two overlapping builds produced distinct DLL digests, 7cd3cb09\u2026 and 4e16f623\u2026. The second publisher was refused while the first held the lock. The surviving receipt bound the first compile to the surviving ZIP, and standalone validation verified it under the winner\u2019s settings."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R4-F2",
      "rationale": "One test-enabled build exercised the active OutputPath redirect with stale passing runners in both locations. Their digests survived, the invocation-owned runner reported 290 passes, and the receipt identified that invocation\u2019s compiled DLL."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R4-F3",
      "rationale": "ReadAll obtains each supplied input\u2019s bytes and identity through one handle. The gate moved the actual replacement snapshot source onto the report destination between samples. The checked write refused it and preserved its bytes."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R4-F4",
      "rationale": "Both missing-directory controls pass. The previous writer can also leave a replacement directory without recon.flag. In that state the new fallback still loses a recorded true value."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R5-F1",
      "rationale": "The gate proves refusal during canonical-path publication and correct admission after rollback. The same physical destination can still skip the lock through an alternate spelling, and a missing sidecar at a build-owned output still selects external mode."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R5-F2",
      "rationale": "A competing lock now causes immediate refusal. The publisher rechecks HEAD and the material fingerprint under the acquired lock before moving either file. The gate changed a tracked source after validation and confirmed refusal with both previous hashes preserved."
    },
    {
      "assessment": "deferral_accepted",
      "finding_id": "R5-F3",
      "rationale": "OWNER AMENDMENT 2 explicitly keeps the singleton-array pin-field issue optional. The checked-in archive fields have the intended scalar types and the archive and DLL byte checks remain effective."
    }
  ],
  "request_id": "kwr-codex-umm-0311-baseline-20261123-20261123181024-1057-r06-a54f87",
  "round": 6,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "The shipping manifest declares UMM 0.31.1. The packaged DLL references UnityModManager 0.31.1.0, 0Harmony12 1.2.0.1 and 0Harmony 2.3.1.1. The gate verifies the competing-publisher, redirected-runner, handle-bound snapshot and late-source-change corrections.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "Once the three blocking findings clear, the owner can judge the draft with static compatibility evidence and native qualification marked NOT RUN."
    ],
    "scope_reviewed": "Final correction review of Waking Rune draft PR #12 at 495635d7e514cd99125a8b98d21250c75bd3e786 under both owner amendments, covering all eight pending findings and the affected build, validation, installer and recovery paths.",
    "unresolved_questions": []
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
