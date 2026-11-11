# WR-04-R4 acceptance: player-facing gaps

This answers the R4 review, which asked for:
- reporter targeting and flight;
- summon orders;
- departure readiness;
- the shop update;
- inspection presentation.

PR #4 stays a draft. Nothing was merged, tagged or published.

## Setup

| | |
|---|---|
| Run | `wr04r4-a`, one guarded transaction, ended and verified. |
| Builds | <ul><li>`f718c4c`: all five items.</li><li>`4f453e7` (0.5.1): read-only harness `stock` and `slots`.</li><li>`237214e` (0.5.2): warning wording; harness `cursor`.</li><li>`bdda53b` (0.5.3): a reporter's AI is off from the moment it is restored.</li><li>`da9fead` (0.5.4): turn-based runner diagnostics.</li><li>`5e05519` (0.5.5): ability damage never kills a fixture.</li></ul> |
| Candidate | 0.5.5 is the last build tested and the one installed. DLL `56bb6091267a5f657fe722460d8821c02723768e8c803a637c14cf486c582587`; zip `artifacts\KingmakerWakingRune-0.5.5.zip` `a3a524fe1671e51e33e71886e342019237f947aae56eceb82cd1b95409b445ef`. |
| Party | The R3 seeds (six level 11 characters, Challenging preset). R3 sanctum saves were used where noted. |
| Saves | Disposable `KWR_AUTOMATION_R4_*` saves, in `runtime-evidence\wr04r4-a\saves`. No owner save was opened. |
| Labels | **PLAYER**: real mouse and keyboard. **SETUP**: a labelled move order or teleport. The engine resolves everything natively. No spell, item, damage or credential was injected this cycle. |

## 1. Reporter targeting and flight: PASS (with one turn-based gap, now closed)

**Setup:**
- A1 from the departure below (PLAYER).
- Tessa spotted the alarm natively from a SETUP move to R3's approach point, then disarmed it from the edge of its area (PLAYER; `Trickery check succeeded… successfully disarms the trap`). State `Disarmed: true, Alerts: 0`. Saved as `R4_A1_ALARM_DISARMED`.
- Ilsa read a **bought** Stone to Flesh scroll on the marked reporter Support-N1 (PLAYER). The cursor over the statue with the scroll aimed was the native `CastCursor`.
- The native survival save decides each restoration; across the attempts it failed several times (bodies came out dead) and passed the rest.

| Check | Result |
|---|---|
| Hostile and targetable | `Runner Support-N1 … flees: its own AI is off; hostile True`. With Hold Person aimed, the cursor over it was the native `CastCursor` with no forbidden mark. |
| Interception | 0.5.2: the restored runner's AI chose a sickle swing (`commands=[UnitUseAbility]`) before the flight took over, and the party killed it. **0.5.3** turns the AI off at the restoration and drops any AI-chosen command (`commands=[]`). Brena cast **Hold Person** (PLAYER) while it fled up the stair: `HoldPersonBuff`, Dexterity 1, stopped at (10.88, 18.97) through six seconds of running time. The party then killed it. `Causes: []`, no messenger, Brena's slot `[spent]`. Saved, reloaded: the slot stayed spent and hit points were unchanged; no rest. |
| Real-time flight in combat | Runner on `UnitMoveTo` throughout combat, up the stair under attack (SETUP teleport of the party away). In one run it was killed at the A2 waypoint by the party's pet; in another it opened A2 with its own brand and completed all seven legs. |
| Turn-based flight | 0.5.3: the runner barely moved. 0.5.4's diagnostics showed its turns were reached (`status=Acting … movement=7.6`) and moves issued, but blocked while the party stood on the stair. With the stair clear: `path of 10 points`, **A2 opened by the runner**, legs 2 to 7, then `Awareness Messenger (… reached Vandiana and reported)`. |
| Mid-route save and reload | Paused at leg 3, past A2; saved `R4_RUNNER_MIDROUTE2`; reloaded. It resumed at leg 3, completed legs 4 to 7, and reported only on arrival (`Reported: true`). |

## 2. Summon orders: PASS

All from R3's `A2_OPEN` save (all runes active), PLAYER throughout.

| Check | Result |
|---|---|
| Order offered at any active rune | "Direct my summoned creatures to attack this rune" was offered at both Control (A3f) and Life (A3g), after Ilsa had summoned. |
| Control by actual summoned damage | Ilsa cast her own prepared **Summon Monster VI (Axiomite)**, then gave the order. <ul><li>Each order ran as a native attack command: `Summon order: Summoned Axiomite … attacks A3f, ordered`, then `renewed after its attacks`.</li><li>Every hit went through hardness: 11, 20, 29, 38, 45, 49/50.</li><li>Result: `RUNE A3f DISABLED by SummonedDamage … 60/50`, then `Summon order ended: … the rune is dark`.</li></ul> |
| Calling them off | Same setup, then "Call the summoned creatures off this rune" at 32/50. `Summon order ended: … recalled by the player`. No further damage arrived, and A3f stayed active. |
| Native poison rider | Ilsa's **Summon Monster III/IV (monitor lizards)**, ordered at the Life plate. <ul><li>Their bites carried the native `MonitorLizardPoisonFeature`, and the plate's saves were denied.</li><li>The rider reached the Life rule: `1 Dexterity from Poison by Summoned Monitor Lizard … PoisonDiseaseDamage Dexterity: 1/3`, the same path the Poison spell uses.</li><li>Further bites only restack the same poison buff, so this summon cannot reach 3 points in one round. The rider route is proven, but no disable came from it.</li></ul> |
| **Defect found and fixed (0.5.5)** | The first rider point killed the Life plate (`hp=-200`, "the plate is gone"). The engine kills any creature whose ability score falls below 1, and a plate has an object's Dexterity of 1. Ability damage on a fixture is now recorded for the rune and the object's own score put back. On 0.5.5: `Fixture A3g: 1 Dexterity damage recorded for its rune; the object's own score is unchanged`, and the plate stayed whole through repeated bites. |

## 3. Departure readiness: PASS

From R3's `PARTY_SEED`: Ilsa's levels 2 to 6 empty, Brena and Marra largely empty. This reproduces the R3 gap.

| Check | Result |
|---|---|
| Notice and cancel | Using the token showed the departure notice. **Cancel** produced "Departure put off before the rest. Nothing was spent." The token remained. |
| Review | OK opened the native spellbook. Ilsa prepared Summon Monster VI in all three level 6 slots, and the game noted "Rest to restore spells". Brena prepared Hold Person three times. |
| Empty-slot warning | The warning listed every empty level per caster (e.g. "Ilsa Venn: 5th level (4), 4th level (6) …"). Nothing was filled for the player. |
| Rest, then transfer | `Departure rest: native scripted party rest started; before: Ilsa Venn … L6 ready 0/3`. The game log read "Rest took 8 hours". `complete: Ilsa Venn … L6 ready 3/3`. Then the checkpoint was verified, arrival was committed, and the rest record was cleared. |
| No rest on reload in the sanctum | No departure rest on any sanctum load this run. Brena's spent Hold Person stayed `[spent]` across a save and reload. |

## 4. Shop update: PASS

The rows below come from the read-only `stock` command, which reads the saved table, the counts it was last reconciled against, and the live shelf.

| Check | Result |
|---|---|
| Existing lodge, before shopping (`PARTY_LEVELED`) | The saved table (239 lines, 759 items) reconciled to 275 lines and 814 items. Only these changed: <ul><li>Dagger +3: 1 → 2.</li><li>Scroll of Stone to Flesh: 0 → 6.</li><li>Every new exotic line, at its catalogue count.</li></ul> |
| Existing lodge, after R3's 250,280 gp of purchases (`PARTY_SEED`) | Every item R3 had bought stayed at its bought-down count. Examples: Cloak of Resistance +2 stayed at 3 against a catalogue of 6; Belt of Mighty Constitution +4 stayed at 0. Only the dagger, the scrolls and the new lines were added. |
| Buying (PLAYER) | Fauchard +1 (labelled Exotic, 2,400 gp) and three Stone to Flesh scrolls (1,650 gp each). Gold fell from 199,720 to 192,370. The shelf showed the +3 dagger at 2, the scrolls at 6 and the fauchard at 2. |
| No refill | After saving and reloading, the scrolls stood at 3 and the fauchard at 1, both reading "saved = live". Bought-out lines stayed at 0. |
| Fresh new game | **Not run as a new game.** The table definition is the same 275 lines that existing lodges reconcile to. |

## 5. Inspection presentation: PASS

The cursor is read with the read-only harness `cursor`, because screenshots don't capture the mouse cursor.

| Check | Result |
|---|---|
| Hover on scenery, out of combat | Statue: `InfoCursor` (Stone figure Support-N1); a rune plate: `InfoCursor` (A3f, A3g). The floor stays `DefaultCursor`. |
| Spells and attacks unaffected | The same statue with Stone to Flesh aimed showed `CastCursor` (`pointerMode=Ability`). The hostile reporter, a living cultist, showed `CastCursor` with Hold Person aimed. Restorations, Hold Person and summon attacks all resolved. |
| Prose | <ul><li>**Arcana 25** gave the basic tier: "This is a Thassilonian rune of control. It is active. It is vulnerable to servant creatures and to commanding magic."</li><li>**Religion 38** gave the deeper tier, the review's Control copy, which replaces the identification and contains no numbers.</li><li>A unit test checks every rune, active and spent, for digits and for "hardness", "DC " and "threshold".</li></ul> |

## Not run, or not proven

- A fresh new game's shop.
- Turn-based interception. Real-time interception is proven, and turn-based flight is proven; the same Hold Person was not cast inside a turn-based fight.
- An unmarked reporter at a latched A2.
- The other R3 open items:
  - glyph spell-resistance and evasion cases;
  - the Krune-summon mist bonus;
  - the hound's see-invisibility, through-wall and tether checks;
  - the Trickery, Use Magic Device, bludgeoning and negative-level routes.
- Door audio, which is for the owner's ear.

## Process notes

- **Overwritten zip.** `Build.ps1` overwrote the tested 0.4.9 zip with an unfinished working-tree build. This is the same mistake as 0.4.2 in R3.
  - The zip was rebuilt from the installed 0.4.9 files. Its DLL is the tested `7191e0cb…`, but the zip's own hash is now `10a74a99…`, not the R3 `247f0360…`.
  - Working-tree builds now go to their own `-dirty` zip (commit `4d7b72b`), and every code change since has had a version bump.
- **Python.** The interpreter installed by accident in R1 was run once this cycle: a `python3 --version` in a shell command. It printed `Python 3.15.2` and changed nothing. "No Python has been used since" no longer holds.
- **Turn-based tutorial.** Switching to turn-based mode showed the native tutorial, which was closed with X. The registry and profile were restored at End.
