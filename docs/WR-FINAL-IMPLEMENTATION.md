# The Waking Rune: implementation and adaptation (playtest candidate)

What the playtest candidate implements, where it adapts the scenario, and what it leaves out. Profile: Subtier 10–11,
six-character baseline, extra challenge on; the player's native difficulty preset applies on top. Earlier milestones'
documents (WR-04 to WR-07A) remain the detailed record of the sanctum, Kurshu, A4 and the awakening; this one covers
the work that completes the adventure and summarises the rest.

**Status words:**
- **Native:** the game's own content, used as it is.
- **Local:** built for this scenario from native parts, acting by the printed rule.
- **Adapted:** built, with a stated difference from the printed rule.
- **Held:** occupies its printed place, has no use here, with the reason.
- **Not in this draft:** stated, with why it does not remove a defining tactic.

## 1. The journey
New Game → difficulty and character creation → the lodge (briefing, recruitment, Quartermaster, rest) → the refuge
token → Kurshu (A1) → the door, wards, statues and runes (A2–A3) → Vandiana and her defenders (A4) → Krune's awakening
and battle (A5) → his death, the aftermath and his loot → the A4 portal home → the lodge debrief → a recorded outcome.

| Ending | When | Lodge |
|---|---|---|
| Success | Krune slain, both artifacts carried home | thanks, the masked figure takes both artifacts and leaves, epilogue |
| Krune slain, artifacts missing | slain, one or both left behind (warned at the portal) | acknowledged as partly done; what was carried is handed over |
| Withdrawn | the portal used before Krune is slain (warned: one way, mission abandoned) | the Decemvirate's contingencies; no success rewards |
| Defeat | the party falls | the game's own game over; nothing is resurrected |
| Diagnostic legacy | a WR-07A diagnostic save whose interim boss was killed | identified as a test save, never a completed adventure |

## 2. Object rules (checkpoint 1)
| Object | Saves | Breaking |
|---|---|---|
| Soul emerald | **Adapted:** a CL 17 magical object, +10 (core magic-item rule; the scenario prints no saves) | hardness 20, 40 hp, break DC 40 (printed); Disintegrate: an unmade save disintegrates it, a made save is 5d6 through hardness |
| A2 conduit door | magical (+10, the feeder's CL 17) while its feeder is armed; otherwise unattended stone: never saves | hardness 8, 75 hp, DC 28 (printed); Disintegrate as above |
| Wall of stone | never (instantaneous: ordinary stone) | per 5-ft section, hardness 8, 60 hp; Disintegrate removes a 10-ft cube (two sections) |
| Rune plates | their printed no-save rule against their vulnerabilities | unchanged (WR-04) |

The proxy units' protective statistics (Constitution 200 and the like) never become an object's saves. Energy against
objects: fire, cold, electricity, acid and sonic halved; force, untyped magic and divine power in full; negative and
positive energy nothing. The broken emerald keeps its saved identity as the "Shattered emerald", out of the game.

## 3. Krune's spells (page 16; extra challenge page 14)
His book holds exactly the printed slots (65 at 1st–9th level, cantrips at will). Quickened and empowered entries are
the game's own metamagic spells in their raised slots. Rune sabotage expends no spells (extra challenge). Mage Armor's
slot is spent (cast before hibernation). The revival's caster-level loss (24 hours) lowers each cast's caster level only.

| Spell | Status | Notes |
|---|---|---|
| Foresight, Summon Monster IV–VII and IX, Horrid Wilting, Dimension Door, Cloudkill, Dispel Magic, Greater Dispel Magic, Acid Fog, Glitterdust, True Seeing, Grease, Waves of Fatigue, Fear, Protection from Energy, Sleet Storm, Stinking Cloud, Acid Arrow, Blindness/Deafness (as Blindness), Web, Obscuring Mist, Shield, True Strike, Acid Splash | Native | the game's own spells and ranges (Medium is 40 ft in this game) |
| Stoneskin | Native (copy) | the game's own spell and buff; the copy drops only the native "party members only" target limit so he can cast it on himself |
| Wall of Stone | Local | a straight wall of up to eight 5-ft sections across the chosen point, 10 ft high; blocks movement and sight; cannot form on a creature; breakable |
| Black Tentacles | Local (0.9.20) | 20-ft spread; **one grapple check per round for the whole area** (CMB = CL + 5, +5 against a creature already held) against each creature's native CMD; a creature entering meets that round's check; a success grapples or keeps the hold and deals 1d6+4; **a failed check against a held creature releases it, with no damage**; each creature is resolved once per round's check (the cast's round never crushes twice); freedom of movement is checked every time, so a creature that gains it is let go; break free natively against DC 10 + CMB |
| Greater Teleport | Adapted | only to legal floor within his sanctum: he fights to the death and never leaves |
| Phase Door | Adapted | cast on himself: CL/2 passages for him alone; each passage is a move action, a step of up to 40 ft through stone to legal floor (no line of sight needed) |
| Globe of Invulnerability | Local | an immobile 10-ft globe: spells of 4th level or lower cast from outside cannot affect those inside; existing effects are not suppressed (adapted) |
| Spell Turning | Local | 1d4+6 levels; single-target spells turned on their caster at the cast; the core partial-turning chance |
| Hostile Juxtaposition | Adapted | the link on a failed Will save; once a round an attack on him lets the linked foe be swapped in (Will negates); the attack is lost rather than redirected |
| Solid Fog, Fog Cloud | Local | the game's mist area at a point (concealment as fog cloud). **Solid fog (0.9.20):** half of each creature's own speed (the game's difficult-terrain multiplier, so a hasted speed is halved too); no 5-foot step in turn-based play; −2 on melee attack and melee damage only; ordinary ranged weapon attacks made from, into or through it fail; rays and other ranged touch attacks resolve normally; freedom of movement moves and attacks normally |
| Planar Binding, Mage's Faithful Hound, Explosive Runes, Fly, Detect Thoughts, Erase, Unseen Servant, Arcane Mark, Mage Hand, Read Magic | Held | no use in this fight or no mechanic in this game (stated per slot) |
| Wish | Adapted | known, cast through the bonded spear: the bond's one daily casting **and** 25,000 gp of diamonds consumed each time; duplicates summon monster VIII or horrid wilting (DC as 9th level); never a refill or an instant victory |

## 4. Gear and artifacts (pages 15–17)
| Item | Status | Notes |
|---|---|---|
| Boots of speed | Local item | haste as a free action, 10 rounds a day spent a round at a time |
| Pearl of power (8th) | Local item | once a day, one spent 8th-level slot back |
| Ring of spell turning | Local item | three times a day, spell turning (its own charges) |
| Wands of acid arrow (CL 9, 23) and summon monster III (12) | Native items | printed remaining charges |
| Acid dart, dimensional steps | Native | the conjuration school's own uses (13 and 17 of 30 ft) |
| Dragon-tooth spear | Artifact (0.9.20) | +3 longspear; Control's competence on **his own** attack rolls with it. **Dancing:** loosed as a standard action, the same spear fights for 4 rounds with his base attack bonus (+8, so its iterative attack) and its own +3 only (the Rune of Control's competence is printed for his own attacks with the spear, and a dancing weapon uses its activator's base attack bonus: the competence stays off the dancing form, by interpretation); it shares his space and is kept at his side every tick, walking or teleporting; it reaches 10 ft, makes no attacks of opportunity and is an attended object (no target of its own); as the 4 rounds end he grasps it (a free action) and it cannot dance again for 4 rounds; if he cannot act then, it drops in his space and he takes it up (a move action in combat; at once out of combat, 0.9.27); if he dies it falls with his gear; his hand is empty meanwhile (no Wish or spear powers). Ray of enfeeblement and slow 3/day each; a telekinetic trip (CMB 20) using his swift action; only Krune commands its powers. **Ego:** a party member who takes it in hand makes a DC 22 Will save each time or is compelled to set it down; from the second attempt on (the count is saved) it first strikes them for 1d8+9; one who masters it must keep it against its telekinetic disarm (CMB 20 against their CMD) every round |
| Rune-carved rod | Artifact (0.9.20; renewal and sharing 0.9.26) | seven printed feats, up to three on one spell, one charge per spell level added, 10 charges kept with the rod (Krune's old resource taken over once) and renewed on a fixed 24-hour schedule: a renewal that falls due while the rod is full still moves the schedule on, so a later expenditure is never refunded by a read. A cast's charges are promised to it for a round, so two casters cannot both draw the last charges; with too few, the spell is cast without the metamagic (never unpaid). **Applied:** Empower, Extend, Maximize, each only where the spell accepts it (its native metamagic list); **Enlarge is adapted** as the game's Reach Spell (the next range band, not Enlarge's doubled range). **Not implemented in this draft:** Silent, Still and Widen (listed as unavailable, never applied or charged). Krune spends Maximize + Empower on his 5th-level-and-up damaging spells and Extend on summons while 5 charges stay in hand. His order is bound to one command, re-checked (rod in hand, charges) when the spell's parameters are taken, and ends with that command, after 12 s, on a load or at his death. A party caster has a toggle per applied feat while the party carries the rod |
| Belt, periapt, ring of freedom of movement, diamonds | Native | as before; diamonds a finite reserve, spent by Wish |

Both artifacts are recovered after his death from one ground bag with the rest of his gear (the same items, nothing
copied or refilled), cannot be dropped, and are carried home for the Society; carrying is enough, wielding is not
required.

## 5. Special abilities (page 17)
| Ability | Status | Notes |
|---|---|---|
| Runes of Power, revival loss | as WR-07A | rune links live; damage kept |
| Contingency (dispel magic on a teleport-impeding effect) | no trigger in this game | the installed game has no effect that impedes teleportation (no dimensional anchor or lock), so it can never fire; recorded unspent |
| Runesight | no eligible target | only Thassilonian runes are sensed; the party carries none in this adaptation |
| Runelord's Dominion | no eligible target | it alters only real Thassilonian rune benefits; the party's palm impression and branded hand are A2 credentials with no benefit to invert, so his director never uses it |
| Freedom of movement, poison immunity | Native | worn items; his ring carries the game's permanent freedom of movement (the native ring enchantment alone only stops holds), so his own tentacles cannot hold him |
| Arcane sight, darkvision, see invisibility | as WR-07A | darkvision counts in Vandiana's darkness |
| Summoner's charm, Augment Summoning | Native (feat); charm per the game's conjuration school | |

## 6. Tactics
His director chooses only; every action is a native command (attack rolls, saves, spell resistance, slots, action economy
and effects stay native). Each decision is taken when the native brain gives him one: in real time when he is free to
act, in turn-based play on his own turn. His spear is offered to the brain only once his standard action is ready, so the
director is asked first each round (seen: a standard spell every round in close quarters, and quickened plus standard
casting in one turn-based turn).
- **Swift:** quickened Dimension Door when overwhelmed; quickened Wall of Stone between him and closing foes; quickened
  Glitterdust on an unseen foe; quickened Black Tentacles on a cluster he can hold; quickened summons; quickened Dispel
  Magic on a buffed foe; quickened Grease on foes beside him, never over his own footing; else the spear's telekinesis at
  a foe at his side who can be knocked down.
- **Standard:** relocation when overwhelmed (stepping through an open phase door passage; Phase Door; Greater Teleport;
  Dimension Door to the legal floor farthest from the foes, within the spell's reach and, except through stone, in his
  sight); then protection (Foresight, Spell Turning, True Seeing, Stoneskin, Globe, Shield); summons; Wall of Stone;
  Horrid Wilting on 3+; a cloud he is immune to (Cloudkill, Acid Fog, Stinking Cloud) in close quarters; Black Tentacles;
  Hostile Juxtaposition; dispels; control spells; single-target spells; then Wish, the pearl, the dancing spear, the
  spear's powers, acid dart, his wand; else his spear.
- **Recover:** withdrawn, he protects himself and summons an army (Wish's summons first) until he is back over 60% or a
  foe reaches him, then returns. While still pressed and able to get away, he stays withdrawing.
- **Never:** a target out of range or out of his line of sight from where he stands (the cast command needs both); a
  fallen foe; **a foe he does not detect now** by the game's own perception (his group's memory: sight, his see
  invisibility, blindsense); an area over foes already inside his own area of the same spell; a spell he has not got; a
  second try of a cast that did not go off for two rounds; anything while dominated, confused, frightened or unable to
  act.
- **Unseen foes (0.9.20):** a foe that vanishes from his perception is remembered only where he last saw it, for 12 s.
  That place, within 30 ft and in his line of sight, is the reason and the aim for a search spell (Glitterdust, True
  Seeing); he never learns where the foe is now.
- **Session state (0.9.20):** the director's cached plan (bound to one Krune), hold-backs, issued cast, sight memory, the
  tentacles' round rolls and a pending rod order are cleared whenever an area unloads or loads; an order dated after the
  clock (a load the reset missed) is void. Everything that must persist is in the saved state.

## 7. Death and the changed sanctum (page 15 adapted)
**His death is processed wherever he is (0.9.20).** The game lets a unit sleep when it is in the fog of war, out of
combat and idle; a sleeping unit's life state, round ticks (fast healing) and brain are not processed. From his
emergence Krune is kept awake (the game's own `Sleepless` flag, saved with him), so the native life controller decides
his death wherever he is: damage at or above his hit points kills him (he has no dying condition and no regeneration).
Measured on seed 998 (INJECTED 300 damage at 293 hp, out of combat, placed behind the A3 core): kept awake, he died in
the same tick; with the keep-awake suspended (a labelled control), he stayed "Conscious" at −7 and his fast healing
brought him back to 2 hp within 15 s, so a lethal hit was undone. While he is in combat the game does not let him
sleep anyway (control at A1's north end: in combat, not sleeping, keep-awake suspended); with it on, a party member's
Acid Fog finished him there, out of the party's view, in the tick his hit points ran out.

Once dead (any lethal cause): the sarcophagus, the empty soul gem and the remaining foci crumble; the revival's light
fades; the mist dissipates and exposes no one again; his body is consumed after 4 seconds, his gear first set down as
one native loot bag. No collapse or countdown; other enemies, summons and spells follow their own rules. Each step is
recorded once and re-applied on every load.

## 8. Return and the lodge
The A4 pedestal's shard matching the expedition's token opens a one-way passage for the whole party (no roll needed),
with a view of the lodge; the other shards are described as unknown destinations. Warnings before abandoning the mission
or leaving an artifact. Refused in combat. A verified save first, then the native area load; a failed save or load
changes nothing and names the save to recover from. The lodge does not replay its setup, briefing, grants, stock or
token; the debrief opens once; the outcome is recorded once (and in a native flag).

## 9. Journal
One native quest, "The Waking Rune", with six objectives: prepare and assemble the expedition; overcome the sanctum's
defenses; confront the revival; defeat Runelord Krune; recover the two artifacts; return and report. Each is derived from
recorded state only (the sanctum-entered flag, the A4 defenders' defeat, his awakening stage, the conclusion record), never
from a dialogue visit, and only ever moves forward, so older saves catch up on load. Victory completes the quest; an
artifact left behind fails only that objective; a withdrawal fails the runelord's objectives and the quest. No objective
names a hidden room or a rune's solution. Waking Rune sessions only, and never while a save is loading.

## 10. Compatibility
- **Saved blueprints keep their names and ids.** A renamed brain action broke every awakened-Krune save in 0.9.13 (fixed
  in 0.9.14); replacements keep the old name.
- **Every blueprint a save can refer to is registered.** His director brain action was never registered (0.9.0–0.9.21),
  so any save made after he woke referred to a missing blueprint and could not load ("Failed to load blueprint by
  guid"). Fixed in 0.9.22, with a start-up self-check that walks every mod blueprint for references to unregistered ones
  (0.9.23: 806 walked, 0 found; it also caught his portrait).
- **WR-07A checkpoint saves** enter the full encounter once, with one notice and nothing reset; a diagnostic death stays a
  test save (outcome "Diagnostic legacy").
- **Older Krune saves:** his brain is brought up to date on load; a ring of freedom of movement saved before 0.9.11 gains
  its freedom of movement on load. A save made after his book was filled keeps the native Stoneskin he cannot cast on
  himself (nothing else in his book changes).
- **New games and the stock campaign** are untouched by scenario state (seen: a second Waking Rune game in the same
  session started fresh; a Main Story game after it showed no scenario state, journal or patches).

## 11. Known limitations of this draft
- The rod's Silent, Still and Widen are not implemented in this draft (listed, never applied or charged); Enlarge is adapted as Reach Spell.
- Contingency, Runesight and Dominion have no trigger or eligible target in this game (section 5).
- Planar Binding, Mage's Faithful Hound, Explosive Runes, Fly, Detect Thoughts, Erase, Unseen Servant, Arcane Mark, Mage
  Hand and Read Magic are held (section 3).
- Borrowed model and cropped portrait; journal title "Linzi's Journal" is the game's own.
- Supplies audit: cold iron +1 weapons added in 0.9.19 (the +3 weapons, which the game treats as cold iron, were
  already stocked; see the ledger on Kurshu's damage reduction); diamond dust for stoneskin added in 0.9.25.
