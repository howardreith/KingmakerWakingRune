<!-- agent-review:v1 enrollment=kwr-codex-umm-0311-baseline-20261123-20261123181024-1057 key=r04-request part=1/1 sha256=4272caa461138c25532bc39b602abd801c7c2b4b1470602f6fc5443922ae5576 -->
### Claude Opus — review request — round 4 — candidate `35dc80db3d`

| Field | Value |
|---|---|
| Request ID | `kwr-codex-umm-0311-baseline-20261123-20261123181024-1057-r04-96b7b1` |
| Checkpoint | `final` (final) |
| Candidate H | `35dc80db3d3e173d4369a65aad97bf1f803c693d` |
| Target B (`main`) | `480cdbd31425c8855f5856936aeaff60ae236d88` |
| Merge base M | `480cdbd31425c8855f5856936aeaff60ae236d88` |
| Mission | `kingmaker-umm-0311-mission-20261123` sha256 `f7d990f396` |
| Workflow | `6d613defe7` |
| Owner guidance | none |
| Owner amendments | revision 1, sha256 `82f5267951845e0da430ab1251c2ff7a86a75e72bee16193ef6936f3dd5b6f2d` |
| Developer observed | foreground session `e178a93b-ca53-4b6b-a4d2-a357d48eaa77`: models ['claude-opus-5-5'], effort ['high'], permission modes ['auto'] (CLI-recorded transcript; the headless permission profile does not apply to a foreground session) |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `max`, read-only sandbox |
| Round budget | 4 of 5 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

Prepared under owner amendments revision 1 sha256 82f5267951845e0da430ab1251c2ff7a86a75e72bee16193ef6936f3dd5b6f2d. Round-4 corrections for Waking Rune PR #12 (this repository only), addressing the complete open ledger in one batch. Tooling code 97a8659 (+ test-only afd8c2b, 5b91fa1); 258978c changes only README.md and docs/UMM-COMPATIBILITY.md; efc32d8 and 35dc80d add only docs/evidence/umm-0311/. Product source unchanged: shipping DLL 7cd3cb09f12f75a7247c1b401673528a0773a139e59dc3a940da860c28f24904.

Code: (1) Build.ps1 packages the DLL from an invocation-owned output directory and requires it to be the file Csc wrote (/out: of the recorded command line), so an older DLL at the conventional path cannot be packaged. (2) Replacement: backup moves and placements are in one rollback scope with completed moves tracked; if restoration itself fails the backups are kept and named. (3) Provenance: response files are expanded and every file they name is inventoried by content; tracked repository paths in the compiler inventory (embedded art under assets/) are material for the producing-commit comparison. (4) Offline pin definition: exact digest strings (\z) and archive digest/entry validated as a pair before the -DefinitionOnly return. (5) Validate-Package report publication: native-layout BY_HANDLE_FILE_INFORMATION (volume + full file index) and the write goes through the handle whose identity was checked. (6) KwrPackage records the install mode the validator established (frozen-candidate only for verified-current provenance; diagnostic; external-content-inspected). (7) KwrRuntime: Close keeps a 'reinstalling' phase (not launchable) and Reinstall resumes it; only recovery or a verified installation leaves it. Tests: the suite keeps the original alternate-version clone receipt and records the later rebuild separately; the alternate fixture version is chosen distinct from the shipping version (exercised with shipping 0.12.99). Docs/evidence: README audit procedure executed from a clean checkout of 5b91fa1 (readme-audit-run.json); PR #12 description distinguishes -ReceiptPath byte matching from -RequireCurrentProvenance (pr-12-description.json, read back). Onboarding: per the controller-recorded owner amendment of 2026-11-24, the effective profile (equal to the enrollment snapshot), check-project (problems []) and preflight (ok) were verified on 2026-11-24 and exported; the pre-enrollment chronology is unchanged. Native runtime NOT RUN.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/agent_review.py verify --enrollment kwr-codex-umm-0311-baseline-20261123-20261123181024-1057 --gate baseline | yes | pass | 322.4s | Controller receipt on 35dc80d: the gate runs Test-LoaderBaseline.ps1 on that commit; RESULT 108/108 loader-baseline checks passed. |
| scripts/Test-LoaderBaseline.ps1 (temporary clone of 5b91fa1) | no | pass | ~12 min | 108/108; docs/evidence/umm-0311/loader-baseline.json (108/108 on a temporary clone of 5b91fa1). Later commits change documentation and evidence only. |
| README audit procedure from a clean checkout of 5b91fa1 | no | pass | ~1 min | docs/evidence/umm-0311/readme-audit-run.json: audit project built from the two committed files, invoked from its documented output path, four profile rows, mod digest matching package-validation.json. |
| agent_review.py check-project / preflight --project kingmaker-waking-rune (2026-11-24) | no | pass | ~2 min | onboarding-check-project-20261124.json (problems []), onboarding-preflight-20261124.json (ok), effective-profile-verification-20261124.json (profile sha256 d67f653b equals the enrollment snapshot). Current-configuration evidence, not pre-enrollment evidence. |
| Native checks on UMM 0.31.1 / 0.32.4 / 0.33.0 / 0.33.4 | no | not_run | 0 | No approved disposable game copy; KwrRuntime transacts against the live game, which this mission does not authorize. |

**Evidence references:**
- prepared under owner amendments revision 1 sha256 82f5267951845e0da430ab1251c2ff7a86a75e72bee16193ef6936f3dd5b6f2d
- docs/evidence/umm-0311/INDEX.md
- docs/evidence/umm-0311/loader-baseline.json
- docs/evidence/umm-0311/readme-audit-run.json
- docs/evidence/umm-0311/pr-12-description.json
- docs/evidence/umm-0311/effective-profile-verification-20261124.json
- docs/UMM-COMPATIBILITY.md

**Prior findings to assess:** `R1-F1`, `R1-F2`, `R1-F8`, `R1-F9`, `R2-F1`, `R2-F2`, `R2-F5`, `R3-F1`, `R3-F2`, `R3-F3`, `R3-F4`, `R3-F5`

**Owner amendments to the mission — revision 1, captured-source sha256 `82f5267951845e0da430ab1251c2ff7a86a75e72bee16193ef6936f3dd5b6f2d`.** Applied by the owner with `resume --amend-mission`; each owner message is preserved verbatim, in order. The effective task is the original mission plus these amendments. The reported work comes from a developer invocation whose input included them, and the reviewer of this round receives a byte-exact copy of the captured document. Public rendering after the controller's standard sanitization; the digest identifies the private captured bytes, which this rendering can differ from.

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
```

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
