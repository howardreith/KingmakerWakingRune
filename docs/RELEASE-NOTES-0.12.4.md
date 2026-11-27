# The Waking Rune 0.12.4

0.12.4 is the stable release. It replaces 0.11.3 as the recommended build.

It contains everything from the 0.12.3 owner playtest preview, rebuilt on the common **UMM 0.31.1** loader baseline. The
gameplay is unchanged from 0.12.3.

## Before you update

- **Update Unity Mod Manager first.** 0.12.4 requires **UMM 0.31.1 or newer**. Earlier releases declared 0.28.2, and older
  loaders will refuse this build.
- **Back up your saves.** Back up or duplicate important Waking Rune saves before opening them in 0.12.4. Saves from the
  sanctum's original layout are migrated to the revised layout. Keep a copy of each important save from before the update
  until the migration has been verified in your game.
- **Not yet tested in-game.** 0.12.4 has not been tested in a running game, and neither has 0.12.3. The 28-mod
  compatibility run has not been done either. Its checks were offline: builds, tests, and package and loader-reference
  validation. See "Known limits" below.

## Changes since 0.11.3

### Loader baseline (new in 0.12.4)

- Declares UMM **0.31.1** as its minimum, matching the other Kingmaker mods' common baseline.
- Compiled against SHA-256-verified upstream UnityModManager 0.31.1, Harmony12 1.2.0.1 and Harmony 2.3.1.1 references,
  not whichever loader happened to be installed. Newer loaders remain supported targets, tested separately.
- Each package has a build receipt (`<zip>.build.json`). It records the producing commit, the build inputs and the pinned
  reference identities. `scripts/Validate-Package.ps1` checks a ZIP's files, manifest, assembly identity and loader
  references before installation.

### Krune combat AI (from 0.12.3)

- Fixed the command loop in which Krune repeatedly ordered a rejected quickened summon and stopped making useful decisions.
- Failed commands now receive a terminal result and release their rod reservation.
- A failed quickened action can fall through to a different standard action.
- Failed plans remain unavailable until the relevant action budget genuinely renews. They are no longer reset by elapsed
  time or HP changes.
- Krune can relocate throughout the sanctum and prepare another offensive, following the scenario's intended tactics.
- Ordinary spear attacks are a last resort rather than his default behavior.
- Black Tentacles has been removed from Krune's production tactics until its implementation receives a dedicated
  qualification pass.

### Kurshu presentation (from 0.12.3)

- Kurshu's first-wound prophecy is now presented as acknowledged dialogue.
- Her blood-written death message and the 90-minute warning are presented through an acknowledged dialogue sequence.
- Her encounter-specific death presentation suppresses inappropriate overkill dismemberment without globally changing gore.

### Restored cultists (from 0.12.3)

- Surviving A1 cultists now have persistent, deterministic roles: attackers, reporters, frightened fugitives and deceptive
  speakers.
- Non-hostile survivors do not automatically start combat.
- Deceptive survivors use party-level Perception checks.
- Harmful-area provenance is saved, so later entrants are distinguished from creatures deliberately caught when the area
  was placed.

### Party checks and interactions (from 0.12.3)

- Party-level investigation checks choose the best eligible party member and legal skill combination.
- The strongest reachable eligible party member performs the emerald's Strength interaction.
- Existing knowledge ledgers and higher-rank retries are preserved.

### Sanctum layout and interaction (from 0.12.3)

- The sanctum has wider clearances for enlarged humanoids and Large pets.
- Existing layout-1 saves are batch-migrated into layout 2. Logical room positions and closed-door boundaries are preserved.
- Rune plates have larger, more reliable interaction targets.
- Decorative upper figures no longer steal rune-plate clicks.
- Visible stone wall treatment replaces large opaque black wall surfaces in the revised areas.

## Verification

- Release build from clean source on `main`, with the offline test suite.
- The ZIP and its build receipt were validated with current provenance.
- The loader references are the pinned UMM 0.31.1, Harmony12 1.2.0.1 and Harmony 2.3.1.1.
- A static loader-API audit of the released DLL was run against UMM 0.31.1, 0.32.4, 0.33.0 and 0.33.4.
- The UMM baseline change was reviewed through the repository's agent-review workflow (PR #12) and accepted by the owner.

## Known limits

Native in-game testing was **not run** for 0.12.4 or 0.12.3. These need playtesting:

- old-save layout migration with live actors, corpses, pets and summons;
- Kurshu's dialogue pause/turn behavior in real-time and turn-based combat;
- Stone to Flesh timing;
- rune-plate mouse targeting and camera presentation;
- enlarged-character traversal;
- Krune's sustained real-time and turn-based combat behavior;
- the original Frost Giant / Extend rejection scenario;
- compatibility with a full 28-mod profile;
- running on UMM 0.31.1 and newer loaders in-game. Static checks only show that the loader members it calls exist.

## Installation

1. Update Unity Mod Manager to 0.31.1 or newer.
2. Keep a copy of your current Waking Rune package and back up important Waking Rune saves.
3. Replace the installed `KingmakerWakingRune` folder with the contents of `KingmakerWakingRune-0.12.4.zip`, or install
   the ZIP through the UMM installer. It holds exactly `KingmakerWakingRune/Info.json` and
   `KingmakerWakingRune/KingmakerWakingRune.dll`.

## Rollback

Restore the 0.11.3 package. Then load a save that has not since been saved under 0.12.x.
