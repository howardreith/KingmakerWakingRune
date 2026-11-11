# WR-05 acceptance: Kurshu the Undying (A1)

**Candidate:** 0.6.15, commit `1891bf0`.
- DLL `271255e9650b0f6880ab22b34602cffdce54f220c843a35a37195f1ce39305c2`
- Zip `artifacts\KingmakerWakingRune-0.6.15.zip`, `94b05fc78eb9b0b0d59018b27e9bb6b01e3c6a5d7447fcfa07d8291b94a8dc1e`
- Branch `feature/wr-05-kurshu`, a child of `feature/wr-04-sanctum-wards` (draft PR #4, unmerged). Draft PR #5.
- 170 unit tests pass: 151 from WR-04, 19 for WR-05 (`KurshuRulesTests`, `DepartureRecordTests`).

**Runs:**
- `wr05-input-a`: checkpoint A, on 0.5.15.
- `wr05-a`: first round, 0.6.0 to 0.6.7.
- `wr05-b`: review round, 0.6.8 to 0.6.15.

Evidence is in `C:\Dev\KingmakerWakingRuneLab\runtime-evidence\<run>`. Since 0.6.8, encounter lines are read from the game's `output_log.txt` (UMM's `Log.txt` flushes late).

**Profile:** Subtier 10–11, six-character baseline, extra challenge, under the game's default **Normal** preset.

**Labels:**
- **PLAYER:** ordinary mouse and keyboard: the shop, equipping, scrolls from the belt, the token, every combat order, ending turns.
- **SETUP:** a labelled harness move: casting a prepared spell from the caster's own spellbook (`spell`, spending the slot natively), placing a unit (`teleport`), a save, a spawned stock foe.
- **INJECTED:** a labelled harness effect resolved by native rules: a supplied tail hit (`grab`), a fixed drain (`drain`), damage of a named kind (`hurt`), spent uses (`kurshufixture`), a corpse, and a one-frame combat pause for a save (`combatsave`).

## Review round (wr05-b): what changed and what it proved

| # | Review item | Result | Build | Evidence |
|---|---|---|---|---|
| R1 | **Preparation after the rest.** The rest no longer departs; control returns in the lodge, and a second token use departs on an explicit OK with no second rest. | **PASS** (PLAYER token, rest, OK; SETUP for the buff casts, PLAYER for scrolls) | 0.6.8–0.6.11 | First use: review, empty-slot warning, rest, "The party has rested… Make your final preparations here". Then Freedom of Movement, Delay Poison, communal resists and protections, Heroism, Barkskin, Magical Vestment, True Seeing and a Freedom of Movement scroll were cast and kept. Second use: "The party has rested for this journey… Depart now?" → "explicit departure after the rest", buffs intact at arrival (`155722-rested1`, `160308-tok3`, `162015-b2dep`). |
| R1a | Spending after the rest never forces another rest | **PASS** | 0.6.10 | 20+ slots and a scroll spent: the second use offered departure only (`DepartureRecordTests`; log "selections are unchanged"). |
| R1b | A changed selection is named, with a choice | **PASS** (PLAYER) | 0.6.10 | Brena re-prepared two spells after the rest: "Since the party's rest: Brena Holt's prepared spells or caster level changed… Depart now as the party stands?". Cancel → "Rest again before departing?" → rest; new selections ready (`161556-b2tok1`, `161609-b2tok2`). The button wording was corrected in 0.6.11. |
| R1c | Checkpoint is the prepared party; loading it grants no rest | **PASS** | 0.6.10, 0.6.11 | Loading `Before_the_Sanctum_of_Sloth` checkpoints restored the buffed party; the token offered departure without a rest (`164507-c3dep`). No sanctum load rested. |
| R2 | **Grapple with its costs** | **PASS** (INJECTED tail hits; native rolls, decisions and actions) | 0.6.11 | See the grapple rows below. |
| R2a | Freedom of movement before the grab | **PASS** | | "Tail hit on Dorn Askel: no grab (FreedomOfMovement)." |
| R2b | Grab, constrict, both grappled | **PASS** | | "Tail grab on Bear: 46 (CMB 27) vs CMD 26: held"; "Constrict on Bear: 13"; her Dex 23 → 19 and `AdditionalAttackBonus` −2 from `KWR_KurshuGrappling`. |
| R2c | Out of reach ends the hold | **PASS** | | First grab on a bear far below: "The hold on Bear ends: the held creature is out of her reach." |
| R2d | She lets go to cast | **PASS** (turn-based) | | "her turn begins: she lets go of Bear: she lets go to cast", then Hold Person. |
| R2e | Maintenance costs her standard action | **PASS** (offensive uses spent by INJECTED fixture) | | "no offensive spell-like ability left, so she maintains the hold on Bear (standard action)"; "Maintains the hold on Bear: 48 (CMB 32) vs CMD 29: squeezes"; "Constrict and tail on Bear: 18". |
| R2f | Victim's escape attempt | **PASS** | | "Bear struggles against the coils: 31 (CMB 16) vs CMD 39: still held" (her CMD includes the grappled Dex penalty). |
| R2g | Freedom of movement after the hold | **PASS** (PLAYER scroll; SETUP placed Brena beside the bear) | | Brena read a Freedom of Movement scroll on the held bear: "The hold on Bear ends: freedom of movement frees Bear." |
| R2h | One hold at a time | **PASS** (pure) | | `TestTheTailHoldsOneCreatureAndFreedomOfMovementDefeatsTheGrab`, `TestTheHoldSurvivesASaveRoundTrip`. |
| R3 | **Feats** | **PASS** | 0.6.9 | Combat Casting and Combat Reflexes present (`AttackOfOpportunityCount 7 [Feat +6]`; "casting defensively check succeeded" in the fight). Improved Iron Will as a native once-a-day Will reroll; unused through the fights observed. 0.6.8 refused to build Kurshu when its feat lookup failed (the game's feat is `IronWillImproved`, a flat +2), by design. |
| R4 | **CMB/CMD chain** | **PASS** | 0.6.8 | Printed 20/24/38 → hard mode 21/25/39 → preset +2 → expected 23/27/41, native 23/27/41. After Feed: expected and native 25/29/43. The removed +1 `AdditionalCMD` was the only unintended contribution. See `WR-05-KURSHU.md`. |
| R5 | **Intelligence drain to 0** | **PASS** (INJECTED drains; PLAYER restoration) | 0.6.15 | "drain 5 reaches 0: comatose, not dead; 4 point(s) held back (score shown 1…)", life Conscious, helpless. After a save and reload the held-back count stays 8 (0.6.15). Brena read Restoration on a comatose Dorn: "Dorn Askel wakes: restoration restored the drained Intelligence; Intelligence 13, drain 0". |
| R6 | **Legitimate victory** | **PASS** (PLAYER fight; SETUP pre-buffs) | 0.6.10 | See "The victory" below. |
| R7 | Defeat to A2 | **PASS** (PLAYER) | 0.6.10 | After her death Ilsa walked onto the A2 stair: "Alarm: Ilsa Venn entered; alert 3". |
| R8 | Persistence after a full restart, dead | **PASS** | 0.6.11 | `KWR_AUTOMATION_WR05B_KURSHU_VICTORY` after a game restart: same entity, dead, slain, uses unchanged, nothing respawned. |
| R9 | Persistence after a full restart, alive mid-fight | **PASS** (INJECTED `combatsave`, real fight state, plus INJECTED 60 cold iron and magic damage) | 0.6.11 → 0.6.12 | `KWR_AUTOMATION_WR05B_LIVE_DAMAGED`: 159/219; every use as saved (Hold Person 0, Displacement 2, Power Surge 1, Feed 1, Improved Iron Will unused); same entity; not defeated. The wound speech was spoken once after the load because the save had been taken before the encounter observed the wound (`WoundSpoken: false` in the save). |
| R10 | Stock campaign unaffected | **PASS** | 0.6.15 | Disposable stock save `KWR_AUTOMATION_R2_STOCK_TBM` (SpiderCave): `SanctumWardPart` absent, turn-based combat (rook PLAYER; SETUP goblin) for four rounds to the goblin's death, a new save, a reload, absent again. Offline scan of the new save found no mod part or name. Zero mod errors in that session. |
| R11 | Legacy notice on screen | **PASS** | 0.6.9 | Loading `KWR_AUTOMATION_R4_A1_ARRIVED`: "legacy save: encounter not inserted… legacy notice shown once" (`154208-legacy-notice`). |
| R12 | Power Surge lapses unused | **PASS** (in play) | 0.6.11 | "Power Surge's round ended unused; the use stays spent." |

## The victory
- **Party:** the seeded level-11 six (wizard, cleric, druid, fighter, rogue, sorcerer, plus the druid's bear).
- **Bought from the Quartermaster** within the existing budget, by ordinary input: Longsword +3, Shortsword +3, Dagger +3, 10 acid flasks, 4 Freedom of Movement scrolls, 2 Greater Invisibility scrolls, potions of Bull's Strength and Cat's Grace (60,400 gp of 192,370).
- **Prepared** in the spellbook review (PLAYER):
  - True Seeing ×2, Delay Poison Communal, Resist Energy Communal, Greater Dispel Magic, Mass Cure Light Wounds.
  - Then one rest, by the new flow.
- **After the rest** (SETUP casts from the casters' own books, PLAYER scrolls):
  - Freedom of Movement on all six, Delay Poison on all.
  - Resist cold and fire, Protection from Cold and Electricity on all.
  - Heroism, Barkskin and True Seeing on the fighter and rogue, Magical Vestment on the fighter.
- **The fight**, every order by click or key (real time with pause, then turn-based via the rook):
  - Ilsa cast Haste on the party.
  - A Greater Invisibility scroll went on Tessa.
  - Ilsa also cast Stinking Cloud by a mis-click on her bar. It was centred on Tessa, who was immune under Delay Poison, and it nauseated Kurshu, who failed her save. It was a legal player action, and it cost Kurshu two turns.
  - Kurshu: Displacement, Power Surge, three Hold Persons (two at DC 22) against Freedom of Movement, two limited-wish Cones of Cold, and a Fireball on the bear.
  - Dorn's Longsword +3 hit through her damage reduction. Tessa's sneak attacks took 81 and then 92.
- **The kill:**
  - Ilsa's Acid Splash (spell resistance overcome, 3 acid) stopped her regeneration for the round.
  - Dorn took her to 4, and Tessa to −11.
  - "DEFEATED at 130826 (slain); deadline started True, due at 136226", then "slain… regeneration ended".
- **Afterward:** the wound speech, the blood message and "Kurshu the Undying is dead, and does not rise" appeared in the dialogue log (`164139-win3-dialoguelog`). All six party members were alive at full or near-full health (Ilsa 110/112).
- **Counters that mattered:**
  - +3 weapons (the engine treats +3 as cold iron).
  - Freedom of Movement (Hold Person).
  - Delay Poison (Stinking Cloud).
  - Communal resists and protections (Cone of Cold).
  - Sneak attack with invisibility.
  - An acid source to end regeneration.

  Touch attacks alone are not a counter to spell resistance. Acid Splash in this game is not affected by spell resistance (its own tooltip), though the observer still logged a spell resistance roll, which it overcame. Acid Arrow (also spell-resistance-free in this game) was not in the wizard's book.

## Difficulty, restated
The first round's report said the loss showed "printed values, working". That was too strong.
- **That loss also carried defects since fixed:**
  - a grapple that cost her nothing;
  - a CMD one point high;
  - two feats missing (she was in fact weaker without them);
  - Intelligence drain that killed instead of leaving the victim comatose;
  - a departure that made pre-combat protection impossible.
- **What the two fights now show:** with the printed counters, bought legally and prepared before departure, a level-11 party beats her without losses. Without them, the same party loses.
- **Not shown:** that her difficulty is "right" in general. Two fights are not a balance study. Nothing was lowered to obtain the win: hit points, spell resistance, damage reduction, resistances, DCs, uses and the preset are as before.

## First round (wr05-a, 0.6.0–0.6.7), as reported then
These code paths are unchanged in 0.6.15 except where a review-round row above supersedes them. The fights in `wr05-b` exercised them again.

| # | Check | Result | Build | Evidence | Notes |
|---|---|---|---|---|---|
| 1 | Arrival by ordinary input; she appears only after control is restored; nothing attacks under the fade | **PASS** (PLAYER) | 0.6.2–0.6.7 | Log: "waiting to appear: transfer still in progress", then "appears at the apex … control restored", 0.7 s of game time after arrival committed; combat joined after | She does not exist until arrival commits, loading ends, the fade is hidden and the game mode is normal or paused. |
| 1a | Before combat: Displacement only if she believes she is hidden | **PASS** (PLAYER) | 0.6.2–0.6.7 | Stealth 27 vs Perception 37 and 23 vs 35: no precast. Stealth 35 vs 29: "she believes she is hidden", precast Displacement (a real use: 3 → 2) | Native skill checks. |
| 2 | Real-time-with-pause fight | **PASS** (PLAYER) | 0.6.2, 0.6.3 | `132249-fight0`, `133115-b2`, `133209-b3` | Party orders, Freedom of Movement and Acidic Spray by clicks; she held the apex and the party reached her by the east stair. |
| 3 | Turn-based fight and action economy | **PASS** (PLAYER rook button; SETUP end-turn for party turns) | 0.6.6, 0.6.7 | "READ turn: Kurshu … Acting", one swift Power Surge plus one standard ability per turn, then the next unit; Feed took her whole turn | Her first surge was armed in the moment before turn-based took over and was spent on her opening hidden action. |
| 4 | Real spell-like abilities and finite uses | **PASS** | 0.6.2–0.6.7 | Hold Person 3 → 0; Stinking Cloud 3 → 0 (0.6.2); limited wish 2 left at arrival, then two Cones of Cold, then 0; native resource = ledger in every sheet | One wish was pre-spent (reconciled 3 → 2 at spawn). |
| 5 | Regeneration and acid | **PASS** (INJECTED damage) | 0.6.4 | 159 → 164 over a round; −36: "unconscious, regenerating"; acid 6 then "slain … regeneration ended" | Native `AddEffectRegeneration` and the native death threshold. |
| 6 | Intelligence drain on a valid target, persisting | **PASS** (PLAYER fight) | 0.6.0 | "Slam hit Brena Holt: Intelligence drain 2 … drain 2", then 4 more, then Brena died at Intelligence 1 (native rule: an ability score below 1 kills); `IntelligenceDrain` status buff | Saved as native stat drain. A reload with a drained character was not separately shown. |
| 7 | DR, resistance and SR with negative controls | **PASS** (INJECTED damage; PLAYER for SR) | 0.6.4; 0.6.3 | magic only 30 → 20; plain 30 → 20; cold iron + magic 30 → 30; fire 40 → 10; plain arrow 7 → 0 (PLAYER); "Spell resistance 26 against Ilsa Venn's AcidicSpray: roll 10, resisted" | DR needs both cold iron and magic. In this engine a +3 weapon counts as cold iron. |
| 8 | Power Surge: one-round effect and cost | **PASS** | 0.6.2–0.6.7 | Armed, then "Power Surge spent on KWR_KurshuHoldPerson" at DC 22 (20 + 2); unsurged Hold Person in the same fight at DC 20; 3 → 0 uses | Lapse when unused is unit-tested; it was not seen in play. |
| 9 | Valid Feed and invalid-corpse controls | **PASS** (INJECTED corpses) | 0.6.4, 0.6.7 | A wolf (not an outsider) and a hell hound (4 HD) ignored; Movanic Deva (12 HD): "FEEDS … 2 Divine Separation level(s) removed, 0 left; other negative levels untouched"; caster level 13 → 15 | 0.6.4 froze her on an unreachable feed path (fixed in 0.6.5). 0.6.6 never fed in turn-based mode (fixed in 0.6.7). |
| 10 | Save and full restart | **PASS** (SETUP save outside combat) | 0.6.4 → 0.6.5 | `KWR_AUTOMATION_WR05_KURSHU_SLAIN`, full restart, reload: same entity re-bound, nothing spawned, uses unchanged (Hold Person 0, Surge 0, Displacement 2), slain kept, blood message not repeated, deadline unchanged | **Mid-combat saving is refused by the game itself** (also for players), so the reload was taken after the fight. The live-Kurshu, damaged-mid-fight reload is **NOT RUN**. |
| 11 | Defeat starts the existing 90-minute deadline once | **PASS** (INJECTED damage) | 0.6.4 | "DEFEATED at 101263 (unconscious, regenerating); deadline started True, due at 106663"; after death and reload, still 106663 | `RitualRules.DefeatKurshu`, idempotent; unit-tested against regeneration, a second fight and repeated callbacks. |
| 11a | Substantial wound speech, once | **PASS** (INJECTED damage) | 0.6.4 | "substantial wound at 159/219: she speaks" (bark and log) | Trigger: the first time she falls to three quarters of her hit points. |
| 11b | Blood message | **PASS** | 0.6.4 | Notification and log, `134137-defeat` | |
| 12 | Continue to A2's existing content | **PASS** (PLAYER) | 0.6.5 | After reload: Alarm alerts 0 and Vandiana unaware (Kurshu never set it off); Ilsa walked onto the warded stair: "Alarm: Ilsa Venn entered; alert 1" | Deeper A2–A5 content is unchanged code and carried forward in the ledger. |
| 13 | Legacy inside-sanctum save | **PASS** (policy), **PARTIAL** (notice; now R11, PASS) | 0.6.0 | `KWR_AUTOMATION_R4_A1_ARRIVED`: "migrated to version 4", "legacy save: encounter not inserted", no Kurshu entity, no clock | The notice moved to the first tick in 0.6.1 (it was logged but not visible on 0.6.0); not re-observed since. |
| 14 | Stock campaigns unaffected | **NOT RUN** natively (now R10, PASS) | — | By construction: every hook runs inside `SanctumScope.IsSanctumLoaded` or on the encounter's own unit and blueprints | |
| 15 | A legitimate victory by a suitably equipped party | **NOT RUN** (now R6, PASS) | — | The one fight that ran to its end was a defeat, `133209-b3` | See "Difficulty". |
| — | Stat sheet observed | **PARTIAL** | 0.6.2 | `WR-05-KURSHU.md` | AC, hp, saves, SR, initiative, attack and DCs match the authored values plus the preset. Native CMB 23, grapple 27 and CMD 42 against authored 21/25/39: calibration was resolved in R4. |

## Defects found and fixed in the review round
| Build | Defect | Fix |
|---|---|---|
| 0.6.8 | Feat lookup `ImprovedIronWill` failed (the game names it `IronWillImproved`, and it is a flat +2), so Kurshu was not built | 0.6.9 builds the printed reroll from `ModifyD20` |
| 0.6.9 | The empty-slot warning still said "Rest and depart anyway?" | 0.6.10 "Rest anyway?" |
| 0.6.10 | The changed-selection prompt said "Choose No" under OK/Cancel buttons | 0.6.11 "Choose Cancel" |
| 0.6.11 | A comatose victim could never wake: restoration clears only the visible drain, not the held-back points | 0.6.12: restoring the score clears the held-back points too |
| 0.6.12 | Turn-based combat skips a helpless creature's turn, so its own recovery check never ran | 0.6.13–0.6.14 check from the encounter's tick (0.6.13's registry missed buffs restored from a save; 0.6.14 scans) |
| 0.6.14 | The held-back count was lost on reload (component properties are not saved) | 0.6.15 keeps it in fields; a comatose creature with nothing held back wakes |

`b179f05` (0.6.14) did not compile; `e7ed8fc` fixed it before any package was built.

## Not run, or open
- **Plane shift**, deliberately not implemented (see `WR-05-KURSHU.md`).
- **Victim escape costs no action.** A held creature's escape attempt is automatic each round and costs it nothing (there is no grapple interface). This favours the party and is documented.
- **The comatose state:** an Intelligence-0 creature is helpless, and she keeps slamming it, so its hit points can still run out, as the bear's did twice in testing.

## Environment
- `wr05-b` ended with `End -KeepInstalled`:
  - **Profile:** 37 differences reverted, added files quarantined.
  - **Steam mirror:** 10 reverted.
  - **Registry:** restored, which resets the turn-based setting.
  - **Params and Mods:** verified.
- **Installed:** 0.6.15 (DLL `271255e9…`).
- **Backed up:** the previous candidate, 0.6.7 (DLL `07a6823c…`), at `C:\Dev\KingmakerWakingRuneLab\runtime-backups\wr05-b\mod-existing`.
- **Saves:** only disposable seeds, our checkpoints and `KWR_AUTOMATION_*` saves were loaded. No owner save was opened. The stock check used a disposable stock save from WR-04 R2, not the owner's campaign.
- **Python:** none used. No software was installed.
