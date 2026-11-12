# WR-07A acceptance: Krune's awakening

**Candidate:** 0.8.7, commit `39cee28`.
- DLL `b24a9de70ad553e3cd84d39ac77c70312aac8b6e546cbd8f4b3c7664da8260b7`
- Zip `artifacts\KingmakerWakingRune-0.8.7.zip`, `477a113f1adca40f495599e3ba4c51e1a83269ed183a4e8beafa847b55cfdb61`
- Branch `feature/wr-07a-awakening`, from `main` at `56b9e40` (the merge of PR #6).
- 212 unit tests pass: the 198 before, including the 128 rune-mask expectations, and 14 new ones in `KruneAwakeningTests`.

**Runs:**
- `wr07a-traverse` (the installed 0.7.22): the ordinary route.
- `wr07a-b1` (0.8.0–0.8.7, one transaction, reinstalled per build).

Evidence is under `C:\Dev\KingmakerWakingRuneLab\runtime-evidence\<run>`:
- screenshots;
- every harness command's transcript (`*-cmd.txt`);
- saves 986–994 and the seeds;
- `umm-log.txt` of the last launch.

**Lost evidence:** the game recreates its own log at each launch. The per-launch logs of 0.8.0–0.8.6 were not collected before relaunching, and are lost. Their facts survive in the command transcripts and screenshots, and in the save payloads where noted.

## Input labels
- **PLAYER:** real mouse or keyboard input only (portrait and world clicks, dialogue answers, the space key, Ctrl+A).
- **PARTY AI:** the game's own party automation in combat.
- **HARNESS-COMMANDED:** a native order dispatched by the harness: `move`, `spell` from the member's own book, `endturn`, `ai`, `equip`.
- **SETUP/INJECTED:**
  - `load`/`save`, `teleport`, `give`, `tbm`;
  - `krunedeadline` (an accelerated global deadline; the native tick still triggers it), `setmask` (foci set dark before a test), `krunepause` (a pause at a stage, for a save there);
  - `damage` on the A4 defenders, `damagekrune`, `hitgem` (one hit through the production object pipeline), `buff`/`unbuff`;
  - `combatsave` (the one-frame combat exit for a save), `krunerelease` (the diagnostic release of the checkpoint).
- **READ:** `krune`, `krunesheet` (the actor's native statistics), `krunecl` (the native ability-parameter rule for one of his spells), `manifest`, `party`, `turn`, `paused`.

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
| **0.8.7** | `39cee28` | `b24a9de70ad553e3` | — (installed) |

## Checks
| # | Check | Result | Build | Evidence and labels |
|---|---|---|---|---|
| A | **Ordinary traversal** A1 → A2 → A3 → east door → A4, no teleport | **PASS** | 0.7.22 | See below. |
| B | **Natural confrontation awakening** | **PASS** | 0.8.1 | See below. |
| C | **Long deadline, party outside A5** | **PASS** (accelerated setup) | 0.8.3, 0.8.5, 0.8.6, 0.8.7 | SETUP `krunedeadline` brought Kurshu's deadline within seconds; the native tick triggered it each time. The party was in A1 (976) or the east corridor (977, defenders INJECTED dead). There is no proximity gate. Each time he woke unobserved, and the notice revealed nothing. |
| D | **Emerald early release** | **PASS** (with injection), late hit **unit-only** | 0.8.5 | See below. |
| E | **Collision and presentation** | **PASS** | 0.8.2, 0.8.7 | Tessa SETUP-placed on the preferred point (10.42, 9.46): he emerged at (11.42, 9.46). Marra SETUP-placed beside the coffin (9.6, 10.7): no overlap, he stands outside the coffin, and nobody was moved (screens `151750-krunemodel`, `155945-brokengem`). Offscreen (B, C): only the generic notification. The camera was never moved. |
| F | **State inheritance** | **PASS** | 0.8.1–0.8.7 | See below. |
| G | **Feeder** | **PASS** | 0.8.6 | See below. |
| H | **Live rune link** | **PASS** | 0.8.6 | See below. |
| I | **Buff semantics** | **PARTIAL** | 0.8.2, 0.8.6 | See below. |
| J | **Persistence** | **PASS** | 0.8.2–0.8.7 | See below. |
| K | **Prototype boundary** | **PASS** (real time); turn-based arrival **NOT RUN** | 0.8.1, 0.8.2, 0.8.7 | See below. |
| L | **Isolation** | **PASS** | 0.8.6, 0.8.7 | See below. |
| — | **Parley by real input** | **PASS** | 0.8.2, 0.8.7 | The native dialogue, with Krune's portrait. 0.8.2: identify → "Thassilon is gone" → disbelief → "Earthfall…" → calculation → "We will not kneel" (screens `151637`–`151724`). 0.8.7: "Thassilon is gone" → disbelief → "Believe what you like. We have come to end you". Every answer was a PLAYER click. Both ended hostile. There is no persuasion route. |
| — | **Battle-bark opening** (A4 fight under way) | **NOT RUN** natively | — | In B, the five who entered A4 were dead before the deadline, so the party was out of combat and he woke unobserved. The bark path is code only. |
| — | **Hostility before the exchange** (an attack or a cast in the moment he emerges) | **NOT RUN** natively | — | The observer is code only. The window before the dialogue opens is one tick. |
| — | **Revival study** | **PASS** | 0.8.5 | Recorded once in the knowledge ledger (save 991: `Krune.Revival`, Arcana, result 36 vs 27). 0.8.6 found the legacy adoption skipped it; 0.8.7 runs it there (`RevivalStudyAttempted: true`). |
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
- **Not shown in play:**
  - The broken-emerald model was not visually confirmed at this zoom; its state is applied at destruction and at every load.
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
- **Not shown in play:** a native Dispel Magic actually removing Mage Armor from Krune. Kael's Dispel Magic was ordered in 0.8.2 during the diagnostic release, but no check against him appears in the captured screens before the party killed him.

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
- **NOT RUN:** the checkpoint arriving during turn-based play.

### L: isolation
- **Seed:** 979, the stock campaign (SpiderCave).
- **Result:**
  - no sanctum state was created by reading it;
  - the game was not paused;
  - a PLAYER click moved the party normally;
  - the `krune` report said "no sanctum state" on 0.8.7;
  - loading it from a held checkpoint left the stock game unpaused.

## Installation and restoration
- **Installed:** 0.8.7 (`Mods\KingmakerWakingRune`, DLL as above, verified after End).
- **Backed up:** the previously installed 0.7.22, at `C:\Dev\KingmakerWakingRuneLab\runtime-backups\wr07a-b1\mod-existing`.
- **Transactions:** `wr07a-traverse` ended with 0.7.22 restored. `wr07a-b1` ended with `-KeepInstalled`. Both restored the profile, the Steam remote mirror, the registry key and UMM Params, and verified Mods. Test saves were quarantined in the transactions' `quarantine` folders and collected into the evidence.
- **Owner data:** no owner save was opened. Seeds were copied into the profile only during transactions.
- **Nothing installed:** no software was installed and the Python installation was not touched. One mistaken `python -` invocation inside a shell command was stopped at once; it ran nothing.

## Not done or not proven
- **Not shown in play:**
  - a battle-bark opening during a live A4 fight;
  - hostility from a cast or attack before the dialogue opens;
  - a native dispel landing on Krune's Mage Armor;
  - a late emerald hit;
  - the checkpoint arriving in turn-based play;
  - the broken-emerald model checked by eye;
  - a fresh pre-departure (975) arrival through to the awakening.
- **Deferred by design:** see "Not part of this increment" in `WR-07A-AWAKENING.md`.
