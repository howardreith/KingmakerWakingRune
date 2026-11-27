<!-- agent-review:v1 enrollment=kwr-codex-umm-0311-baseline-20261123-20261123181024-1057 key=r15-request part=1/2 sha256=a850773533db6bd3658409112ec86cd147e27e7a3e17300dc216966c34f15041 -->
_Part 1 of 2._

### Claude Opus — review request — round 15 — candidate `7de6a0d57e`

| Field | Value |
|---|---|
| Request ID | `kwr-codex-umm-0311-baseline-20261123-20261123181024-1057-r15-358d61` |
| Checkpoint | `final` (final) |
| Candidate H | `7de6a0d57ed0f3ca59ededf6d1e17caf74bfca8e` |
| Target B (`main`) | `480cdbd31425c8855f5856936aeaff60ae236d88` |
| Merge base M | `480cdbd31425c8855f5856936aeaff60ae236d88` |
| Mission | `kingmaker-umm-0311-mission-20261123` sha256 `f7d990f396` |
| Workflow | `6d613defe7` |
| Owner guidance | none |
| Owner amendments | revision 9, sha256 `40434ffc57b6ea28b5e4600c5b724c85c8831eab3014419806bf9d80caa1a919` |
| Developer observed | foreground session `e178a93b-ca53-4b6b-a4d2-a357d48eaa77`: models ['claude-opus-5-5'], effort ['high'], permission modes ['auto'] (CLI-recorded transcript; the headless permission profile does not apply to a foreground session) |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `max`, read-only sandbox |
| Round budget | 15 of 15 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

Prepared under owner amendments revision 9 sha256 40434ffc57b6ea28b5e4600c5b724c85c8831eab3014419806bf9d80caa1a919 under owner authorization umm-0311-waking-receipt-pair-20261127-06 (the one review it permits, round 15). Round-14 review correction for Waking Rune PR #12 in the external-package staging helper only: R14-F1. Code and tests 41d3a48 (scripts/KwrPackage.ps1 and scripts/Test-LoaderBaseline.ps1); 7de6a0d adds evidence and documentation only. Product sources and material build inputs unchanged.

The condition was first observed on the reviewed head 166fbb9 (docs/evidence/umm-0311/reproduce-r15.json).

How both files are bound: the selected pair is the ZIP held by the helper's handle and the receipt beside that ZIP's physical path. (1) Selection: right after the ZIP handle is confirmed to hold the classified file, the receipt is opened through KwrFileIdentityV9.OpenReadShared (read and delete sharing, the same as an external ZIP, so a publisher's rename or rollback is never obstructed) and its file identity is recorded with the ZIP's; file or folder not found records 'absent'. (2) Pair check, run at selection, again after the ZIP and receipt bytes are copied, and again after validation before any qualification is returned: the ZIP's physical path must name the held ZIP identity, and the receipt name must name the recorded receipt identity (one identity-bearing open, IdOrAbsent) or still be absent. (3) The receipt bytes are read from the held receipt handle, not from the name, so the bytes validated belong to the identity checked; after validation the private receipt copy must still hash to those bytes. So a temporary pair that occupies both paths during the receipt observation cannot qualify the selected ZIP: if it is still there at a check the ZIP path names another file; if the original pair is back the receipt name names another receipt. ReadAll was not merely moved earlier and no ZIP-only check was added; no folder timestamp is used, and only these two files decide (unrelated entries of the folder do not). Receipt absence keeps its handling (an absence must still hold at each check: 'receipt appeared'). Build-owned pairs are still read under the canonical publication lock (the lock keeps that pair together; their receipt bytes now also come from the held receipt handle).

Retained pair: retained production pair at <dev>\KingmakerWakingRuneLab\worktrees\umm-0311-final-pair-12bf7d5\producer: producing commit 12bf7d51b3129fa40cff9ca1471eda647ef7bb70 (clean), ZIP 8f757331b42ad157939108702fb59c8c9aae031d0416ee271ac792a0aaa4d344, receipt ba8e64d33d79ff474f8675d31c696d41c6da33f79111e6c6e208587a6994e2c8, packaged DLL 7cd3cb09f12f75a7247c1b401673528a0773a139e59dc3a940da860c28f24904, MVID ccc59117-f992-44ce-9355-540d35f181f0; validated again with the corrected tooling after the round-15 focused clone was removed: validator exit 0, verified-current, producer status entries 0; no material build input changed between 12bf7d5 and 41d3a48, so it was not rebuilt (docs/evidence/umm-0311/postcleanup-r15.json; built in round 13, docs/evidence/umm-0311/retained-r13.json). Its DLL has the full SHA-256 and MVID of the DLL in docs/evidence/umm-0311/api-audit.json, so the existing four-loader static audit applies unchanged (not rerun). R8-F2 stays under the owner's amendment: the real file symbolic-link report case remains NOT RUN with the host's error. R11-F4 stays deferred. Native gameplay and combined-stack testing NOT RUN.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/agent_review.py verify --enrollment kwr-codex-umm-0311-baseline-20261123-20261123181024-1057 --gate baseline | yes | pass | 756.0s | Controller receipt gate-baseline-7de6a0d57ed0-d4433dcb35e-ac9ec8ca62a-20261127T175607798993Z.log on 7de6a0d: Test-LoaderBaseline.ps1, 180/180 executed checks passed, in fresh Windows PowerShell processes on a temporary --no-hardlinks clone; recorded separately as NOT RUN: report-through-a-file-symbolic-link-input-cannot-overwrite-the-target-receipt: this host refused to create a file symbolic link: Administrator privilege required for this operation.. |
| Invoke-KwrFocusedR15.ps1 -Mode Reproduce on 166fbb9, -Mode Focused and -Mode PostCleanup on 41d3a48 (exported as focused-r15-driver.ps1.txt) | no | pass | 29s (12s + 13s + 4s; plus a first Focused attempt of 12s that passed the same 5 cases but exited 1 because its output path was wrong, kept in the fleet records) | Reproduce 2 of 2 (defect observed), focused 5 of 5 (corrected, with ordinary controls), post-cleanup 1 of 1: retained production pair at <dev>\KingmakerWakingRuneLab\worktrees\umm-0311-final-pair-12bf7d5\producer: producing commit 12bf7d51b3129fa40cff9ca1471eda647ef7bb70 (clean), ZIP 8f757331b42ad157939108702fb59c8c9aae031d0416ee271ac792a0aaa4d344, receipt ba8e64d33d79ff474f8675d31c696d41c6da33f79111e6c6e208587a6994e2c8, packaged DLL 7cd3cb09f12f75a7247c1b401673528a0773a139e59dc3a940da860c28f24904, MVID ccc59117-f992-44ce-9355-540d35f181f0; validated again with the corrected tooling after the round-15 focused clone was removed: validator exit 0, verified-current, producer status entries 0; no material build input changed between 12bf7d5 and 41d3a48, so it was not rebuilt (docs/evidence/umm-0311/postcleanup-r15.json; built in round 13, docs/evidence/umm-0311/retained-r13.json). Publisher rollback is covered by the gate rows cited below, not by this driver. |
| Native checks | no | not_run | 0 | Not authorized. |

**Evidence references:**
- prepared under owner amendments revision 9 sha256 40434ffc57b6ea28b5e4600c5b724c85c8831eab3014419806bf9d80caa1a919
- docs/evidence/umm-0311/INDEX.md
- docs/evidence/umm-0311/reproduce-r15.json
- docs/evidence/umm-0311/focused-r15.json
- docs/evidence/umm-0311/postcleanup-r15.json
- docs/evidence/umm-0311/focused-r15-driver.ps1.txt
- docs/evidence/umm-0311/retained-r13.json
- docs/evidence/umm-0311/retained-build-receipt-r13.json
- docs/evidence/umm-0311/api-audit.json
- docs/UMM-COMPATIBILITY.md
- controller gate receipt gate-baseline-7de6a0d57ed0-d4433dcb35e-ac9ec8ca62a-20261127T175607798993Z.log

**Prior findings to assess:** `R14-F1`

**Owner amendments to the mission — revision 9, captured-source sha256 `40434ffc57b6ea28b5e4600c5b724c85c8831eab3014419806bf9d80caa1a919`.** Applied by the owner with `resume --amend-mission`; each owner message is preserved verbatim, in order. The effective task is the original mission plus these amendments. The reported work comes from a developer invocation whose input included them, and the reviewer of this round receives a byte-exact copy of the captured document. Public rendering after the controller's standard sanitization; the digest identifies the private captured bytes, which this rendering can differ from.

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

----- BEGIN OWNER AMENDMENT 6 (captured 2026-11-27T04:52:31Z; message sha256 202caf5acb280d405a448022cab9a678ad07265720bbd4af1a8bb7ad4c5be8de) -----
NEW OWNER AMENDMENT:
umm-0311-readiness-closeout-20261126-03

TARGET OUTCOMES

Bring these two existing PRs to AWAITING_OWNER_REVIEW if their final
reviews approve them:

- TRE #220.
- Waking #12.

This amendment does not authorize merging, tagging, releasing,
installing, or launching either project.

Dice, Preserver, Mounted, Skip, and Buff remain completed for this
mission. Do not reopen their findings, change their releases, rebuild
their packages, or run additional qualification for them.

REVIEW AND VALIDATION LIMITS

TRE:
- Existing enrollment: PR #220.
- Reported current PR head: a35ca74489.
- Retained production commit: c67881f3fb.
- Reviews used: 9.
- Raise the TOTAL review cap to 10, allowing exactly one additional
  documentation-only review submission.
- Keep the existing validation allowance at 6,000 seconds.
- Do not add validation time.

Waking:
- Existing enrollment: PR #12.
- Reported current PR head: 10ac278.
- Retained production commit: 6869e55.
- Reviews used: 9.
- Keep the TOTAL review cap at 10.
- The unsubmitted tenth review remains available; do not count the
  blocked developer report as a completed review.
- Keep the existing validation allowance at 5,100 seconds.
- Do not add validation time.

I authorize supported resumption of each current blocked enrollment
for this amendment and its one permitted review submission.

Apply the owner amendment and any review-cap change through the
documented separate operations where the controller requires that.
Preserve every prior stop, finding, assessment, and spending record.

Do not create replacement enrollments, reset counters, disable
guards, or extend these limits automatically.

WAKING — NARROW OWNER DECISION ON R8-F2

I explicitly amend this mission’s acceptance criteria for R8-F2:

Execution of the real Windows FILE symbolic-link fixture is accepted
as a documented, nonblocking evidence limitation for this mission,
because this host cannot create that fixture under its existing
permissions.

This decision withdraws only the requirement to execute that one
fixture before this mission’s owner-review handoff.

It does not:
- Claim that the symbolic-link fixture passed.
- Treat a HardLink or directory junction as equivalent evidence.
- Accept a known defect that can overwrite the actual ZIP or its
  receipt.
- Waive any of the eight other pending corrections.
- Change the required configured gate.
- Establish native gameplay or combined-stack compatibility.

Keep the real file-symbolic-link case recorded as NOT RUN, including
the actual privilege error. Keep its existing test available.

Use the development system’s documented disposition for a requirement
withdrawn by an owner amendment: cite this amendment in the R8-F2
rebuttal. Do not mark the missing execution as fixed, passed, or a
false positive.

The reviewer must assess the remaining protection as implemented:
- Physical target and protected receipt are bound to the ZIP actually
  read.
- Report output cannot overwrite that ZIP or its protected receipt.
- The destination write retains its existing identity check.

If the reviewer identifies a concrete remaining defect in those
protections, that defect remains blocking. This evidence exception
does not waive it.

Do not enable Developer Mode, change user rights, request elevated
execution, or create new infrastructure for this one fixture in this
pass.

WAKING — SUBMIT THE CORRECTIONS ALREADY PREPARED

Prepare the updated developer report under this amendment and submit
the still-available tenth review.

The reviewer must assess:
- R5-F1.
- R9-F1 through R9-F7.
- R8-F2 under the narrowly amended evidence requirement above.

Do not predeclare the eight corrections independently verified.
Their source changes and tests are prepared, but the reviewer must
still assess them.

Preserve and reuse:
- Production commit 6869e55.
- Current code/evidence head 10ac278, subject to any necessary
  documentation-only amendment record.
- worktrees\umm-0311-final-pair-6869e55\producer.
- ZIP 3d7804a9…
- DLL 7cd3cb09…
- The matching receipt and successful post-cleanup current-provenance
  validation.
- The existing static audit bound to that same DLL.

Report the evidence accurately:
- 166 executed gate checks passed.
- One actual file-symbolic-link case was NOT RUN.
- The separate focused checks passed 10/10.

Reuse the correctly bound gate and focused results wherever the
existing controller permits. Do not rebuild the retained production
pair or rerun focused checks merely because the owner amendment
changed the report.

If a fresh configured gate is mandatory, run only what the workflow
requires after checking the live remaining allowance. The previous
gate took about 640 seconds. Do not spend the remaining allowance on
optional preliminary reruns.

If the review approves the current corrections and the amended
evidence disposition, finalize normally and verify the current
AWAITING_OWNER_REVIEW handoff.

SHARED GIT STORE AND LEFTOVER FOLDERS

Leave these protected remnants in place during this closeout:

- C:\Dev\KingmakerGunslingerLab\worktrees\
  focused-r9-35e4876cf5ba4935933f23594f0c5f09

- C:\Dev\KingmakerGunslingerLab\worktrees\
  probe-s34-326444

The line breaks above are for readability, not part of either path.

Do not clear attributes, change permissions, force-delete shared
files, run another blanket repair, or work around a cleanup refusal.

Do not ask the owner to perform recursive deletion as a substitute
for a refused automated cleanup.

Record the known attribute side effect and the limits of the repair
evidence. Do not claim content corruption without evidence, and do
not claim exact restoration without evidence.

The protected remnants are not a new readiness prerequisite.

Preserve both final producer directories and their artifacts.

No new disposable clone should be necessary for TRE’s wording
correction. If a temporary clone is required by an existing Waking
gate, use an existing supported independent-copy route that avoids
shared Git object hard links, such as git clone --no-hardlinks.
Do not create a new cleanup framework.

SCOPE AND EXECUTION

Keep the existing scope freeze:
- UMM 0.31.1 minimum and pinned compile baseline.
- Agreed static loader checks.
- Correct packages, receipts, and evidence.
- Direct regressions in the mission’s existing changes.

Accepted optional findings and deferrals remain nonblocking.
Do not reopen them or start a broad new audit.

Unrelated improvements and future hardening remain follow-up work.
Concrete blockers in the required changed behavior remain blocking.

Serialize this mission’s expensive validation through one foreground
coordinator. Check the working directory, required references, and
existing host guards before any required gate.

Do not stop another session’s game or process, bypass locks or host
guards, or use another enrollment’s game copy.

Capture useful output and exit status from the start of each run.
Count all actual validation spending, including failed runs.

Native gameplay and combined-stack testing remain NOT RUN.
No installation, game launch, save changes, live UMM changes,
Nexus publication, force-push, history rewrite, published asset
replacement, or permission bypass.

Memory Leak Fix remains excluded.

A blocked repository must not stop independent authorized work in
the other repository.
----- END OWNER AMENDMENT 6 -----

----- BEGIN OWNER AMENDMENT 7 (captured 2026-11-27T13:08:13Z; message sha256 e8232496a2b8e73c9b8d9aa94c4eaec41da2ad31d1d252fd06ceb277a4960e4a) -----
NEW OWNER AMENDMENT:
umm-0311-tre-merge-waking-correction-20261127-04

WAKING — ONE COHERENT CORRECTION BATCH

Current PR: #12.
Current head:
10ac278808e71e2138110519364358f13ab9d62b

The retained 6869e55 production pair remains historical evidence.
The latest independent review is round 10, changes_required.

Correct only these five required findings and direct regressions
from their corrections:

1. R9-F2 — unlocked reader can obstruct rollback.

   Ensure every ZIP handle opened before canonical ownership and
   lock admission permits deletion/rename by the publisher.

   If a path initially classified as external now identifies a
   build-owned package, refuse it or establish the proper canonical
   lock before admission.

   Extend the existing disposable rollback fixture: redirect a
   directory junction after external classification and overlap the
   subsequent read with receipt-placement failure.

   Require reader refusal and restoration of both previous hashes
   at their final paths.

2. R10-F1 — moved target receipt can escape report protection.

   Capture the identities of existing conventional target receipts
   when establishing the ZIP’s protected names, before copying the
   ZIP bytes, and retain those identities through the checked
   destination write.

   An explicit refusal when protection of a linked input cannot be
   established is acceptable. Preserve ordinary independent-copy
   behavior.

   Extend the existing moved-input fixture using a hard-linked ZIP,
   no explicit ReceiptPath, and a target receipt moved to ReportPath
   during snapshot acquisition.

   Require refusal and unchanged ZIP and receipt hashes.

3. R9-F4 — literal compiler-input names.

   Decode each inventory entry according to its existing kind.
   Remove a reference alias only from an actual reference entry.

   Preserve complete literal response-file and file-option names,
   including equals signs, and resolve their canonical tracked
   spelling using the existing machinery.

   Exercise the existing response-file route with a tracked root
   filename containing "=". Show genuine-producer qualification and
   refusal of a producer differing only in that file.

   Include a focused control for the file-option decoding boundary.

4. R10-F2 — authoritative material-source paths.

   Derive material Git paths from the freshly evaluated compiler
   inventory already used by validation, or verify the receipt’s
   repository classifications against that inventory before relying
   on them.

   Preserve existing generated-input and genuine external-input
   handling. Do not add a new receipt or provenance framework.

   Use the existing tracked root-source fixture with a receipt whose
   source classification disagrees with the live inventory and a
   claimed producer differing only in that source.

   Require refusal of the incorrect pair and qualification of the
   genuine pair.

5. R10-F3 — fixtures must establish their advertised Git changes.

   Represent deletion explicitly in the variant helper. Do not use
   null-to-string conversion as a deletion signal.

   Construct each producer variant from the appropriate common
   baseline, changing exactly its named path.

   Before counting the validator result, assert and record the
   expected Git path, change type, and tree presence or absence.

   Isolate the casing controls from the equals-sign source.
   Correct both the configured gate fixtures and the focused driver.

   Preserve historical results and counts, while accurately
   explaining what the defective old rows did not establish.

Batch these corrections before the expensive validation.
Use the existing helpers and fixtures. No new framework, library,
general parser project, or broad MSBuild audit is authorized.

WAKING — PRESERVE ACCEPTED RESULTS

Do not reopen the accepted findings generally:
R5-F1, R9-F1, R9-F3, R9-F5, R9-F6, and R9-F7.

Preserve their existing controls while correcting the affected
paths.

The R8-F2 owner amendment remains in force:
- The real Windows file-symbolic-link fixture remains available.
- Its execution remains NOT RUN with the recorded privilege error.
- Hard-link evidence is not relabelled as a symbolic-link pass.
- No Developer Mode, user-rights, or elevation changes are authorized.
- The evidence exception does not waive R10-F1 or any other concrete
  defect in protecting the actual ZIP and its receipt.

WAKING — VALIDATION AND RETAINED CANDIDATE

Run focused reproductions first, checking each fixture’s defining
preconditions before counting its result.

After material helper corrections are complete, build and retain
one corrected final production ZIP/receipt pair using the existing
workflow. Do not relabel the old 6869e55 receipt as current.

Reuse that final pair for applicable checks. Preserve:
- Producing commit.
- Durable producer location.
- ZIP and receipt hashes.
- Packaged DLL hash and MVID.
- Successful current-provenance validation after fixture cleanup.

Reuse the existing four-loader static audit only if the new DLL’s
full SHA-256 and MVID match the audited DLL. Otherwise run the
existing audit on the corrected DLL.

Run the configured gate on the corrected candidate and submit the
review. Report executed passes and NOT RUN cases separately.

Preserve the old producer and evidence until the corrected handoff
has been accepted. Do not destroy the new retained candidate during
fixture cleanup.

WAKING — EXACT AUTHORIZED LIMITS

I authorize supported resumption of the current blocked enrollment
for the five findings above.

Raise the TOTAL review cap from 10 to 12.

This permits:
- One review of the coherent correction batch.
- One further review only if needed for remaining required findings
  or direct regressions.

If a no-progress stop occurs between those submissions, I authorize
one further supported continuation within this same cap and time
allowance. Preserve the stop and its history.

Apply one +1,200-second validation adjustment:
- Previous total: 5,100 seconds.
- New total: 6,300 seconds.
- Preserve all actual spending.

Read the live ledger before applying the adjustment and record this
amendment as its authorization. Apply it once.

Based on the reported 4,355 seconds spent, the adjusted remaining
allowance would be approximately 1,945 seconds. The live ledger is
authoritative.

These are ceilings, not spending targets. Finalize immediately after
an approving review; do not use the second review unnecessarily.

Apply amendments and cap changes using the documented operations,
separately where required. No replacement enrollment, counter reset,
guard removal, automatic extension, or further resumption is allowed
beyond this authorization.

Stop Waking at AWAITING_OWNER_REVIEW if approved. Its merge, tag, and
release are not authorized by this amendment.

COMMON BOUNDARIES

Dice, Preserver, Mounted, Skip, and Buff remain completed and
untouched. Memory Leak Fix remains excluded.

Keep the frozen UMM 0.31.1 mission scope. Optional findings and
accepted deferrals remain nonblocking. Record unrelated improvements
as follow-up without beginning them.

No gameplay features, controller redesign, evidence framework,
native campaign, installation, game launch, save changes, live UMM
changes, another enrollment’s game copy, or Nexus publication.

No force-push, history rewrite, published tag/asset replacement, or
permission bypass.

Use one foreground coordinator for expensive validation. Check the
working directory, reference inputs, and existing host conditions
before launching it. Do not stop another session’s processes or
bypass its locks.

Capture stdout/stderr and exit status from the beginning. Count all
validation spending, including failed runs, focused reproductions,
builds, packaging, and gates.

Keep non-validation Git/finalize elapsed time separately identified.
Do not diagnose the reported Git slowdown as part of this mission.

Leave the protected TRE remnants and both existing producer folders
untouched. No attribute sweeps, forced recursive deletion, shared
object-store repair, or cleanup workaround.

Use the existing supported independent-copy route for necessary new
disposable clones so their Git object files are not hard-linked into
the lab store. Do not create a new cleanup framework.

One blocked repository must not stop independent authorized work in
the other.
----- END OWNER AMENDMENT 7 -----

----- BEGIN OWNER AMENDMENT 8 (captured 2026-11-27T15:07:54Z; message sha256 09ca15100d10a34afd4829519ad1227808695dcb07facc01e4dfcf6917a48a7a) -----
Continue C:\Dev\missions\kingmaker-umm-0311.

This is a NEW owner authorization, identified as:

umm-0311-waking-final-correction-20261127-05

Save this instruction verbatim as a new owner-authorization file. Apply it once through the existing development system and existing enrollment. Preserve all previous spending, rounds, amendments, evidence, and history.

Read the current FLEET-STATUS, live validation ledger, remote PR head, controller status, and round-12 review before acting. If this authorization has already been applied, do not apply its allowances again.

OBJECTIVE

Bring Waking Rune PR #12 to AWAITING_OWNER_REVIEW by correcting the four required findings from round 12 and any direct regressions caused by those corrections.

The frozen UMM baseline remains 0.31.1. Do not lower it further or expand the loader/platform matrix.

COMPLETED REPOSITORIES

TRE, Dice, Preserver, Skip, Buff, and Mounted remain complete for this mission.

TRE #220 is merged as 52755b61. Its accepted archive is b4d49a1e. Retain published v0.0.197 unchanged, with release source 329ad1b0.

The retained TRE c67881f3fb development candidate remains PR evidence only. Do not relabel its audit as evidence for the published release.

Do not reopen findings, rebuild, retag, republish, or perform additional qualification for any completed repository. Preserve Mounted’s accepted prerelease status and existing stable release.

WAKING — STARTING STATE

Repository: howardreith/KingmakerWakingRune
PR: #12
Branch: codex/umm-0311-baseline-20261123
Reported head: 15590edb523716aebef880203959af735a69660a
Controller: BLOCKED, 12/12, no progress on R10-F1
Reported validation spending: approximately 6,249 of 6,300 seconds

Round-12 review:
https://github.com/howardreith/KingmakerWakingRune/pull/12#issuecomment-7221203348

Refresh these facts before applying the amendment. Do not overwrite unexpected concurrent work.

NEW BOUNDED ALLOWANCES

I authorize:

1. Raising this existing enrollment’s absolute review cap from 12 to 14: at most two additional review submissions.

2. Adding exactly 2,700 seconds (45 minutes) to the existing validation allowance, raising its absolute total from 6,300 to 9,000 seconds.

3. One resumption of the current no-progress stop.

4. If round 13 requires another explicit continuation, at most one further continuation within this same cap and allowance, solely for the four findings below or direct regressions introduced by their corrections.

Use the existing amendment, resumption, and budget mechanisms. Preserve the ledger and counters. Do not create a replacement enrollment, reset spending, or grant any additional extension.

This authorization does not authorize Waking’s merge, tag, release, installation, or native gameplay.

EXACT CORRECTION SCOPE

R10-F1 — initial report identity

Capture the existing report’s identity, or a missing-file result, with a direct identity-bearing open. Remove the separate existence-check/open gap.

Carry that result through the final checked write. A receipt substituted after the initial observation must survive.

Extend the existing fixture to start with a distinct existing report and substitute the receipt during initial destination lookup. Verify safe refusal and unchanged original ZIP and receipt hashes. Retain the ordinary existing-report success case.

R12-F1 — names belonging to the ZIP actually read

Ensure hard-link names and protected receipts correspond to the same ZIP handle that supplies the inspected bytes.

Where that correspondence cannot be established, refuse the report operation without writing over an input. The reviewer explicitly permits this conservative refusal; use the smallest correction that preserves the supported ordinary paths.

Preserve the delete sharing required for publisher rollback.

Extend the existing two-ZIP hard-link fixture: replace the opened pathname after physical-path capture and before name enumeration, using equal link counts. Select the original ZIP’s receipt as the report destination and verify preservation of the original ZIP and receipt.

Address R10-F1 and R12-F1 together so one correction does not reopen the other.

R11-F2 — external ZIP and receipt selection

Bind receipt selection to the selected external package snapshot. A package and receipt moving away and returning must not silently turn receipt-backed validation into receiptless validation.

Use the existing fixture with a receipt that ordinary staging should reject. Exercise temporary removal and restoration across the receipt probe.

Retain the stable independent receiptless-copy control and preserve the established publisher rollback behavior. Do not solve this by refusing every ordinary external package.

R12-F2 — compiler reference binding

Include the parsed reference binding in both compiler-inventory equality and the build-input fingerprint, using a consistent representation in the build and current-provenance validation.

In the existing fixture, distinguish:
- a literal Rules=Local/Accessibility.dll path; and
- alias Rules bound to Local/Accessibility.dll.

Use identical DLL bytes and preserve the tracked-file setup specified by the review, so the test isolates the compiler-binding change.

Record the actual compiler arguments. The old pair must be refused after the binding changes, and the original valid configuration must qualify again when restored.

OPTIONAL FINDINGS AND SCOPE CONTROL

R11-F4 remains deferred and nonblocking. Do not spend this allowance on its diagnostic wording.

Do not reopen accepted deferrals or unrelated findings. Any additional correction must identify its direct connection to one of the four findings or to a regression introduced while fixing them.

Record unrelated observations as follow-up work without implementing them in this pass.

Do not redesign the controller, build system, receipt architecture, evidence framework, or host coordination. Do not add gameplay features or a broader filesystem-hardening campaign.

VALIDATION AND EVIDENCE

First use the existing focused fixtures to reproduce the four reported conditions on the old behavior, then verify the corrections and their ordinary-success controls. Do not treat the previous passing gate as evidence that these exact cases already passed.

Run the configured full gate once the focused cases are ready. A second full gate is available only if a necessary correction after the first review requires it and the remaining allowance permits it.

Retain a production ZIP/receipt pair that qualifies under the corrected provenance rules. If the material-input or fingerprint changes invalidate the old pair, build a replacement once after those changes are settled. Do not edit an old receipt to make it appear current.

Validate the retained pair after disposable fixture cleanup, preserving the complete producer layout required by admission.

The existing four-loader static audit may be reused only if the final DLL’s full SHA-256 and MVID match the audited DLL. Otherwise perform the necessary audit within the same allowance.

The real file-symbolic-link execution exception remains in force. Keep that case explicitly NOT RUN with the recorded privilege limitation. Do not enable Developer Mode, elevate privileges, or substitute a hard-link pass for a symbolic-link pass.

Native gameplay and combined-stack testing remain NOT RUN.

Preserve the disclosed loss of the earlier JSON summaries and their surviving console exports. Do not recreate the missing files as though they were original machine outputs or rerun solely to recover their format.

For new runs, capture stdout, stderr, exit status, and duration from the start. Use distinct output paths so commands cannot overwrite prior evidence.

BUDGET AND HOST CONDITIONS

Count all validation spending, including failed attempts, focused reproductions, builds, packaging, post-cleanup validation, and gates, using the existing accounting rules.

Read the live ledger before applying the one-time adjustment and before expensive work. Plan the retained build, gate, and required final validation together so the allowance is not exhausted after producing an unvalidated candidate.

Serialize this mission’s expensive operations. Respect the existing host guards and other sessions’ locks and game processes. Wait or perform independent nonconflicting work when blocked.

Do not stop another session’s process or weaken a guard.

PRESERVATION AND EXCLUSIONS

Leave the protected TRE folders, shared Git store, all existing retained producer folders, and the earlier attribute-incident record untouched.

Do not clear file attributes or perform cleanup through shared Git hard links. Use the existing safe disposable-work mechanism for new fixtures.

Memory Leak Fix remains excluded.

No installation, game launch, save changes, live UMM changes, use of another enrollment’s game copy, Nexus publication, force-push, history rewrite, or overwrite of published tags/assets.

FINISHING AND STOP CONDITIONS

If the reviewer returns ready_for_owner_review, run the existing finalize procedure and verify the resulting reviewed source, archive head, remote PR head, clean worktree, and released controller lock.

Stop at AWAITING_OWNER_REVIEW. Keep PR #12 a draft pending my acceptance. Do not merge or release it.

If the cap, allowance, permission controls, or a blocker outside this authorization prevents completion, preserve the exact state 

---

<!-- agent-review:v1 enrollment=kwr-codex-umm-0311-baseline-20261123-20261123181024-1057 key=r15-request part=2/2 sha256=038708e27699ffa1678dde975df1e8d95d8711fb999432fb2ff034a26020063f -->
_Part 2 of 2._

and report it. Do not self-extend or begin another enrollment.

FINAL REPORT

Update FLEET-STATUS and mission memory with:

- The six completed repositories, unchanged.
- Waking’s actual source, PR head, archive if any, and controller state.
- Each of the four findings: reproduced, corrected, verified, or still open.
- Focused and gate results, including failures and separate NOT RUN cases.
- Retained producer, ZIP/receipt hashes, DLL hash/MVID, and audit reuse basis.
- Actual validation spending and remaining allowance.
- Any precise remaining blocker.

Do not mark the seven-repository mission complete while Waking still requires approval or its authorized completion steps remain outstanding.
----- END OWNER AMENDMENT 8 -----

----- BEGIN OWNER AMENDMENT 9 (captured 2026-11-27T17:39:55Z; message sha256 f842e61497059b770ded8cad3858f887c7bd08cf8f716aedd59dd98fb92d6269) -----
Continue C:\Dev\missions\kingmaker-umm-0311.

This is a NEW owner authorization:

umm-0311-waking-receipt-pair-20261127-06

Save this instruction verbatim and apply it once through the existing development system and existing enrollment. The previous authorization ending in -05 is fully used; do not reapply it.

Refresh FLEET-STATUS, the live validation ledger, controller state, remote branch, and PR head before acting.

OBJECTIVE AND STARTING STATE

Correct Waking PR #12 finding R14-F1 and obtain its next formal review.

Reported current head:
166fbb909da03d1ea98b02868cca53b37eed9191

Reported controller state:
BLOCKED at 14/14, review-round cap reached.

Reported validation spending:
Approximately 8,170 of 9,000 seconds.

Round-14 review:
https://github.com/howardreith/KingmakerWakingRune/pull/12#issuecomment-7223161258

All four findings from round 12, and R13-F1, are verified. Preserve those corrections. R11-F4 remains an accepted nonblocking deferral.

TRE, Dice, Preserver, Skip, Buff, and Mounted remain complete and untouched. Preserve their accepted releases and release channels.

BOUNDED AUTHORIZATION

I authorize:

- One resumption of the current cap stop.
- One additional formal review submission, raising the existing enrollment’s absolute cap from 14 to 15.
- One additional 900 seconds of validation allowance, raising the existing absolute allowance from 9,000 to 9,900 seconds.

Preserve all prior spending, rounds, amendments, and evidence. Apply the adjustment once using the existing mechanism.

No replacement enrollment, counter reset, further extension, or additional review beyond round 15 is authorized.

CORRECTION SCOPE: R14-F1 ONLY

Keep the correction within the external-package staging helper, its existing fixture, and directly necessary evidence.

The selected external ZIP and its receipt must be observed as one consistent pair for qualification. A temporary replacement must not supply a clean receipt that qualifies the original dirty pair.

Capture the adjacent receipt’s identity and bytes as part of selecting the external ZIP, carry that observation through snapshot creation, and verify that the receipt used belongs to the selected pair before granting frozen-candidate provenance.

Refuse a changed or ambiguous pair safely.

Do not merely move ReadAll earlier or add another ZIP-only identity check. Do not reintroduce folder timestamps or make unrelated sibling activity invalidate an otherwise stable package.

Preserve the canonical publication lock, delete sharing required for publisher rollback, existing receipt-absence handling, and ordinary external-copy behavior.

Do not redesign the receipt architecture or create a general filesystem concurrency framework. Do not reopen verified findings or implement optional R11-F4.

FOCUSED EVIDENCE BEFORE THE GATE

Extend the existing disposable external-pair fixture.

1. Use two independent ZIP copies with identical bytes and distinct receipt identities.
2. Establish that the stable clean pair qualifies.
3. Establish that the stable dirty pair is refused by default staging.
4. Reproduce R14-F1 on 166fbb9: select the dirty original, temporarily substitute the clean pair during the receipt read, and restore the original before the subsequent checks.
5. Show that the corrected helper refuses that mixed selection.
6. Record the selected ZIP identity, both receipt identities, input hashes, and actual qualification or refusal.

Keep the ordinary controls already required by this helper: stable clean and dirty pairs, a stable receiptless copy, unrelated sibling changes, receipt absence followed by restoration, and publisher rollback.

Use the existing fixture and deterministic boundaries. This is not authorization for an exhaustive filesystem-interleaving campaign.

Explain briefly how the implementation binds both files throughout selection and copying, so the correction is not justified solely by one passing test arrangement.

PACKAGE AND AUDIT REUSE

Prefer reusing the retained production pair from:

worktrees\umm-0311-final-pair-12bf7d5\producer

Verify its full identities against the committed records and confirm that material build inputs remain unchanged.

A change confined to KwrPackage.ps1 and its fixture should not require rebuilding the mod. If another changed file is a material build input, assess that honestly; do not alter an old receipt or weaken the fingerprint to preserve reuse.

Validate the retained pair using the corrected tooling after disposable fixture cleanup. Preserve the producer layout and all existing retained packages.

Reuse the four-loader static audit only after confirming the complete DLL SHA-256 and MVID match the audited binary. Do not rerun the audit solely because staging or test code changed.

GATE, REVIEW, AND BUDGET

After the focused reproduction and controls pass, run the configured candidate gate and submit review 15 through the existing controller.

Count all validation spending, including reproductions, failed attempts, focused checks, packaging or builds if actually necessary, retained-pair validation, and gates.

Read the live ledger before expensive work. Preserve complete stdout, stderr, exit status, and duration using distinct output paths.

Serialize this mission’s expensive operations and respect existing host guards. Wait for other sessions’ games or transactions to finish; do not stop them or bypass their locks.

Do not spend this allowance on unrelated cleanup, optional diagnostics, additional loader versions, or performance investigation.

EXISTING EXCLUSIONS

UMM 0.31.1 remains the frozen minimum and compile baseline.

The file-symbolic-link execution exception remains accepted. Keep that case explicitly NOT RUN with its privilege limitation; do not elevate privileges or enable Developer Mode.

Native gameplay and combined-stack testing remain NOT RUN.

No installation, game launch, save changes, live UMM changes, Nexus publication, or use of another enrollment’s game copy.

Memory Leak Fix remains excluded.

Leave the protected TRE folders, shared Git store, existing producer folders, and attribute-incident record untouched. Do not clear attributes or clean up through shared Git hard links.

FINISHING

If review 15 returns ready_for_owner_review, run the existing finalize procedure. Verify and record the reviewed source, archive head, remote PR head, clean worktree, and released controller lock.

Stop at AWAITING_OWNER_REVIEW. Keep PR #12 a draft pending my acceptance. This instruction does not authorize merging, tagging, or releasing Waking.

If the reviewer still requires changes or the allowance prevents completion, preserve the exact state and report it. Do not self-extend, suppress a finding, or mark readiness manually.

FINAL REPORT

Update FLEET-STATUS and mission memory with:

- R14-F1’s old-code reproduction, correction, and reviewer disposition.
- Actual focused and gate results, with NOT RUN cases listed separately.
- Current source, PR head, controller state, and archive if finalized.
- Retained-pair identity and the basis for package and audit reuse.
- Actual validation spending and remaining allowance.
- Any precise remaining blocker.

Keep the six completed repositories unchanged. Do not mark the seven-repository mission complete while Waking’s approval and completion steps remain outstanding.
----- END OWNER AMENDMENT 9 -----
```

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
