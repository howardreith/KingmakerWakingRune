# WR-07A acceptance: Krune's awakening

**Candidate:** 0.8.13, commit `2666721` (after the review closeout; the first candidate was 0.8.7, `39cee28`).
- DLL `7ac4908810dc4861979c333445a14ae145f99fb58004f0ca903501bca4b648e4`
- Zip `artifacts\KingmakerWakingRune-0.8.13.zip`, `09af622a729923637fd6ad237da1656454b02ae4e82707ce8d1bd949bcce868c`
- Branch `feature/wr-07a-awakening`, from `main` at `56b9e40` (the merge of PR #6).
- 214 unit tests pass: the 198 before (including the 128 rune-mask expectations), 15 in `KruneAwakeningTests`, and one in `WardRulesTests` for the object energy share.

**Runs:**
- `wr07a-traverse` (the installed 0.7.22): the ordinary route.
- `wr07a-b1` (0.8.0–0.8.7, one transaction, reinstalled per build).
- `wr07a-c1` (0.8.8–0.8.13, the review closeout, one transaction). See "Review closeout" below.

Evidence is under `C:\Dev\KingmakerWakingRuneLab\runtime-evidence\<run>`:
- screenshots;
- every harness command's transcript (`*-cmd.txt`);
- saves 986–994 and the seeds;
- `umm-log.txt` of the last launch.

**Lost evidence:** the game recreates its own log at each launch. The per-launch logs of 0.8.0–0.8.6 were not collected before relaunching, and are lost. Their facts survive in the command transcripts and screenshots, and in the save payloads where noted.
- In `wr07a-c1` each launch's log was collected before the next launch: `umm-log-launch1-0.8.8.txt` to `umm-log-launch4-0.8.11.txt`, and `umm-log-launch7-0.8.13.txt`. The two 0.8.12 launches survive in the timestamped `output_log-*.txt` snapshots, which carry the mod's lines (`output_log-190237.txt`, `output_log-191433.txt`).

## Input labels
- **PLAYER:** real mouse or keyboard input only (portrait and world clicks, dialogue answers, the space key, Ctrl+A).
- **PARTY AI:** the game's own party automation in combat.
- **HARNESS-COMMANDED:** a native order dispatched by the harness: `move`, `spell` from the member's own book, `endturn`, `ai`, `equip`.
- **SETUP/INJECTED:**
  - `load`/`save`, `teleport`, `give`, `tbm`;
  - `krunedeadline` (an accelerated global deadline; the native tick still triggers it), `setmask` (foci set dark before a test), `krunepause` (a pause at a stage, for a save there);
  - `damage` on the A4 defenders, `damagekrune`, `hitgem` (one hit through the production object pipeline), `buff`/`unbuff`;
  - `combatsave` (the one-frame combat exit for a save), `krunerelease` (the diagnostic release of the checkpoint);
  - `rest` (the game's own per-character rest, with no game time passed), `castpoint` (a spell granted for one cast at a point).
- **READ:** `krune`, `krunesheet` (the actor's native statistics), `krunecl` (the native ability-parameter rule for one of his spells), `concealment` (the game's own concealment rule for one attacker and target), `manifest`, `party`, `turn`, `paused`, `slots`, `cantarget`, `screen`, `catalog`.

## Builds and the defects testing found
Each build is committed and packaged; no package was replaced.

| Build | Commit | DLL SHA-256 (first 16) | What testing found |
|---|---|---|---|
| 0.8.0 | `dee68f1` | `c1208afbcae3e03b` | Krune's blueprint failed to build: the worn items' enchantments field belongs to the simple-equipment class, and a cloned item keeps its donor's cached enchantment list. |
| 0.8.1 | `ff04e73` | `1d3e14e8f95087f5` | The awakening worked end to end. Three defects: (1) the game's unit enumeration skips units out of the game, so a reload before emergence would lose the hidden actor and a retry could make a second; (2) the battle log showed a mist Will save for the immune Krune; (3) dimensional steps duplicated a native conjuration-school resource. |
| 0.8.2 | `dbaaf8a` | `d73a6bfd5e4ff9c6` | The legacy box named "No" for a "Cancel" button; one dialogue line called lit foci "dark". |
| 0.8.3 | `93da3a4` | `954d583361242870` | — |
| 0.8.4 | `3fb38d8` | `293f54f2ddcc31b6` | Loaded from a save made at ActorReady after a full restart, he emerged with 259 HP for mask 85 instead of 253. Facts added out of the game wait to activate, and the calibration's one-time value was lost in between. |
| 0.8.5 | `b7df20a` | `b74b5691066a9b4b` | In turn-based play the one-round stagger (six seconds of game time) ran out before his first turn, because each turn spends game time. About half a second ran between the end of the dialogue and the checkpoint. |
| 0.8.6 | `5b5a807` | `c5b82103252ccd0c` | A legacy adoption skipped the revival study. The harness `krune` report threw with no sanctum state. One log label still said "receiver". |
| 0.8.7 | `39cee28` | `b24a9de70ad553e3` | The first candidate. The review found the two [P2] defects that 0.8.8 fixes. |
| 0.8.8 | `f307d15` | `8361abdabf210951` | Review closeout code. Committed preparation counted only a cast already running, so an order given while paused and not yet begun was missed. |
| 0.8.9 | `80e0ac2` | `1b7b8f0cfde8497f` | An order given while paused was still missed: it waits in the unit's command queue, which was not read. The run also showed that he must perceive the caster at that moment (Kael had moved during a skirmish), so a diagnostic line now records what he sees each perceiver doing. |
| 0.8.10 | `4bef771` | `0278cc8613e5ed7b` | Turn-based combat refuses the pause mode and unpauses every tick: the checkpoint was recorded, but turns ran on, and Krune's first turn spent his opening stagger. |
| 0.8.11 | `8832b78` | `2d6f1de966b4c739` | Turn-based combat gave the still-neutral Krune a turn between his emergence and his opening, which also spent his stagger. |
| 0.8.12 | `d352216` | `bae3a5e18c3c6162` | Disintegrate on the emerald counted half: the game files force and untyped magic damage as an energy type. The revival study's wording was not yet the reviewed one. |
| **0.8.13** | `2666721` | `7ac4908810dc4861` | — (installed) |

## Checks
| # | Check | Result | Build | Evidence and labels |
|---|---|---|---|---|
| A | **Ordinary traversal** A1 → A2 → A3 → east door → A4, no teleport | **PASS** | 0.7.22 | See below. |
| B | **Natural confrontation awakening** | **PASS** | 0.8.1 | See below. |
| C | **Long deadline, party outside A5** | **PASS** (accelerated setup) | 0.8.3, 0.8.5, 0.8.6, 0.8.7 | SETUP `krunedeadline` brought Kurshu's deadline within seconds; the native tick triggered it each time. The party was in A1 (976) or the east corridor (977, defenders INJECTED dead). There is no proximity gate. Each time he woke unobserved, and the notice revealed nothing. |
| D | **Emerald early release** | **PASS**: injected (0.8.5) and by legal play (0.8.12, closeout C4); late hit **unit-only** | 0.8.5, 0.8.12 | See below. |
| E | **Collision and presentation** | **PASS** | 0.8.2, 0.8.7, 0.8.8 | Tessa SETUP-placed on the preferred point (10.42, 9.46): he emerged at (11.42, 9.46). Marra SETUP-placed beside the coffin (9.6, 10.7): no overlap, he stands outside the coffin, and nobody was moved (screens `151750-krunemodel`, `155945-brokengem`). The point is checked again as he steps out (closeout C1). Offscreen (B, C): only the generic notification. The camera was never moved. |
| F | **State inheritance** | **PASS** | 0.8.1–0.8.7 | See below. |
| G | **Feeder** | **PASS** | 0.8.6 | See below. |
| H | **Live rune link** | **PASS** | 0.8.6 | See below. |
| I | **Buff semantics** | **PASS** (native dispel in closeout C5) | 0.8.2, 0.8.6, 0.8.12 | See below. |
| J | **Persistence** | **PASS** | 0.8.2–0.8.7, 0.8.12 | See below. |
| K | **Prototype boundary** | **PASS** (real time, and turn-based from 0.8.11) | 0.8.1, 0.8.2, 0.8.7, 0.8.12 | See below and closeout C3. |
| L | **Isolation** | **PASS** | 0.8.6, 0.8.7 | See below. |
| — | **Parley by real input** | **PASS** | 0.8.2, 0.8.7 | The native dialogue, with Krune's portrait. 0.8.2: identify → "Thassilon is gone" → disbelief → "Earthfall…" → calculation → "We will not kneel" (screens `151637`–`151724`). 0.8.7: "Thassilon is gone" → disbelief → "Believe what you like. We have come to end you". Every answer was a PLAYER click. Both ended hostile. There is no persuasion route. |
| — | **Battle-bark opening** (A4 fight under way) | **PASS** | 0.8.10, 0.8.12 | Closeout C3. In B the party was already out of combat. |
| — | **Hostility before the exchange** (committed battle preparation) | **PASS** (cast HARNESS-COMMANDED; hover/cancel PLAYER) | 0.8.10 | Closeout C3. |
| — | **Darkvision in darkness** | **PASS** | 0.8.8 | Closeout C2. |
| — | **Revival study** | **PASS** | 0.8.5 | Recorded once in the knowledge ledger (save 991: `Krune.Revival`, Arcana, result 36 vs 27). 0.8.6 found the legacy adoption skipped it; 0.8.7 runs it there (`RevivalStudyAttempted: true`). The reviewed wording is in 0.8.13; no check succeeded in the closeout runs, so it was not seen on screen. |
| — | **Older saves** | **PASS** | 0.8.2, 0.8.6, 0.8.7 | 985 (version 5, due): the one-time box; OK adopted it from the recorded ConfrontationDeadline. 978 (due): Cancel kept a legacy exploration save (no Krune, the receiver kept, one notice); after a save and reload it was not asked again. 974 (version 4, not due): migrated to 6 with its deadline (136226) intact and no box. |

### A: ordinary traversal
- **Seed:** a disposable copy of 976 (post-Kurshu, A4 placed): six members and Marra's bear.
- **The A2 opening (PLAYER):**
  - Brena's palm impression from Support-N1, by a dialogue click.
  - The A2 door clicked, then "Press the Sihedron impression": `A2 latch released … feeder bypassed for this contact`.
- **The walk (PLAYER):** Ctrl+A, then floor clicks along A3 to beside the east door. "Open Door" clicked; then a click on the lit A4 floor. The party walked in; the glyph fired and the engagement committed 1d6+6 = 10.
- **The cause of the earlier failure:** the latched A2. WR-06's "native pathing could not reach the east corridor" is corrected: A2 is the only way from A1 into the ring, and it was closed in 976.
- **Side effects:**
  - Three stray clicks on the alarm's highlighted stair made Tessa disarm the alarm natively.
  - One HARNESS-COMMANDED move (Tessa) checked connectivity and is not part of the route.
- **Evidence:** `wr07a-traverse`.

### B: natural confrontation awakening
- **Seed:** 977.
- **Setup:** Ilsa's AI was turned off (HARNESS-COMMANDED); the party walked into A4 by PLAYER clicks, and Ilsa was sent back by a PLAYER click. The game's own auto-pause on combat start and on a spotted trap was answered with the space key (PLAYER).
- **Fight:** PARTY AI against the native defenders.
- **The awakening:**
  - The stored deadline (131093) came due natively: `AWAKENING DUE at 131094: ConfrontationDeadline`. There was no reroll.
  - It came with "3 defender(s) still standing".
  - He was committed, created, emerged at 131097 and handed off; the checkpoint held.
- **Honest note:** the five who entered A4 were killed before the deadline (Ilsa survived, kept back), so he woke unobserved.
- **An earlier attempt** ended in a native game over when Ilsa, the main character, was killed in A4. It is unrelated to the new code.
- **Screens:** `150324-checkpoint`.

### D: emerald early release
- **Seed:** 977, with the defenders INJECTED dead (no engagement, no clock).
- **The real attack:** the party walked in (PLAYER). Dorn opened the emerald's dialogue and chose "Attack the emerald" (PLAYER): his native hits of 13, 14 and 9 did nothing against hardness 20. Striking the emerald committed the confrontation clock (due 131120).
- **The breaking blow:** INJECTED `hitgem` 60 from Dorn through the production object pipeline.
  - **Why injection:** this party cannot get a blow past hardness 20 by legal play. The gem ignores critical hits and precision damage, and Dorn's best one-handed hit is about 16.
- **Result:**
  - `EmeraldDestroyed` at 131068, before 131120.
  - The stagger was applied at emergence, with 5.7 s left and frozen while the dialogue was open.
  - The parley was answered "We have come to end you" (PLAYER); he turned hostile and the checkpoint held.
- **Not shown in play here** (see closeout C4 for the legal break and the broken model):
  - A late emerald hit on the real gem was not tried natively. It is covered by `TestFirstTriggerWinsAndLateEmeraldIsNotASwitch` and `TestSameBoundaryDeadlineComesBeforeTheEmerald`.
- **The stagger in both modes** (labelled release; the checkpoint save 991, reloaded):
  - **Real time:** the stagger kept its 5.2 s across the save and reload, and was gone after game time passed 131077, its scheduled end.
  - **Turn-based** (`tbm` SETUP, 0.8.6): Krune had the first turn and was staggered through it. `the opening stagger ends with his first turn (turn-based)` at 131072.8, before its game-time end.
  - On 0.8.5 the same test found the defect above: he never acted staggered.

### F: state inheritance
Native statistics (READ) and the native ability-parameter rule (`krunecl`):

| Mask | HP | Fast healing | AC (preset +2) | Saves | Init | Caster level / class level |
|---|---|---|---|---|---|---|
| 0, intact | 293 | 3 | 35 | 17 / 16 / 22 | 8 | 17 / 17 |
| 85, mixed: two Life, Defense, Armor dark | 253 | 1 | 29 | 14 / 13 / 19 | 8 | 15 / 17 |
| 127, all seven dark | 233 | 0 | 27 | 14 / 13 / 19 | 6 | 14 / 17 |

- Ability scores are 18 / 18 / 20 / 31 / 20 / 24 and BAB 8 in every case.
- Fast healing was seen healing 3 a round.

### G: feeder
- **The grant:** Brena pressed her bare hand to A2 (PLAYER). The feeder rolled 10d6 = 36, empowered to 54, and granted it to the receiver until 134588.
- **Before the handoff:** INJECTED 20 on the receiver: ledger 54 → 34.
- **The handoff:** the accelerated awakening handed 34 over with the original expiry 134588. Krune's temporary HP was 34.
- **The unrelated control:** an INJECTED native False Life left the ledger at 34.
- **Further damage:** INJECTED 10, then 30. Ledger 24 → 0; only the feeder's own modifier was counted.
- **Restart:** a save, a full game restart and a reload left the ledger at 0, never restored to 54.
- **After he woke:** Dorn's melee hits on the A2 leaf (PLAYER "Attack the door") set the feeder off twice (33 and 55). Both went to the real Krune through the ledger: strongest only (temporary HP 55), each grant with its own hour.
- **Not shown in play:** a grant expiring before emergence, and the acceleration. Both are unit-tested.

### H: live rune link
- **Setup:**
  - A2 opened by the impression (PLAYER) before the awakening. After it, long work is refused by WR-04's rule; that was seen too ("The ritual will not wait for slow work now").
  - The party moved to the east corridor (HARNESS-COMMANDED).
  - The accelerated awakening; the release (INJECTED); 40 damage (INJECTED).
- **The rune:** Marra's own Entangle (HARNESS-COMMANDED from her book) held the Alacrity plate for 10 rounds: `RUNE A3b DISABLED … mask now 2`.
- **His links:** `rune links updated: mask 0 -> 2; damage kept 10 -> 10`. AC 35 → 33 and initiative 8 → 6; caster level stayed 17 (the revival snapshot).
- **Nothing reset:** no damage healed by the change, no resource refilled.

### I: buff semantics
- **Mage Armor:** INJECTED removal (as a dispel would): AC 33 → 29. A save and reload: it stayed removed (`BaselineApplied: true`). The Runes of Power, fast healing and calibration remained.
- **Which effects are spells:** the sheet marks the game's "from a spell" flag. Mage Armor, darkvision and see invisibility are spells; the runes are not.
- **Mist immunity:** in the captured battle log (screen `152020-dispel2`), Marra and Tessa, standing in the mist, rolled their own native Will saves, and no save appears for Krune. In code, no save is rolled for him (0.8.2). 0.8.1 had shown one in the battle log.
- **Native dispel:** 0.8.2 did not show one before the party killed him. Closeout C5 shows Kael's Dispel Magic removing Mage Armor, and its absence after a full restart.

### J: persistence
- **Before due:** every seed.
- **After commit, out of the game:** a save at ActorReady (`krunepause` SETUP), then a full restart and reload. He emerged once, with the correct 253 HP (0.8.5).
- **At the checkpoint:**
  - A normal save (986) and saves through the labelled combat fixture (987, 991). One actor, no dialogue replayed, the legacy choice not asked again, the stagger not renewed, the checkpoint held again.
  - The combat fixture makes everyone leave and rejoin combat, so initiative is re-rolled.
- **Death is authoritative:** he was killed during a diagnostic release (native party attacks after the labelled release). A save (988) and reload: still dead (−134), one actor, no receiver re-indexed, nothing recreated.

### K: prototype boundary
- **The hold:** B held three standing defenders and Krune together. The space key (PLAYER) did not unpause, and game time stayed at 131097.4.
- **The handoff:** at the parley handoff, combat began natively (Krune's initiative 18) and was frozen in the same tick.
- **What it is not:** no victory, XP or loot; the scenario was not completed.
- **Turn-based arrival:** see closeout C3.

### L: isolation
- **Seed:** 979, the stock campaign (SpiderCave).
- **Result:**
  - no sanctum state was created by reading it;
  - the game was not paused;
  - a PLAYER click moved the party normally;
  - the `krune` report said "no sanctum state" on 0.8.7;
  - loading it from a held checkpoint left the stock game unpaused.

## Review closeout (`wr07a-c1`, 0.8.8–0.8.13)
The review of PR #7 at `337455f` asked for two corrections and a focused set of checks. Results, in its order:

| # | Item | Result | Build |
|---|---|---|---|
| C1 | Emergence point checked again as he steps out | **PASS** | 0.8.8 |
| C2 | Krune's darkvision in Vandiana's darkness | **PASS** | 0.8.8 |
| C3 | Live fight: battle opening, turn-based hold, stagger, committed cast | **PASS** | 0.8.10–0.8.12 |
| C4 | The emerald broken by legal play; broken model; no re-awakening | **PASS** | 0.8.12 (energy rule fixed in 0.8.13) |
| C5 | Native Dispel Magic on Mage Armor, then a restart | **PASS** (with the notes below) | 0.8.12 |
| C6 | Wording | **DONE** | 0.8.8, 0.8.13 |
| C7 | "Exact" sheet claim | **CORRECTED** | docs, PR text |

### C1: placement checked again at emergence
- **Setup (SETUP/INJECTED):** 977; the defenders INJECTED dead; `krunepause ActorReady`, a save there (995, `C1_AR_OCCUPIED`); Tessa SETUP-placed on the chosen point (10.42, 9.46); a reload.
- **Result:** `emergence point (10.42,9.46) is occupied now; he steps out at (11.42,9.46) instead, and nobody is moved`. Then `EMERGED at (11.42,9.46) … 15 ft … spent once as 1 use … (16 of 17 uses … left)`. Tessa stayed at (10.42, 9.46). Screen `174017-occupied`.
- **Unit test:** `TestEmergenceRechecksAPointChosenEarlier`, including "nowhere clear: he waits".

### C2: darkvision in Vandiana's darkness
- **Setup:** 976; the awakening accelerated offscreen; Vandiana's darkness INJECTED on her (`KWR_VandianaDarkness`); the labelled release.
- **The game's own concealment rule** (READ `concealment`, with the darkness patch), Krune against Vandiana, 17 ft, she in darkness:
  - with his permanent darkvision: **None** (sees in darkness True);
  - with an INJECTED Blur on her: **Partial** (unrelated concealment unaffected);
  - Blur removed: **None**;
  - his darkvision INJECTED removed (as a dispel would): **Partial** (sees in darkness False).
- **Control:** Vandiana against Ilsa (not in darkness): None.
- **Range:** the 60 ft limit is in the shared check; only 17 ft was exercised.

### C3: awakening during a live fight
- **Real-time battle opening** (0.8.10, again on 0.8.12):
  - **Setup:** 977; PLAYER Ctrl+A and a floor click into A4; PARTY AI fought all four living defenders; SETUP `krunedeadline 1`.
  - **0.8.12 result:** `EMERGED`, then in the same tick `opening: BattleBark` and `hostile (BattleUnderWay: the A4 fight was under way)`. The bark "Who dares brawl in my sanctum? Kneel, all of you!" appeared in the log. No modal dialogue. The checkpoint paused both sides, and game time stayed at 131042.0 across reads. Screens `181213-livebark` (0.8.10) and `191403-livebark812` (0.8.12).
- **Turn-based** (0.8.12; `tbm` SETUP; the emerald released by INJECTED `hitgem` during a live turn-based A4 fight):
  - **The hold:** `checkpoint holds turn-based combat: the turn of nobody () in round 1 is frozen as it stands`, at 131033.2. Game time stood, and the stagger kept its 5.4 s.
  - **After the labelled release:** the order resumed unchanged (Dorn, Brena, Marra, then round 2 with Jorgas). Krune was not in the initiative until he saw the party. No side got a free turn, and nothing depended on who won initiative.
  - **The stagger:** a rerun with Tessa SETUP-placed in his view. His first turn came with `staggered True`, then `the opening stagger ends with his first turn (turn-based)`.
  - **No renewal:** the combat-save fixture (997, `C3_TB_AFTER_STAGGER`) reloaded with `OpeningStaggerTurnServed: true` and `staggered False`.
  - One rerun ended in a native game over (Ilsa killed by Jorgas); unrelated to the new code.
- **Committed preparation** (0.8.10; 977 with `krunepause Emerged`, so the moment before the exchange is held):
  - **Negative (PLAYER):** Kael's spell cursor armed, hovered over Krune, cancelled by right-click. The parley opened and he was not hostile. Screens `175612-targeting`, `175615-cancelled`.
  - **Positive (HARNESS-COMMANDED):** Kael's own Haste ordered at the held moment, with Kael SETUP-placed in Krune's view. `before the exchange he sees: Tessa Quill []; Kael Ashen [UnitUseAbility started]`, then `hostile before the exchange: Kael Ashen was casting Haste as the exchange would begin`. No parley; the handoff and the checkpoint followed. Screen `180021-hostilecast`.
  - **Why the positive is harness-commanded:** a PLAYER click of a harmful spell on the still-neutral Krune issued no order. A Fireball aimed at the floor did not issue one either; the cause was not found.

### C4: the emerald broken by legal play
- **Why the weapons cannot, and what can:** hardness 20 applies to each resolved hit. Precision and critical damage do not apply, and Dorn's best hit stays at or below 20 even with Bull's Strength. The Strength DC 40 is out of reach. The five energies are halved.
  - The party's own scroll of Disintegrate (PLAYER, from Ilsa's belt) hit, but the emerald proxy saved: it is a fixture with Constitution 200, so +95 Fortitude.
  - The **printed numbers were not changed**.
- **Preparation:**
  - Brena's two regular level-6 slots were changed from Heal and Greater Dispel Magic to Blade Barrier in the spellbook screen (PLAYER).
  - Then SETUP `rest party:1`, the game's own per-character rest with no game time passed. It stands in for the rest before the dive: sleeping in the sanctum would run past Krune's 90-minute deadline. It also refreshed her other slots.
  - The defenders were INJECTED dead, so there was no A4 fight.
- **The break (PLAYER, from the action bar, at the emerald):**
  - **First attempt:** Blade Barrier 30 → 10 and 39 → 19 (11/40 left). Kael's Fireball 40 counted 20 → 0. The confrontation clock started by the first strike was about to run out, so the attempt was abandoned by reloading.
  - **Second attempt:** Blade Barrier 49 → 29, then 28 → 8 (3/40). The game was paused by the hourglass (PLAYER). Kael's empowered Fireball (his rod) 66 counted 33 → 13: **0/40, destroyed**.
  - **The awakening:** `EmeraldDestroyed` at 131090, inside the clock (131124). He emerged staggered, unobserved, and the checkpoint held at 131093.9.
- **The broken model by eye:** `185910-emerald-before-after` crops the altar before the break (a whole green gem) and after (a dark socket with green shards), both in normal light.
- **No re-awakening:**
  - SETUP save 998 (`C2_LEGALBREAK`), reloaded: one commit (131090), `CombatReady`, the stagger still owed with 6.0 s left, no new commit line.
  - A PLAYER click on the emerald gave only the info cursor.
  - INJECTED `hitgem` 60: `destroyed True`, same cause and time.
- **The fixed defect (0.8.13):** on 0.8.12 Disintegrate's saved 22 counted as 11, because the game files force and untyped magic damage as an energy type and the object rule halved every energy type. From 0.8.13 only fire, cold, electricity, acid and sonic are halved. Seen on 0.8.13: Disintegrate 17 counted 17; an empowered Fireball 66 counted 33. Unit test `TestOnlyTheFiveEnergiesAreHalvedAgainstStone`.
- **Left as found, for the owner:**
  - the proxy's +95 Fortitude makes a "Fortitude partial" spell always partial against the emerald (the module prints no saves for it);
  - the emerald keeps the name "Flawless emerald" after it breaks.

### C5: native dispel of Mage Armor, and a restart
- **Setup:** from 998 (the checkpoint).
  - HARNESS-COMMANDED `ai off` for all six.
  - An INJECTED native Paralyzed on Krune, meant to keep him still. It is not from a spell, so dispel skips it.
  - INJECTED `krunerelease`.
- **The dispel (HARNESS-COMMANDED, Kael's own Dispel Magic (Target), native rule):**
  - The first removed see invisibility.
  - The second removed Mage Armor: AC 35 → 31.
  - Runes of Power, calibration, fast healing and darkvision remained.
- **What went wrong in the run:**
  - His Ring of Freedom of Movement made the paralysis useless.
  - He walked through Brena's two Blade Barriers, still standing from C4, taking 152 damage, and attacked. Brena was killed (−18) and Kael brought to 3.
  - The party did not attack him. He was alive at 111/293.
- **Getting a save:** the game refuses to save in combat. An INJECTED native Domination buff changed his side and ended combat. Then SETUP save 999 (`C2_DISPELLED`).
- **The restart:** the game was closed and relaunched, and 999 loaded.
  - `MageArmorBuff` was absent, with AC 31.
  - Runes of Power, calibration, permanent darkvision and fast healing 3 were present.
  - `BaselineApplied: true`, and no new baseline line.
  - The Domination buff did not survive the load (the game dropped it; there is no fixture-protection line), and he was hostile (Mobs) again.

### C6 and C7: wording and the sheet claim
- "My sleep was to be a century." (0.8.8).
- The revival study now reads "… reads the rite in it. His soul was held apart from his preserved body. The ritual has reunited them." (0.8.13).
- The sheet is not claimed to match the book exactly: initiative 8 against the printed +4 and CMD 35 against 37 are documented adjudications (`WR-07A-AWAKENING.md`).
- No "fight him anyway" button, Krune victory, combat AI, artifact powers, new portrait or scenario repeat was added.

## Installation and restoration
- **Installed:** 0.8.13 (`Mods\KingmakerWakingRune`, DLL as above, verified after End).
- **Backed up:**
  - the install before the closeout (0.8.7) at `C:\Dev\KingmakerWakingRuneLab\runtime-backups\wr07a-c1\mod-existing`;
  - the one before WR-07A (0.7.22) at `…\wr07a-b1\mod-existing`.
- **Transactions:** `wr07a-traverse` ended with 0.7.22 restored. `wr07a-b1` and `wr07a-c1` ended with `-KeepInstalled`. All three restored the profile, the Steam remote mirror, the registry key and UMM Params, and verified Mods. Test saves were quarantined in the transactions' `quarantine` folders and collected into the evidence.
- **Stray file removed:** two blueprint catalogs the READ `catalog` command wrote into the game folder, by a relative path, were deleted at once. Later catalogs went to the scratch folder.
- **Owner data:** no owner save was opened. Seeds were copied into the profile only during transactions.
- **Nothing installed:** no software was installed and the Python installation was not touched. One mistaken `python -` invocation inside a shell command was stopped at once; it ran nothing.

## Not done or not proven
- **Not shown in play:**
  - a late emerald hit;
  - hostility from a PLAYER-issued cast before the exchange (the positive case was HARNESS-COMMANDED; see C3);
  - darkvision beyond 60 ft;
  - the revival study's success text on 0.8.13;
  - a fresh pre-departure (975) arrival through to the awakening.
- **Open for the owner:** the emerald proxy's fixture Fortitude, and its name after it breaks (C4).
- **Deferred by design:** see "Not part of this increment" in `WR-07A-AWAKENING.md`.
