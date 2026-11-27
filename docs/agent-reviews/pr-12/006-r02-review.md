<!-- agent-review:v1 enrollment=kwr-codex-umm-0311-baseline-20261123-20261123181024-1057 key=r02-review part=1/1 sha256=3f1dc8b8bbadfa33f3e58ed5ddf8c1ecedb107d43010a35b596f1c26f506ed35 -->
### Codex GPT-6.2 Astra — review — round 2 — candidate `c328c65e00`

**Decision:** `changes_required` — The Windows gate passed and the recorded DLL has all three required baseline references. Readiness still fails because an older DLL can receive a current build receipt, validation can damage a frozen package or its receipt, and the recorded draft description contradicts the build evidence.

## Continuation for Claude

Correct the build and receipt checks together. Bind the receipt to the producing source, material inputs and actual MSBuild output. Run the configured gate on the submitted manifest as well as the alternate-version fixture. Complete offline pin validation and preserve existing files and transaction identity when validation, replacement or staging fails. Keep diagnostic packages distinguishable from qualified candidates.

Exercise the findings in temporary checkouts and destinations, then obtain a fresh controller gate receipt. Verify the README audit instructions from a clean checkout, supply the effective onboarding protections and refresh the draft description through the permitted workflow. Keep the correction within Waking Rune tooling, documentation and evidence. Preserve the live installation and the four native NOT RUN results.

#### Findings

**R1-F1** · P1 · defect · blocks readiness — Current provenance still accepts incorrect source and incomplete compiler inputs
- Location: `scripts/Validate-Package.ps1:196`; acceptance: mission §5, mission §6
- Trigger: Change the receipt's producing commit or version, or change an unrecorded material compiler input while retaining the original ZIP and receipt.
- Expected: Current qualification verifies the producing commit's material source, packaged product version and effective compiler inputs. Later documentation-only commits may reuse the same bytes.
- Actual: The validator checks only the syntax of receipt.commit and never compares receipt.version. A receipt naming the pre-baseline merge base can still report verified-current if its other fields are retained. The inventory also omits material inputs. For example, BaseAddress from LocalGamePaths.props is absent, and Win32Resource records a path without hashing the file at that path.
- Why it blocks / does not: A successful current-provenance check can attribute the package to the wrong source or qualify it after an input that changes the DLL has changed.
- Requested correction: Validate the recorded commit against its material Git inputs and the receipt version against the packaged manifest. Extend the fingerprint to cover effective build inputs that can change the product, including file contents where a compiler setting names a file.
- Requested evidence: Reject a receipt naming an incompatible commit or product version. Show an unchanged ZIP losing current qualification after a previously unrecorded compiler input changes, a rebuilt ZIP qualifying, and a documentation-only commit retaining qualification.

**R2-F1** · P1 · defect · blocks readiness — The receipt can be issued for a stale DLL from the conventional output folder
- Location: `scripts/Build.ps1:61`; acceptance: mission §5, mission §6
- Trigger: Build a changed product with a redirected MSBuild output while an older, otherwise valid Waking Rune DLL remains in bin/Release.
- Expected: The wrapper packages the output produced for the measured inputs, or verifies that reuse of an existing output is valid for those inputs.
- Actual: The inventory is emitted after reference selection, even when incremental MSBuild skips compilation. Build.ps1 then copies bin/Release/KingmakerWakingRune.dll regardless of the evaluated production output path. If LocalGamePaths.props redirects only the product build, a previous baseline DLL at the conventional path can pass the art and reference checks and receive the new source fingerprint.
- Why it blocks / does not: The packaging command can create a fresh, internally consistent receipt for a DLL other than the one built from the reported source.
- Requested correction: Bind packaging to the product invocation's actual output. Use an isolated fresh compile or verify incremental reuse against the material fingerprint before issuing the receipt.
- Requested evidence: In a warmed temporary checkout, leave the previous DLL at the conventional path, redirect the production output and make a source change that alters the DLL. Show that the ZIP contains the newly produced digest or that packaging refuses. Cover a changed input with preserved timestamps if incremental reuse is retained.

**R1-F2** · P2 · defect · blocks readiness — Offline validation exits before checking the complete pin definition
- Location: `scripts/Get-PinnedLoaderReferences.ps1:36`; acceptance: mission §5, mission §6
- Trigger: Inspect a receiptless ZIP when a pin retains its canonical name and version but has an invalid digest or archive definition.
- Expected: Both entry points apply all checks on the pin definition before either needs cache files or downloads.
- Actual: DefinitionOnly returns at line 36. Per-pin SHA-256, version, URL and archive-hash validation follows at lines 51–59. A malformed Harmony2 digest therefore fails the build helper while the standalone validator can still accept a receiptless package under that definition.
- Why it blocks / does not: The standalone pre-install check still accepts a pin definition that production refuses.
- Requested correction: Run the complete definition validation before the offline return. Keep acquisition and inspection of cached DLL bytes in the build path.
- Requested evidence: With the cache absent, show standalone rejection of a malformed pin digest and a missing required archive digest, alongside a passing genuine definition and package.

**R1-F4** · P2 · defect · blocks readiness — A validation report can still overwrite a frozen input
- Location: `scripts/Validate-Package.ps1:37`; acceptance: mission §5, mission §6
- Trigger: Select the adjacent build receipt as ReportPath without passing ReceiptPath, or use an existing file alias as the report destination.
- Expected: Report generation preserves the package and its build receipt in both content-only and receipt-aware validation.
- Actual: The collision list contains only PackagePath and an explicitly supplied ReceiptPath. A content-only check can successfully write its report over the adjacent <zip>.build.json. The comparison also uses path strings, so an existing hard link to an input escapes the check.
- Why it blocks / does not: A successful manual validation can destroy the receipt needed to qualify the frozen ZIP or alter the ZIP through another name.
- Requested correction: Protect the conventional adjacent receipt even when ReceiptPath is omitted. Handle existing report destinations that identify the same file as a protected input.
- Requested evidence: Run content-only validation with the adjacent receipt as ReportPath and exercise an aliased input in a temporary directory. Verify both original digests survive.

**R2-F2** · P2 · defect · blocks readiness — A failed replacement can destroy the previous validated ZIP
- Location: `scripts/Build.ps1:134`; acceptance: mission §5, mission §6
- Trigger: Replace an existing package when one destination is locked or the second final move fails.
- Expected: An authorized replacement preserves the previous validated pair until the new pair is installed successfully.
- Actual: Replacement deletes both old destinations with errors suppressed before moving the new files. On Windows, a locked old receipt can survive deletion while the old ZIP is removed. The new receipt move then fails and the catch deletes the new ZIP, leaving the old receipt without either ZIP.
- Why it blocks / does not: A recoverable placement failure can destroy the previous tested candidate and leave the version unusable for a normal retry.
- Requested correction: Make replacement recoverable across both destination operations. Preserve or restore the old ZIP and receipt if either placement fails.
- Requested evidence: In a temporary checkout with an existing valid pair, induce a failure placing the second file. Verify both previous hashes, validate the previous pair, and show a successful retry after the failure is removed.

**R2-F3** · P2 · defect · blocks readiness — The configured gate replaces the shipping version before testing it
- Location: `scripts/Test-LoaderBaseline.ps1:164`; acceptance: mission §4, mission §6
- Trigger: Run the baseline gate on a commit whose shipping product version differs from the fixed test version.
- Expected: The gate builds and validates the submitted manifest and exercises the version-bump regression in a separate case.
- Actual: The configured gate changes Info.json.Version to 0.12.99 and commits it before its first positive build. The controller receipt names H, while every package built by that gate uses the synthetic manifest. A missing or incorrectly typed shipping Version can be replaced by the fixture and pass the gate.
- Why it blocks / does not: The required gate can pass a submitted manifest that the production packaging route would reject.
- Requested correction: Run a positive build and standalone receipt check on the unmodified candidate before changing the version for the cold-cache and legacy regression cases.
- Requested evidence: Show the configured gate reporting the submitted source SHA, manifest version and package digest, plus the distinct alternate-version fixture. An invalid committed Version should fail the production case.

**R2-F4** · P2 · defect · blocks readiness — The install helper rejects receipt-backed diagnostic builds
- Location: `scripts/KwrPackage.ps1:21`; acceptance: mission §5, mission §6, mission §7
- Trigger: Supply the normal -dirty ZIP and sidecar produced by Build.ps1 to the staging helper.
- Expected: Diagnostic staging checks the available receipt and identifies the result as diagnostic. Frozen-candidate qualification continues to require clean, current provenance.
- Actual: Build.ps1 still writes a valid receipt for a working-tree diagnostic ZIP. The install helper treats every sidecar as a request for clean frozen-candidate qualification, so it rejects that diagnostic solely because dirty is true. The repository records actual use of this workflow in docs/ACCEPTANCE-WR-06.md. Removing the sidecar would discard the available byte checks.
- Why it blocks / does not: The corrected helper breaks an existing diagnostic workflow and makes a package with less provenance easier to stage than the same package with its receipt.
- Requested correction: Distinguish diagnostic staging from frozen-candidate qualification while preserving ZIP, DLL and pin checks for both. Carry the distinction into the transaction record.
- Requested evidence: Using the helper and a temporary destination, stage a genuine dirty diagnostic pair with its correct label, reject a mismatched receipt, and show that the diagnostic cannot qualify as a frozen candidate.

**R2-F5** · P2 · defect · blocks readiness — A failed reinstall leaves the previous package marked ready to launch
- Location: `scripts/KwrRuntime.ps1:253`; acceptance: mission §6, mission §7
- Trigger: Staging fails after Reinstall has removed the previous mod but before it saves the new package identity.
- Expected: A failed reinstall either restores the previous verified installation or leaves an explicit recoverable state that cannot launch as the previous package.
- Actual: Reinstall removes the previous mod and extracts into its final destination before saving any change to the transaction. If extraction or the new installed-DLL digest check fails, the finally block cleans the private copy, but the journal retains the previous package hash and launchable phase. Invoke-Launch accepts that phase.
- Why it blocks / does not: A subsequent run can be attributed to the previous artifact while the installation is partial or contains different bytes.
- Requested correction: Verify staged contents before replacing the installed directory and coordinate replacement with the transaction journal, or mark the transaction non-launchable before mutation and provide recovery.
- Requested evidence: Exercise a failed extraction or installed-digest check against temporary directories and a temporary journal. Verify that the recorded package matches the resulting installation or that launch is refused until recovery.

**R1-F8** · P2 · required_gap · blocks readiness — The revised audit example still depends on an executable outside the documented setup
- Location: `README.md:64`; acceptance: mission §8
- Trigger: Follow the static-audit block from a clean checkout with no preinstalled LoaderApiAudit executable.
- Expected: A developer with the documented profile files can build and run the supplied audit from the repository root and identify the exact DLL it assessed.
- Actual: The package selection and two-file layout are corrected. The audit example builds an unspecified copy of a project supplied only as two .txt files, then calls LoaderApiAudit.exe by bare name. A normal dotnet build writes it under bin/Release/net8.0 and does not put it on PATH. The introduction also describes every row as NuGet-assembled, though the owner row uses measured installed files.
- Why it blocks / does not: The documented commands still do not reproduce the reported audit from a clean checkout using the stated prerequisites.
- Requested correction: Show how to create the net8.0 project from the two committed files and invoke it by project or explicit output path. Identify the measured owner-copy directory separately from the three component-assembled profiles.
- Requested evidence: Run the revised commands from a clean checkout with the documented prerequisites and show four profile results whose mod digest matches the validated package.

**R1-F9** · P2 · evidence_gap · blocks readiness — The effective protected paths and pre-enrollment project check remain unconfirmed
- Location: `docs/evidence/umm-0311/onboarding-profile-summary.json:64`; acceptance: mission §4
- Trigger: Use the exported onboarding records to verify the mission's prerequisites.
- Expected: The repository-specific onboarding evidence establishes which protected paths were effective and which validation occurred before enrollment.
- Actual: The preflight record is dated 2026-11-23T23:09:42Z. The supplied check-project result is dated 2026-11-24T01:01:16Z, after enrollment. The summary records the gate, guarded push, runtime command denials and eight lock paths, but protected_paths is null and no effective inherited path policy is shown.
- Why it blocks / does not: The records establish several protections and a later successful check. They do not establish the required pre-enrollment check-project result or effective protected paths.
- Requested correction: Supply a read-only record of the effective protected-path policy, including any inherited defaults, and the historical check-project evidence. Identify any historical step that cannot be confirmed.
- Requested evidence: Provide the dated pre-enrollment output or contemporaneous enrollment record and the effective path protections. Keep the profile user-local and preserve controller state.

**R2-F6** · P2 · required_gap · blocks readiness — The recorded draft PR description contradicts the Windows build evidence
- Location: `docs/evidence/umm-0311/pr-12-description.json:7`; acceptance: mission §8
- Trigger: Read the adopted draft's validation section for the final handoff.
- Expected: Draft PR #12 accurately describes the baseline, the package and gate results, the four static rows and the outstanding native qualification.
- Actual: The only supplied PR readback says full Kingmaker compilation and Windows packaging were not run and that game references were unavailable. It omits the Windows build, standalone package validation, 73-case gate and four-profile static results now documented for this work.
- Why it blocks / does not: The owner-facing draft gives a materially incorrect account of what was verified.
- Requested correction: Check and update the draft description through the permitted PR workflow, then capture its current body. Preserve controller ownership of review comments and status.
- Requested evidence: Supply a current read-only PR description identifying the reviewed build and actual validation, with native checks clearly marked NOT RUN.

#### Prior finding assessments

- **R1-F1** — `fix_rejected` — The gate verifies invalidation for a helper edit and CheckForOverflowUnderflow. The commit is still checked only for syntax, receipt.version is unused, and additional material compiler inputs escape the fingerprint.
- **R1-F10** — `fix_verified` — The index identifies the exported digests as Git-blob hashes and provides a byte-preserving Python example. All 15 indexed values matched the committed blobs.
- **R1-F2** — `fix_rejected` — The dropped Harmony2 pin, inconsistent UMM minimum and additional adapter now fail in the controller gate. The shared offline path still returns before validating the complete pin definition.
- **R1-F3** — `fix_verified` — Both metadata readers obtain the assembly definition name. The gate rejected NotWakingRune under the product filename and accepted the genuine candidate.
- **R1-F4** — `fix_rejected` — The gate confirms correct relative bracketed paths and explicitly supplied input collisions. The adjacent receipt remains writable through ReportPath when ReceiptPath is omitted, and file aliases evade the string comparison.
- **R1-F5** — `fix_verified` — The real helper retained the captured source and digest after the caller ZIP moved and cleaned up after a rejected receipt. Both callers use the captured values and finally cleanup. R2-F5 concerns a separate failure after destination mutation.
- **R1-F6** — `fix_verified` — ZIP creation, sidecar writing and validation now take place in private staging. The injected post-sidecar failure left no new candidate and a normal retry passed. R2-F2 concerns later placement over an existing pair.
- **R1-F7** — `fix_verified` — The cold clone acquired its own pins, built under a different product version and rejected the genuine legacy DLL for its UMM reference. R2-F3 covers the newly introduced omission of a positive build of the submitted manifest.
- **R1-F8** — `fix_rejected` — The README now distinguishes the branch ZIP from the historical v0.12.3 release and shows the correct layout. The audit invocation still lacks a usable project setup and executable path for a clean checkout.
- **R1-F9** — `fix_rejected` — The preflight, later check-project run, lock list and draft readback are now available. The effective protected paths and historical check-project timing remain unverified. The supplied PR body establishes the separate documentation defect R2-F6.

#### Evidence assessment

- **Inspected:** The mission, fixed request, ten prior findings, developer report, transcript notice, controller receipt and log, full feature diff, README and compatibility guide.; The manifest, production project, loader pins, MSBuild targets, build and validation scripts, runtime staging callers, boundary suite and repository records of diagnostic package use.; The exported Windows build receipt, direct compiler record and package report for version 0.12.3 identify ZIP SHA-256 51be31ccdd5a861096e8c2140ecd06bd0be32e08ce25c659577d6fca0577a24f and DLL MVID ccc59117-f992-44ce-9355-540d35f181f0.; The audit output, profile inventory, newest-stable selection, audit source, onboarding records and captured draft PR body.
- **Executed verification:** The controller receipt binds the baseline gate to c328c65e00f8aa15d016f0637a1174b4ee007dd4. It records exit 0 in 144.5 seconds. The matching log contains 73 passing cases, 290 passing product logic tests, direct Csc reference paths and rejection of the genuine v0.12.3 DLL with its lowered manifest.; The gate built version 0.12.99 at temporary commit 115859c50e355e8d842f0ecb73b3de0a2c781ca5. Its DLL SHA-256 was 7cd3cb09f12f75a7247c1b401673528a0773a139e59dc3a940da860c28f24904. Its ZIP differs from the separately recorded 0.12.3 ZIP.; Read-only checks matched the supplied 5,505-line diff to Git, verified the gate-log digest and all 15 indexed Git-blob SHA-256 values, and matched all 149 source files and 19 resources in the production receipt to the snapshot.; Using the snapshot files and the exported compiler inventory, I reproduced buildInputsSha256 e50fa87726e8a8ec5a15af4f029c80bc11bbcb3ddd621ba402031fd11cd19301. Git shows only documentation and evidence changes between the production receipt's commit c0a6732 and the review candidate.
- **Source inspection only:** The gate exercised the package helper against a temporary destination. Its Begin/Reinstall wiring check inspected script text. No live transaction or game behavior was exercised.; The static audit reports 14 types and 39 members resolved for the same DLL on 0.31.1, 0.32.4, owner-installed 0.33.0 and selected newest stable 0.33.4. Assembly binding, finalizer behavior, travel and save/reload remain unobserved.
- **Required gaps:** Demonstrate production-output identity, complete material-input invalidation and receipt provenance on the unmodified shipping manifest.; Demonstrate offline pin rejection, input preservation, recoverable package replacement and accurate installation state across the identified failure paths.; Provide reproducible audit commands, effective onboarding protections and a draft PR readback consistent with the Windows and static validation results.
- **Limitations:** The whole foreground transcript was withheld. The controller log confirms the gate run. The separate 0.12.3 build and four-profile audit are supported by their committed records.; Review was restricted to read-only snapshot inspection. The uncovered failure cases below are source findings. The current remote PR body and user-local profile were outside the snapshot.; Native execution and combined-stack testing were NOT RUN. The repository harness targets the live installation, and the evidence identifies no approved disposable game copy.

#### Summary

- **Delivered behavior:** Waking Rune declares UMM 0.31.1. The recorded production DLL references UnityModManager 0.31.1.0, 0Harmony12 1.2.0.1 and 0Harmony 2.3.1.1. The direct compiler, standalone package validator and temporary installation helper have passing evidence for the tested cases.
- **Scope reviewed:** Final correction review of Waking Rune draft PR #12 at c328c65e00f8aa15d016f0637a1174b4ee007dd4, including all ten prior findings and the changed build, package, staging, documentation and evidence paths.
- **Pending later-phase work:** none
- **Unresolved questions:** Which protected paths were effective at enrollment, and is a contemporaneous check-project result available?
- **Remaining owner review:** Disposition of the corrected draft after a current controller handoff.; Availability and authorization of a disposable Kingmaker fixture for native New Game, Refuge travel, dialogue/finalizer and save/reload qualification before making runtime-support claims.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: none.

Codex CLI `0.170.0`, thread `01a20b1b-0420-7850-a69b-959c7b9f3381`, duration 675.1s, response sha256 `3cf35714d647bc1f0dc04f64a9261bb14490fe4a96d30cd0bb6dbfe5b35ddd50`, request `kwr-codex-umm-0311-baseline-20261123-20261123181024-1057-r02-7ba829`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "c328c65e00f8aa15d016f0637a1174b4ee007dd4",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The Windows gate passed and the recorded DLL has all three required baseline references. Readiness still fails because an older DLL can receive a current build receipt, validation can damage a frozen package or its receipt, and the recorded draft description contradicts the build evidence.\n\n## Continuation for Claude\n\nCorrect the build and receipt checks together. Bind the receipt to the producing source, material inputs and actual MSBuild output. Run the configured gate on the submitted manifest as well as the alternate-version fixture. Complete offline pin validation and preserve existing files and transaction identity when validation, replacement or staging fails. Keep diagnostic packages distinguishable from qualified candidates.\n\nExercise the findings in temporary checkouts and destinations, then obtain a fresh controller gate receipt. Verify the README audit instructions from a clean checkout, supply the effective onboarding protections and refresh the draft description through the permitted workflow. Keep the correction within Waking Rune tooling, documentation and evidence. Preserve the live installation and the four native NOT RUN results.",
  "evidence_assessment": {
    "executed_verification": [
      "The controller receipt binds the baseline gate to c328c65e00f8aa15d016f0637a1174b4ee007dd4. It records exit 0 in 144.5 seconds. The matching log contains 73 passing cases, 290 passing product logic tests, direct Csc reference paths and rejection of the genuine v0.12.3 DLL with its lowered manifest.",
      "The gate built version 0.12.99 at temporary commit 115859c50e355e8d842f0ecb73b3de0a2c781ca5. Its DLL SHA-256 was 7cd3cb09f12f75a7247c1b401673528a0773a139e59dc3a940da860c28f24904. Its ZIP differs from the separately recorded 0.12.3 ZIP.",
      "Read-only checks matched the supplied 5,505-line diff to Git, verified the gate-log digest and all 15 indexed Git-blob SHA-256 values, and matched all 149 source files and 19 resources in the production receipt to the snapshot.",
      "Using the snapshot files and the exported compiler inventory, I reproduced buildInputsSha256 e50fa87726e8a8ec5a15af4f029c80bc11bbcb3ddd621ba402031fd11cd19301. Git shows only documentation and evidence changes between the production receipt's commit c0a6732 and the review candidate."
    ],
    "inspected": [
      "The mission, fixed request, ten prior findings, developer report, transcript notice, controller receipt and log, full feature diff, README and compatibility guide.",
      "The manifest, production project, loader pins, MSBuild targets, build and validation scripts, runtime staging callers, boundary suite and repository records of diagnostic package use.",
      "The exported Windows build receipt, direct compiler record and package report for version 0.12.3 identify ZIP SHA-256 51be31ccdd5a861096e8c2140ecd06bd0be32e08ce25c659577d6fca0577a24f and DLL MVID ccc59117-f992-44ce-9355-540d35f181f0.",
      "The audit output, profile inventory, newest-stable selection, audit source, onboarding records and captured draft PR body."
    ],
    "limitations": [
      "The whole foreground transcript was withheld. The controller log confirms the gate run. The separate 0.12.3 build and four-profile audit are supported by their committed records.",
      "Review was restricted to read-only snapshot inspection. The uncovered failure cases below are source findings. The current remote PR body and user-local profile were outside the snapshot.",
      "Native execution and combined-stack testing were NOT RUN. The repository harness targets the live installation, and the evidence identifies no approved disposable game copy."
    ],
    "required_gaps": [
      "Demonstrate production-output identity, complete material-input invalidation and receipt provenance on the unmodified shipping manifest.",
      "Demonstrate offline pin rejection, input preservation, recoverable package replacement and accurate installation state across the identified failure paths.",
      "Provide reproducible audit commands, effective onboarding protections and a draft PR readback consistent with the Windows and static validation results."
    ],
    "source_inspection_only": [
      "The gate exercised the package helper against a temporary destination. Its Begin/Reinstall wiring check inspected script text. No live transaction or game behavior was exercised.",
      "The static audit reports 14 types and 39 members resolved for the same DLL on 0.31.1, 0.32.4, owner-installed 0.33.0 and selected newest stable 0.33.4. Assembly binding, finalizer behavior, travel and save/reload remain unobserved."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "mission \u00a75",
        "mission \u00a76"
      ],
      "actual": "The validator checks only the syntax of receipt.commit and never compares receipt.version. A receipt naming the pre-baseline merge base can still report verified-current if its other fields are retained. The inventory also omits material inputs. For example, BaseAddress from LocalGamePaths.props is absent, and Win32Resource records a path without hashing the file at that path.",
      "blocking_rationale": "A successful current-provenance check can attribute the package to the wrong source or qualify it after an input that changes the DLL has changed.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Current qualification verifies the producing commit's material source, packaged product version and effective compiler inputs. Later documentation-only commits may reuse the same bytes.",
      "id": "R1-F1",
      "location": {
        "file": "scripts/Validate-Package.ps1",
        "line": 196
      },
      "priority": "P1",
      "requested_correction": "Validate the recorded commit against its material Git inputs and the receipt version against the packaged manifest. Extend the fingerprint to cover effective build inputs that can change the product, including file contents where a compiler setting names a file.",
      "requested_evidence": "Reject a receipt naming an incompatible commit or product version. Show an unchanged ZIP losing current qualification after a previously unrecorded compiler input changes, a rebuilt ZIP qualifying, and a documentation-only commit retaining qualification.",
      "title": "Current provenance still accepts incorrect source and incomplete compiler inputs",
      "trigger": "Change the receipt's producing commit or version, or change an unrecorded material compiler input while retaining the original ZIP and receipt."
    },
    {
      "acceptance_ids": [
        "mission \u00a75",
        "mission \u00a76"
      ],
      "actual": "The inventory is emitted after reference selection, even when incremental MSBuild skips compilation. Build.ps1 then copies bin/Release/KingmakerWakingRune.dll regardless of the evaluated production output path. If LocalGamePaths.props redirects only the product build, a previous baseline DLL at the conventional path can pass the art and reference checks and receive the new source fingerprint.",
      "blocking_rationale": "The packaging command can create a fresh, internally consistent receipt for a DLL other than the one built from the reported source.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The wrapper packages the output produced for the measured inputs, or verifies that reuse of an existing output is valid for those inputs.",
      "id": "R2-F1",
      "location": {
        "file": "scripts/Build.ps1",
        "line": 61
      },
      "priority": "P1",
      "requested_correction": "Bind packaging to the product invocation's actual output. Use an isolated fresh compile or verify incremental reuse against the material fingerprint before issuing the receipt.",
      "requested_evidence": "In a warmed temporary checkout, leave the previous DLL at the conventional path, redirect the production output and make a source change that alters the DLL. Show that the ZIP contains the newly produced digest or that packaging refuses. Cover a changed input with preserved timestamps if incremental reuse is retained.",
      "title": "The receipt can be issued for a stale DLL from the conventional output folder",
      "trigger": "Build a changed product with a redirected MSBuild output while an older, otherwise valid Waking Rune DLL remains in bin/Release."
    },
    {
      "acceptance_ids": [
        "mission \u00a75",
        "mission \u00a76"
      ],
      "actual": "DefinitionOnly returns at line 36. Per-pin SHA-256, version, URL and archive-hash validation follows at lines 51\u201359. A malformed Harmony2 digest therefore fails the build helper while the standalone validator can still accept a receiptless package under that definition.",
      "blocking_rationale": "The standalone pre-install check still accepts a pin definition that production refuses.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Both entry points apply all checks on the pin definition before either needs cache files or downloads.",
      "id": "R1-F2",
      "location": {
        "file": "scripts/Get-PinnedLoaderReferences.ps1",
        "line": 36
      },
      "priority": "P2",
      "requested_correction": "Run the complete definition validation before the offline return. Keep acquisition and inspection of cached DLL bytes in the build path.",
      "requested_evidence": "With the cache absent, show standalone rejection of a malformed pin digest and a missing required archive digest, alongside a passing genuine definition and package.",
      "title": "Offline validation exits before checking the complete pin definition",
      "trigger": "Inspect a receiptless ZIP when a pin retains its canonical name and version but has an invalid digest or archive definition."
    },
    {
      "acceptance_ids": [
        "mission \u00a75",
        "mission \u00a76"
      ],
      "actual": "The collision list contains only PackagePath and an explicitly supplied ReceiptPath. A content-only check can successfully write its report over the adjacent <zip>.build.json. The comparison also uses path strings, so an existing hard link to an input escapes the check.",
      "blocking_rationale": "A successful manual validation can destroy the receipt needed to qualify the frozen ZIP or alter the ZIP through another name.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Report generation preserves the package and its build receipt in both content-only and receipt-aware validation.",
      "id": "R1-F4",
      "location": {
        "file": "scripts/Validate-Package.ps1",
        "line": 37
      },
      "priority": "P2",
      "requested_correction": "Protect the conventional adjacent receipt even when ReceiptPath is omitted. Handle existing report destinations that identify the same file as a protected input.",
      "requested_evidence": "Run content-only validation with the adjacent receipt as ReportPath and exercise an aliased input in a temporary directory. Verify both original digests survive.",
      "title": "A validation report can still overwrite a frozen input",
      "trigger": "Select the adjacent build receipt as ReportPath without passing ReceiptPath, or use an existing file alias as the report destination."
    },
    {
      "acceptance_ids": [
        "mission \u00a75",
        "mission \u00a76"
      ],
      "actual": "Replacement deletes both old destinations with errors suppressed before moving the new files. On Windows, a locked old receipt can survive deletion while the old ZIP is removed. The new receipt move then fails and the catch deletes the new ZIP, leaving the old receipt without either ZIP.",
      "blocking_rationale": "A recoverable placement failure can destroy the previous tested candidate and leave the version unusable for a normal retry.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "An authorized replacement preserves the previous validated pair until the new pair is installed successfully.",
      "id": "R2-F2",
      "location": {
        "file": "scripts/Build.ps1",
        "line": 134
      },
      "priority": "P2",
      "requested_correction": "Make replacement recoverable across both destination operations. Preserve or restore the old ZIP and receipt if either placement fails.",
      "requested_evidence": "In a temporary checkout with an existing valid pair, induce a failure placing the second file. Verify both previous hashes, validate the previous pair, and show a successful retry after the failure is removed.",
      "title": "A failed replacement can destroy the previous validated ZIP",
      "trigger": "Replace an existing package when one destination is locked or the second final move fails."
    },
    {
      "acceptance_ids": [
        "mission \u00a74",
        "mission \u00a76"
      ],
      "actual": "The configured gate changes Info.json.Version to 0.12.99 and commits it before its first positive build. The controller receipt names H, while every package built by that gate uses the synthetic manifest. A missing or incorrectly typed shipping Version can be replaced by the fixture and pass the gate.",
      "blocking_rationale": "The required gate can pass a submitted manifest that the production packaging route would reject.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The gate builds and validates the submitted manifest and exercises the version-bump regression in a separate case.",
      "id": "R2-F3",
      "location": {
        "file": "scripts/Test-LoaderBaseline.ps1",
        "line": 164
      },
      "priority": "P2",
      "requested_correction": "Run a positive build and standalone receipt check on the unmodified candidate before changing the version for the cold-cache and legacy regression cases.",
      "requested_evidence": "Show the configured gate reporting the submitted source SHA, manifest version and package digest, plus the distinct alternate-version fixture. An invalid committed Version should fail the production case.",
      "title": "The configured gate replaces the shipping version before testing it",
      "trigger": "Run the baseline gate on a commit whose shipping product version differs from the fixed test version."
    },
    {
      "acceptance_ids": [
        "mission \u00a75",
        "mission \u00a76",
        "mission \u00a77"
      ],
      "actual": "Build.ps1 still writes a valid receipt for a working-tree diagnostic ZIP. The install helper treats every sidecar as a request for clean frozen-candidate qualification, so it rejects that diagnostic solely because dirty is true. The repository records actual use of this workflow in docs/ACCEPTANCE-WR-06.md. Removing the sidecar would discard the available byte checks.",
      "blocking_rationale": "The corrected helper breaks an existing diagnostic workflow and makes a package with less provenance easier to stage than the same package with its receipt.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Diagnostic staging checks the available receipt and identifies the result as diagnostic. Frozen-candidate qualification continues to require clean, current provenance.",
      "id": "R2-F4",
      "location": {
        "file": "scripts/KwrPackage.ps1",
        "line": 21
      },
      "priority": "P2",
      "requested_correction": "Distinguish diagnostic staging from frozen-candidate qualification while preserving ZIP, DLL and pin checks for both. Carry the distinction into the transaction record.",
      "requested_evidence": "Using the helper and a temporary destination, stage a genuine dirty diagnostic pair with its correct label, reject a mismatched receipt, and show that the diagnostic cannot qualify as a frozen candidate.",
      "title": "The install helper rejects receipt-backed diagnostic builds",
      "trigger": "Supply the normal -dirty ZIP and sidecar produced by Build.ps1 to the staging helper."
    },
    {
      "acceptance_ids": [
        "mission \u00a76",
        "mission \u00a77"
      ],
      "actual": "Reinstall removes the previous mod and extracts into its final destination before saving any change to the transaction. If extraction or the new installed-DLL digest check fails, the finally block cleans the private copy, but the journal retains the previous package hash and launchable phase. Invoke-Launch accepts that phase.",
      "blocking_rationale": "A subsequent run can be attributed to the previous artifact while the installation is partial or contains different bytes.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "A failed reinstall either restores the previous verified installation or leaves an explicit recoverable state that cannot launch as the previous package.",
      "id": "R2-F5",
      "location": {
        "file": "scripts/KwrRuntime.ps1",
        "line": 253
      },
      "priority": "P2",
      "requested_correction": "Verify staged contents before replacing the installed directory and coordinate replacement with the transaction journal, or mark the transaction non-launchable before mutation and provide recovery.",
      "requested_evidence": "Exercise a failed extraction or installed-digest check against temporary directories and a temporary journal. Verify that the recorded package matches the resulting installation or that launch is refused until recovery.",
      "title": "A failed reinstall leaves the previous package marked ready to launch",
      "trigger": "Staging fails after Reinstall has removed the previous mod but before it saves the new package identity."
    },
    {
      "acceptance_ids": [
        "mission \u00a78"
      ],
      "actual": "The package selection and two-file layout are corrected. The audit example builds an unspecified copy of a project supplied only as two .txt files, then calls LoaderApiAudit.exe by bare name. A normal dotnet build writes it under bin/Release/net8.0 and does not put it on PATH. The introduction also describes every row as NuGet-assembled, though the owner row uses measured installed files.",
      "blocking_rationale": "The documented commands still do not reproduce the reported audit from a clean checkout using the stated prerequisites.",
      "blocks_readiness": true,
      "category": "required_gap",
      "expected": "A developer with the documented profile files can build and run the supplied audit from the repository root and identify the exact DLL it assessed.",
      "id": "R1-F8",
      "location": {
        "file": "README.md",
        "line": 64
      },
      "priority": "P2",
      "requested_correction": "Show how to create the net8.0 project from the two committed files and invoke it by project or explicit output path. Identify the measured owner-copy directory separately from the three component-assembled profiles.",
      "requested_evidence": "Run the revised commands from a clean checkout with the documented prerequisites and show four profile results whose mod digest matches the validated package.",
      "title": "The revised audit example still depends on an executable outside the documented setup",
      "trigger": "Follow the static-audit block from a clean checkout with no preinstalled LoaderApiAudit executable."
    },
    {
      "acceptance_ids": [
        "mission \u00a74"
      ],
      "actual": "The preflight record is dated 2026-11-23T23:09:42Z. The supplied check-project result is dated 2026-11-24T01:01:16Z, after enrollment. The summary records the gate, guarded push, runtime command denials and eight lock paths, but protected_paths is null and no effective inherited path policy is shown.",
      "blocking_rationale": "The records establish several protections and a later successful check. They do not establish the required pre-enrollment check-project result or effective protected paths.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "The repository-specific onboarding evidence establishes which protected paths were effective and which validation occurred before enrollment.",
      "id": "R1-F9",
      "location": {
        "file": "docs/evidence/umm-0311/onboarding-profile-summary.json",
        "line": 64
      },
      "priority": "P2",
      "requested_correction": "Supply a read-only record of the effective protected-path policy, including any inherited defaults, and the historical check-project evidence. Identify any historical step that cannot be confirmed.",
      "requested_evidence": "Provide the dated pre-enrollment output or contemporaneous enrollment record and the effective path protections. Keep the profile user-local and preserve controller state.",
      "title": "The effective protected paths and pre-enrollment project check remain unconfirmed",
      "trigger": "Use the exported onboarding records to verify the mission's prerequisites."
    },
    {
      "acceptance_ids": [
        "mission \u00a78"
      ],
      "actual": "The only supplied PR readback says full Kingmaker compilation and Windows packaging were not run and that game references were unavailable. It omits the Windows build, standalone package validation, 73-case gate and four-profile static results now documented for this work.",
      "blocking_rationale": "The owner-facing draft gives a materially incorrect account of what was verified.",
      "blocks_readiness": true,
      "category": "required_gap",
      "expected": "Draft PR #12 accurately describes the baseline, the package and gate results, the four static rows and the outstanding native qualification.",
      "id": "R2-F6",
      "location": {
        "file": "docs/evidence/umm-0311/pr-12-description.json",
        "line": 7
      },
      "priority": "P2",
      "requested_correction": "Check and update the draft description through the permitted PR workflow, then capture its current body. Preserve controller ownership of review comments and status.",
      "requested_evidence": "Supply a current read-only PR description identifying the reviewed build and actual validation, with native checks clearly marked NOT RUN.",
      "title": "The recorded draft PR description contradicts the Windows build evidence",
      "trigger": "Read the adopted draft's validation section for the final handoff."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F1",
      "rationale": "The gate verifies invalidation for a helper edit and CheckForOverflowUnderflow. The commit is still checked only for syntax, receipt.version is unused, and additional material compiler inputs escape the fingerprint."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F10",
      "rationale": "The index identifies the exported digests as Git-blob hashes and provides a byte-preserving Python example. All 15 indexed values matched the committed blobs."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F2",
      "rationale": "The dropped Harmony2 pin, inconsistent UMM minimum and additional adapter now fail in the controller gate. The shared offline path still returns before validating the complete pin definition."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F3",
      "rationale": "Both metadata readers obtain the assembly definition name. The gate rejected NotWakingRune under the product filename and accepted the genuine candidate."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F4",
      "rationale": "The gate confirms correct relative bracketed paths and explicitly supplied input collisions. The adjacent receipt remains writable through ReportPath when ReceiptPath is omitted, and file aliases evade the string comparison."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F5",
      "rationale": "The real helper retained the captured source and digest after the caller ZIP moved and cleaned up after a rejected receipt. Both callers use the captured values and finally cleanup. R2-F5 concerns a separate failure after destination mutation."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F6",
      "rationale": "ZIP creation, sidecar writing and validation now take place in private staging. The injected post-sidecar failure left no new candidate and a normal retry passed. R2-F2 concerns later placement over an existing pair."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F7",
      "rationale": "The cold clone acquired its own pins, built under a different product version and rejected the genuine legacy DLL for its UMM reference. R2-F3 covers the newly introduced omission of a positive build of the submitted manifest."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F8",
      "rationale": "The README now distinguishes the branch ZIP from the historical v0.12.3 release and shows the correct layout. The audit invocation still lacks a usable project setup and executable path for a clean checkout."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F9",
      "rationale": "The preflight, later check-project run, lock list and draft readback are now available. The effective protected paths and historical check-project timing remain unverified. The supplied PR body establishes the separate documentation defect R2-F6."
    }
  ],
  "request_id": "kwr-codex-umm-0311-baseline-20261123-20261123181024-1057-r02-7ba829",
  "round": 2,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "Waking Rune declares UMM 0.31.1. The recorded production DLL references UnityModManager 0.31.1.0, 0Harmony12 1.2.0.1 and 0Harmony 2.3.1.1. The direct compiler, standalone package validator and temporary installation helper have passing evidence for the tested cases.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "Disposition of the corrected draft after a current controller handoff.",
      "Availability and authorization of a disposable Kingmaker fixture for native New Game, Refuge travel, dialogue/finalizer and save/reload qualification before making runtime-support claims."
    ],
    "scope_reviewed": "Final correction review of Waking Rune draft PR #12 at c328c65e00f8aa15d016f0637a1174b4ee007dd4, including all ten prior findings and the changed build, package, staging, documentation and evidence paths.",
    "unresolved_questions": [
      "Which protected paths were effective at enrollment, and is a contemporaneous check-project result available?"
    ]
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
