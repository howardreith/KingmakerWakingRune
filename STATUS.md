# Status: The Waking Rune

**Milestone:** WR-07A, Krune's awakening. The candidate is ready for owner review. It ends at a labelled development checkpoint before Krune's combat encounter, which is the next milestone.

## WR-07A: candidate 0.8.7
- **Candidate:**
  - commit `39cee28` on `feature/wr-07a-awakening` (from `main` at `56b9e40`);
  - DLL `b24a9de70ad553e3cd84d39ac77c70312aac8b6e546cbd8f4b3c7664da8260b7`;
  - zip `artifacts\KingmakerWakingRune-0.8.7.zip`, `477a113f1adca40f495599e3ba4c51e1a83269ed183a4e8beafa847b55cfdb61`;
  - 212 unit tests pass (198 before, with the 128 rune-mask expectations, plus 14 new).
- **Installed:** 0.8.7. **Backed up:** 0.7.22, at `runtime-backups\wr07a-b1\mod-existing`. Transaction `wr07a-b1` restored and verified.
- **The ordinary route:** A1 → A2 (palm impression) → A3 → east door → A4, by real input. The WR-06 teleport was needed only because A2 was latched in seed 976.
- **What it does:**
  - **One real Krune:** a native conjurer 17, his printed sheet exact before the preset. His identity is committed in the save; the hidden receiver is retired and its feeder ledger is handed over.
  - **His entrance:** emergence by the game's own dimensional steps onto clear A5 floor.
  - **His opening:** a parley in the native dialogue, a battle bark, or an unobserved waking. He is hostile on every path.
  - **The checkpoint:** a labelled development checkpoint that pauses everything.
  - **The runes:** the revival's caster-level loss comes from the commit's mask for 24 hours; the rune links stay live.
  - **Early release:** the emerald releases him with a one-round stagger that works in both combat modes.
  - **Older saves:** one-time choice.
- **Results:** `docs/ACCEPTANCE-WR-07A.md` (A–L and the rest). The design and contract are in `docs/WR-07A-AWAKENING.md`, the progress notes in `docs/WR-07A-PROGRESS.md`, and the ledger section in `docs/WR-04-LEDGER.md`.
- **Not shown in play:**
  - a battle-bark opening during a live A4 fight;
  - hostility from a cast before the dialogue;
  - a native dispel landing on his Mage Armor;
  - a late emerald hit;
  - the checkpoint arriving in turn-based play;
  - the broken emerald checked by eye.
- **New harness commands:**
  - **READ:** `krune`, `krunesheet`, `krunecl`, `manifest`;
  - **SETUP:** `krunedeadline`, `krunepause`;
  - **INJECTED:** `setmask`, `hitgem`, `krunerelease`; target `krune`.
- **New seeds** (in `runtime-evidence\wr07a-b1\saves`):
  - 986–988, 991 and 993: checkpoints and a dead Krune;
  - 989–990: saved at ActorReady;
  - 992: the feeder spent;
  - 994: a kept legacy save.

## Next exact action
Owner review of the WR-07A draft PR. If it is accepted, the next milestone is Krune's combat encounter (WR-07B):
- his spells and slots;
- the artifacts' powers;
- the summons' mist resistance;
- his tactics;
- the removal of the checkpoint.

The sections below are earlier milestones, kept for history.


# WR-06 (history)

**Milestone (then):** WR-06, the Lashmistress encounter in A4, since merged as PR #6. Its third pass was the owner's closeout review. The review closed the four original findings, and Phase A (WR-05 closeout, 0.6.16) was accepted.

## Third pass (0.7.20–0.7.22): candidate 0.7.22
- **Candidate:**
  - commit `793622e`;
  - DLL `64f2d95698df88d795b7adb0ef0139c1a28e0d5c8d58ab43b898195cd8b368d1`;
  - zip `artifacts\KingmakerWakingRune-0.7.22.zip`, `b0c809de1902381b4aa901e523640bf400dbbd997bed216386804c53e0881f04`;
  - 198 unit tests pass.
- **Installed:** 0.7.22. **Backed up:** 0.7.19, at `runtime-backups\wr06-pass3\mod-existing`. Transaction `wr06-pass3` restored and verified.
- **Fixed:**
  - nonlethal recovery is scoped to a whip-hit ledger;
  - Sanctuary's kept failure bars only an enemy's hostile targeting;
  - confusion is read from its condition (0.7.21);
  - the encounter's spell effects are dispellable (0.7.22).
- **Proven:**
  - the foreign-nonlethal negative control;
  - the Sanctuary failure branch with an allegiance change;
  - a running director command cancelled at a control change;
  - the darkness dispelled;
  - the whip in another wielder's hand;
  - a real nonlethal knock-out with restart persistence and healing;
  - **a victory against 0.7.22** (party AI; one of six fell; the deadline passed natively afterwards).
- **Labels:** PLAYER now means real mouse or keyboard input only. Harness dispatches are HARNESS-COMMANDED (see `ACCEPTANCE-WR-06.md`).
- **Owner decision:** hostile NPCs cannot be knocked out by nonlethal in this game (they die at 0), so the whip stops one short on them. Whether to add an encounter-local knock-out is open.
- **New seeds:** 984 (Kael knocked out by the whip) and 985 (after the 0.7.22 victory), in `runtime-evidence\wr06-pass3\saves`.
- **New harness commands (INJECTED):** `setnonlethal`, `rest`, `damage`, `armcontrol`.

The sections below describe the second pass and are kept for history; where they differ, the third pass above is current.

**Branches** (do not merge, tag or release without owner review):
- `feature/wr-04-sanctum-wards`: draft PR #4, base `main`.
- `feature/wr-05-kurshu`: draft PR #5, base the WR-04 branch. Head `caa2e09`.
- `feature/wr-06-lashmistress`: draft PR #6, base the WR-05 branch.

Merge order: PR #4, then #5, then #6.

Results:
- `docs/ACCEPTANCE-WR-06.md`: the review's findings and what was done, the second pass's builds, defects and checks, then the first pass.
- `docs/WR-06-LASHMISTRESS.md`: sheets, ability coverage with the audit of what is not implemented, adaptations, countdown and saves.
- `docs/WR-04-LEDGER.md`: the cumulative ledger, with a WR-06 review-pass section.

## Candidate
- Version 0.7.19, commit `4f8632a`.
- DLL SHA-256 `32e45672ea18ae5d3b8a656c2f22147d4ae0d55710a0465f4f427ab7a5b43695`.
- Zip `artifacts\KingmakerWakingRune-0.7.19.zip`, SHA-256 `aaf30457fcb39aec1fbc6dacacee632e3b066a569c6f68023d14857f2dea3b89`. Tested last and now installed.
- 196 unit tests pass.
- Test builds 0.7.11–0.7.18 each have their own zip; none was replaced. A `0.7.15-dirty` diagnostic package is not a candidate.

## What the second pass changed
- **Preparations:** settled whoever acts; the record keeps who acted and what was spent; they end for good when they can no longer happen. None gates the director.
- **Control:** the director yields to charm, domination, confusion and fear, cancelling only its own commands, and resumes when the effect ends. The tactics read native allegiance.
- **Darkness:** one more native concealment entry, with every native exemption, darkvision and true seeing. Before 0.7.16 the area never appeared, and neither did Magic Circle's.
- **Order's Wrath and Unholy Blight:** the outsider rule (1d6 per caster level) and the [lawful] / [evil] descriptors.
- **Whip:** a weapon enchantment dealing real nonlethal damage to the game's own total, with the armor rule. Healing, recovery, rest and revival are handled.
- **In-combat potions really drunk:** three engine details fixed. The game's AI treats item abilities as spent, it replaced an unstarted move-action drink, and Blur cannot target a non-party drinker. The engagement also ends the Blackstrikes' ambush.
- **Formerly deferred, now in:**
  - the naga's Shield (native);
  - Disruptive;
  - summoner's charm;
  - arcane bond (an accepted adaptation);
  - Magic Circle against Good;
  - Sanctuary;
  - Chill Touch.

  What is still not implemented is audited by category.
- **Older 0.7.x saves:** upgraded in place.

## Proven in the second pass
Native play, with labelled setup and injections:
- preparation during combat;
- control yield and resume in real time;
- darkness and its counters;
- the outsider burst;
- the whip's armor rule, nonlethal damage and healing;
- the naga's sting: save, immunity, cure and persistence;
- an undetected peek;
- dispel and restart;
- fresh arrival from the lodge;
- a turn-based engagement with every party order by hand;
- each new ability;
- the older-save upgrade;
- stock isolation.

Details and labels are in the acceptance report.

## Not done or not proven
- **Not shown in play:**
  - a director command cancelled at the instant of a control change (count 0 each time);
  - control landing in turn-based play;
  - a failed Sanctuary save;
  - the darkness itself dispelled;
  - a knock-out by nonlethal;
  - the whip in another wielder's hand;
  - a messenger warning;
  - emerald destruction in WR-06.
- **No fresh A4 victory against 0.7.19.** The 0.7.10 victory was won while the darkness, in-combat potions and Magic Circle silently did nothing; the defenders are stronger now.
- **Not implemented:** Wind Wall and the naga's Silent Image (explicit incompleteness), Bleed and the naga's detect thoughts (no such mechanic), and the noncombat and information spells (out of scope).
- **Not part of WR-06:** Krune (A5), the return portal, the finale, Society rewards. Plane Shift (WR-05) stays deferred.

## Environment state
- **Installed:** candidate 0.7.19.
- **Backed up:** 0.7.10, at `C:\Dev\KingmakerWakingRuneLab\runtime-backups\wr06-pass2\mod-existing`.
- **Restored and verified:** transaction `wr06-pass2` (profile, Steam mirror, registry, UMM Params and Mods).
- **Saves:** no owner save was opened. No Python was used and nothing was installed.

## How to test again
```powershell
.\scripts\Build.ps1                  # refuses to replace an existing clean package
.\scripts\KwrRuntime.ps1 Begin -RunId <new-id> -PackagePath .\artifacts\KingmakerWakingRune-0.7.19.zip
.\scripts\KwrRuntime.ps1 Launch      # then Command "load Manual_97x", Shot / Click (window pixels are 2x the screenshot's) / Key ..., Collect BEFORE Close
.\scripts\KwrRuntime.ps1 End -KeepInstalled
```

**Seeds** (in `runtime-evidence\wr06-recon\saves`, plus 980–981 in `runtime-evidence\wr06-pass2\saves`):

| Save | Contents |
|---|---|
| `974` | Kurshu slain, state version 4: the upgrade path |
| `975` | The lodge, before departure: the fresh-arrival path |
| `976` | A4 placed (saved by 0.7.10) |
| `977` | East door opened, defenders prepared |
| `978` | After the 0.7.10 A4 victory |
| `979` | Stock |
| `980` | Mage Armor dispelled, mid-combat (INJECTED combat exit) |
| `981` | Brena asleep from the naga's sting |

**Harness additions:**
- **Read-only:** `catalog <path>`, `a4 [name]`, `a4ai [name]`, `areas`, `nonlethal`.
- **INJECTED:**
  - `armslam <member> <n>` (Phase A);
  - `a4cast <defender> <spell> [target | x y]`;
  - `a4attack <defender> <target>`;
  - `a4take <defender> <item>`;
  - `buff <target> <buff> <seconds> [caster]`;
  - `unbuff <target> <buff>`.
- **PLAYER:** `ai <member> on|off`, `a4order <defender> <target>`, `equip <member> <item>`.
- **SETUP:** `give <item> [count]`.
- **Targets:** `party:N`, `a4:<name>`, `name:<text>`, `unit:<id>`.

## Next exact action
Owner review of PR #6 (second pass), with PR #4 and #5. If accepted, the next milestone is Krune (A5). A fresh legitimate A4 victory against 0.7.19 could come first, if the owner wants one.
