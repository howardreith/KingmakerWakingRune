# WR-FINAL-DRAFT progress notes

Durable working notes for completing The Waking Rune as an end-to-end playtest candidate. Newest decisions last in each
section. Results with evidence labels go to the acceptance ledger, not here.

**Branch:** `feature/wr-final-draft`, from `main` at `c110f8b` (the merge of PR #7). Builds start at 0.9.0.

## Checkpoints
| # | Checkpoint | State |
|---|---|---|
| 1 | Emerald object rule; remaining-work ledger | code and runtime proof done (0.9.0, run `wrfinal-p1`) |
| 2 | Krune's real casting, resources and artifact powers | in progress |
| 3 | Tactical combat; the prototype stop removed | pending |
| 4 | Death, artifact recovery, world consequences | pending |
| 5 | Return portal, retreat, lodge debrief, terminal outcomes | pending |
| 6 | Usability, compatibility, the legitimate full journey | pending |
| 7 | One playtest candidate; owner review | pending |

## Checkpoint 1: object rules (0.9.0)
- `ObjectRules` (pure): the emerald is a CL 17 magical object, save +10 (2 + CL/2). The A2 leaf saves as a magical
  object (CL 17 feeder) only while its feeder is armed and not suppressed; otherwise it is unattended stone and never
  saves, a natural 20 included. Rune plates, statues and props are not covered (their own rules stand).
- `ObjectSavePatch` replaces the proxy's stat on the native save rule; the d20, DC and natural 1/20 stay native.
- Disintegrate (guid `4aa7942c…`, shared by its scrolls and wands) on the gem or the leaf: no save made → disintegrated
  outright; save made → the reduced damage, full (force/untyped), through hardness. One destruction, guarded by state.
- The broken emerald keeps its proxy identity, in the game, named "Shattered emerald" (CustomName) on every load; its
  break/attack choices were already hidden, and striking it no longer engages A4.
- Runtime (`wrfinal-p1`, SETUP controlled d20 `objectroll`): gem d20 1 + 10 vs DC 15 → disintegrated, awakening once;
  gem d20 20 + 10 → 10 damage, 0 past hardness 20; stock wolf: native death, no object line; armed door d20 20 + 10 →
  16 − 8 = 8; seed 998 (broken under 0.8.12) loads as "Shattered emerald", same commit.
- Not run natively: the door's never-save branch (unit-tested).
- Existing behaviour kept: a unit-implemented object can be targeted by spells the game lets target it; no targeting rule
  was widened.

## Checkpoint 2–3 so far (0.9.1–0.9.5, run `wrfinal-p2`)
- **Book** (`KruneBook` pure, `KruneSpellbook` native): one entry per printed slot; quickened/empowered as native
  `MetamagicData` custom spells in the raised slot; local spells; held slots (`KWR_KruneHeld_*`, never castable, with a
  reason). The book is sized to exactly the printed count per level (`KruneSlotsPatch`), no separate school slot (the
  game's school slot takes only plain conjuration spells from its list; none of his 8th-level entries qualify). Filled
  once (`KruneState.CombatInitialized`), after `UpdateAllSlotsSize(true)` (0.9.1 placed 45 of 65 without it). Seen:
  65/65, Mage Armor's slot spent.
- **Director** (`KruneTactics` pure, `KruneDirector` native, `KwrKruneDirectorAction` in his brain, score 20, then the
  native melee): ordered option lists for swift and standard; the first option he can really cast, aim and reach
  (`InRange`: half a metre inside the game's fixed range; Medium = 40 ft) is issued as `KwrDirectedCast` (not replaced by
  the brain for 6 s before it starts). A cast that did not spend its slot within 4 s is held back 2 rounds. Idle reasons
  and skipped options are logged. Yields when not Mobs, confused, frightened, cowering or unable to act.
- **Saved brain:** a saved unit keeps its old action list; `KruneCombat.EnsureBrain` brings it up to the blueprint on every
  load (0.9.1's director never ran on a saved Krune).
- **Prototype stop removed:** the handoff starts combat (`CombatStartedAt`); a WR-07A checkpoint save is retired once on
  load (`PrototypeRetired`, one notice, nothing reset); the pause and turn-based freeze patches are gone.
- **Local spells built** (`KruneLocalSpells`, `KruneLocalComponents`, `KruneWalls`, `KruneMoves`, `WallRules`): Greater
  Teleport (unlimited, legal sanctum floor), Phase Door (CL/2 passages, each a 40-ft step through stone), Fog Cloud,
  Solid Fog (speed −25, −2 melee), Black Tentacles (CMB CL+5 vs native CMD; FoM/CantMove immunity defeats it; native
  break-free vs DC 10+CMB; 1d6+4 crush), Globe (≤4th-level spells from outside: immune via the SR rule), Spell Turning
  (1d4+6 levels; single-target spells redirected at the cast rule; partial % rule), Hostile Juxtaposition (link on failed
  Will; once a round swap on an attack, the attack is lost — bounded), Wall of Stone (up to 8 sections, 60 hp, hardness 8,
  navmesh cut + fog blocker + stone mesh, one proxy per wall, never saves, disintegrate takes 2 sections, saved in
  `SanctumState.Walls`, rebuilt on load).
- **Seen in play (0.9.2–0.9.4, seed 996, parley by PLAYER, PARTY AI):** quickened tentacles, quickened walls (one raised),
  Foresight, Spell Turning (7 levels), quickened Summon Monster V and IV, Horrid Wilting. The party all had Freedom of
  Movement: the tentacles correctly held none.
- **Open in checkpoint 2–3:** artifacts (spear dancing/powers/telekinesis, rod metamagic), Wish via the bond and diamonds,
  boots of speed, pearl of power, ring of spell turning, acid dart/wand use seen, contingency, runesight, dominion; the
  relocation phase seen; turn-based fight; performance.

## Checkpoints 4–5 so far (0.9.6–0.9.7, run `wrfinal-p2`)
- **Conclusion** (`ConclusionRules` pure, `SanctumState.Conclusion`, on the main character's part, so it travels to the
  lodge): death, aftermath, loot, return and debrief are separate, once-only events; the outcome (Success,
  KruneSlainArtifactsMissing, Withdrawn, DiagnosticLegacy) is decided only at the debrief. Native flag `KWR_ScenarioOutcome`.
- **Aftermath** (`KruneAftermath`): the sarcophagus (now its own object, `SanctumDressing.Sarcophagus`) is replaced by
  rubble; the empty gem shows broken without setting `Gem.Destroyed`; foci show broken, lights off, plate proxies out of
  the game; the A5 glow and flash off; the mist stops exposing (`WardHazards.MistGone`) and its particles stay off
  (`SanctumMist.Dissipated`, reset per load). Body consumed 4 s after death. Re-applied on every load.
- **Loot** (`KruneLoot`): before the body goes, every lootable item he holds moves (same instances) into one native
  `DroppedLootBag` in the area's main state. Artifacts identified by their unique blueprints (items have no saved id).
  The artifacts cannot be dropped (`ArtifactDropPatch`).
- **Portal** (`PortalTransit`, the RefugeTransferFlow host): the pedestal offers "look through the matching shard" and
  "step through to the lodge (one way)" with no roll; warnings from `ConclusionRules.ReturnWarning`; refused in combat;
  verified save "Before the portal home"; `LoadArea(EnterPoint)`; return committed on lodge load.
- **Lodge** (`DebriefService`, `LodgeDialogs.Debrief*`): opens once when returned; success / partial / withdrawn
  exchanges; handover of exactly the artifacts (inventory and every character's slots) recorded before removal; the
  masked figure leaves (out of the game on every load); epilogue box. Token reuse is already refused once the sanctum
  was entered (`SanctumPolicy.CheckTravel`).
- **Seen (0.9.6, seed 996, INJECTED kill to reach the end quickly):** slain once; 9 items in the bag; rubble, no mist,
  glow gone; PLAYER: loot collected, pedestal, view, confirm; save verified; lodge restored (no setup replay); return
  committed (spear and rod carried, all six); success debrief; handover of both; outcome Success; a lodge save reloaded:
  no debrief replay.
- **Next:** gear and artifacts (boots of speed, pearl of power, ring of spell turning, Wish via the bonded spear and
  diamonds, the spear's dancing, lesser powers and telekinesis, the rod's metamagic and the wield/Ego check), then the
  withdrawal and defeat paths, the journal, supplies, compatibility, a legal victory, and the full legitimate journey.

## Engine inventory for Krune (catalog of 82,017 blueprints)
- **Native spells:** Foresight; Summon Monster IV–IX (Base + Single variants); Horrid Wilting; Dimension Door
  (`DimensionDoorCasterOnly`); Cloudkill; Dispel Magic (Target/Point) and Greater (Target/Area); Acid Fog; Glitterdust;
  True Seeing; Grease; Waves of Fatigue; Fear; Stoneskin; Protection from Energy; Sleet Storm; Stinking Cloud; Acid
  Arrow; Blindness; Web; Mage Armor; Obscuring Mist; Shield (`MageShield`); True Strike; Acid Splash; Ray of
  Enfeeblement; Slow; conjuration Acid Dart (`ConjurationAcidDartAbility`).
- **Not in the game:** Wall of Stone, Black Tentacles, Greater Teleport, Phase Door, Globe of Invulnerability, Spell
  Turning, Solid Fog, Fog Cloud, Hostile Juxtaposition, Planar Binding, Mage's Faithful Hound (WR-04 built a ward
  version), Explosive Runes, Detect Thoughts, Erase, Unseen Servant, Fly (no flight), Wish, Contingency, Telekinesis,
  dancing weapons, boots of speed, pearl of power, ring of spell turning, Dimensional Anchor/Lock.
- **Native metamagic:** custom spells in a book (`Spellbook.AddCustomSpell`, `MetamagicData`); native rods (Empower,
  Extend, Maximize, Quicken, Reach) via `MetamagicRodMechanics`.
- **AI integration:** a `BlueprintAiAction` subclass (as A4's `KwrAiDrinkPotion`) lets the native brain decide when he
  can act, in both modes, while the action supplies a native command.

## Remaining-work ledger (classification)
**Playthrough blockers** (must be fixed before "complete"):
- the WR-07A checkpoint stops the game at the handoff;
- Krune has only an interim melee brain and no spells;
- no death aftermath, recoverable artifacts or persistent outcome;
- no return route; no lodge debrief; no ending;
- no objective journal.

**Integration boundaries to test in this mission:** summons and mist resistance; rune effects on his real output;
contingency; control effects through save/reload; artifacts held, dancing, dropped and recovered; death and hazard
teardown; portal travel and handover; retreat, defeat, completion reload; a second new game; a stock campaign after.

**Accepted bounded adaptations carried forward:** hostile-NPC nonlethal limit; Kurshu's deferred Plane Shift; the A4
spell omissions (Wind Wall, Silent Image, …); the borrowed Krune body and cropped portrait.

**Optional, outside the critical path:** faction missions; chronicle boons and unlocks; summoning secrets; new art.
