# WR-07A: Krune's awakening

This increment makes Krune wake up. It covers his real actor, his entrance and opening, and the handover of the sanctum's state to him. It stops at a labelled development checkpoint before his combat encounter. **The full Krune fight, his tactics, his artifacts' powers, the return portal and the finale are not part of it.**

Profile: Subtier 10–11, six PCs, extra challenge on. The player's own difficulty preset applies on top and is not compensated.

**Code:** `src/KingmakerWakingRune/Sanctum/Krune/`.
- `KruneRules.cs`, `KruneManifest.cs`: pure, unit-tested (linked into the tests).
- `KruneBlueprints.cs`: the actor, his facts, items and resources.
- `KruneComponents.cs`: the rune links, the revival caster-level loss, the spear's competence, Control's ward.
- `KruneDialog.cs`: the opening exchange.
- `KruneAwakening.cs`: the lifecycle at play, the opening, the checkpoint, the legacy choice.

## Sources used
| Printed page | What it gives | Where it lands |
|---|---|---|
| 8–9 | Kurshu's defeat starts 90 minutes | the existing `RitualRules` global deadline (unchanged) |
| 9 | feeder temporary HP; a grant that expires naturally speeds awakening by a round | the existing `FeederRules` ledger (unchanged); handed to the real Krune |
| 11 | A4's 1d6+6 rounds; the scouting exception | the existing confrontation clock (unchanged) |
| 13–14 | the hanging sarcophagus, mist, soul return, dimensional steps out of the coffin, demands, disbelief then calculation, hostility on battle preparation; breaking the gem early: awake at once, standing, staggered 1 round; the revival study (Arcana or Spellcraft DC 27) | the lifecycle, the opening, the stagger |
| 14 | extra challenge: rune sabotage expends no spells; wish known; 75,000 gp of diamonds; one quickened summon monster V becomes summon monster IX | the manifest |
| 15–16 | the dragon-tooth spear (+3 longspear, major artifact) and the rune-carved rod (minor artifact, 10 charges) | items with stable identities; powers deferred |
| 16–17 | the stat block, prepared spells, gear, special abilities | the actor and the manifest |
| 18 | the rune benefits and the revival's caster-level loss for 24 hours | `KruneProjection` (unchanged) and the revival snapshot |
| cover, 16 | his appearance | donor body and a portrait cropped from the page-16 art |

## The awakening contract
### Trigger
The existing `RitualState` decides when: `AwakeningDue`, `Reason`, `AwakeningAt`. There is no second clock.
- **Global deadline:** 90 minutes from Kurshu's defeat.
- **Confrontation deadline:** A4's stored 1d6+6 rounds, one round sooner once if a feeder grant expired naturally.
- **The earlier one wins.** Vandiana's death does not cancel it, and opening the east door alone does not start the A4 clock. Nothing is rerolled on re-entering combat. Pause, dialogue and loading advance nothing, as before.
- **The emerald** (`RitualRules.RequestImmediateAwakening`) releases him early, with the opening stagger, only while nothing is due yet.
  - **Late emerald:** once awakening is due, breaking or hitting the emerald changes nothing (no new cause, no stagger). Before this build, the stagger flag was set before that question was asked.
  - **Same boundary:** a deadline reached at or before the moment the emerald breaks came first. The deadline is the cause and there is no stagger.

### Lifecycle (saved as `SanctumState.Krune`, state version 6)
| Stage | Means | Recorded |
|---|---|---|
| Dormant | not due; the hidden receiver stands in | — |
| Committed | due and committed once | cause, effective time (the ritual's, never the presentation's), the revival mask snapshot, whether the stagger is owed (only for an emerald release; the old flag alone is not trusted) |
| ActorReady | one real actor exists, out of the game until he steps out; his identity is committed; the feeder ledger now protects him; the receiver is retired | `EntityId`, actor time, feeder amount handed over, receiver id |
| Emerged | he is in the game on A5 floor | place, the dimensional step spent once, the stagger applied once, the sanctum baseline applied once |
| OpeningResolved | the opening is over and he is hostile | opening mode, parley beats (identified, told of the fall, calculated), hostility cause and detail |
| CombatReady | the hostile handoff; then the checkpoint | checkpoint reached and time; a diagnostic release only by the harness |

- **One step at a time:** a stage advances only from the stage before it. Repeated callbacks, loads and simultaneous triggers replay nothing.
- **Failure:** if no actor can be placed (or his blueprint failed to build), the awakening stays Committed with a logged diagnostic and is retried every 5 seconds. Nothing is consumed.
- **Retry:** a retry adopts a partial actor (found by `KrunePart` in the whole unit pool) rather than making a second one.

### One Krune
- **The actor:** `KrunePart` on a native unit (`KWR_RunelordKrune`). He is never a fixture: no `WardFixturePart`, no protection, no ignored-combat or immortality flags, no `CantMove`/`CantAct`.
- **The resolver:** `WardFixtures.Krune` is the receiver until the actor's identity is committed, then the actor, living or dead.
  - A dead or unloaded actor never falls back to the receiver.
  - No receiver is ever remade once retired.
  - It is looked up by identity in the whole pool, since the game's unit enumeration skips units out of the game.
- **Who uses the resolver:** the feeder grants, mist immunity, the ownership of his summons, ward contexts and inspections all read it. The sarcophagus inspection says he has stepped out.
- **Handover order:**
  1. The receiver's real consumption is reconciled into the ledger.
  2. Elapsed grants expire at their own times.
  3. The actor is built (or adopted).
  4. His identity is committed.
  5. The ledger's remaining protection goes to him with its original expiry: no new hour, nothing rerolled.
  6. The receiver is retired: its feeder buff is removed without counting as consumption, it leaves the game, and it is dropped from the fixture index. Only its id is kept.
- **Feeder grants after he wakes** go to the real Krune through the same ledger (strongest only, each with its own hour). Seen in play: 33, then 55.

### Placement
- **Candidates:** a deterministic list starting 1.55 squares east of the coffin (facing the altar), then rings outward.
- **Footprint:** every candidate's whole footprint must be walkable A5 floor, outside the coffin's shadow, near a navmesh node, and clear of every creature in the game by their radii.
- **An occupied preferred point** moves him elsewhere. He is never placed on anyone.
- Nobody is moved for him.

## The actor: source to runtime
Built on the game's own wizard class:
- 17 levels as a conjuration specialist; the opposition schools are evocation and illusion.
- The human race's +2 goes to Intelligence.
- The donor is Bartholomew Delgado's body: a robed human caster with no random customization. Its own facts, spells and items are not imported.

The printed totals already contain his belt, his inherent bonuses and his runic bonuses, so each is entered once:

| Line | Printed | Contributions entered once | Native (intact runes, before preset) | Preset (Normal) | Observed |
|---|---|---|---|---|---|
| Str / Dex / Con | 18 / 18 / 20 | base 14 / 14 / 16 + belt of physical perfection +4 (native item, worn) | 18 / 18 / 20 | — | 18 / 18 / 20 |
| Int / Wis / Cha | 31 / 20 / 24 | base 20 (+2 human) / 11 / 15 + inherent +3 (wish) + runic enhancement +6 (Su) | 31 / 20 / 24 | — | 31 / 20 / 24 |
| HP | 293 (17d6+231) | 173 from himself (calibrated once from the native class total without the preset) + Life runes 3 × 40 | 293 | — | 293 |
| AC | 33 (touch 23, flat-footed 25) | 10 + Dex 4 + Mage Armor 4 + dodge 4 + deflection 5 + natural 6 | 33 | +2 | 35 |
| Fort / Ref / Will | 17 / 16 / 22 | wizard 5/5/10 + Con 5 / Dex 4 / Wis 5 + Great Fortitude, Lightning Reflexes, Iron Will + resistance 5 | 17 / 16 / 22 | — | 17 / 16 / 22 |
| BAB / CMB | +8 / +12 | native | 8 | — | 8 |
| Init | +4 | Dex 4 + Alacrity's insight 4 | 8 | — | 8 |
| Fast healing | not in the block | page 18: 1 per lit Life rune | 3 | — | 3 (seen healing 3 a round) |
| Caster level | 17 | wizard 17; the revival loss below | 17 | — | 17 (the native ability-parameter rule) |
| Spear | +18/+13, 1d8+9 | BAB 8 + Str 4 + enhancement 3 + Control's competence 3 (spear only) | as printed | +2 attack | not attacked with in a checked state |

**Where the printed numbers disagree** (each shown, not hidden):
- **HP:** 17d6+231 lists 231 fixed points. Con 85 + Toughness 17 + Life 120 = 222, so 9 points are unattributed. Treatment: the total is calibrated to the printed 173 without Life, so the 9 are kept inside the calibration and no bonus is invented.
- **Initiative:** the block prints +4, but Alacrity's +4 insight is "already factored into his stats". Treatment: the components are used (Dex 4 + insight 4 = 8), because the accepted rune projection halves that insight to 2. A printed +4 that excluded it could not be halved.
- **CMD:** printed 37; the native value from its components is 35. Not adjusted.
- **Bonus spells:** this game gives a non-party caster bonus spells from the casting score's base value (Int 22), not the printed 31 (as WR-06 found for Vandiana). His slots are the combat increment's.

**Natively present:**
- Feats: Augment Summoning, Combat Casting, Empower Spell, Great Fortitude, Greater Spell Focus (conjuration), Iron Will, Lightning Reflexes, Quicken Spell, Simple Weapon Proficiency, Spell Focus (conjuration), Toughness.
- Not in this game: Scribe Scroll (the name looked up), Eschew Materials, Inscribe Rune.
- Immune to poison: a periapt of proof against poison, worn (an item enchantment giving the immunity).
- Freedom of movement: a ring of freedom of movement carrying the game's own `PermanentFreedomHoldImmun` enchantment.
- Permanent darkvision and see invisibility: spell effects at CL 17, dispellable; they do not return.
- Mage Armor: the game's own buff at CL 17, cast before hibernation and sustained by the sanctum. It is applied once at emergence with its remaining duration (17 hours from the awakening), is dispellable, and does not return on reload.
- No free Foresight, True Seeing, Stoneskin or Spell Turning.
- Dimensional steps: the game's own conjuration-school resource, 17 uses of up to 30 feet (the printed 510 feet). The opening's step out of the coffin spends one use, once.
- Acid dart: the game's own school resource, 13 a day as printed (combat use deferred).
- Rod charges: a native resource, 10 a day.
- Brain: an interim native melee action only, so a released diagnostic run is not inert. His tactics are the combat increment's, and no player meets this brain: the checkpoint holds first.
- Portrait: cropped from the page-16 art (`assets/portraits/RunelordKrune`). The model is the donor's robed body with the spear; the rune tattoos and green robe are not reproduced.

**Not native, recorded only:** arcane sight (permanent), runesight, contingency, Runelord's dominion, the spells' preparation, the artifacts' powers, boots of speed, the pearl of power and the ring of spell turning. See the manifest.

## Rune consequences on the actor
`KruneProjection` and its 128 mask expectations are the contract and are unchanged.

| Rune | Lit | Dark (high tier) | On the actor |
|---|---|---|---|
| Each Life | +40 HP, fast healing 1 | +20 HP, 0 | untyped HP; the game's own fast healing |
| Alacrity | +4 dodge AC, +4 insight init | +2, +2 | typed modifiers |
| Armor | +6 natural | +3 | natural armor |
| Defense | +5 deflection, +5 resistance on saves | +2, +2 | typed modifiers |
| Control | +3 competence with his spear | +1 | spear attacks only |

**Control's protection (this project's interpretation):** while Control is lit, a creature he summoned that attacks him meets the game's protection-from-evil numbers (+2 deflection), whatever its alignment. Dark, the protection is gone: a nonnumerical benefit is lost, not halved, and the source has no printed halving for it. The bodily-contact ward is not modelled.

**Caster level by number of dark foci:** 17, 16, 16, 15, 15, 15, 15, 14 (0 to 7 dark). His class level, spell access and Hit Dice never change. The loss applies to his own spells and abilities through the game's ability-parameter rule; an item's caster level (a wand) is not lowered.

### The revival penalty against the live rune links (adjudication)
- **The revival snapshot:** the commit snapshots the mask. The caster-level loss is read from that snapshot for 24 hours of game time from the awakening; after that it ends.
- **No restart:** later sabotage never restarts the revival or its 24 hours.
- **The live links:** they follow the current mask. A rune darkened after he wakes updates his HP, fast healing, AC, initiative, saves and spear bonus at once. Seen: Alacrity dark after waking, AC 35 → 33 and initiative 8 → 6, caster level still 17.
- **Damage is kept (native damage-preservation policy).** The game keeps damage taken, not hit points left. A lower maximum leaves the same damage and fewer hit points, and nothing is healed. A maximum at or below his damage is fatal, as any loss of maximum hit points is.
- **No repeats:** the same mask again changes nothing. A reload re-applies nothing.
- **Dispel:** the runes, fast healing and calibration are not spells, and native dispel (which requires the game's "from a spell" flag) cannot touch them. Mage Armor, darkvision and see invisibility are spells and can be dispelled.

## Entrance
1. **The soul's return:** a flare at the emerald if it is still whole (none if it was broken), and the coffin's flashes surge for a few seconds. Visual only.
2. **Emergence:** three seconds of game time after the actor is ready. He steps out with the game's own dimension door effect at the coffin and on him.
3. **What stays:** the coffin, chains, remaining foci, altar and mist. Nobody is moved.
4. **Camera:** it is never moved. If the party sees him, the battle log says so; if not, a notification says only "Somewhere deep in the sanctum, a vast presence stirs." Nothing is revealed.
5. **Early release:** the emerald is already shown broken: a dark socket and green shards replace the gem, in every load. He appears standing, with one round of staggering:
   - **Real time:** the game's own Staggered condition for one round of game time. No game time passes while paused, in dialogue, at the checkpoint or loading, and it is never re-applied (seen: 5.2 s left before and after a save and reload).
   - **Turn-based play:** each turn spends game time, so a round of game time could run out before his first turn. The stagger is kept until his first completed turn ends, then removed. Seen in play.
6. **Action sequencing:**
   - The opening (soul return, the step, the exchange) happens outside initiative.
   - When he turns hostile he joins the combat natively and rolls his own initiative. He gets no extra turn and does not act out of order.
   - The checkpoint holds before any of his turns.
   - The combat increment should treat the opening's step as spent (recorded in the state).
7. **Presentation and time:** presentation never moves `AwakeningAt`.

## Opening (adaptation)
The module gives his behaviour, not a script. The wording is this adaptation's own, in `KruneDialog.cs`, and each line of his is marked "(In Thassilonian)". He is imperious, then incredulous about the millennia, then calculating.
- **Greeting:** he demands submission and names.
- **Answers:**
  - the party can identify itself (Pathfinders, Absalom);
  - it can say Thassilon fell ten thousand years ago (disbelief, then, after "Earthfall…", the calculation);
  - it can challenge him openly;
  - it can say nothing.
- **Every path ends with him hostile.** There is no persuasion, recruitment, reward or bypass.
- **Which opening:**
  - **A4 fight under way:** a brief bark, and he turns hostile at once. No modal dialogue, and the A4 combat is not reset.
  - **No fight, and he perceives someone of the party side** (in A4/A5 and in his line of sight): the native dialogue. It pauses both sides alike.
  - **Nobody perceived:** he wakes alone and turns hostile ("he woke with nobody in sight").
- **Hostility before the exchange:** an actual attack roll or a spell actually cast by someone he perceives, before the dialogue opens. A cancelled cursor, a hover or the inventory produce no rule and never count.
- **Credentials:** a palm impression or branded hand is only an A2 credential. It makes no one a Lissalan.
- **The revival study** (page 14): one native Arcana check by the best-placed party member in A4/A5 at the commit, once, recorded in the existing knowledge ledger, descriptive only. It never gates anything. Seen: 36 vs 27.
- **No replays:** a load never replays the dialogue, a beat, the study or the opening effects. An exchange interrupted by a load (the game cannot save in dialogue) is resolved as ended, never replayed.

## The development checkpoint
At the combat-ready handoff, in the same tick, the whole simulation is paused and a labelled box says:

> Development checkpoint (prototype boundary, not part of the story): Krune's awakening is implemented. His combat encounter is the next milestone.

- **Nothing unpauses the game while it holds.** A Harmony guard refuses an unpause, and a frame that runs after a load is paused at once. The native pause is a game mode that stops every controller, the turn-based one included. The checkpoint arriving in the middle of turn-based play has not been tested.
- **No free turns:** neither A4 nor Krune can take a turn while the other side is frozen.
- **Still available:** inspection and menus. Saving works where the game allows it (out of combat); a test in combat uses the labelled combat-save fixture.
- **What it is not:** no victory, XP or loot, and the scenario is not completed. He has no invulnerability: he is a real unit that a released run can and did kill.
- **Release:** only the labelled harness (`krunerelease`, INJECTED) releases it, for native diagnostics. That is never a finished fight.
- **Offscreen:** if the awakening happens out of sight, the actor still exists on time, and the notice reveals nothing.

## Saves
- **New games, pre-departure saves, and sanctum saves not yet due:** the default state gains the lifecycle. Version 4 and 5 states migrate with their mask, grants, actors and deadlines untouched (seen: 974).
- **Version 5 saves where an older build recorded the awakening as due:** a one-time box (OK / Cancel).
  - **OK:** initialize it from the recorded cause and time and the current resources. The current saved mask is the revival snapshot, labelled `RevivalMaskFromMigration` because no history was kept. Expired temporary HP stays expired.
  - **Cancel:** keep a legacy exploration save with no Krune added, with one notice.
  - Asked once. Seen: 985 adopted, 978 kept.
- **Saves made during a stage** rebind by identity:
  - at ActorReady, the hidden actor emerges once and the baseline is applied then (0.8.5 fix);
  - at the checkpoint, it is held again;
  - a dead Krune stays dead.
- **Nothing is reset:** no party reset, rest or refill. A missing view or index entry is never permission to recreate him.

## Not part of this increment (deferred to the combat milestone)
- **Spells:** his spell preparation and casting, quickened and empowered spells, summoning (and the +4 mist resistance for his real summons, whose hook is ready and scoped to genuine ownership), wish, contingency.
- **Artifacts:** the spear's dancing, ego, telekinesis and wield-check; the rod's metamagic.
- **Other abilities:** Runelord's dominion, runesight, arcane sight, boots of speed, the pearl of power, the ring of spell turning.
- **Tactics:** his tactics and morale.
- **The rest of the scenario:** the mist's removal and the conclusion (his death, the crumbling, the portal), and the Society's rewards.
- **Diamonds:** they are in his inventory as 15 native diamonds; their loot policy belongs to the conclusion.
