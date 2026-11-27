<!-- agent-review:v1 enrollment=kwr-codex-umm-0311-baseline-20261123-20261123181024-1057 key=r09-request part=1/1 sha256=e75ba8e0b1375948eb041ab3cfe6ae5e84e612a93a19123cad55cdb33be89c4b -->
### Claude Opus — review request — round 9 — candidate `855a86a39f`

| Field | Value |
|---|---|
| Request ID | `kwr-codex-umm-0311-baseline-20261123-20261123181024-1057-r09-9a2381` |
| Checkpoint | `final` (final) |
| Candidate H | `855a86a39f6d6e0c28162ca70a827abb15be9a25` |
| Target B (`main`) | `480cdbd31425c8855f5856936aeaff60ae236d88` |
| Merge base M | `480cdbd31425c8855f5856936aeaff60ae236d88` |
| Mission | `kingmaker-umm-0311-mission-20261123` sha256 `f7d990f396` |
| Workflow | `6d613defe7` |
| Owner guidance | none |
| Owner amendments | revision 5, sha256 `f863cbdbe93acb6edfaf9b2e6a26d1feb3c516b27eaa3929b286b07d4223e3b4` |
| Developer observed | foreground session `e178a93b-ca53-4b6b-a4d2-a357d48eaa77`: models ['claude-opus-5-5'], effort ['high'], permission modes ['auto'] (CLI-recorded transcript; the headless permission profile does not apply to a foreground session) |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `max`, read-only sandbox |
| Round budget | 9 of 10 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

Prepared under owner amendments revision 5 sha256 f863cbdbe93acb6edfaf9b2e6a26d1feb3c516b27eaa3929b286b07d4223e3b4 under owner amendment umm-0311-final-two-20261126-02. Round-9 corrections for Waking Rune PR #12: R1-F1, R8-F1, R5-F1 and R8-F2 as one batch across the existing input and package helpers. Code and tests eec7a90; 855a86a adds evidence and documentation only. Product sources unchanged.

R1-F1 and R8-F1: the compiler inventory classifies every location physically - the repository root, the intermediate folder and each existing input resolved with GetFinalPathNameByHandle (KwrFileIdentityV5.FinalPathOf) - so a junction, symbolic link, short name or other letter case cannot make an ignored repository source look external; a source outside the repository with more than one hard-link name, whose ownership cannot be established, is refused instead of exempted. Repository paths resolve to Git's tracked spelling with case-insensitive literal matching (two tracked spellings refused as ambiguous) in both the Git membership check and the producing-commit comparison (Get-KwrInventoryRepositoryPaths), so a tracked CaseRoot.cs selected as caseroot.cs is compared with the receipt's commit.

R5-F1: New-KwrValidatedPackageCopy opens the ZIP once (no write sharing) and takes its identity, physical path, hard-link count and every hard-link name from that handle. A file with a name in the build-owned artifacts folder is read only under that name's publish lock and with that name's receipt, whatever path or alias-side receipt the caller used; under the lock the canonical name must still be the file read, and the copy and receipt are taken while the lock is held (a probe showed another hard-link name can be removed while a read handle is open, so the lock, not the handle, excludes the publisher's placement and rollback). A linked file with no build-owned name is declined; independent copies keep their handling. The receiptless post-copy link-count sample is gone.

R8-F2: the validator protects the package as the file actually read - its physical target and every hard-link name, each with its conventional receipt - before the existing checked-handle report write; when those names cannot be established, no report is written. A file symbolic link could not be created on this host (Windows: administrator privilege required), so the symbolic-link variant was not executed; the hard-link variant of the same report case was. R8-F3 (optional) is resolved by the new type name. The earlier 149/149 boundary-suite result stays attributed to 79f1687. Native runtime NOT RUN.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/agent_review.py verify --enrollment kwr-codex-umm-0311-baseline-20261123-20261123181024-1057 --gate baseline | yes | pass | 687.9s | Controller receipt gate-baseline-855a86a39f6d-d4433dcb35e-ac9ec8ca62a-20261127T025000575071Z.log on 855a86a: Test-LoaderBaseline.ps1 158/158 passed in fresh Windows PowerShell processes on a temporary clone of the candidate; recorded as not run: report-through-a-file-symbolic-link-input-cannot-overwrite-the-target-receipt: this host refused to create a file symbolic link: Administrator privilege required for this operation.. |
| Focused driver Invoke-KwrFocusedR9.ps1 on eec7a90 (exported as focused-r9-driver.ps1.txt) | no | pass | 71s | docs/evidence/umm-0311/focused-r9.json: 11 of 11, run before the gate in a disposable clone (real Build.ps1, Validate-Package.ps1, KwrPackage.ps1 in fresh processes); the symbolic-link case not run (host refusal). |
| Identity probe (docs/evidence/umm-0311/identity-probe-r9.txt) | no | pass | 0.5s | KwrFileIdentityV5 compile and members: physical path through letter case, a junction and a hard link; hard-link names; showed that another hard-link name can be removed while a read handle without delete sharing is open (the design relies on the publish lock for that exclusion). |
| Native checks | no | not_run | 0 | Not authorized. |

**Evidence references:**
- prepared under owner amendments revision 5 sha256 f863cbdbe93acb6edfaf9b2e6a26d1feb3c516b27eaa3929b286b07d4223e3b4
- docs/evidence/umm-0311/INDEX.md
- docs/evidence/umm-0311/focused-r9.json
- docs/evidence/umm-0311/identity-probe-r9.txt
- docs/UMM-COMPATIBILITY.md
- controller gate receipt gate-baseline-855a86a39f6d-d4433dcb35e-ac9ec8ca62a-20261127T025000575071Z.log

**Prior findings to assess:** `R1-F1`, `R5-F1`, `R8-F1`, `R8-F2`, `R8-F3`

**Owner amendments to the mission — revision 5, captured-source sha256 `f863cbdbe93acb6edfaf9b2e6a26d1feb3c516b27eaa3929b286b07d4223e3b4`.** Applied by the owner with `resume --amend-mission`; each owner message is preserved verbatim, in order. The effective task is the original mission plus these amendments. The reported work comes from a developer invocation whose input included them, and the reviewer of this round receives a byte-exact copy of the captured document. Public rendering after the controller's standard sanitization; the digest identifies the private captured bytes, which this rendering can differ from.

```text
# Owner amendments to the mission

Cumulative record kept by the agent-review controller. Each owner message below was applied by the owner through `resume --amend-mission` and is preserved verbatim, in order, between its labeled marker lines. Later amendments supersede conflicting earlier requirements; requirements they do not change still apply.

----- BEGIN OWNER AMENDMENT 1 (captured 2026-11-24T14:59:19Z; message sha256 2d9ec2f5f285244cf921c892cbc19f039c66578388755d3a29ab4ed460083033) -----
For Waking, after verifying the current effective profile and preflight against
the enrollment's actual configuration, accept the absence of contemporaneous
pre-enrollment check-project evidence as a documented historical process gap.
Preserve the truthful chronology. Record this narrow exception through the
supported mission-amendment mechanism. It waives no current code, configuration,
or validation defect.
----- END OWNER AMENDMENT 1 -----

----- BEGIN OWNER AMENDMENT 2 (captured 2026-11-26T13:58:01Z; message sha256 c3d3998ac1dc60dec6597a2f22d10b3d1bff078dc72b8377a0c4590f72b77b11) -----
PHASE B — RESUME THE FOUR PAUSED REPOSITORIES

Once Phase A has completed, or a repository has reached a concrete blocker
within its authorization, proceed with the remaining authorized work.
One blocked repository must not stop independent progress in the others.
Report any incomplete Phase A publication explicitly.

Resume the existing enrollments and PRs:

TRE:
- Repository: howardreith/KingmakerGunslingerAndOtherTabletopRules
- PR: #220
- Last recorded head: a7d293a533
- Reviews used: 5.
- New TOTAL review cap: 8.

Buff:
- Repository: howardreith/KingmakerBuffPlanner
- PR: #26
- Last recorded head: e636c2c563
- Reviews used: 5.
- New TOTAL review cap: 8.

Waking:
- Repository: howardreith/KingmakerWakingRune
- PR: #12
- Last recorded head: 5432227c86
- Reviews used: 5.
- New TOTAL review cap: 8.

Skip:
- Repository: howardreith/KingmakerSkipCinematic
- PR: #3
- Last recorded head: 45f5c206a2
- Reviews used: 7.
- New TOTAL review cap: 10.

This authorizes three additional review submissions per repository.
The caps above are absolute totals, not additional-round values.

I also explicitly authorize one supported resumption of each existing
no-progress-blocked enrollment under this assignment. Use the controller's
documented resume mechanism. Preserve completed rounds, findings, evidence,
and history; do not manually erase counters or create replacement
enrollments to escape previous stops.

Start from the recorded unresolved findings:

- TRE: R1-F4, R4-F3, R5-F1, R5-F2, R5-F3, R5-F4.
- Buff: R1-F3, R4-F1, R4-F4, R5-F1, R5-F2, R5-F3.
- Waking: R1-F1, R4-F1, R4-F2, R4-F3, R4-F4, R5-F1, R5-F2.
  R5-F3 remains optional.
- Skip: R5-F3, R6-F1, R6-F2, R6-F4, R7-F1, R7-F2, R7-F3, R7-F4.

Read the actual finding text and current mission records before fixing.
Do not reconstruct requirements from finding titles alone.

Batch coherent corrections before submitting a review. Use focused,
realistic reproductions first, then the required candidate gate.
Make sure a regression test reaches the behavior it claims to test;
an earlier unrelated refusal is not evidence that the target behavior works.

Stop each repository at AWAITING_OWNER_REVIEW as soon as its required
findings and checks are satisfied. Do not merge or release these four.

SCOPE FREEZE

The authorized correction scope remains:

- Declare UMM 0.31.1 consistently.
- Compile against the agreed pinned 0.31.1 baseline through the relevant
  supported build routes.
- Perform the agreed static checks for the specific higher loader versions.
- Keep package, receipt, and evidence claims accurate.
- Correct concrete regressions introduced by this mission.
- Complete the integration and release work expressly authorized above.

No new gameplay features, general build-system redesign, broad MSBuild
interpreter, budget framework, evidence framework, or native-test campaign.

Prefer the smallest adequate correction using existing infrastructure.
Simplifying or removing unnecessary mission-added machinery is acceptable
when it preserves the required behavior and truthful evidence.

Previously accepted optional findings and deferrals remain nonblocking.

New findings must identify a concrete defect within this frozen scope,
including a direct regression from the current corrections or integration.
General hardening ideas and unrelated cleanup go into follow-up notes.

A demonstrated data-loss defect, broken rollback, incorrect release
artifact, or false evidence claim introduced by this work remains a real
blocker. Fix it narrowly or remove the unnecessary offending path; do not
waive it merely to obtain a ready verdict.

Communicate this scope to the controller/reviewer through the existing
supported mechanism. Do not fabricate approval or weaken required gates.

VALIDATION ALLOWANCE AND ACCOUNTING

I authorize up to 30 minutes of NEW validation work per repository for
this assignment. This is a ceiling, not a target.

For Mounted, explicitly declare a separately budgeted integration/release
campaign with a 30-minute allowance using the existing supported mechanism.
Keep the completed correction campaign's ledger unchanged.

For the other repositories, record the authorization through their existing
accounting or allowance mechanism. Apply any required allowance adjustment
once, with this assignment as its source, preserving prior spending and
history. Do not introduce new metering infrastructure.

Count all validation work under this assignment, including:
- Direct reproductions and focused tests.
- Failed runs and reruns.
- Builds and required gates.
- Packaging and release qualification.

Direct invocation does not make a run uncounted. Where no automated ledger
exists, record durations in the existing mission status record.

Inspect nested commands so a focused check does not unexpectedly launch a
broad campaign. Reuse valid results for unchanged inputs when permitted.

Do not start a known run that cannot fit the remaining allowance.
Complete necessary cleanup safely and record actual costs.

No automatic extensions beyond the stated review and validation allowances.
If a repository reaches a real stop, report its precise blocker and continue
other independent authorized work.
----- END OWNER AMENDMENT 2 -----

----- BEGIN OWNER AMENDMENT 3 (captured 2026-11-26T16:27:22Z; message sha256 93783515c5613c7950d2eb9979b3b35bbe9a870c39e801f38d2851453a622d52) -----
PHASE B — SPECIFIC CORRECTIONS

I authorize one supported resumption of the CURRENT blocked state in each
of TRE, Buff, Waking, and Skip. Preserve the same enrollments, PRs,
completed rounds, findings, evidence, and spending.

Review caps are absolute totals:
- TRE #220: retain cap 8; reported 5 used.
- Buff #26: retain cap 8; reported 6 used.
- Waking #12: retain cap 8; reported 6 used.
- Skip #3: increase cap from 10 to 12; reported 10 used.

Do not automatically resume a subsequent no-progress stop or extend
these caps again.

TRE #220

Starting reported head: b16ce7a47c.

Finish the missing evidence for the corrections already committed:
- R4-F3: ordinary admission through the real child and report projection,
  using the corrected JSON handling.
- R5-F4: restoration of all five incoming caller settings after success
  and a representative failure.

Preserve the correctly attributed results already obtained for R1-F4,
R5-F1, R5-F2, and R5-F3. Do not describe the prior 62/63 admission run as
an overall pass.

Before another build, inspect any surviving output from the failed
467-second attempt. If it is unavailable, say so explicitly.

Capture complete build stdout/stderr and exit status from the start of
the next attempt. Diagnose a failure before spending another broad run.

Use a retained, correctly identified frozen candidate where the existing
workflow permits. Run the missing focused cases and required gate;
do not repeat the entire admission suite merely to exercise one case
unless a concrete dependency or required gate makes that necessary.

Export the relevant results, retain their producing identities, and
submit the next review after the required evidence is complete.

Buff #26

Starting reported head: 45427841.

Correct these four required findings as one coherent batch:

- R5-F1: protect the previous release BEFORE moving it. Permit cleanup
  only after confirmed promotion or restoration. Preserve and identify
  the recovery location if restoration has not completed.

  Exercise interruption/unwind immediately after the backup move on a
  disposable target, and retain the occupied-destination control.

- R6-F1: use Windows-compatible case-insensitive matching for the
  existing applicable production configuration paths.

  Retain acceptance of unrelated test-project user settings. Do not
  broaden the scan to unrelated files.

- R4-F1: use two ZIPs that EACH pass the standalone validator before
  demonstrating deterministic-build disagreement.

  Keep the required timestamps. Use a valid differing ZIP representation,
  such as a supported compression difference, under the disposable
  fixture's cleanup root.

  Capture both validator successes, distinct package digests, the actual
  inter-build refusal, and unchanged previous-release hashes.
  Correct the interpretation of the old evidence without altering it.

- R6-F2: run the existing four-loader static audit on the packaged DLL
  from the corrected freeze. Bind its results to that ZIP, DLL hash,
  MVID, and source. Preserve the old audit as historical evidence.

R6-F3 remains optional and nonblocking.

Waking #12

Starting reported head: 495635d7.

Correct these three required findings as one coherent batch:

- R1-F1: include actual authored compiler inputs in provenance checks
  independently of Git ignore rules, and use Windows-compatible matching
  for selected artwork.

  Exercise ignored mixed-case artwork and an ignored authored .cs file
  that the production project really compiles.

  Preserve narrow existing generated-file exceptions and documentation
  controls. Do not build a new input-inventory framework.

- R5-F1: identify the physical build-owned publication directory through
  the existing identity/path machinery, including alternate spellings.

  Resolve-Path alone does not establish equivalence for the reported
  Windows short-path alias.

  A build-owned ZIP must use the publication lock and must be refused if
  its required receipt is absent, including after an interrupted
  publisher releases the lock. Preserve the supported handling of
  genuinely external packages.

- R4-F4: restore recon correctly for an interrupted legacy reinstall
  when the replacement directory already exists but recon.flag does not.

  Use the journal's current reconEnabled field or its legacy recon field
  as appropriate for interrupted recovery. Preserve normal reinstall
  behavior. Exercise both Boolean values in the existing disposable
  recovery fixture.

Keep the accepted R5-F3 deferral and optional R6-F1 nonblocking.

Skip #3

Starting reported head: 4f3ef4aa.

Correct the path bug and close the two specific evidence gaps together:

- R10-F1: resolve a relative ProductZip through PowerShell's literal
  filesystem-path semantics BEFORE passing an absolute path to .NET
  File.Copy.

  A bare Path.GetFullPath(relativePath) can still use the wrong process
  working directory and is not an adequate correction.

- R8-F1: use the already-built clean PRODUCTION ZIP for the positive
  handoff through the real copy/validation/extraction helpers.

  Combine this with the relative-path regression: place a different
  same-named ZIP in the process working directory and demonstrate that
  the caller-selected production ZIP is used.

  Compare the admitted ZIP and extracted DLL identities to the genuine
  production package's manifest.

- R9-F3: exercise the actual runner setup/cleanup path after successful
  validation and handoff, followed by a later setup failure.

  Verify removal of owned temporary copies and extraction directories.
  A missing-validator failure occurs too early; cleanup invoked only
  by the fixture's own finally does not prove the runner's cleanup.

  Use disposable filesystem targets and make the fixture incapable of
  reaching a native game transaction.

Preserve the removal of unnecessary mission-added native-runner machinery.
Do not reintroduce it to implement these tests.

VALIDATION ALLOWANCES

Read the current records and apply these one-time changes:

- TRE: add 20 minutes to the release-and-resume assignment allowance,
  increasing its total from 30 to 50 minutes.
- Waking: add 10 minutes, increasing its total from 30 to 40 minutes.
- Buff: retain the existing 30-minute allowance.
- Skip: retain the existing 30-minute allowance.
- Mounted: retain its existing 30-minute integration/release allowance.

Preserve all prior spending, including failed runs and direct tests.
Do not reset ledgers or borrow unused time from completed repositories.

Count all new validation: reproductions, direct tests, builds, gates,
packaging, and reruns. Use existing accounting; add no budget tooling.

Capture logs before launching expensive work. Serialize expensive
validation on the shared Windows host to avoid avoidable contention.
Batch corrections before candidate qualification.

Use focused behavior checks first. Broaden only for a concrete remaining
risk or an existing required gate. Reuse unchanged evidence only with
accurate source and artifact attribution.

Do not start a known run that cannot fit the remaining allowance.
Finish necessary cleanup safely and record actual costs.

BOUNDARIES AND COMPLETION

The previous scope freeze remains in force:
- UMM 0.31.1 minimum and pinned compile baseline.
- Agreed static loader checks.
- Accurate packages, receipts, and evidence.
- Direct regressions introduced by this mission.
- Mounted's already-authorized integration and alpha release.

No new gameplay features, general build-system redesign, controller
redesign, evidence framework, or native-test campaign.

Native gameplay and combined-stack testing remain NOT RUN.
No installation, game launch, save changes, live UMM changes, use of
another enrollment's game copy, or Nexus publication.

Accepted optional findings and deferrals remain nonblocking.
New required findings must identify a concrete defect within the frozen
scope. Record unrelated improvements as follow-up.

Stop TRE, Buff, Waking, and Skip at AWAITING_OWNER_REVIEW.
They are not authorized for merge or release in this continuation.

One blocked repository must not prevent independent progress elsewhere.

Update FLEET-STATUS and mission memory with:
- Dice and Preserver completed.
- Mounted's actual review, merge, and publication outcome.
- Each remaining PR's source/archive head and controller state.
- Findings resolved or still required.
- Validation producing commits, results, and actual spending.
- Any exact permission rejection or other blocker.

Preserve all history and do not mark the seven-repository mission complete
while required work remains.
----- END OWNER AMENDMENT 3 -----

----- BEGIN OWNER AMENDMENT 4 (captured 2026-11-26T22:28:13Z; message sha256 233be3c4599450123c6cf60e0d1f7dade7b930492a609d24fffc7a40ae7df850) -----
WAKING — VALIDATE THE EXISTING CORRECTION

Repository: howardreith/KingmakerWakingRune
PR: #12
Current candidate: 79f16870661acb5df26e842f43dfe7ed04ff2261
Rounds used: 7; retain TOTAL cap 8.

I authorize one supported resumption of the CURRENT validation block.
Preserve the same enrollment, PR, findings, and history.

Add 15 minutes to this assignment's validation allowance, increasing its
total from 40 to 55 minutes. Preserve all prior spending.

Run the required, fully captured 149-case baseline gate on the unchanged
79f1687 candidate first.

Do not begin another speculative correction batch before validating the
existing one-line predicate fix.

If the gate passes, submit round 8 with accurate dispositions for:
- R1-F1.
- R7-F2.
- R5-F1.
- R7-F1.

Bind the submitted results and applicable package/static-audit identities
to their actual producing source and bytes.

The 141/149 gate on 6b23b26 remains a failed overall run. Its passing rows
may be cited accurately with the demonstrated source continuity, without
representing that run as a full pass.

Describe link evidence literally: the exported fixture exercised a hard
link. Do not state that a symbolic-link path was executed unless it was.
Present the existing source checks and evidence for the reviewer to assess.

If the gate fails, diagnose the specific failure using the captured output
before any further run. Make only corrections required by that concrete
failure within the frozen scope and remaining allowance.

Stop at AWAITING_OWNER_REVIEW. No Waking merge or release is authorized yet.

VALIDATION COORDINATION

Use one foreground coordinator to launch expensive validation for this
assignment. Other workers may inspect or prepare changes, but must return
them to the coordinator for builds and gates.

Do not overlap this mission's expensive builds, suites, or gates.

Use the existing host guards and coordination mechanisms. Check required
host conditions before expensive work. If another session's game or
transaction blocks a guarded operation, wait for a suitable window or
continue independent nonconflicting work.

Do not stop another session's game/process, bypass its lock, or create a
new host-coordination framework.

Capture stdout/stderr and exit status from the start of each expensive
invocation. Preserve useful failure output before removing temporary work.

Count all validation spending, including failed runs, direct reproductions,
builds, packaging, and gates. Read live records before applying each
one-time allowance adjustment.

Reuse correctly identified unchanged artifacts where the existing workflow
permits. Build a necessary final candidate once and reuse it for its
applicable checks.

No automatic review-cap increases, further no-progress resumptions, or
additional time extensions beyond this instruction.

COMMON BOUNDARIES

Keep the frozen mission scope:
- UMM 0.31.1 minimum and pinned compile baseline.
- Agreed static loader checks.
- Correct packages, receipts, evidence, and direct mission regressions.
- The merges and Buff release expressly authorized here.

No new gameplay features, general build-system redesign, controller
redesign, evidence framework, or native-test campaign.

Memory Leak Fix remains excluded.
No installation, game launch, save changes, live UMM changes, use of another
enrollment's game copy, or Nexus publication.

Accepted optional findings and deferrals remain nonblocking.
Use normal branch protections, permissions, and publishing controls.
Do not force-push, rewrite history, overwrite published tags/assets, or
work around permission denials.

A blocked repository must not stop independent authorized work elsewhere.

----- END OWNER AMENDMENT 4 -----

----- BEGIN OWNER AMENDMENT 5 (captured 2026-11-27T01:46:42Z; message sha256 ff313c69d79a1d57d558353d7efc6f332811140685493364d05efbdc4239b7d8) -----
NEW OWNER AMENDMENT
ID: umm-0311-final-two-20261126-02

NEW BOUNDED AUTHORIZATION

TRE #220:
- Reported current head: ffdd7fa7a5.
- Reviews used: 7.
- New TOTAL review cap: 9.
- Add 20 validation minutes, increasing this assignment's allowance
  from 80 to 100 minutes.

Waking #12:
- Reported current head: 79f1687.
- Reviews used: 8.
- New TOTAL review cap: 10.
- Add 30 validation minutes, increasing this assignment's allowance
  from 55 to 85 minutes.

These are maximum allowances, not work that must be consumed.

I authorize supported resumption of the current blocked enrollments.
This amendment also covers one further supported continuation per repo,
if needed to address the listed findings or direct regressions from their
correction, within the two-submission limit and validation allowance.

Preserve every stop, review, finding, counter and spending record.
Use documented controller operations; do not edit state or disable guards.
Apply mission amendments and cap changes separately where required.

No further extension beyond these limits is authorized.

TARGET OUTCOME

Bring both existing PRs to AWAITING_OWNER_REVIEW.
Do not merge or release TRE or Waking in this continuation.

Use one coherent correction batch per repo. Do not build a separate full
production candidate for each finding.

WAKING — FOUR REQUIRED FINDINGS

Correct these as one batch across the existing input and package helpers.

R1-F1 and R8-F1: compiler-source ownership and Git spelling.

- Establish the selected source's physical repository-relative location
  before granting an external-source exception.
- A junction pointing back into the repository must not disguise an
  ignored repository source as an external input.
- Resolve repository inputs to Git's tracked spelling using literal,
  Windows-compatible matching.
- Use the returned canonical paths in both Git membership and the
  producing-commit comparison.
- Preserve genuine generated files, supported genuine external inputs,
  non-ASCII filenames and ordinary clean builds.
- If physical ownership cannot be established for an ambiguous linked
  input, refuse it instead of granting an exemption.

Exercise:
- The ignored LocalRoot.cs selected through a directory junction back
  into the clone.
- A tracked root source selected with different casing: its genuine
  producer must pass, and a receipt naming a producer missing or differing
  in that source must be refused.
- Preservation of the previous publication pair and successful
  qualification after removing the negative fixture.

R5-F1: package ownership independent of an alias-side receipt.

- A receipt beside a file link must not disable the link/publication
  checks.
- Bind source identity and classification to the actual file being read.
- Use the canonical publication lock and receipt where ownership can
  be established.
- Explicitly reject ambiguous linked package inputs where necessary.
- Preserve independent ordinary external copies.
- A separate pathname/link-count sample after copying is insufficient.

Exercise the existing external hard-link fixture WITH a matching
alias-side receipt:
- While the canonical publication lock is held.
- After lock release while the canonical receipt is absent.

If classification still involves separate observations that can change
during rollback, cover that interval or remove the separation.

R8-F2: protect the actual ZIP target's receipt from report writes.

This finding concerns a symbolic-link INPUT PACKAGE whose target's
conventional receipt is selected as ReportPath.

- Protect the actual target ZIP and its receipt, or refuse the ambiguous
  linked input/report operation before writing.
- Retain the existing checked-handle report write.
- Use an actual symbolic-link fixture and record its link type.
- Demonstrate refusal and unchanged target ZIP and receipt hashes.

Use existing helpers and disposable fixtures. Do not introduce a general
filesystem framework or promise support for every alias arrangement.

Run focused checks first, then freeze one corrected candidate and run the
existing gate with the necessary cases. Preserve prior 149/149 evidence
under 79f1687; do not label it as execution on the new source or force the
new suite to retain that exact count.

Use fresh PowerShell processes for validation.
R8-F3 remains optional and nonblocking.

HOST AND DRIVER DISCIPLINE

One foreground coordinator launches expensive validation for this mission.
Other workers may prepare changes, but must return them for scheduled
builds and gates.

Before an expensive invocation:
- Set and verify the intended repository working directory.
- Verify the selected reference bundle using existing checks.
- Check the existing host requirements.
- Ensure the final candidate will survive fixture cleanup.
- Start complete stdout/stderr and exit-status capture.

Do not overlap this mission's expensive validations.
Do not repeatedly launch builds while another session makes the host
unavailable. Continue independent nonconflicting work where possible.

Do not stop another session's game/process, bypass a host guard, or build
new host-coordination infrastructure.

Count all validation spending, including failed runs and direct probes.
Preserve prior charges and distinguish waiting from actual validation.

SCOPE AND STOP CONDITIONS

Keep the existing frozen scope:
- UMM 0.31.1 minimum and pinned compile baseline.
- Agreed static loader checks.
- Correct packages, receipts and evidence.
- The listed defects and direct regressions from their correction.

No new gameplay work, general build-system or provenance redesign,
controller changes, new libraries, or native-test campaign.

Native gameplay and combined-stack testing remain NOT RUN for this mission.
No installation, game launch, save changes, live UMM changes, use of another
enrollment's game copy, or Nexus publication.
Memory Leak Fix remains excluded.

Accepted optional findings and deferrals remain nonblocking.
Do not reopen the five completed repositories.

Stop each remaining repo at AWAITING_OWNER_REVIEW.
Stop at the stated review or validation limits if unresolved.
A blocker in one repo must not prevent independent authorized work in
the other.

Update FLEET-STATUS and mission memory with:
- The five completed repositories.
- This new amendment ID and its one-time allowance applications.
- TRE and Waking's actual source/archive heads and controller states.
- Resolved and remaining required findings.
- Producing commits, candidate locations, artifact identities, checks
  and actual spending.
- Any exact host, permission or cleanup blocker.

Do not mark the seven-repository mission complete while required work
remains.
----- END OWNER AMENDMENT 5 -----
```

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
