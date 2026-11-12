# WR-06 acceptance: The Lashmistress (A4)

**Candidate:** 0.7.10, commit `4630671`.
- DLL `4733827d4a050b31bf5165466af5e75974808cfc42f07716f9a0ce9288999776`
- Zip `artifacts\KingmakerWakingRune-0.7.10.zip`, `95517866a8b2128c195cd26d7dc791722ff502594505f4ea3a006cff6a3d9abb`
- Branch `feature/wr-06-lashmistress`, a child of `feature/wr-05-kurshu` at `caa2e09` (draft PR #5, unmerged). That is itself a child of `feature/wr-04-sanctum-wards` (draft PR #4).
- 187 unit tests pass, 13 of them for WR-06 (`LashmistressRulesTests`).

The Phase A repair (Kurshu restoration order, 0.6.16) is on the WR-05 branch, committed separately. Its acceptance is in `ACCEPTANCE-WR-05.md`.

**Run:** `wr06-recon`, one guarded transaction for every WR-06 build from 0.7.0 to 0.7.10. Evidence is in `C:\Dev\KingmakerWakingRuneLab\runtime-evidence\wr06-recon`.

**Profile:** Subtier 10–11, six PCs, extra challenge, the game's Normal preset.

**Labels:**
- **PLAYER:** ordinary input: opening the door, the stealth toggle, turning the protagonist's AI off, the turn-based toggle.
- **PARTY AI:** the game's own party AI, a player setting, which gave every other party combat action.
- **SETUP:** a labelled harness move: `teleport` to the east door, `move` orders, `save`, `load`.
- **INJECTED:** none were used in A4 combat.

**Seeds** (in `runtime-evidence\wr06-recon\saves`):
- `Manual_974_…SEED_KURSHU_SLAIN`: the WR-05 victory save, state version 4, used for the upgrade path.
- `976 …A4_PLACED`
- `977 …A4_DOOR_PREPARED`
- `978 …A4_VICTORY`
- `979 …STOCK_TBM`: the disposable stock save.

## Builds and defects found
| Build | Defect found in play | Fix |
|---|---|---|
| 0.7.0–0.7.1 | (Recon) a read-only catalog of the installed game's classes, spells, items and units; unit names need a loaded game | Names read safely |
| 0.7.2 | Vandiana's wizard book short two entries; acid dart 5/6; channel DC 18 (character level) | 0.7.3: memorize order; full daily uses once at placement; channel at cleric level |
| 0.7.3 | Still short: the game gives an NPC bonus spells from the **base** casting score, so the worn headband added none | 0.7.4: printed Int 17 as base, headband carried as loot |
| 0.7.4 | Dimension Door (caster-only variant) not on the school list | 0.7.5: the game's own `DimensionDoorBase`, its self-only variant cast from it |
| 0.7.5 | An upgraded save's earlier warning never prepared Darkvision; a three-minute potion was drunk on a distant warning | 0.7.6: carried forward once; potions wait for the door or combat |
| 0.7.6 | **Setting off the glyph committed the countdown and put the victim in combat with Vandiana:** its saving throw named her as caster | 0.7.7: the blast comes from the glyph itself (her spell context kept); ward rules are not engagements |
| 0.7.7 | Agarik, in ambush, sat out a fight in his own room (each spawned unit had its own group) | 0.7.8: one native encounter group |
| 0.7.8 | **Vandiana cast only acid dart:** the brain's usable-action list was taken at the level-up rest, before her books were filled | 0.7.9: recomputed after preparation and on load (it restores nothing) |
| 0.7.9 | In combat a forced potion command never ran (the AI's queue is never empty) | 0.7.10: potions are the defenders' own brain actions; saved brains brought up to date on load |

## Checks
| # | Check | Result | Build | Evidence |
|---|---|---|---|---|
| B | Opening the east door prepares without starting the clock | **PASS** (PLAYER door click) | 0.7.6 | Combat log: "Vandiana … uses item: Potion of Cat's Grace", "Agarik/Jorgas uses item: Potion of Bull's Strength", "Dark Naga uses ability: Invisibility" ×2. Naga 2nd-level slots 7→5; Agarik invisible, Jorgas not. `confrontation False`, `Engaged false`, no combat. |
| B | An undetected scout does not start the clock | **PARTIAL** | 0.7.7 | The glyph and the hound (Tessa set off the glyph; the hound "noticed") raised awareness only: "Glyph detonated", "Awareness Glyph", "Awareness Hound", with `confrontation False`. Tessa (stealth) was then spotted by the naga inside A4, 16 ft from it, and that engagement correctly committed. A truly undetected peek was not achieved. |
| C | A warning prepares once, through no walls | **PASS** (upgrade path); reporter **NOT RUN** | 0.7.6 | The seed's earlier alarm awareness: "awareness: VandianaDarkvision requested" → "begins … performed", her wizard 2nd-level slot spent, `KWR_VandianaDarkvisionBuff` for 9 h, native log "casts Darkvision". Nobody moved, and no combat until A4 was entered. |
| D | Prepared, base and buffed states; False Life is spent protection; nothing refills on reload | **PASS** | 0.7.4–0.7.10 | Sheets in `WR-06-LASHMISTRESS.md`. Temporary HP 14 at placement and 0 after the fight. Spent potions and slots stayed spent across a full restart (977 reloaded on three later launches; 978 after the victory). Dispelling her Mage Armor was **NOT RUN**. |
| E | A meaningful engagement in real time with pause | **PASS** | 0.7.10 | The victory below. |
| E | …and turn-based | **PARTIAL** | 0.7.10 | The defenders acted natively in turn-based combat: Vandiana's Haste on all four, the naga's Displacement and Lightning Bolts, Brena brought down. The party's AI does not act in turn-based mode, so their first round was ended without actions. The fight was then reloaded. |
| F | Blackstrike concealment and defenses | **PARTIAL** | 0.7.8–0.7.10 | Ambush stealth, invisibility and hiding while in combat observed (`stealth True` while `combat True`). Evasion and uncanny dodge are present as native facts. Blur and hide in plain sight in combat were not captured in a surviving log. |
| F | Naga poison, immunity, spells | **PARTIAL** | 0.7.10 | All its 3rd- and 2nd-level slots and three 1st-level slots were spent on Displacement, Invisibility ×2, Lightning Bolt and other spells. A sting's sleep save was not captured in a surviving log, and the poison code has no unit test; it was reviewed against the source. |
| F | Vandiana's two progressions and representative spells | **PASS** | 0.7.10 | After the victory her books showed as spent:<br>• Cleric: Command, Divine Favor, Darkness, Hold Person, Sound Burst, Blindness, Dispel Magic, Cure Critical Wounds, Order's Wrath, Unholy Blight.<br>• Wizard: Grease ×2, Glitterdust, Haste, Slow, Confusion, Enervation, Dimension Door, Baleful Polymorph.<br>• Channel 4 of 5.<br>Captured: "Command (halt) on Marra Fen: Will 30 vs 15: resisted". Divine CL 8 and arcane CL 9 with their own DCs (16 and 15 at 2nd level). Order's Wrath and Unholy Blight per-target results are unit-tested; their in-play lines were lost with the log. |
| G | Engagement starts one stored 1d6+6; reload does not reroll | **PASS** | 0.7.7–0.7.10 | "Ritual confrontation committed … 1d6+6 = 7 / 8 / 9 / 10" in separate trials from the pre-engagement save, once each. After the victory and a full restart: `roll 10 deadline 131143` unchanged. |
| G | Defeating Vandiana does not cancel the clock; the awakening fires once during A4 | **PASS** | 0.7.10 | The awakening came due mid-fight (`due True reason ConfrontationDeadline` at 131147) and the fight continued. "ALL A4 DEFENDERS DEFEATED … awakening due True (ConfrontationDeadline); the countdown is not cancelled." Nothing despawned, no Krune. |
| G | Emerald destruction stays immediate | **NOT RUN** in WR-06 | — | Unchanged WR-04 rule. Striking the emerald now also engages, without touching that rule. |
| H | No second hound or glyph; attribution and tether follow the real Vandiana | **PASS** | 0.7.7 | No fixture was created for a placed Vandiana. The fixture was retired ("retired (out of the game…)"), and the resolver returns `KWR_A4Vandiana`. The glyph fired once with her context. The hound barked once (existing fixture). |
| I | Persistence before combat, during preparation, and after victory, through full restarts | **PASS** | 0.7.6–0.7.10 | 976, 977 and 978 each reloaded after a full game restart: same entity IDs, deaths kept, slots and potions spent, buffs continuing, fixture retired, clocks unchanged. Live mid-combat save **NOT RUN** (no combat-save fixture used). |
| J | A legitimate victory | **PASS** (PARTY AI; SETUP placement) | 0.7.10 | Below. |
| K | Stock isolation | **PASS** | 0.7.10 | `Manual_979` (SpiderCave): `SanctumWardPart` absent, A4 state null, 0 A4 lines, 0 mod errors. A stock goblin fight ran with the global concealment patch and observer present. The one exception in the log is the game's own bug-report screen at startup. |
| — | Upgrade of a version 4 save already inside | **PASS** (safe case); unsafe case unit-tested only | 0.7.2+ | "older save checked …: upgraded: the party is safely outside A4". `TestAnOlderSaveIsUpgradedOnlyWhenEverythingSaysItIsSafe` covers each unsafe fact. |
| — | Fresh pre-departure save reaches the encounter | **NOT RUN** live | — | Same `Spawn` path as the upgrade. `Initialize` → `Active` is unit-tested. |
| A | Phase A ordering | **PASS** | 0.6.16 | `ACCEPTANCE-WR-05.md`: cases A–C across a full restart. |

## The victory (0.7.10, real time with pause)
- **Setup:**
  - From `977` (door already opened, defenders prepared), the party stood at the east door, placed there earlier by SETUP `teleport` from A1 (the A2 door and the A3 runes were not crossed in this run).
  - PLAYER: Ilsa, the protagonist, had her AI turned off and was moved back down the hall out of sight.
  - SETUP `move` orders took the other five into the hallway and the room.
- **Combat:** every combat action on the party's side came from the game's own party AI.
  - The glyph went off (awareness only).
  - The engagement committed 1d6+6 = 10.
  - Vandiana hasted her allies, and the naga cast Displacement and Lightning Bolts.
- **Course of the fight:**
  - Agarik fell at about round 5 and Jorgas at about round 8.
  - Vandiana cured herself with Cure Critical Wounds (41 → 64) and cast through both books.
  - The awakening came due when its 10 rounds ran out, with the naga and Vandiana still standing.
  - The naga fell, then Vandiana at 131233.
- **Party:** Brena, Tessa and Dorn fell (negative hit points). Ilsa, Marra and Kael stood.
- **No** damage injection, refill, invulnerability, preset change or combat save was used.

An earlier diagnostic try (0.7.9), with the party's AI and Ilsa leading, was a defeat: Ilsa died and the game ended. A turn-based try was ended as described above. Two fights are not a balance study.

## Open
- **Mechanics evidence not captured:** an undetected peek; Darkness concealment in play; Blur and hide in plain sight in combat; a naga sting's sleep save; a dispelled Mage Armor; a live mid-combat save; a messenger warning; the fresh pre-departure path; emerald destruction in WR-06.
- **Deferred abilities** are listed in `WR-06-LASHMISTRESS.md`.
- **Turn-based:** the party's AI does not act, so a turn-based fight needs every party order given by hand.
- **Calibration** sits on a buff that death removes (corpses only).
- **Krune's encounter (A5)** is the next milestone.

## Environment
- `wr06-recon` ended with `End -KeepInstalled`:
  - **Profile:** 30 differences reverted, added files quarantined.
  - **Steam mirror:** 4 reverted.
  - **Registry:** restored, which also resets the turn-based setting changed in testing.
  - **Params and Mods:** verified.
- **Installed:** 0.7.10 (DLL `4733827d…`).
- **Backed up:** 0.6.16 (DLL `8bb3a2e7…`), at `runtime-backups\wr06-recon\mod-existing`.
- **Saves:** only disposable seeds and `KWR_AUTOMATION_*` saves were loaded. No owner save was opened. The stock save is the WR-05 disposable one.
- **Python:** none used. No software was installed.
