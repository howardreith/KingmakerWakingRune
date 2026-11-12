# WR-06 acceptance: The Lashmistress (A4)

**Candidate:** 0.7.19, commit `4f8632a`.
- DLL `32e45672ea18ae5d3b8a656c2f22147d4ae0d55710a0465f4f427ab7a5b43695`
- Zip `artifacts\KingmakerWakingRune-0.7.19.zip`, `aaf30457fcb39aec1fbc6dacacee632e3b066a569c6f68023d14857f2dea3b89`
- Branch `feature/wr-06-lashmistress`, a child of `feature/wr-05-kurshu` at `caa2e09` (draft PR #5, unmerged). That is itself a child of `feature/wr-04-sanctum-wards` (draft PR #4).
- 196 unit tests pass, 22 of them for WR-06 (`LashmistressRulesTests`).

The Phase A repair (Kurshu restoration order, 0.6.16) is on the WR-05 branch, committed separately. Its acceptance is in `ACCEPTANCE-WR-05.md`. The review accepted it.

The report has two passes.
- **Second pass (0.7.11–0.7.19):** the owner's review of PR #6 at `2c818d4`. Run `wr06-pass2`, evidence in `C:\Dev\KingmakerWakingRuneLab\runtime-evidence\wr06-pass2`. It comes first below.
- **First pass (0.7.0–0.7.10):** run `wr06-recon`, evidence in `C:\Dev\KingmakerWakingRuneLab\runtime-evidence\wr06-recon`. It follows, unchanged.

## Second pass: the review's findings (0.7.11–0.7.19)

### What the review found, and what was done
| Finding | Done | Shown in play |
|---|---|---|
| P2: combat preparations stay pending forever and block the director | Preparations are settled from the field whoever acts, and record who acted and what was spent. They end for good when they can no longer happen. Nothing gates the director but its own running preparation command. | Yes (check 1) |
| P2: the director does not yield to charm, domination, confusion | A control-ownership guard on preparing and directing: identity stays, allegiance and control are read live. On a change, only the director's own commands are cancelled. It resumes when the effect ends, and the tactics read native allegiance. | Yes (check 2) |
| P2: darkness adds concealment over native exemptions | The darkness now acts as one more native partial-concealment entry: every native exemption, darkvision within 60 ft, and true seeing. | Yes (check 3) |
| P2: the bursts omit the outsider rule and descriptors | Full-share outsiders take 1d6 per caster level (max 10d6), and the spells carry [lawful] / [evil]. | Yes (check 4) |
| The whip has no effect and its rule sits on Vandiana | A weapon enchantment: native damage written to the game's own nonlethal total, plus the armor rule. Healing, recovery, rest and revival are handled. | Partly (check 5) |
| "Full printed lists" and weak deferrals | Shield (naga), Disruptive, summoner's charm, arcane bond (adaptation), Magic Circle against Good, Sanctuary and Chill Touch are castable or active. The rest are audited by category in `WR-06-LASHMISTRESS.md`. | Yes (checks 10–13) |

### Builds and the defects they found
Each build is a committed, packaged candidate with its own zip; none was replaced.

| Build | Commit | DLL SHA-256 (first 16) | What it changed | Found by testing it |
|---|---|---|---|---|
| 0.7.11 | `a818a81` | `ddd240f2d20302317` | The review's fixes and abilities above | **In combat no defender ever drank a potion:** the game's AI treats an ability from an item as already spent, so 0.7.10's potion actions could never fire. Credit for the director's potions also went to "its own action". |
| 0.7.12 | `f050b79` | `40f9b296e01d38eb3` | A potion AI action; director credit; a held Chill Touch is not wasted (0.7.11's Vandiana charged it, then cast Command) | **The brain re-chose Blur every 0.35 s and never drank it:** a potion is a move action, and the native brain replaced the unstarted drink. |
| 0.7.13 | `2aa15cb` | `1fee34d49929c452` | The drink may wait one round | **Every drink, the director's included, ended "Interrupt":** the Blackstrikes' native ambush kept them flickering in and out of combat. |
| 0.7.14 | `bd335b1` | `e4d640bca13a4abd` | The engagement ends the ambush | **`canTarget False` on Agarik himself:** the game's target check refuses Blur, which cannot target allies, on any non-party creature, the drinker included. The director's hiding also replaced a begun drink. |
| 0.7.15 | `7a5ade7` | `828949a6c0d77b17` | A defender may drink its own potion; hiding waits for the drink | **Darkness never existed:** an area made in code had no effect link, so the game threw as it appeared. Found with the read-only `areas` diagnostic (in a `-dirty` diagnostic package, not a candidate). Magic Circle had the same defect. |
| 0.7.16 | `b79e1eb` | `6e48811a47b67de8` | Empty effect links for both areas | — |
| 0.7.17 | `e74dba9` | `7cbe2b2131c9240c` | Harness only: `buff`, `areas` | — |
| 0.7.18 | `8d9c474` | `02995e19792cb81f` | Harness only: `name:` targets | **The log said "nothing spent" for a Blur the brain had drunk a moment before the engagement requested it.** |
| 0.7.19 | `4f8632a` | `32e45672ea18ae5d` | That record now says "already in effect when requested" | — (installed) |

### Labels (second pass)
- **PLAYER:** door clicks, the departure confirmation, selecting the scout by portrait, each party member's per-character AI toggle (`ai`), and every party order in the turn-based check (`spell` from the member's own book, `attack`, `endturn`).
- **SETUP:**
  - `teleport` and `move`;
  - spells granted to a party member for one cast (`cast`);
  - `spawnfoe` (a Movanic Deva for the outsider check, a stock goblin);
  - `save`;
  - `tbm`, the rook button's handler.
- **INJECTED:**
  - `a4cast` and `a4attack`: a defender's decision in place of its brain's; the spell or attack and everything after it is native;
  - `a4take`: one potion taken;
  - `unbuff`: a party member's Delay Poison or Barkskin removed;
  - `buff`: a native Domination, Confusion or sleep-poison buff applied where a failed save would apply it;
  - `combatsave`: units leave combat for one frame so the game will save.

### Checks (second pass)
| # | Check | Result | Build | Evidence |
|---|---|---|---|---|
| 1 | **Preparation during combat**: its own brain's potion completes it and frees its tactics; an unavailable or interrupted one ends and gates nothing | **PASS** | 0.7.15 (and 0.7.19) | 976, the fight started a second after the door was heard. "Agarik's own brain drinks Blur", then "preparation AgarikBlur performed by its own action; resource 1 -> 0", then repeated "Agarik: no foe adjacent, hides in plain sight (Blur preparation: performed by its own action)". Jorgas's Blur was taken first (INJECTED): "abandoned: its potion or slot is gone and the effect never appeared" after one round, and he hid anyway. 0.7.14: "Agarik … hides in plain sight (Blur preparation: pending)", so a pending preparation gates nothing. Interrupted director commands in 0.7.13/0.7.14 logged "did not take effect: … result Interrupt" and moved to their next attempt. The naga's two Invisibility preparations, issued by the director, completed though combat began between them. |
| 2 | **Control**: the director yields to domination, confusion and fear, and resumes after | **PASS** (real time), **PARTIAL** (turn-based) | 0.7.17, 0.7.11 | Domination, the game's own `DominatePersonBuff` (INJECTED application, caster Kael): "CONTROL A4-Jorgas yielded: its allegiance has changed (charm or domination); 0 director command(s) cancelled". Bull's Strength was requested by the door and not issued while yielded; his potions stayed. The game's own save broke it twice: "CONTROL A4-Jorgas resumed … the effect has ended (yielded 2 time(s)); nothing refilled". The director then issued what he still owed. While yielded: no hiding, no potion, no command from the director. An attack order was INJECTED: Dominate Person gives no direct control in this game. Confusion (INJECTED, 18 s): yielded, and resumed 18 s later. A legitimate confusion from a fight (0.7.11): "CONTROL A4-Jorgas yielded at 131194.1: confused". **Not shown:** a running director command cancelled at the moment control changed (every observed count was 0), and a control effect landing in turn-based play (Kael's Fear did not take). |
| 3 | **Darkness and its counters** | **PASS** (dispel of the darkness itself not shown) | 0.7.16 | First the `areas` diagnostic showed no darkness area at all; fixed in 0.7.16. Then the area holds Vandiana and the naga. "Brena attacking Vandiana: partial concealment (20%)". "Tessa (true seeing) attacking Vandiana: no darkness concealment (attacker has true seeing)". "Agarik attacking Brena: no darkness concealment (attacker has darkvision within 60 ft)". "Kael attacking Dark Naga: no darkness concealment (native concealment already applies); result Total": the naga's invisibility is untouched. Party members inside carry the marker. Three targeted dispels on Vandiana removed Cat's Grace and Mage Armor, one effect per cast natively, but not the Darkness. Magic Circle: `ProtectionFromGoodBuff` on Vandiana inside her circle. |
| 4 | **Outsider burst** | **PASS** | 0.7.18 | Unholy Blight centred on a Movanic Deva (SETUP spawn). "Movanic Deva (NeutralGood, outsider) share Full, Will 26 vs 18 saved, CL 8 8d6 = 27 -> 13", while "Tessa Quill (TrueNeutral) share Half … CL 8 4d8". The descriptors are on the blueprints, and the dice are unit-tested. |
| 5 | **Whip** | **PASS**, partly unit-tested | 0.7.16–0.7.17 | "Whip … on Ilsa Venn: no damage, armor bonus 4, natural armor 4" and "… on Brena Holt: no damage, armor bonus 3". With Kael's Barkskin removed (INJECTED, natural 6 → 2): "whip … on Kael Ashen: 3 nonlethal, total 0 -> 3". Then a Cure Serious Wounds: "40 heals lethal 95 -> 55 and nonlethal 3 -> 0". **Unit-tested only:** a knock-out by nonlethal, the after-fight revival, hourly recovery and rest, and a different wielder (the rule is on the item's enchantment). |
| 6 | **Naga sting** | **PASS** | 0.7.18 | Brena (Delay Poison removed, INJECTED): "Sting on Brena Holt: Fortitude 14 vs 21 failed: asleep for 4 minutes". Tessa, with Delay Poison: the same poison applied INJECTED was refused natively (`applied False`). Neutralize Poison (SETUP-granted) removed the sleep. A save made while she slept (981) still showed her asleep after a full game restart. |
| 7 | **Scouting**: a genuinely undetected peek | **PASS** | 0.7.18 | From 976. Tessa was invisible (SETUP-granted), the rest back down the corridor. She was selected alone and opened the door (PLAYER), stepped into the doorway, saw the glyph and passed its Reflex save, then withdrew. "Engaged": false, "confrontation False", no combat. Detection or a hostile act engages, shown throughout: "ENGAGED: CombatJoined …", "ENGAGED: HostileAct (harmful spell Slow …)". |
| 8 | **Dispel and restart** | **PASS** | 0.7.16 | Mage Armor dispelled, then a mid-combat save (INJECTED `combatsave`, 980), a full game restart and a reload. Mage Armor still gone, its slot still spent, Haste and Baleful Polymorph still spent, temporary HP not refilled. The darkness area returned with the same identity and still marked her. |
| 9 | **Fresh arrival** | **PASS** | 0.7.18 | 975, the lodge, through the Refuge Token's own entry point and the departure box's OK (PLAYER). "Sanctum state initialized (fresh)", "waiting to place the defenders: transfer still in progress", then all four placed and calibrated. Books filled 20 of 20 and 22 of 22, the pre-arrival spells spent once. Policy Active, the fixture never created, and the resolver gives the real Vandiana. Kurshu's encounter began in A1 as usual; it was not fought (the WR-05 victory seeds A4's checks). |
| 10 | **Turn-based, every party order by hand** | **PASS** (both sides act); party lost | 0.7.18 | Four rounds: each party member's own spells (Slow, Fear, Dispel Magic, Haste, Entangle, Cure, Magic Missile) or attacks, then the turn ended; the defenders acted natively. Kael's own Slow committed the clock in turn-based play: "1d6+6 = 11 … due in 66 seconds". The Blackstrikes' brains drank Blur. Five of six fell; Ilsa stood when it was stopped. **Not seen in turn-based play:** a control effect landing, Disruptive, hiding. |
| 11 | Disruptive | **PASS** | 0.7.11–0.7.15 | "Disruptive: Ilsa Venn casts AcidSplash defensively while Jorgas threatens: DC 15 -> 19"; "Kael Ashen casts MagicMissile defensively while Jorgas threatens: DC 17 -> 21". |
| 12 | Sanctuary, Chill Touch, summoner's charm, arcane bond, Shield | **PASS**, but Sanctuary's barred branch is unit-tested only | 0.7.11–0.7.19 | Sanctuary: Brena, Dorn, the bear and Marra each saved once, at their first real attack ("tries to act against her (UnitAttack): Will vs 15 passed"). Then "Sanctuary … ends: she cast Slow". No attacker failed at DC 15. Chill Touch: "9 touches held (CL 9)"; "on Brena Holt: Fortitude 16 vs 14 saved"; "8 touches left"; and in 0.7.11, dissipation by another spell. Summoner's charm: "MonitorLizardSummoned lasts 9 + 2 rounds". Arcane bond: "recovers Haste (level 3)" in several fights. Shield: `MageShieldBuff` on the naga, cast by its own brain. |
| 13 | Older-save upgrade (0.7.10 save 976) | **PASS** | 0.7.11, 0.7.19 | "prepared Sanctuary / MagicCircleAgainstGood / ChillTouch ×2 (unspent) is now the spell"; "KWR_VandianaArcaneBond added"; "KWR_BlackstrikeDisruptive added"; brains brought up to date; the bond's use 1/1. |
| 14 | Stock isolation | **PASS** | 0.7.18 | `Manual_979` (SpiderCave): no `SanctumWardPart`, and no A4, darkness, Disruptive, Sanctuary or nonlethal line through a stock goblin fight. The only new exception is native, during area unload while leaving the sanctum (`ObstaclesHelper.TryConnectUnits`). |

**Diagnostic defeats:** two real-time fights (0.7.11, 0.7.12) ended in a game over when the party AI walked Ilsa into the melee. From 0.7.13 on, her AI was switched off (PLAYER setting) and she was kept back. These were checks, not victory attempts.

**About the 0.7.10 victory:**
- It stands as recorded below, but it was won against an A4 whose darkness, in-combat potions and Magic Circle never worked.
- The ambush also kept the Blackstrikes out of reach until a foe came close.
- From 0.7.15 on the defenders are stronger in exactly those ways.
- A fresh legitimate victory against 0.7.19 was **not** attempted in this pass.

**Cosmetic, not changed:** the 0.7.19 log line reads "preparation … performed by already in effect when requested …".

## First pass (0.7.0–0.7.10)

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

## Open (after both passes)
- **Not shown in play:**
  - a director command cancelled at the instant control changes;
  - a control effect landing in turn-based play;
  - a Sanctuary save failed;
  - the darkness itself dispelled;
  - a knock-out by nonlethal damage;
  - the whip in another wielder's hand;
  - a messenger warning;
  - emerald destruction in WR-06.

  Each is covered by unit tests or by the native rule it hands to.
- **Not implemented** (audited in `WR-06-LASHMISTRESS.md`):
  - Wind Wall and the naga's Silent Image (mechanics unavailable; explicit incompleteness);
  - Bleed and the naga's detect thoughts (unavailable);
  - the noncombat and information spells (out of scope).
- **No fresh victory against 0.7.19:** see the note on the 0.7.10 victory.
- **Calibration** sits on a buff that death removes (corpses only).
- **Bursts and ward fixtures:** a burst that reaches the A3 statues or the rune plates rolls against them too. The WR-04 observer measures a rune only against its own thresholds (electricity, force, sonic) and never lets proxy damage accumulate.
- **Krune's encounter (A5)** is the next milestone, after the owner's review of this pass.

## Environment (second pass)
- `wr06-pass2` ended with `End -KeepInstalled`:
  - **Profile:** 21 differences reverted, added files quarantined.
  - **Steam mirror:** 4 reverted.
  - **Registry:** restored, which resets the turn-based setting.
  - **Params and Mods:** verified.
- **Installed:** 0.7.19 (DLL `32e45672…`).
- **Backed up:** 0.7.10 (DLL `4733827d…`), at `runtime-backups\wr06-pass2\mod-existing`.
- **Saves:** only the disposable seeds and new `KWR_AUTOMATION_*` saves (980 dispelled, 981 asleep, both collected into the evidence folder). No owner save was opened.
- **Python and installs:** none used, nothing installed.

## Environment (first pass)
- `wr06-recon` ended with `End -KeepInstalled`:
  - **Profile:** 30 differences reverted, added files quarantined.
  - **Steam mirror:** 4 reverted.
  - **Registry:** restored, which also resets the turn-based setting changed in testing.
  - **Params and Mods:** verified.
- **Installed:** 0.7.10 (DLL `4733827d…`).
- **Backed up:** 0.6.16 (DLL `8bb3a2e7…`), at `runtime-backups\wr06-recon\mod-existing`.
- **Saves:** only disposable seeds and `KWR_AUTOMATION_*` saves were loaded. No owner save was opened. The stock save is the WR-05 disposable one.
- **Python:** none used. No software was installed.
