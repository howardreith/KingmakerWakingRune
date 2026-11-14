# WR-PLAYTEST-01: owner playtest repairs and preparation improvements

Branch `feature/wr-playtest-01` from `main` at `dc5de41` (PR #8 merged). Candidate **0.10.0**.

| | |
|---|---|
| Code commit | `77d68a7` |
| Package | `artifacts\KingmakerWakingRune-0.10.0.zip`, SHA-256 `d6fc231b2699de025f91b0087bedb90971ce9d0e594369b8a4f25403ae9d2def` |
| DLL | `9784da6fddba635c3f0b8d47d953a2f0f07e41eb84b7e926912f9297659ef5d2` (portraits and token icon embedded; the package holds only `Info.json` and the DLL) |
| Unit tests | 242 pass |
| Game, loader | Pathfinder: Kingmaker 2.1.7b, Unity Mod Manager 0.28.2 |
| Optional provider tested | Mr. Bard's Tabletop Rules Expansion (UMM id `KingmakerGunslinger`) **0.0.188**, release zip `KingmakerGunslinger-0.0.188-umm-0311-minimum.zip` (SHA-256 `64d83955…d412`, matches its `SHA256SUMS.txt`); no `FeatureModules.json`, so every module on (its default) |
| Runtime run | guarded transaction `wrplay-p1` (evidence `runtime-evidence\wrplay-p1`) |

Labels as before: **PLAYER** (real input), **PARTY AI**, **HARNESS-COMMANDED** (an order or callback issued by the
harness, not UI input), **SETUP/INJECTED** (a fixture: a granted spell or item, a forced roll, a position).

## Owner notes and results

| Owner note | Cause found | Result | Evidence |
|---|---|---|---|
| Missing lodge portraits | Not reproduced on the lab install, which loaded them from the loose `Portraits` folder. The released zip wrote its entries with backslashes (Windows PowerShell 5.1 `Compress-Archive`), which some extractors turn into flat file names, losing the folder the mod read. | **Fixed.** The six approved portrait sets and the token icon are embedded in the DLL; the build refuses a package with any of the 19 images missing or undecodable; zip entries use forward slashes. Portrait GUIDs unchanged; saved NPCs take the blueprint portrait, so existing saves need no repair. | Clean install of the candidate (only the DLL): Sheila, Ambrus, the masked figure, Quartermaster and Coordinator seen in their dialogues (PLAYER clicks), `*-portrait-*.png`, `*fu-*.png`, `*qm-dialog*` |
| Nod cue shows the wrong or no portrait | The cue had `NoSpeaker` and no portrait. | **Fixed** with the native `SpeakerPortrait`: the masked figure's portrait and name, the text in the narrator's colour, still nobody speaking; the debrief handover gets the same treatment. | Code; **not seen in game** (the briefing plays only in a new game, which was not created in this run) |
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
companion, wounds healed, no clock advance; then the unchanged checkpoint and transfer. A saved marker makes it once per
committed departure (a retry after a failed transfer, or a reload of the checkpoint, departs without a second refresh);
an older two-use record is dropped without dialogues. Seen (HARNESS-COMMANDED token callback on the old lodge seed): the
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

## Not run in this pass (honestly open)
- A new game: the 500,000 grant on a fresh start and the nod cue on screen.
- The Reliable firearm bought at the Quartermaster, loaded, reloaded and fired by PLAYER input (provider stock verified
  in game only).
- In-game effects of the phylacteries, mixed belts and skill items (chosen from their enchantment components); learning
  and casting representative scrolls, including 6th level and a variant-selection wrapper.
- The token through the belt and inventory UI (the ability's own callback was used), a failed transfer and retry
  (covered by the record policy's unit tests), and readiness for a newly levelled caster.
- Alacrity in turn-based combat (real time shown, success and interruption); unmarked runner and control-change runs.

## Installation and restoration
Transaction `wrplay-p1` ended (02:45): profile 23 differences reverted (test saves quarantined in
`runtime-backups\wrplay-p1\quarantine`), Steam mirror 6 reverted, registry restored, `Params.xml` and Mods verified. The
provider was installed only inside the transaction and removed before it ended. Candidate 0.10.0 installed (DLL
verified); 0.9.30 backed up in `runtime-backups\wrplay-p1\mod-existing`. No quicksave or quickload was used; test saves
were named harness saves.
