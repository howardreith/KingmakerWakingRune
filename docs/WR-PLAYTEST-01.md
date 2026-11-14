# WR-PLAYTEST-01: owner playtest repairs and preparation improvements

Branch `feature/wr-playtest-01` from `main` at `dc5de41` (PR #8 merged). Candidate **0.10.13** (after the owner's review
of 0.10.0; see "Review pass").

| | |
|---|---|
| Code commit | `53a2cdd` |
| Package | `artifacts\KingmakerWakingRune-0.10.13.zip`, SHA-256 `f7639a7b52d9a45f6f160c6b13e0ccc1de6e4eb4f7b63badbe38d8078f065834` |
| DLL | `9cead25f4280e96074257c9ad60f59f77bab14cd7046f1449684da5a89e26460` (portraits and token icon embedded; the package holds only `Info.json` and the DLL) |
| Unit tests | 249 pass |
| Game, loader | Pathfinder: Kingmaker 2.1.7b, Unity Mod Manager 0.28.2 |
| Optional provider tested | Mr. Bard's Tabletop Rules Expansion (UMM id `KingmakerGunslinger`) **0.0.188**, release zip `KingmakerGunslinger-0.0.188-umm-0311-minimum.zip` (SHA-256 `64d83955…d412`, matches its `SHA256SUMS.txt`); no `FeatureModules.json`, so every module on (its default) |
| Runtime runs | `wrplay-p1` (0.10.0), `wrplay-p2` (0.10.1-0.10.11), `wrplay-p3` (0.10.12-0.10.13), each a guarded transaction (evidence `runtime-evidence\<run>`) |

Labels as before: **PLAYER** (real input), **PARTY AI**, **HARNESS-COMMANDED** (an order or callback issued by the
harness, not UI input), **SETUP/INJECTED** (a fixture: a granted spell or item, a forced roll, a position).

## Review pass (0.10.1 to 0.10.13)

The owner review of PR #9 (at `7812e30`) asked for one fix (a departure record that could send a changed party in
unprepared) and the first real-use tests of the new features. Both are done below; six further defects turned up in
those tests and are fixed. Runs `wrplay-p2` (0.10.1-0.10.11) and `wrplay-p3` (0.10.12-0.10.13); every step names the
build it ran on. Diagnostic builds (marked `-diag`) were used only to read state, never as candidates.

### Defects found and fixed in this pass

| Build | Defect | Fix |
|---|---|---|
| 0.10.1 | **Review finding:** the departure marker (`ready:<time>`) meant "depart as is" on any later use, so a failed transfer or a checkpoint reload followed by a changed preparation, a recruit or a level-up left them unprepared. | The record lists each readied member and each prepared choice. A retry or reload reconciles against it: unchanged departs as it stands; a new member is readied in full; a slot prepared with a different spell, a slot a level-up added and spontaneous spells a level-up added are made ready; nothing else is refreshed. Unit-tested (`DepartureRecordTests`, 9 tests). |
| 0.10.2 | Found in play: a slot prepared again **with the same spell** (the native spellbook leaves it unready until a rest) looked unchanged. | The native `Spellbook.Memorize` is observed while a departure is on record; a slot prepared since is readied at the next use. |
| 0.10.3 | Found in play: a real-time reporter out of sight stood at the closed east door for good. Its hands guard (a weapon being stowed) stayed set while unseen, and the native controller starts no command that waits for hands. | A fixture action out of the player's sight does not wait for the hands. |
| 0.10.4 | Found in play: an unmarked reporter that turned on an intruder at the latched A2 never resumed its flight once the door opened. | When A2 becomes passable it breaks off and flees on. |
| 0.10.5 | Found in play: a reporter **seen** in real-time combat could not pass the open A2 door: the native combat pathfinder routes around standing units, and the door's invisible proxy stands in the doorway. | Ward fixtures (invisible proxies) are not obstacles to unit avoidance. |
| 0.10.6 | Found in play: ward fixtures out of the game (alarm, glyph, hound, a destroyed door or gem) were recreated at every load. | Fixtures are indexed from every unit the save holds; earlier surplus copies are kept out of the game (one line in the log). |
| 0.10.7-0.10.8 | Found in play: a wrapper scroll of a spell **with variants** (Resist Energy, Dispel Magic, the summonings) went straight to targeting from a belt or the inventory: an item's button has no variant choice. | Such spells are stocked as the game stocks them, one scroll per variant (native where one exists, now 113 native variant scrolls; a wrapper on the variant otherwise), each learned as the parent spell. The earlier parent wrappers stay registered so saves holding one load. Only 9 cantrip wrappers remain (no provider). |
| 0.10.10 | The scenario description still said the runelord's chamber was "not yet playable". | Text corrected. |
| 0.10.11-0.10.13 | Found in play: a part-spent wand sold to the Quartermaster merged into the merchant's full stack (the native vendor table stacks everything), and after a save the native table keeps only blueprint and count, so it came back full. | On the Quartermaster's shelf only, charged items merge only with equal charges; their charges are saved with the game and given back when the shelf is rebuilt from a save. |

### Closeout table (the review's areas)

| Area | Required closeout | Result | Label and evidence (`runtime-evidence\wrplay-p2`, `\wrplay-p3`) |
|---|---|---|---|
| Departure | Changed-party/preparation fix | **Done** (0.10.1, 0.10.2), see above | unit tests; logs below |
| | Inventory route | **Passed** (0.10.1): token right-click → Use readied the party (legacy record migrated) | PLAYER clicks; `093509-dep-inv-rclick`, `093534-dep-inv-use-b` |
| | Belt route | **Passed** (0.10.1 and 0.10.2): token in the belt panel | PLAYER clicks; `093619-dep-belt-b`, `095054-dep2-belt` |
| | Failure and unchanged retry | **Passed** (0.10.1): save-failed fault, back in the lodge, token kept; a slot Orin spent afterwards stayed spent on the retry ("already readied and is unchanged") | fault INJECTED; PLAYER retry; output log `logs-0.10.1` |
| | Changed preparation after a failure | **Passed** (0.10.1): Magic Missile → Enlarge Person through the spellbook (PLAYER) was readied: "Orin: 1 newly prepared slot (Enlarge Person)"; ready on arrival | `093737-dep-book-drag` |
| | Newly levelled caster | **Passed:** Sela (bard 12, spontaneous) "2 spontaneous spells gained by level" (0.10.1), 7/7 and 4/4 on arrival; Maren (druid 12, prepared) her new 6th-level slot "Dispel Magic, Greater" readied (0.10.2) | experience SETUP; level-up PLAYER (`093843-dep-lvl2`); new slot filled HARNESS-COMMANDED (native Memorize) |
| | New recruit | **Passed** (0.10.2): Ned (cleric 11) recruited at the Coordinator and swapped in through party selection, then "Ned readied" | PLAYER (`094343-legit-r7-done`, `094455-dep-partysel4`); his spells chosen HARNESS-COMMANDED |
| | Same changes after loading the pre-departure checkpoint | **Passed** (0.10.2): from `Before the Sanctum of Sloth`: "Maren: 1 newly prepared slot (Dispel Magic, Greater); Orin: 1 newly prepared slot (Haste); Ned readied"; Orin's earlier-spent Mage Shield stayed spent | PLAYER belt route; the Haste was prepared again by PLAYER |
| | Sanctum load | **Passed:** loading sanctum saves runs no readiness (no departure lines; record none) | READ |
| Fresh start and portraits | 500,000 gp and the nod cue from the packaged build | **Passed** (0.10.9, new game, pregen): "Party treasury initialized once: 0 -> 500000 gp", 500000 in the inventory; the nod cue shows the masked figure's portrait and name ("The masked figure listens and nods silently."); Ambrus and Sheila portraits in the briefing | PLAYER; `120958-ng-gold`, `120700-ng-brief-1` (nod), `120710-ng-brief-3` |
| Shop | Ordinary generated scroll | **Passed** (0.10.6): Scroll of Resistance bought, used from the inventory on Orin: ResistanceBuff, scroll gone | PLAYER |
| | 6th-level scroll | **Passed** (0.10.8): Scroll of Summon Huge Earth Elemental bought, copied (Orin's book), prepared, rested (the Quartermaster's native rest), cast through the spellbook's variant choice: a Summoned Huge Earth Elemental appeared, slot spent | PLAYER; `113959-huge-cast` |
| | Variant-selection scroll | **Defect found** (0.10.6, see table); after the fix (0.10.8): Scroll of Dispel Magic (Target) learned as Dispel Magic, prepared, cast through the variant choice on Orin: his Mage Armor dispelled, slot spent. A variant scroll cast from the belt applied its buff (Protection from Fire) | PLAYER; `114027-dm-variants` |
| | Mixed belt and skill items | **Passed** (0.10.8): Belt of Physical Might +2 on Bryn: Strength shows "Enhancement +2 [Belt of Physical Might +2]", Constitution raised; Trapspringer's Gloves: Trickery 8 = Dex 3 + "Competence +5 [Trapspringer's Gloves]" | PLAYER; `120203-bryn-str`, `115137-bryn-trickery` |
| | Channeling phylacteries | **Passed for the Lesser Phylactery of Positive Channeling** (0.10.9): Yadmila's Channel Energy caster level 11 → 13, rank bonus +2, dice 6 → 7 (native parameters); channels healed 19 and 29 (single rolls). Not exercised: the normal phylactery and the negative ones | PLAYER channel; parameters READ (`params`) |
| | Reliable firearm | **Passed** (0.10.10, provider 0.0.188): Reliable Pistol +1 and 10 paper cartridges bought; a character without firearm proficiency cannot equip it (native "Can't be equipped"); Bryn took a Gunslinger level and equipped it; Use Paper Cartridges; Reload Firearm loaded one (cartridges 10 → 9; powder and ball untouched); fired at a wolf: hit for 4 (misfire range 1); reloaded in combat; fired again: hit for 3 | PLAYER; experience SETUP; wolf SETUP; provider log `ability.loaded`, `attack.round-consumed`; `122535-gun-fire.mp4` |
| | Charged-item buyback | **Defect found and fixed** (0.10.11-0.10.13); on 0.10.13: a 29-charge Wand of Cure Light Wounds sold (108 gp) with 24 retail wands on the shelf stays its own entry (×1, 29 charges, 435 gp) beside the retail ×24 (31 charges, 465 gp), and is still that after a save and load | PLAYER; `wrplay-p3\125346-p3-sold`, `125933-p3b-reload`, `125937-p3b-r2` |
| | Gloves of Dexterity on a visited shop | **Passed** (0.10.6): the retail line is gone from the old save's shop; a pair given to the party (SETUP) stayed owned, sold for 1,000 gp, and stayed on the shelf through a save, load and resupply | PLAYER sale; READ stock |
| Cultists | Uninterrupted real-time flight | **Passed after fixes** (0.10.3, 0.10.5): party and pet AI off (the portrait setting, HARNESS-COMMANDED), the marked reporter crossed A2 and the east door and reported to Vandiana, also with Orin in sight of A2. Hitches: one cluster of 150-210 ms frames around the A2 opening and the combat start (the owner's auto-pause pauses once at combat start, as set) | `101937-runner-rt-n1-fixed.mp4`, `105828-runner-seen-a2.mp4` |
| | Unmarked reporter at a latched door | **Passed after fix** (0.10.4): N3 waited at the latched A2 (turning on the nearest intruder); when a marked runner opened it, N3 broke off and fled on to Vandiana | `102252-unmarked-n3.mp4` |
| | Controlled reporter | **Passed:** under Orin's Dominate Person the reporter stood with no commands of its own ("under other control; its flight is suspended"); when it threw the domination off it resumed its route | spell SETUP grant, cast HARNESS-COMMANDED (native save); `110119-controlled-n1-dominate3.mp4` |
| | Mid-route restart | **Passed:** saved after leg 3 (past A2), loaded: the runner continued from leg 4 (east door, report) with no A2 action or alarm repeated | SETUP named save |
| Runes | Turn-based Alacrity | **Passed:** Hold Monster at round 1; disabled during round 6 ("Paralyzed observed on for 5.0 rounds"); "The rune plate goes dark. Its link to Krune is broken." (unidentified, so unnamed) | spell SETUP, cast HARNESS-COMMANDED; End Turn PLAYER; `100455-tb-alac-dark` |
| | Real-ally and outside-sanctum controls | **Passed:** in the sanctum, Bryn in Controlled Fireball's area took nothing (the spell selects enemies only, natively) while the plate took Fire 33; in the lodge, centred on Bryn: no damage and no rune-targeting line | spell SETUP, cast HARNESS-COMMANDED |
| | Rear-facing disabled plate | **Passed:** A3b (Alacrity), back to the camera under vines: hover "Thassilonian rune plate (disabled)"; inspection opens "This rune is disabled. Its link to Krune has been broken." | PLAYER hover and click; `095507-rear-A3b-hover2`, `095537-rear-A3b-inspect` |

### Observed, not changed
- **With the provider**, its summoning module turns each Summon Monster spell into one variant per creature, and a
  scroll is stocked for each: 884 lines instead of 566 (`docs\WR-PLAYTEST-01-STOCK.md`).
- **Firearms need firearm proficiency** (the provider's rule): an ordinary party cannot use the shop's guns without a
  Gunslinger level or the proficiency feat.
- **Native errors seen**, none from this mod's code: an obstacle-group exception while an area unloads (also in
  `wrfinal-p5`, `wrfinal-p6`, `wrplay-p1`), a bug-report canvas error at start-up, and an inventory doll error at game
  exit.

### Not run in this pass
- The normal and negative channeling phylacteries (the lesser positive one was).
- Turn-based reporter flight was not re-run here (it was in `wrplay-p1`); the reporter fixes of this pass were
  exercised in real time.
- A complete journey or a fresh six-character party (not requested).

## Owner notes and results (first pass, 0.10.0)

Rows updated by the review pass say so.


| Owner note | Cause found | Result | Evidence |
|---|---|---|---|
| Missing lodge portraits | Not reproduced on the lab install, which loaded them from the loose `Portraits` folder. The released zip wrote its entries with backslashes (Windows PowerShell 5.1 `Compress-Archive`), which some extractors turn into flat file names, losing the folder the mod read. | **Fixed.** The six approved portrait sets and the token icon are embedded in the DLL; the build refuses a package with any of the 19 images missing or undecodable; zip entries use forward slashes. Portrait GUIDs unchanged; saved NPCs take the blueprint portrait, so existing saves need no repair. | Clean install of the candidate (only the DLL): Sheila, Ambrus, the masked figure, Quartermaster and Coordinator seen in their dialogues (PLAYER clicks), `*-portrait-*.png`, `*fu-*.png`, `*qm-dialog*` |
| Nod cue shows the wrong or no portrait | The cue had `NoSpeaker` and no portrait. | **Fixed** with the native `SpeakerPortrait`: the masked figure's portrait and name, the text in the narrator's colour, still nobody speaking; the debrief handover gets the same treatment. | **Seen** in a new game in the review pass (`120700-ng-brief-1`) |
| Shop coverage and quantities | Small counts (1-6), gaps found by effect audit (no Trickery item, few skill items), no channel phylacteries or mixed belts. | **Done.** See "Stock". | `docs/WR-PLAYTEST-01-STOCK.md`, shop screenshots |
| 500,000 gp | | **Done.** New games 500,000 once; a pre-departure lodge save that had 450,000 gets +50,000 once. | Old lodge seed: "910 -> 50910 gp (+50000)", once; completed-debrief seed: no credit |
| Excessive departure prompts | | **Replaced** by one use: readiness, checkpoint, transfer. | See "Departure" |
| Restored cultist died at once and briefly started combat | The body was spawned hostile, then rolled its survival save, then was marked for death: a hostile living actor existed between the two. | **Fixed.** The save is rolled while the body is neutral and inert; only a survivor turns hostile. A failure leaves one corpse and says why. | See "Restoration" |
| Fleeing cultist: repeated pauses and hitches | In turn-based play, the runner's door action, when its turn could no longer afford it, was ordered again on every tick (nine times in one turn) and logged each time. The synchronous path wait measured 0-4 ms (not a stall source). Each combat start with the owner's auto-pause pauses, as set; combat ended once when the runner left sight. | **Fixed:** one door order per turn (a turn that cannot make it ends), real-time re-orders at most once a second of game time, legs end only on observed progress. Pause settings untouched. | See "Runner" |
| A runner stopped after opening a door | Each door leg was marked complete on the request, not when the door opened. Every real-time stop observed in this run turned out to be a runner killed by the party (PARTY AI, including ranged attacks). | **Fixed** (legs advance only when the door is seen open); full route through both doors to Vandiana shown. | See "Runner" |
| Alacrity's shutdown hard to notice | No progress line; the disable line named the rune even when unidentified. | **Done.** "The rune's motion falters. Keep it bound." as a hold begins; a line if it slips; one notice, a flare and a sound on success, naming the rune only once identified. | Log and screenshots `alacrity-*` |
| Maximized Controlled Fireball did not disable the fire-vulnerable rune | Controlled Fireball selects only **enemies** (`AbilityTargetsAround m_TargetType=Enemy`, then `ContextConditionIsEnemy` full or minimum damage). The plate is in a peaceful fixture faction, so it was never a target: nothing was delivered, and the attempt still counted as a failure. | **Fixed** by a scoped adaptation (see below). | See "Controlled Fireball" |
| Rear-facing disabled runes hard to recognize | The only change was on the plate's face. | **Done.** Hover name "(disabled)" (refreshes at once), pale ash at the plate, the light out, and an inspection that opens "This rune is disabled. Its link to Krune has been broken." | `cand-A3e`, `cand-A3g`, `rune-A3a-hover` |

## Stock
- **Quantities:** at least 12 of every equipment line, 99 of every potion and scroll, 24 of every wand, 999 diamond dust;
  provider ammunition 999. Normal prices; no blanket price ceiling.
- **Additions:** Phylacteries of Positive and Negative Channeling (normal and lesser), the +2 mixed belts (Physical
  Might, Flow, Form) and Physical Perfection +2 (all under 20,000 gp), Headband of Mental Perfection +2, skill equipment
  chosen by actual effect (Trickery: Trapspringer's Gloves, Rogue's Companion; also Athletics, Stealth, Knowledge, Lore,
  Perception, Persuasion and Use Magic Device items), protection to +5, Boots of the Light Step, Bracers of Archery,
  lesser and normal metamagic rods (Maximize included; greater rods and the normal Quicken rod stay out).
- **Wizard scrolls:** every Wizard spell of levels 0-6 in the installed game, read from the Wizard spell list itself (so
  another enabled mod's spells are included). A native scroll is used where one exists; otherwise a wrapper on the working
  spell (37 in the base game). The audit fails on any spell without a scroll and without a recorded reason: none.
- **Gloves of Dexterity** are no longer on the shelf. For a visited shop, the native catalogue difference
  (decompiled `SharedVendorTables.GetLootDifference`) removes the retail count; owned or sold copies are untouched. That
  removal was not inspected separately in game.
- **Resupply:** when the shop opens, a line below its catalogue count is topped up to it with new merchant stock;
  nothing above it changes, sold goods are never duplicated, and selling still pays less than buying. One pass per
  opening. Seen: the old seed's first opening topped up 39 lines (130 items) its earlier purchases had emptied; buying all
  12 Bastard Swords +1 cost 28,800 gp (PLAYER).
- **Provider (optional):** detected through UMM (installed and enabled) and its own `FeatureModules.json`, items found by
  their published GUIDs, no assembly reference. Pistol, Musket and Blunderbuss (base, +1 to +3, Reliable +1 to +3),
  powder, ball, paper cartridges and the Gunsmith's Kit; Wakizashi, Katana, Nodachi and the Elven Branched Spear (base,
  masterwork, cold iron, +1 to +3). Named and campaign-loot weapons are left out. Seen: 51 lines with the provider, none
  and no errors without it. **Dependency:** once the shop has been opened with the provider, the save's vendor table refers
  to its items, so that save should not be loaded with the provider removed.

## Departure
One use of the token: the native per-unit rest (`RestController.ApplyRest`) on every living member and animal
companion, wounds healed, no clock advance; then the unchanged checkpoint and transfer. A saved record makes it once per
committed departure; since the review pass it records each member and prepared choice, so a retry after a failed
transfer, or a reload of the checkpoint, readies only what changed since (see "Review pass"). An older two-use record is
dropped without dialogues. Seen (HARNESS-COMMANDED token callback on the old lodge seed): the
legacy record migrated; Maren's spent 6th-level slot and Orin's spent Mage Armor and two False Life slots ready again;
Orin's Stoneskin, Mage Armor, False Life, Barkskin and resistances kept; arrival committed with the whole party.

## Restoration
Failed survival (SETUP forced, Support-N2): the Fortitude save in the log, "Flesh returns, but the cultist cannot
survive the transformation and collapses.", one corpse, **no combat start**. Surviving attacker (forced pass): turns
hostile, combat starts and the party kills it (PARTY AI). Surviving reporter (forced pass): flees. A3 husk (A3c-L):
"long dead; crumbling begins". The forced outcome is a harness fixture only; the roll is still made and logged.

## Runner
- **Before (diag build, turn-based):** "open A2 with its own brand" ordered nine times in one turn.
- **After:** two orders (one per turn: the first turn ends cleanly, the next delivers); A2 opened with its own brand,
  then the east door, and the report reached Vandiana.
- **Real time:** that run's legs 4-7 (east door, report) ran in real time after combat ended, without stalls.
- **Not shown:** every real-time attempt to watch the A2 crossing ended with the runner killed by the party (PARTY AI,
  ranged too), which is the intended interception. Unmarked runners, control changes and a mid-route reload were not
  re-run in this pass.

## Controlled Fireball
Scoped adaptation (`Patches\RuneTargetingPatches.cs`): in the sanctum, for a damaging ability cast by the party's side,
the seven rune plates count as enemies in the enemy-only area selection and in the "is enemy" choice of full or minimum
damage. Real allies, other creatures, other spells and other campaigns are untouched. Damage, saves, hardness 8 and the
35-point window are unchanged and counted once by the existing observer. Native demonstration on the active Armor plate
(A3e), each from the same fixture save:
- **Fireball** (SETUP grant): Fire 35, 27 after hardness. That fails the window legitimately, and the curse save follows.
- **Controlled Fireball, before:** "A spell delivered at the focus changed nothing", counted as a failure.
- **Controlled Fireball, after:** Fire 34, 26 after hardness, the plate now included. That also fails legitimately.
- **Maximized Controlled Fireball, after:** the native Maximize Metamagic Rod (SETUP item and spell, HARNESS-COMMANDED
  rod toggle and cast from Orin's book) dealt Fire 60, 52/35 after hardness, and Armor was disabled. Shown again on the
  final candidate.

## Not run in the first pass
Everything listed here in the first pass was run in the review pass (see its closeout table), apart from the items under
"Not run in this pass" there.

## Installation and restoration
Transaction `wrplay-p1` ended (02:45): profile 23 differences reverted (test saves quarantined in
`runtime-backups\wrplay-p1\quarantine`), Steam mirror 6 reverted, registry restored, `Params.xml` and Mods verified. The
provider was installed only inside the transaction and removed before it ended. Candidate 0.10.0 installed (DLL
verified); 0.9.30 backed up in `runtime-backups\wrplay-p1\mod-existing`. No quicksave or quickload was used; test saves
were named harness saves.
