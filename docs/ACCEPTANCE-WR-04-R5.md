# WR-04-R5 acceptance: legacy reporters, departure retries, saved summon orders

This answers the R5 review, which named three defects:
- a legacy neutral reporter never reached its migration;
- a failed departure left a stale "already rested" flag;
- summon orders were lost on reload and never rechecked ownership.

It also asked for a short list of interaction checks. PR #4 stays a draft. Nothing was merged, tagged or published.

## Setup

| | |
|---|---|
| Run | `wr04r5-a`, one guarded transaction, ended and verified. |
| Builds | <ul><li>`c30be66` (0.5.6): the three repairs, the blocked turn-based runner, and the package guard.</li><li>`97a85f4` (0.5.7): a migrated reporter's group hostility.</li><li>`7a90fa1` (0.5.10): summon allegiance after a reload.</li><li>`4fbef87` (0.5.11): the summon renewal loop.</li><li>0.5.8, 0.5.9, 0.5.12 to 0.5.15 (`7053378`, `a87e5e4`, `0458e0e`, `defbfc4`, `43b79e1`, `0a7407b`): harness commands only, listed under Labels.</li></ul> |
| Candidate | 0.5.15, the last build tested and the one installed. DLL `462a2ab28ea532a8d5313afd91285b267ff8dfe2d912f36861db14eccb23bced`; zip `artifacts\KingmakerWakingRune-0.5.15.zip` `b80c26174ed86aa07bd719f4203e29d0230ca74275744d83cfba917bfb209ec8`. Gameplay code is unchanged since 0.5.11; 151 unit tests pass. |
| Party | The R3 seeds (six level 11 characters, Challenging preset), R3 and R4 disposable saves, and the R5 saves below. No owner save was opened. |
| Labels | **SETUP**: a labelled harness step. **PLAYER**: real keyboard input; this run used ESC and Space only, see Process notes. The engine resolved every roll, save, attack, spell and rest natively. The harness steps called the same entry points the UI calls: <ul><li>`token`: the token ability's own callback.</li><li>`answer`: the message box's own Yes or No handler.</li><li>`memorize`: the spellbook API used by dragging a spell into a slot.</li><li>`spell`: a spell cast from the caster's own prepared slot, which is spent.</li><li>`tbm`: the rook button's handler.</li><li>`endturn`: a party turn that the engine allows to end.</li><li>`perform`: a dialogue answer chosen for a named party member.</li><li>`attack`: an attack order.</li></ul>Positions are SETUP teleports. **INJECTED**: a granted spell (Stone to Flesh, Confusion), as earlier rounds labelled them, or a forced fault (`save-failed`). |

## 1. Legacy neutral reporter: FIXED and proven

**Repair.**
- Each statue now records `HostileSettled`. `SettleHostility` runs before the control test.
- A reporter in the neutral faction is migrated once, the way a native faction change does it. That means no player faction, no `ChangeFaction` effect on it, and not yet settled.
- While a charm, domination or other faction effect holds it, nothing changes, and the migration waits for the effect to hand its faction back.
- New restorations are settled at spawn.

| Check | Result |
|---|---|
| Legacy save | Made under the backed-up R3 build 0.4.9 from `R3_BEFORE_RUNNER`. N1 was restored with INJECTED Stone to Flesh (survival 16 vs 15), ran legs 1 to 3, and was saved as `R5_LEGACY_NEUTRAL_RUNNER`. In that build reporters are neutral. |
| 0.5.6 load | `Migration: runner Support-N1 … now hostile and resumes its route at leg 3/7`, but the next line read `hostile False`. **Defect found:** the runner keeps its own group, and setting the same group id does not regroup it, so the group's cached attack factions stayed neutral until a reload. Fixed in **0.5.7**: the cache is rebuilt explicitly. |
| 0.5.7 to 0.5.15 load | `… is now hostile (True) and resumes its route at leg 3/7`. It completed legs 4 to 7, and `Awareness Messenger (… reached Vandiana and reported)` was raised only on arrival. |
| Targetable | Read-only `cantarget`: Hold Person and Magic Missile `True; enemy=True faction=Mobs`. R3's same check on a neutral reporter read `canTarget=False`. |
| Treated as an enemy | Brought into Brena's sight (SETUP), it started native combat (initiative rolled). Brena's own attacks hit it for 6 and 2. It kept fleeing, reported only on arrival, then died. |
| A controlled legacy reporter stays controlled | **Not run.** Before R4 the party could not charm or dominate a neutral reporter: enemy-only spells could not target it (R3's finding), so no legacy save can hold one. The guard is in code: a reporter in the player faction or holding a `ChangeFaction` effect is not migrated. Control release for summons was run instead (section 3). |

## 2. Departure readiness on retry: FIXED and proven

**Repair.** The session-wide flag is replaced by a record of each member at the end of the departure rest:
- the spell and readiness of every prepared slot;
- spontaneous slots left;
- caster levels;
- ability resources;
- hit point and ability damage.

On a retry, the party departs without a second rest only when every member still matches the record. A member who joined or left, or whose record changed, sends the party back through the full flow, with a notice naming the cause. A load or a committed arrival clears the record.

All from `R3_PARTY_SEED` in the lodge.

| Check | Result |
|---|---|
| First departure | <ul><li>`token` (SETUP), then the notice; Yes (SETUP).</li><li>The native spellbook opened (Ilsa's page). ESC (PLAYER) closed it.</li><li>The empty-slot warning appeared; Yes (SETUP).</li><li>Native rest: game time 43391 → 72242, and the log read "Rest took 8 hours".</li><li>INJECTED `save-failed`: `Refuge transfer abandoned before departure: … Nothing was spent`. The party stayed in the lodge.</li></ul> |
| Changed preparation | Brena prepared Hold Person in an empty level 2 slot (SETUP `memorize`; not ready until rest). `token` gave `the earlier departure rest no longer holds: Brena Holt's spells, abilities or health changed`, and the notice read: "The party has changed since its last rest for this journey (Brena Holt's spells, abilities or health changed), so it rests again before departing." Review, warning and rest followed. Brena's Hold Person was ready afterwards. INJECTED `save-failed` again. |
| Second rest's length | 72242 → 158674, 24 hours. The native scripted rest takes the game's own minimum rest time (`Camping.GetMinimumRestTime`), which lengthens a rest begun within a day of the last. This is native, not the mod's. |
| Unchanged retry | `token` gave `the party already rested for this departure and nothing has changed since; leaving without a second rest`. Then: checkpoint verified; `Departure rest record cleared: arrival committed`; `tokens left 0`. Game time 158706 → 158714, so no rest. |
| Not run | A recruit joining between attempts. It is the same comparison (a member with no record, reported as "X joined"). |

## 3. Saved summon orders and ownership: FIXED and proven

**Repair.**
- Orders are saved in the sanctum state (version 3) as summon, summoner and rune identities only.
- On every load, the first tick checks each order: rune lit, plate present, summon alive, still linked to that summoner, and not under another's control.
- Valid orders resume; in turn-based combat, only on the summon's own turn. Invalid ones are removed, along with the attack in hand on that plate and its manual target.
- Ownership is rechecked on every renewal, on faction changes and on control changes.
- Nothing extends a summon's duration or damage, or gives it an extra action.

| Check | Result |
|---|---|
| Order and partial assault | From `R3_A2_OPEN`, all runes active. <ul><li>Ilsa cast Summon Monster VI from her own slot (SETUP `spell`; slot spent). An Axiomite appeared.</li><li>"Direct my summoned creatures" (SETUP `perform`) gave `attacks A3f, ordered`.</li><li>Windows ran 20, 31, 36 and 42 of 50. Saved as `R5_CONTROL_MIDASSAULT`, holding one order (summon `61cecaff…`, summoner Ilsa, A3f) and a window at 42/50.</li></ul> |
| Full restart, 0.5.9 | **Defect found:** on load the order ended with `it has changed allegiance`. The engine does not save a summon's group id, so after a reload it stands in a group of its own and the group comparison failed. Fixed in **0.5.10**: allegiance is read from hostility either way and from any faction effect. |
| Full restart, 0.5.10 and 0.5.11 | The game was closed and relaunched before loading. At load: window 42/50, one order. <ul><li>`found in the save and still valid`, then `resumed after loading`.</li><li>0.5.10: the next blow, 17 slashing less hardness, made 51/50.</li><li>0.5.11: 48, then 55/50, `RUNE A3f DISABLED by SummonedDamage`, `order ended: the rune is dark`.</li></ul>The window carried on from the saved 42; nothing was counted twice. |
| Recall after a reload | `recalled by the player` straight after the order was resumed. Twenty seconds later the window was still 42/50 and A3f stayed active. |
| Redirecting an order, 0.5.10 | **Defect found:** directing the attacking Axiomite from Control to Life hung the game, and the harness closed only the owned process. Re-issuing an attack interrupts the running one; the interruption counted as an ended attack and renewed it, forever. The turn-start renewal had the same exposure. Fixed in **0.5.11**: only an attack that ran its course is renewed; a failed one ends the order; a running attack on the same plate is not replaced. On 0.5.11 the same redirect worked, with normal renewals at Life. |
| Control released | Ilsa's Summon Monster IV from her own slot gave a Dire Wolf, ordered at Life together with the Axiomite. An INJECTED Confusion burst: the wolf failed its save, giving `order ended: Summoned Dire Wolf … it is under another effect's control`. No order reached the wolf afterwards. The Axiomite saved and kept its order (three renewals in the next 15 seconds). |
| Summon ends | The Axiomite's duration ran out: `order ended: … the summon is gone`. |
| Not run | A summon dominated or charmed away. Same check, by hostility or a faction effect. |

## 4. The review's short list

| Check | Result |
|---|---|
| Turn-based Hold Person interception | `R4_A1_ALARM_DISARMED`, turn-based on (SETUP `tbm`), the party placed beside the top of the stair (SETUP), marked N1 restored (INJECTED Stone to Flesh, as before). <ul><li>Round 1: Brena's Hold Person from her own slot. The runner was held.</li><li>Round 2: Tessa (SETUP attack order) killed it.</li><li>Leg 0; A2 still latched; no awareness cause; no messenger.</li></ul>Earlier attempts show the rest of the native behaviour. One runner saved against the first cast. Held runners lost their turns while rounds went on (to round 15 in one attempt). With the party left idle, one broke free on a later round's save and escaped through A2. |
| Unmarked reporter at a latched A2 | Turn-based, the party clear of its path (SETUP), N3 restored (unmarked; INJECTED Stone to Flesh). It reached the A2 waypoint (leg 1/7). The door stayed latched and it had no brand, so it turned back on the party, as written. Seven rounds passed; nothing was reported, and only the hound's earlier cause stood. In real time the party's own combat AI killed it on the way, twice. |
| Blocked turn-based runner (review note) | Party across the stair (SETUP). Every runner turn: `turn move: path of 2 points`, then `blocked where it stands; its turn ends`. Rounds 1 to 6 passed in 60 seconds. It was not pushed through anyone. |
| Trickery rune route | Tessa at Armor (SETUP `perform … 4`), native check against DC 35. <ul><li>23, 24 and 31 failed, each with a curse save (27 and 28 passed; 13 failed, so a first brand).</li><li>Then **35**: `RUNE A3e DISABLED by TrickerySkill`.</li></ul> |
| Use Magic Device rune route | Kael at Control: **41** against DC 35, `RUNE A3f DISABLED by UseMagicDeviceSkill`. |
| Fresh new game's shop | **Not run.** It needs character creation, which needs the mouse (see Process notes). The R4 table reconciliation stands. |

## Not run, or not proven

- A charmed or dominated legacy reporter (it cannot arise; see section 1) and a dominated summon.
- A recruit joining between departure attempts.
- A fresh new game's shop.
- These stay open on the ledger (`WR-04-LEDGER.md`):
  - glyph spell-resistance, evasion and dispel;
  - the Krune-summon mist bonus;
  - the hound's see-invisibility, through-wall and tether checks;
  - Defense's single bludgeoning hit;
  - the other open rows.
- Door audio, which is for the owner's ear.

## Process notes

- **Mouse input did not reach the game in-world this run.** The harness's mouse events opened a main-menu item, but in-game, hovers and clicks on portraits, the floor and a message box's OK did nothing, and `cursor` reported nothing hovered. Keyboard input worked throughout. The cause was not found; R4 used the same harness with the mouse. That is why this run's steps are SETUP harness calls to the UI's own entry points rather than PLAYER clicks. Earlier PLAYER results stand as recorded.
- **A stray click** during the main menu's opening animation pressed Continue and loaded the newest save. That was this run's own disposable `R5_LEGACY_NEUTRAL_RUNNER`, not an owner save.
- **Hung game** (0.5.10, above). `Close` asked the game to exit, then terminated only the owned process.
- **Packages.** Every build had its own version (0.5.6 to 0.5.15). `Build.ps1` now refuses to replace an existing clean package unless `-ReplacePackage` is passed with the owner's authorization. A working-tree build still writes its own `-dirty` file.
- **Python** was not used this round.
- **Saves** (`runtime-evidence\wr04r5-a\saves`): `R5_LEGACY_NEUTRAL_RUNNER` (0.4.9, neutral N1 at leg 3), `R5_CONTROL_MIDASSAULT` (0.5.9, order saved at 42/50), `R5_TB_HELD` (a later turn-based state, not cited), plus the copied R3 and R4 seeds.
