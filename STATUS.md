# Status: The Waking Rune

**Milestone:** WR-05, Kurshu the Undying in A1. The review round is done, and the restoration-order closeout is fixed in 0.6.16. Ready for owner review.

**Branch:** `feature/wr-05-kurshu`, a child of `feature/wr-04-sanctum-wards`.
- PR #4 (WR-04) is still open and unmerged, so the WR-05 draft PR #5 targets the WR-04 branch and shows only WR-05.
- Do not merge, tag or release without owner review. Merge PR #4 first.

Results:
- `docs/ACCEPTANCE-WR-05.md`: both rounds, PASS / PARTIAL / NOT RUN by check, and the victory.
- `docs/WR-05-KURSHU.md`: the source-to-runtime sheet, the maneuver chain, the feats, and every adaptation (grapple, Intelligence drain).
- `docs/WR-05-PROGRESS.md`: checkpoint A and both rounds.
- `docs/WR-04-LEDGER.md`: the cumulative ledger, with both WR-05 sections.
- `docs/WR-04-RULES.md`: the departure row, revised for the two-step departure.

## Candidate
- Version 0.6.16, commit `b630849`: the reviewed 0.6.15 (`1891bf0`) plus the restoration-order repair.
- DLL SHA-256 `8bb3a2e72b08be9b744edaf2a304d7a124ea439ea41e5109ad31674459691848`.
- Zip `artifacts\KingmakerWakingRune-0.6.16.zip`, SHA-256 `8e78d52ceb2aec89bd16a53bd3e7f167faae4b00c8959bb353aeeb3ffc276c20`. Tested in `wr06-a` and now installed; 0.6.15 is backed up at `runtime-backups\wr06-a\mod-existing`.
- 174 unit tests pass (23 for WR-05).
- Earlier test builds 0.6.0–0.6.15 have their own versions and zips; none were replaced.

## What WR-05 adds
- **Kurshu as a real hostile combat unit.**
  - Large outsider, drawn with the Erinyes model at Large size.
  - Native class Hit Dice.
  - Stats from the printed Subtier 10–11 block with the extra challenge (Weakness and one negative level removed); the player's preset applies on top.
- **Native defenses:** regeneration 5 (acid), DR 10/cold iron and magic, resistance 30, SR 26, trip immunity.
- **Feats:** native Combat Casting and Combat Reflexes, and the printed Improved Iron Will reroll.
- **Attacks:**
  - Two slams with 1d4 Intelligence drain. Drain to 0 leaves the victim comatose; restoration wakes it.
  - A tail grab: a real grapple with its costs (maintain or let go each turn, one hold, freedom of movement defeats it), with constrict.
- **Spell-like abilities** with saved daily uses, including bounded limited wishes, one already spent.
- **Power Surge, Divine Separation and Feed.**
- **Tactics:** her tactics run through the native brain, with considerations for her printed priorities, plus a bounded director for feeding, the grapple and a self-only teleport.
- **Narrative:** the wound speech, the blood message, and the existing 90-minute deadline, started once on her defeat.
- **Departure:** rest, then prepare in the lodge, then depart by a second use of the token. Spending never forces another rest.
- **Saves:** version 4. Pre-departure saves get the fight. Saves already inside the sanctum keep their state, with a notice.

## Proven
Both rounds, in native play unless labelled:
- ordinary input, the fresh shop, arrival gating, the hidden check, finite spell-like abilities and the pre-spent wish, Power Surge (including a lapse);
- DR, resistance and SR, regeneration and acid;
- the grapple, with injected tail hits;
- comatose drain and restoration, with injected drains;
- the maneuver chain before and after Feed, and Feed in both modes;
- a **legitimate victory**, then defeat to A2;
- reloads after a full restart, dead and alive (the live save through a labelled one-frame combat pause);
- stock isolation and the legacy notice.

Details and labels are in the acceptance report.

## Not done or not proven
- **Not implemented, deliberately:** plane shift and the non-combat at-will abilities.
- **A held creature's escape attempt** is automatic and costs it no action (no grapple interface). This favours the party.
- **Balance:** two fights, one won with the printed counters and one lost without them, are not a balance study.
- **Carried forward:** earlier ledger rows that are still open.

## Owner decisions
- **Merge order:** PR #4, then the WR-05 PR.
- **Python 3.15 removal** remains yours to authorize. None was used.

## Environment state
- **Installed:** candidate 0.6.16, per the owner's standing preference (run `wr06-a`; 0.6.15 backed up at `runtime-backups\wr06-a\mod-existing`).
- **Backed up:** the 0.6.7 install it replaced, at `C:\Dev\KingmakerWakingRuneLab\runtime-backups\wr05-b\mod-existing` (DLL `07a6823c…`).
- **Restored and verified:** transaction `wr05-b` (profile, Steam mirror, registry with the turn-based setting reset, UMM Params and Mods).
- **Saves:** no owner save was opened.

## How to test again
```powershell
.\scripts\Build.ps1                  # refuses to replace an existing clean package
.\scripts\KwrRuntime.ps1 Begin -RunId <new-id> -PackagePath .\artifacts\KingmakerWakingRune-0.6.16.zip
.\scripts\KwrRuntime.ps1 Launch      # then Shot / Click / Key / Command ..., Close, Collect
.\scripts\KwrRuntime.ps1 End -KeepInstalled
```

**Seeds** (in `runtime-evidence\wr05-b\saves`):
- `Manual_334_Before_the_Sanctum_of_Sloth_2.zks`: the departure checkpoint of the buffed, re-equipped party. The token departs without a rest.
- `Manual_335_KWR_AUTOMATION_WR05B_KURSHU_VICTORY.zks`: after her death.
- `Manual_337_KWR_AUTOMATION_WR05B_LIVE_DAMAGED.zks`: alive mid-fight at 159/219.
- `Manual_960_KWR_AUTOMATION_WR05B_STOCK_TBM.zks`: the stock-campaign check.
- The first-round seeds remain in `wr05-a`.

**Harness additions in WR-05:**
- **Read-only:** `kurshu` (with the maneuver chain, hold and feats), `known <member>`, `paused`, `blueprint <name>`, `builds`.
- **INJECTED:**
  - `hurt <member> <kind> <n>`, `corpse <kind> [m]`, `kurshudefeat`;
  - `grab <member>` (a tail hit), `drain <member> <n>`, `armslam <member> <n>` (a fixed slam drain delivered inside the next native Intelligence heal);
  - `kurshufixture <damage> <uses…>`, `combatsave <name>`.

## Next exact action
Owner review of PR #4, then of the WR-05 draft PR #5.
