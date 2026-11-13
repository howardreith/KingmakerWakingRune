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
