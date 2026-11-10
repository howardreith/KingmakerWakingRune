# WR-04-R2 review repairs: acceptance

**Status: candidate 0.4.2 for owner review. Not merged, tagged or published.** This answers the review of `504d483` point by point. Earlier rows are in `docs/ACCEPTANCE-WR-04-R1.md` and `docs/ACCEPTANCE-WR-04.md`; the summary matrix below restates what changed.

| | |
|---|---|
| Final candidate | commit `44a4b78` (docs commits follow); DLL SHA-256 `68db679cd292a19ee57ef3fc65b8c324428670a07c7daf56cab3d97e1eefefdc`; the delivered zip hash is in STATUS.md |
| Builds in this run | `923f18e` (first half of run `wr04r2-a`), `b1f9e45` (cultist Con, logged refusals), `44a4b78` (cultist hp fact, sleeper rolls no save, spent feeder buff removed). Each row names the build it ran on. |
| Unit tests | 148 passed, 0 failed |
| Run | `wr04r2-a`, one guarded transaction; evidence in `runtime-evidence\wr04r2-a` (screenshots, `umm-log.txt`, `output_log-*.txt`, harness receipts, collected disposable saves) |
| Saves | Owner's ordinary campaign `Manual_303_KMC_C7_NO_MODS_OWNER_SEED` (read inside the transaction, byte-identical to its snapshot afterwards); disposable six-character test party "Serin Vale" (`KWR_AUTOMATION_WR03_RANGERS`, from WR-03) travelling by token to a fresh sanctum; new disposable saves `KWR_AUTOMATION_R2_*`. |
| Difficulty | The test party's save uses stats preset `Decline`; the owner's saves use `ExtraDecline` with enemy difficulty `Weak` (read from the R1 test save). |

Labels: **PLAYER** real mouse and keyboard; **HARNESS** a labelled test command (position, grant, spawn, injected damage, or running a dialogue action). Effects still resolve natively.

## Review findings

### 1. Turn-end hook created sanctum state in ordinary campaigns — FIXED, PASS (native, `923f18e`)

- Code: reads go through `WardStore.Existing` (never `Ensure`); only `OnSanctumLoaded` creates the part. `TurnEndPatch` returns at once outside the sanctum; `TurnEnded` checks the area and loading before reading state.
- Regression, in the owner's ordinary campaign save (SpiderCave, four characters): `wardpart` false on load. Turn-based mode was switched on in Options (PLAYER). A goblin was spawned (HARNESS). Then came native turn-based rounds with turns ended by the end-turn button (PLAYER), Amiri attacking until the goblin died, and the combat ending. `wardpart` stayed false throughout. A disposable save `R2_STOCK_TBM` was made and reloaded: still false. An offline scan of every JSON entry in that save found no `SanctumWardPart` and no mention of the mod.
- The lodge does not create the part either. It first appears on arrival in the sanctum.
- Existing saves: a read-only scan of the owner's 290 saves found the part in one save only, `Quick_432`, which is in the sanctum area. Four saves of an older format could not be opened and were not examined.
- Not run: a negative control on the 0.4.1 DLL.

### 2. Long work could falsely complete an interrupted Alacrity attempt — FIXED, PASS

- Code: both repairs the review offered. Long work is refused while any focus has an open window, a sustain, a pending cast or expiring negative levels (`RuneRules.HasTimedAttempt`). The evaluator also counts only observed duration (`RuneSustain.LastSeenActiveAt`), so elapsed time alone can never succeed.
- Pure tests: the review's exact sequence (on at 0 and 12, found off at 300) fails and does not disable; a status seen on through 29.75 s and still on at 30 s disables; a legacy sustain without a sighting cannot succeed on a lapse; each kind of timed attempt blocks work; an idle focus or a permanent negative level does not.
- Native (`923f18e`): Hold Monster on A3b (HARNESS grant) began a paralysis sustain. A one-minute impression on N1, ordered during it, was refused: no `Work:` line and no item. The control repeat after the attempt resolved completed (`advanced the simulation 60 of 60 seconds`, item received). A3b then disabled natively: `Paralyzed observed on for 5.0 rounds`, with each round's new Hold Monster save denied as a paralysis vulnerability.
- Refusals are now logged (`Work refused: …`) from `b1f9e45` on. That build was not re-run for this row.

### 3. Mist sleep vanished without a recorded wake — CAUSE IDENTIFIED, FIXED, PASS on `Decline`

- **Cause:** R1 run b's save uses the `ExtraDecline` stats preset. In that preset the engine's own `RuleApplyBuff` caps every negative-descriptor buff on a party member at three rounds, and blocks a repeat of the same descriptor within one combat. The mist sleep carries the Sleep descriptor, so that run's sleep could not outlast three rounds. The mod also left the record "asleep" when the buff was gone, so the next round's save ran as if awake.
- **Repair:** the apply rule (applied, immunity, story-mode immunity, duration, preset) and every removal (game time, end time, call path) are now logged. Any end of the sleep counts as a wake and resets the streak; a refused sleep leaves the record awake; a sleeper rolls no further save.
- Native results:
  - **Sleeps and holds (PASS, `923f18e`).** Dain failed Will 17 three times, then slept. The apply trace read `applied=True … duration=permanent difficultyStats=Decline`. He stayed asleep through 45 s of uneventful rounds with no removal.
  - **Through a full restart (PASS, `b1f9e45`).** Save `R2_MIST_ASLEEP`, closed the game, relaunched, reloaded: still asleep, Dex 1 (native helpless).
  - **Ally wake (PASS, `b1f9e45`).** Serin used Wake Ally, a standard action (HARNESS order). The removal path read `… WardRuntime.WakeSleeper < KwrWakeAction.RunAction < … AbilityExecutionProcess.ApplyEffect`.
  - **Damage wake (PASS, `b1f9e45`).** The hound's bite hit the sleeper for 9. The removal path read `… WakeSleeper < KwrMistSleepWake.OnEventDidTrigger < … Rulebook.Trigger < WardHazards.Bite`.
  - **Streak reset (PASS).** After the ally wake Dain needed a fresh run: streak 1, 2, a natural-20 reset to 0, then 1, 2, 3 and sleep again.
  - **No save while asleep (PASS, `44a4b78`).** After reloading a sleeper, about four rounds passed with no Will lines in the battle log.
  - **Turn-based path (PASS, basic).** Mara slept during turn-based combat; the apply trace read `inCombat=True`. The real-time mist tick is off in turn-based combat, so this exposure came through the native turn-end hook. Switching to real time mid-combat produced no error and no double exposure.
- **NOT RUN:** the `ExtraDecline` behaviour natively on this build; the trace would record it.
- **Owner decision:** whether the scenario's sleep should exempt itself from the preset's three-round cap. The candidate respects the preset.

### 4. Rune save exception was descriptor-only — FIXED, PASS for Life (native, `923f18e`)

- Code: `RuneEffectScan` reads the effect's action tree, including parent contexts, for negative-energy damage, energy drain and energy damage. `RuneVulnerability.QualifyingEffects` gives each to the plate whose routes name it, and only that plate. The blanket auto-failure patch is not restored.
- Pure tests: Life denies undescribed negative energy and energy drain; Alacrity and Control save against negative energy; Alacrity denies undescribed cold; Armor saves against cold; statues and Krune always save.
- Native: Harm on A3a (HARNESS grant, real cast). Log: `Rune save denied: A3a Will 99 vs DC 11 from HarmDamage; descriptor None; effects NegativeEnergyDamage`. Without the repair the plate would have passed and taken half. It took the full `NegativeEnergy 110` and went dark: `NegativeEnergyDamage: 102/27 after hardness 8`.

### 5. Cultist statistics — CAUSE IDENTIFIED, FIXED, PASS (native)

Probe readings (blueprint, base, modified, damage, drain, every modifier's source, race, classes, gear, facts) at spawn, before the survival save, at the first tick and after reload showed:

- The clones kept the bandit donor's `CustomizationPreset`, which picks a random race and body at every spawn. That, with readings of helpless or dead bodies (the native `Helpless` modifier makes Dex 1), explains R1's varying scores. The preset is cleared. N4 and N2 now read `race HumanRace` with printed Str, Dex, Int, Wis and Cha (`923f18e`).
- Con read 14 and Fort 5 because the native `HumanRace` +2 lands on the already-racial printed 12. Fixed in `b1f9e45`: Con 12, Fort +4.
- Hit points read 26: native NPC cleric dice 17, Con and the donor's Toughness. A negative blueprint share was clamped by the engine (24 in `b1f9e45`). Fixed in `44a4b78` with a hidden saved fact: N2 spawned at `hp 16/16` and kept max 16 after a save and reload.
- Final block on `44a4b78`: human cleric 3, Con 12, Fort +4, Ref +2, Will +5, hp 16, masterwork sickle, light steel shield, chainmail. **AC 16** instead of the printed 18 comes only from the game's global `DifficultOpponent` feature (−2 at this save's difficulty), which it gives every enemy. It is left as a legitimate difficulty modifier, as the review asked; under the owner's preset it will differ again.
- The survival save is native (N4 survived on Fort 19; N2 failed twice on Fort 9 from the same saved dice state). Nothing overwrites statistics after spawn. The donor's feature list carried no difficulty component, so no copy was needed.

### 6. Hound attack semantics — FIXED, PASS (native, `b1f9e45`)

- Code: a native `RuleAttackRoll` with the native `Bite2d6` weapon. The hound is natively `Invisible` for that roll only. The unconditional bonus is held at the printed +10; damage is 2d6+3 piercing, magic, doubled on a confirmed crit.
- Native: `Hound bite on Dain Reed: d20 9 + 12 vs AC 4 (flat-footed True): Hit` against a sleeping, helpless target. Then `d20 13 + 12 vs AC 17 (flat-footed True): Hit` once he was awake. The +12 is the printed +10 plus the engine's +2 for an attacker the target cannot see; flat-footed comes from the same native rule.
- Also observed: the hound barked at the first creature to enter its chamber (Mara, in the mist), stayed active for its printed discharge window, and expired with no one in bite range. That is correct.
- Not run: a target with see invisibility, mirror images, the through-wall negative and the tether.

## Credited items carried from R1, re-run this cycle

| Item | Result |
|---|---|
| Feeder depletion with an unrelated source, restart, full depletion | **PASS** (`923f18e` and `44a4b78`). Feeder grant 57 (HARNESS), then a native Aid cast on Krune (+15). 20 damage from Krune's own source: the engine spent Aid's 15 first, then 5 of the feeder's, and the ledger recorded exactly 5 (57 → 52). Another 20: ledger 32. Saved, full restart, reload: ledger 32 and Krune's temporary hp 32. A final 40: ledger 0, grant expiry unchanged (no acceleration), and on `44a4b78` the spent buff left Krune. One display caveat: the total read 52 instead of 32 for one tick after the second hit, because the engine reduces modifiers in place before recalculating; the next hit proved the real value was 32. |
| Defense success at DC 25 | **PASS** (`923f18e`). Serin, CL 11 with harness-granted Greater Dispel Magic, rolled checks of 24, 19, 24 and 21, each a failure, each followed by a native curse save (all passed). The fifth attempt rolled 25 vs DC 25: `RUNE A3c DISABLED by Dispel`. |
| A2 by a recovered hand | **PASS**. The N4 corpse's hand was recovered (HARNESS-run dialogue action) and presented: `A2 latch released … feeder bypassed for this contact (armed=True)`, then the native door opened. |
| Armor success | **FAIL to achieve** (not a defect). One CL 11 Fireball dealt `Fire 36` (28/35 after hardness), and the window closed as a real failure with a curse save. A second caster in the same round did not reach the plate: Kira walked through the mist on the way and fell asleep. Fire Storm orders stalled on pathing. This low-level test party has no second competent caster. |
| Control by an obtainable route | **NOT RUN** |

## Matrix changes from R1

| Row | R1 | Now |
|---|---|---|
| Turn-based boundaries | NOT RUN | **PASS, basic**: stock-campaign isolation, sanctum mist exposure through the turn-end hook, mid-combat switch to real time |
| Feeder spent HP stays spent | NOT RUN natively | **PASS** |
| Mist typed bonus and wake | PARTIAL (anomaly) | **PASS** for sleep, hold, restart, both wakes and streak reset on `Decline`; ExtraDecline cap identified but not natively re-run; Krune-summon bonus still pure only |
| Hound | PARTIAL | **PASS** for native attack resolution; through-wall, tether and see-invisibility NOT RUN |
| Fixture save scoping | PARTIAL | **PASS** for Life (Harm) and Alacrity (Hold Monster); other plates by pure test |
| Cultist identity | PARTIAL / open defect | **PASS** except AC, which carries the game's global difficulty modifier |
| A13–A17 rune routes | PARTIAL | Life (negative energy), Alacrity (paralysis 5 rounds) and Defense (dispel 25) **PASS** on R2 builds; Armor FAIL to achieve; Control NOT RUN |
| Long work policy | PASS, narrow | **PASS**, refusal during a timed attempt shown natively |

## Still not done

- Integrated six-character walkthrough at Subtier 10–11. The only six-character test save is a WR-03 party with one level 11 druid and five much weaker characters, so it was used for the focused regressions but is not a valid Subtier 10–11 walkthrough. A real level 10–11 party save is needed.
- Armor success, a Control route by an obtainable means, poison summon, entangle, cold and energy routes.
- Interception of the runner; glyph penetration, evasion and disarm; the alarm disarm at 26; the Krune-summon mist bonus natively.
- Door audio by ear (the owner's confirmation).
- `ExtraDecline` mist behaviour natively on this build, and the owner's decision about it.

## Harness notes

- A unit with a stuck move order overrides an injected position until it gets a new order. The in-game menu pauses the game and stalls queued orders. Both cost time and are not mod behaviour.
- Kingmaker restores its dice state with a save: N2's survival roll was 9 on both loads of the same state.
