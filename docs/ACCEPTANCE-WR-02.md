# WR-02 acceptance report

**Candidate:** commit `f75782e` · package `KingmakerWakingRune-0.2.0.zip` SHA-256 `3041352663f8222f6686de101d46dc77736b68b7b0ccb3c2048391d79efca9dd` ·
DLL SHA-256 `280e0c70c98de7addb857d61f51d3f5d42160f494035b87c42921ba7fc4fe80b`. Later commits change only docs and `scripts/KwrRuntime.ps1`.
**Environment:** Kingmaker 2.1.7b (Steam, cloud sync disabled), UMM 0.33.2, Harmony 1.2.0.1 API, 1280×720.
**Method:** real mouse and keyboard input on the packaged candidate through the guarded harness. Screenshots were inspected. Read-only state reports (`*.txt`) were written by the harness probe; it changes nothing in the game.

Evidence (outside the repo; contains game imagery): `C:\Dev\KingmakerWakingRuneLab\runtime-evidence\wr02-accept-f75782e\`. Filenames are prefixed by check. The tested zip is kept there. `...\wr02-r01-recon\` is development history and holds the `Recruit completed` log line.

| ID | Check | Result | Evidence and notes |
|---|---|---|---|
| A1 | New game reaches a populated lodge: exactly 11 authored NPCs, six outward-facing agents | **PASS** | `*-A1-arrival.*`: 11 `KWR_*` units. Agents: three at z=+4.3 yaw 0, three at z=−4.6 yaw 180. |
| A2 | Warding agents occupied | **PASS** (stills) | Raised casting pose visible in `*-A8-closeup.png`, `A8-leaders.png`. No combat, spell effects or log entries. Looping is by code and stills; no video was taken. |
| A3 | Quiet murmuring | **PARTIAL** | Native chant events post with non-zero Wwise IDs (log). **Audibility NOT RUN**: the harness has no audio capture. Please listen for level and character. |
| A4 | Correct portraits, speakers | **PASS** | Ambrus, Sheila, Masked Figure, Quartermaster, Coordinator all correct in dialogue, including after a full restart (`*-A8-masked-speaks`, `*-A8-ambrus-followup`, `*-A10-briefing-recovered`, `*-A14-coordinator`, `*-A14-qm-dialog`). Files for all three sizes inspected in `A15-portrait-contact-sheet.png`; small crops are readable. The native UI does not show these NPCs' Small or Full images during this flow, so those two sizes were checked as files only. |
| A5 | Opening, narration, focus, four answers, hub returns, "I understand" | **PASS** | `*-A2-*`. The stage direction is speakerless narration and the camera moves to the masked figure, then back to Ambrus. The masked figure speaks its answer. |
| A6 | Masked figure nods | **NOT VERIFIED** | The cue plays the native `DialogBow` gesture (the closest available without bespoke animation); still captures cannot show the motion. |
| A7 | Protagonist XP and native level-ups to 11 | **PASS** | Exactly 155,000 XP (read from the game's XP table; level 12 needs 220,000). Serin Vale, Druid 1→11 with every level chosen by hand: `*-A3-*`, `canLevel=False` at 11. Wolf companion advanced. |
| A8 | Five free recruits at level-11 XP; cancel; spellcaster | **PASS** | Aria Voss (Bard) and four Fighters, each exactly 155,000 XP, treasury unchanged: `*-A4-*`. Cancel left no ghost, no charge: `*-A4-cancelled`. Bard took a level-2 spell choice natively. |
| A9 | Recruit level-ups to 11 | **NOT RUN** | Recruits keep native pending level-ups (Bard at 2, Fighters at 1). The full 1→11 path was exercised on the protagonist only. |
| A10 | Starting gold 450,000; recruitment leaves it alone | **PASS** | 450,000 on arrival and after all five recruits. Native chargen's class gold (for example 800) is refunded by the Coordinator's flow. |
| A11 | Buy, sell, cancel, reopen, equip | **PASS** | Cancel unchanged at 450,000 (`*-A5-after-cancel`). Buy 2,400 + 2,500 → 445,100. Sell torch +27 → 445,127. Composite Longbow +1 equipped on Bram Holt (`*-A5-equipped`). Sell price is 25% of cost, so no profit loop. |
| A12 | Navigation and both services, no modal overlap | **PASS** | Party of six walked the room (`*-A8-party-moved`). Both services reached by walking. Dialogue, shop, chargen and Group Manager never overlapped. |
| A13 | Full restart and normal load preserve everything | **PASS** | Identical before and after (`*-A7-before-restart.txt`, `*-A7-reloaded.txt`): party, 445,127 gp, purchases, shop 240 lines / 758 items, all NPC ids, briefing flag. No replay and no duplicates. Shop reopened after a further restart: `*-A14-shop-open`. |
| A14 | Second new game independent | **PASS** | New game and NPC ids, fresh 450,000 gp, full shop 239 / 759: `*-A11-second-game.*`. |
| A15 | Stock starts and saves | **PASS** | Main Story after a Waking Rune session in one process: no `KWR` flags, NPCs or grants (`*-A12-main-story.*`). A stock save loads clean (`*-A13-stock-loaded.*`). |
| A16 | Ordinary mercenary recruitment | **NOT APPLICABLE** | Not gameplay-tested. Free recruitment passes its own XP and callback; no Harmony patch or global state touches recruitment, and `Patches/` and `Selection/` are unchanged since WR-01. |
| A17 | WR-01 save upgrade | **PASS, with limitation** | `KWR_AUTOMATION_A4_FINAL`: Sheila keeps her id, the 10 missing NPCs are added once, grants apply once, briefing plays. If the hero was saved out of the NPCs' line of sight, native fog hides the speakers' models during the briefing (`*-A10-briefing-recovered.png`); text and portraits are correct. |
| A18 | Interrupted briefing recovers | **PASS** | Process exit mid-briefing, reload: it starts again from its checkpoint and can be finished (`*-A10-*`). The game disallows saving during dialogue, so there are no mid-dialogue saves. |
| A19 | Follow-up questions without replay | **PASS** | Sheila or Ambrus reopen the hub after the briefing; nothing regranted (`*-A6-followup`, `*-A8-*`). |

Unit tests: 17/17 (lodge policy, once-only grants, recruit refund, treasury, briefing gate). The only exception in the game log is the native `BugReportCanvas` startup NRE, also present without WR-02 code paths running.

## Environment restoration
Receipts from `End`: profile (14 test-added files quarantined), Steam save mirror (3 quarantined), registry and UMM `Params.xml` restored and verified. Quarantine: `C:\Dev\KingmakerWakingRuneLab\runtime-backups\wr02-accept-f75782e\quarantine`. The owner's WR-01 install was snapshotted, set aside for the run and restored: DLL `72a011dd…` matches. The WR-02 candidate is **not installed**.

## Known limitations and disclosures
- **Package version:** only `WakingRune_WR02_Claude_Package.zip` (version 1) was present, so the portrait mapping was corrected and sizes regenerated per the execution prompt. Inspected source images: the `AmbrusValsin` folder holds Sheila and vice versa.
- **Models** are generic base-game units: the masked figure is a hooded robe (no literal mask on the model); its portrait shows the mask.
- **Two service-NPC lines** ("The Society's stores are open…", "Need more hands…") were authored so those conversations have an opening line. They are not in the supplied script.
- **Harness probe:** the DLL contains an inert test helper that only runs if a `capture.flag` file exists in the mod folder; the package does not ship one.
- **During recon** two screenshots captured the desktop instead of the game (the game presents directly to the display). They were deleted, and `Shot` now captures from inside the game.
- **Harness change:** `Begin` used to refuse when a mod was already installed. It now snapshots that install and `End` restores it.
- 1024×768 was not rechecked; the WR-02 UI is entirely native.
