<!-- agent-review:v1 enrollment=kwr-codex-umm-0311-baseline-20261123-20261123181024-1057 key=r07-dispositions-9947de42e3 part=1/1 sha256=9947de42e32ac475952a152a2ab40a0a9b1d1cd05179a9e6ac2fb302c0c0236c -->
### Claude Opus — finding dispositions — round 7

Candidate after corrections: `79f16870661acb5df26e842f43dfe7ed04ff2261`

- **R5-F1** — `fixed` — commits: `6b23b26`, `79f1687`
  controller gate baseline on 6b23b26 (gate-baseline-6b23b26ccfe3-d4433dcb35e-ac9ec8ca62a-20261126T174740687857Z.log): 141 of 149: installer-read-through-a-hard-link-during-publication-declined (a hard link to the live output in another folder during the placement pause: 'is a file link', declined), with the independent external copy still content-inspected; the KwrPackage.ps1 code is unchanged in 79f1687.
- **R7-F1** — `fixed` — commits: `6b23b26`, `79f1687`
  controller gate baseline on 6b23b26 (gate-baseline-6b23b26ccfe3-d4433dcb35e-ac9ec8ca62a-20261126T174740687857Z.log): 141 of 149: installer-read-through-the-zip-short-filename-during-publication-declined (the complete short path ...\ARTIFA~1\KINGMA~3.ZIP contends on the publisher's lock) and zip-short-filename-qualifies-and-cannot-overwrite-its-receipt (the clean pair through KINGMA~2.ZIP qualifies as frozen-candidate; a content-only report aimed at the long-name receipt is refused; both input hashes unchanged); code unchanged in 79f1687.
- **R7-F2** — `unresolved_blocker` — commits: `6b23b26`, `79f1687`
  controller gate baseline on 6b23b26 (gate-baseline-6b23b26ccfe3-d4433dcb35e-ac9ec8ca62a-20261126T174740687857Z.log): 141 of 149: committed-non-ascii-source-builds-and-qualifies and provenance-current-with-committed-non-ascii-source passed (Git's default listing quotes the name as \303\211...); the ignored-source control beside it failed only because of the predicate defect, corrected in 79f1687 but not gated.
- **R1-F1** — `unresolved_blocker` — commits: `6b23b26`, `79f1687`
  The 6b23b26 gate's attribute-named/root-source and ignored-source cases failed because the check never fired (predicate defect); 79f1687 corrects it and a focused probe shows the ignored attribute-named source detected; the required gate was not run within the allowance.
- **R7-F3** — `fixed` — commits: `6b23b26`, `79f1687`
  docs/UMM-COMPATIBILITY.md: the reproduction note now reads 'Its checks (120 in round 5)', without the cross-reference to per-round counts.
- **R5-F3** — `deferred_optional` — commits: —
  Accepted deferral (optional). Follow-up note: type-check ArchiveEntry and ArchiveSha256 before PowerShell enumerates a one-element array.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/agent_review.py verify --enrollment kwr-codex-umm-0311-baseline-20261123-20261123181024-1057 --gate baseline (6b23b26) | yes | fail | 563.8s | controller gate baseline on 6b23b26 (gate-baseline-6b23b26ccfe3-d4433dcb35e-ac9ec8ca62a-20261126T174740687857Z.log): 141 of 149; the 8 failures are the tracked-source cases and the ones that follow them (the check never fired), corrected in 79f1687. |
| Focused probes on 79f1687's check (Get-KwrCompilerInputs with an ignored attribute-named source; the corrected predicate under StrictMode) | no | pass | 8s | The compile entry keeps the repository name src/KingmakerWakingRune/Blueprints/obj/LocalOnly.AssemblyAttributes.cs (not a generated: label) and the corrected check lists it as untracked. Not a gate. |
| Required controller gate on 79f1687 | yes | not_run | 0 | Does not fit the remaining allowance (about 2.6 minutes; the gate takes about 10). |
| Native checks | no | not_run | 0 | Not authorized. |

_Model-generated developer response (Claude opus / high), posted by the agent-review controller through the owner's GitHub account; not a human review._
