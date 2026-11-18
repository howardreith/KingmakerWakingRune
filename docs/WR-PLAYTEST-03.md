# WR-PLAYTEST-03 — owner playtest follow-up

**PARTIAL — 0.12.1 is ready for native qualification.** The Release build and the complete offline suite passed, with 280 tests and zero failures. The draft PR is open for owner review. The installed game still has 0.11.3.

| 0.12.1 candidate | Verified value |
|---|---|
| Reviewed PR head used as base | `0acf8e558c75cc9d24b58e988d73939c10d417ab` |
| Clean source commit | `3c7781e566c952d28b4cc829922ca76b9e5b0dc7` |
| Installable local package | `artifacts/KingmakerWakingRune-0.12.1.zip` |
| ZIP SHA-256 | `b587869a9d340d28500cf62a2f6a0ef315fad0905ebd414a58f76573efdf8ee4` |
| Packaged DLL SHA-256 | `60d1b6d70c3aa4cac576c7717252752c820bd29be724072a304e8939885d03c6` |
| Packaging | PASS. The build repeated all 280 tests and decoded 19 embedded images. Independent inspection verified the two archive entries and the archived DLL digest. The 0.12.0 ZIP and its archived DLL still match their recorded digests. |

## Owner review findings for 0.12.1

| Owner finding | Result in the reviewed source and offline suite | Native result |
|---|---|---|
| Kurshu's panel bypassed the native start and could leave time frozen | PASS for the acknowledgement and clock state tests. Both panels run native start, cue selection, camera, answers, finish and teardown. Only the last Continue acknowledges a notice. Defeat commits the deadline independently. A malformed start leaves the notice pending. The reader protects live queues, projectiles, area effects and the turn wait, holds simulation for both sides and restores the prior pause and time scale on exit. | NOT RUN for actual panel, pause/resume, speed, initiative, queued actions, replacement and unload |
| Old saves searched from a compressed coordinate | PASS for old-world samples in A1, north/east/south/west A3, A4 and A5. Invalid positions search nearest-first from the original logical coordinate. The tests compare the chosen radius with all legal sampled alternatives, including a Large humanoid and a Large pet. Ground height, footprint, closed-door side and leaves, fixtures, bodies and navmesh constrain runtime placement. Per-actor completion allows an unresolved actor to retry. | NOT RUN for old saves, traversal and full restart |
| Started casts counted as committed | PASS for the command lifecycle. Only the matching successful RuleCastSpell on the executing command's exact AbilityData commits. Relocation also requires a successful command end and measured arrival on legal ground within 0.8 m of its intended destination. Only delivery records retreat and applies the Dimension Door restriction. The command classifies, releases its rod offer and retires the plan before notifying native end subscribers. Native teardown runs in a finally block. | NOT RUN for native slots, action costs, rod charges, actual teleport and interrupted delivery |
| A six-second boundary reopened rejected spells | PASS. Turn-based exclusion follows the native turn object. Real time observes an expended action becoming ready or a material state change in HP, enemies, phase, rod charges or ready slots. Target and range failures exclude that target. Other failures exclude the spell, metamagic and action across targets. | NOT RUN for native cooldown and turn transitions |
| Rejection test only exercised a set | PASS. The adapter exercises pre-Init and post-Init rejection, pre-rule interruption, wrong ability/rule, commit, delivery failure, post-commit interruption, rod acceptance/decline and end notification ordering. A rejected quickened summon falls through the tactical lists to a committed standard Foresight in the same turn. Repeated decisions cannot redispatch the rejected order in that state. | NOT RUN for `krunerejectsummon` with a living summoned Frost Giant and Extend offered |
| Multiple skill buttons allowed fishing | PASS for selecting the best eligible member and skill, one roll, new ranks and new participants. Each clue has one Study choice. The actual roller enters the knowledge ledger. The other participants' considered ranks close the same opportunity. Legacy answer blueprint identities remain available to saves. | NOT RUN for dialogue choices, hidden totals and saved ledgers in game |
| Emerald selected an actor before establishing reach | PASS for ordered fallback and exactly one contact. The native path request considers one adventurer at a time in current Strength bonus, distance and party order. An approach that ends outside contact falls through before any roll. A Strength failure ends the interaction. | NOT RUN for a blocked strongest character followed by a reachable second character |
| Non-damaging spells or persistent areas could provoke the wrong survivor | PASS for exposure to a direct harmful party spell and a survivor caught in a freshly placed harmful area. The tests also cover helpful spells, environmental effects, later entrants and expired areas. Runtime observes the targeted cast, effect application, saving throw or buff, and tracks exposed survivors by area context. | NOT RUN for neutral targeting, charm, immobilization and reload |

The information-check audit covered the seven rune plates and their figures, A2 metal and inscription, flail, apex mark, gem, altar tools, ritual light, pedestal, mist, revival, Kurshu's ambush and cultist deception. Rune identification compares Knowledge (Arcana) and Lore (Religion). The adaptation represents printed Spellcraft with Arcana. Kingmaker exposes no independent Linguistics or Spellcraft stat, so the printed Linguistics route is unavailable. The A2 metal clue compares Arcana and Nature. Revival uses Arcana, while ambush and deception use Perception. Personal Trickery, UMD, combat and A2 door-force actions retain their acting character.

After the complete offline run, the second static review compared the source diff against the installed dialogue, game-mode, command, pathfinder, rule and teleport implementations. It checked the pause-stack IL branch, the scheduled-dialog local, both queue interruption sites, projectile cleanup, state persistence and cast/end callback order. The build uses the Harmony assembly already referenced by the installed Harmony12 adapter for exception finalizers. The package contains the mod DLL and its UMM manifest.

## Owner authorization checkpoint

**Native matrix — NOT RUN.** The previous automatic approval review rejected the Waking Rune guarded transaction because machine policy reserves live Kingmaker Mods transactions for the Mounted Combat guarded tooling. The 0.12.1 pass did not start a transaction. The owner profile below is evidence from the supplied log. This lab's live Mods directory contains only Waking Rune.

The exact request is to permit the repository's guarded Waking Rune transaction under the machine policy, temporarily stage the verified 0.12.1 package, run the matrix using disposable named saves, collect the evidence and finish with `End` to restore the existing installation. The requested entry command from the repository root is

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/KwrRuntime.ps1 Begin -RunId wrplay3-0121-native -PackagePath artifacts/KingmakerWakingRune-0.12.1.zip
```

The owner-profile summon reproduction requires the logged compatibility profile to be available for guarded staging. No software installation is authorized. Outstanding native evidence includes both Kurshu modes and simulation resumption, the Frost Giant/Extend fault and ordinary cast accounting, relocation and pursuit, ten rounds or terminal combat in each mode, a legal post-A4 victory, old-layout saves, enlarged traversal, the seven-plate mouse matrix, wall views and cutaway video, comparative Stone to Flesh timings and stock-campaign isolation.

## Immutable 0.12.0 record

**PARTIAL.** The 0.12.0 Release build passed 270 offline tests. The candidate has no native qualification or installation. The standing installation is 0.11.3.

| Candidate record | Value |
|---|---|
| Base | `ccb012e043941bf91de5538a81ecfee39469b9e3` on main after PR #10 |
| Source | `be99fe0fe1df8fcdd27b08a0c5783dfaad5118bc` on `feature/wr-playtest-03` |
| Package | `artifacts/KingmakerWakingRune-0.12.0.zip`, clean tree, built with `scripts/Build.ps1` |
| ZIP SHA-256 | `b0eebc92f3d11155b8d3e90d338e121fa4bd3bbc5c699f8ff6acb0f3befe0644` |
| Packaged DLL SHA-256 | `3a278c40492947b650029c81a114154b56ba33aef5d9b3fc76dfbb20c83f96ed` |
| Archive check | Exactly the UMM manifest and DLL under `KingmakerWakingRune/`. Independently hashed the DLL inside the ZIP. All 19 expected embedded images decoded during packaging. |
| Owner evidence | `WR-PLAYTEST-03-Claude-Package.zip`, SHA-256 `8cb0b10f5f41b2593ab83691458cd8830f14c9d8c23b3951ca75773242f0c8f8` |

## The failed summon

**READ** from the owner's `output_log(20261118-015536).txt`. Times are game seconds. Line numbers count from the beginning of the supplied file.

| Time | Observation |
|---:|---|
| 73611.8 | At 27% HP with an adjacent foe, Krune enters recovery and orders quickened Dimension Door. |
| 73617.8 | He orders quickened Black Tentacles, then Wish to summon at (8.9, 0.0, 9.5). Extend on the Wish spends one rod charge, leaving nine. |
| 73623.8 | A summoned Frost Giant is present. Three orders for quickened SummonMonsterVBase with Extend target the previous Wish point. The first is on line 485519. |
| 73625.0 | Eight further orders target (8.8, 0.0, 9.1), from line 502611 to 531097. Across both timestamps there are eleven orders and ten recorded uncast endings. |

In 0.11.3 the native command's end callback closed the rod order. The director learned that a cast had failed by comparing time since issue with a four-second threshold. Its next decision could select the same ready swift slot and overwrite that timestamp before the threshold elapsed. The log also shows the base summon name at dispatch. It does not record the engine's first rejection reason. Target occupancy, variant replacement and the rod must therefore be tested separately before attributing that rejection to any one of them.

The 0.12.0 candidate binds a directed plan and rod offer to the actual command. Its end callback records a cast, rejection, interruption, lost action, invalid target or range, unavailable resource, or native failure based on the execution state available there. It releases the matching rod offer and excludes a failed ability, metamagic, action type and unit or rounded landing point for that round. This lets a failed swift plan fall through to another option. A declined rod offer still permits the spell. Slots and actions are spent by native execution. Summon selection resolves the child ability and checks the landing footprint. The watchdog checks twice per real second and rechecks an empty actionable queue after two unpaused seconds on Krune's turn. It can dispatch a different plan, pursue a previously seen position or finish a turn.

The offline regression queries the same rejected swift plan 1,000 times without advancing the round, checks that a standard plan stays available, then advances the round and checks reconsideration. The harness command `krunerejectsummon` arms a **SETUP/INJECTED** rejection after native Init and before Start, only when the quickened V summon has an Extend offer and a living summoned Frost Giant is present. No run of that fixture is recorded. It exercises recovery even if the original profile-specific rejection no longer happens naturally.

## Original owner notes and 0.12.0 results

| Owner observation or requirement | Candidate change and offline evidence | Native status |
|---|---|---|
| Krune repeatedly orders an uncast summon | Exact-command completion, per-round exclusion, variant selection, reservation cleanup and bounded watchdog. Frozen-round regression passes. | NOT RUN for the Frost Giant, rod, slot and action accounting |
| Tentacles and early spear melee | Both printed Tentacles preparations remain accounted for. Production tactical lists omit them and the retained ability rejects casting. The old area can expire. The held spear requires an unused standard, no legal magic plan and a visible foe in reach. Policy tests cover both exhaustion and refusal. | NOT RUN for old areas and exhausted-caster combat |
| Withdrawal and return | Destination search spans authored regions. Placement checks floor, bodies, footprint and doorway clearance. Self-only Dimension Door can cross the map. The director uses real slots, cooldowns, limited steps and phase passages, saves its retreat and last seen foe, and can prepare and pursue. | NOT RUN for actual delivery, pursuit, re-entry, reload, summons and control effects |
| Wound prophecy | The native speaker cue uses Kurshu’s unit, name and inherited portrait. Continue advances the panel. The lethal-first-wound regression passes. Scoped dialogue patches preserve command queues and pause simulation for both sides. | NOT RUN for portrait rendering, RT pause, initiative and queued actions |
| Blood runes and overkill | Defeat commits the saved 90-minute deadline independently of the pending presentation. Three acknowledged cues present the blood, the established message and the deadline. Only Kurshu’s visual parameters suppress the dismember prefab. The death path still handles damage, XP, loot, acid and regeneration. Disintegrate keeps its native special effect. Blood narration can run without a surviving unit view. | NOT RUN for ordinary and extreme lethal damage, acid/permanent death, regenerating defeat, Disintegrate and restart |
| Party information and emerald | Inspection, revival, ambush and deception use eligible active expedition members. Each offered skill uses its highest eligible modifier with one native roll per attempt. Emerald orders the strongest available adventurer to make contact. Ties use distance then party order. A2 force remains personal because the feeder strikes the body making contact. | NOT RUN for buffed scores, reach failures, interposition and reloading the ledger |
| Restored cultists | Ten fixed identities, neutral survival processing, saved role and route, conversation and provocation. A3 dead outsiders retain the crumble route. See manifest below. | NOT RUN for all survival, dialogue, interception and reload outcomes |
| Stone to Flesh hitch | Prefabs are requested during area setup. Delivery no longer performs full diagnostic stat dumps. Explicit `statuestats` can still collect one. Delivery and placement/native spawn have separate elapsed-time diagnostics. | NOT RUN for comparative timings or video |
| Plates and top figures | One two-sided selection disc, diameter five feet, on the fixture’s existing selection collider. The upper model is decorative. Its old proxy is retired and side figures remain separate. | NOT RUN for seven plates, default/rotated views, both resolutions, active/disabled states and all three cursor modes |
| Black wall and cramped routes | Layout 2 uses stone faces and caps. Camera-facing wall height is independent of physical walls. A shared 15% horizontal expansion updates grid-authored placements. Door-side and arrival-visibility checks pass offline. | NOT RUN for the owner angle, all orientations, enlarged humanoid, large companion and A5 circulation |
| Save compatibility and isolation | New per-encounter state, stable identities, scoped patches and versioned placement repair. Existing restored cultists retain their historical roles. | NOT RUN for A1/A3/A4/A5 upgrades, lodge, active deadline, awake/dead Krune, completed outcomes and disposable stock campaign |
| Sustained combat and victory | Prior post-A4 fixture located at `runtime-evidence/wrfinal-p5/seeds/Manual_1022_KWR_AUTOMATION_P15_BEFORE_KRUNE_REPLAY.zks`. The owner archive contains no exact combat save. | NOT RUN for ten rounds or terminal outcome in either mode, or legal party victory |

## A1 manifest and adaptations

| Result after surviving Fortitude DC 15 | Stable support IDs |
|---|---|
| Attack | N2, N4, T2 |
| Report to Vandiana on arrival | N1, N3, T1, E1 |
| Flee without reporting | W1, W2 |
| Claim to be a prisoner, seek release, then report | E2 |

IDs have the prefix `Support-`. E2 promises to ask the gatekeeper to extinguish the runes. A hidden DC 22 Perception check may reveal the seven-pointed sign made by the speaker. The player can question the claim, block the path, release the survivor or attack. A direct attack can provoke hostility. Survival failure leaves a corpse. Saved outcomes, entity IDs, credentials, route progress and reporting determine what happens after loading. W1, W2 and E2 restored in previous builds retain the responses already underway.

The scenario supplies the Lissalan allegiance, hostility or reporting, A3’s dead husks, Kurshu’s two messages, the deadline at defeat, five-foot plates, stone chambers with twenty-foot ceilings, and Krune’s finite prepared resources and withdrawal tactics. The two fugitives, the false prisoner and her Perception check are original CRPG adaptations. Other owner-requested adaptations are party-best information checks, strongest-character emerald interaction, increased horizontal clearance, whole-sanctum Dimension Door and suspension of Black Tentacles. The high-tier six-character extra-challenge profile is retained.

## Geometry, migration and performance evidence

The horizontal unit changes from 1.524 m to 1.7526 m per plan square. Ceiling height and physical spell ranges keep their original units. World travel distances between fixed grid points grow 15%, which can change when a short-range cast or charge becomes legal. Dimensional-step distance is calculated against world scale. The 15% choice is provisional pending a native traversal that establishes the smallest effective width.

On an old sanctum load the migration interprets each saved actor’s position in the old grid, validates its current location against the new floor and closed doors, and searches outward in the same mapped space if repair is necessary. Completed actor IDs are retained if another actor cannot be placed. An unresolved body stays where it was and the layout upgrade can retry. Authored fixtures rebind by identity. Native save round trips are outstanding, including dynamic bodies around the A5 sarcophagus.

| Restoration timing | 0.11.3 owner log | 0.12.0 and 0.12.1 |
|---|---|---|
| A1 cultist | Lines 11525 and 11536 record successful Fortitude saves. No frame duration accompanies either. | NOT RUN |
| A3 dead husk | No attributable frame timing | NOT RUN |
| Native spell control | No attributable frame timing | NOT RUN |

The owner log has zero harness frame-stall markers. A frame-time delta or a claim that the hitch is gone would require measurements. The supplied before image is `evidence/wall_issue.png` in the owner archive, with a local copy at `artifacts/wr03-owner-wall.png`. It shows the southwest black surface intruding on the corridor. No candidate screenshot, wall comparison, traversal or performance video was captured.

## Runtime profile and installation

**READ** from the owner’s baseline, Kingmaker 2.1.7b and UMM 0.33.0.0. The log reports the following mods loading. The candidate was built against the installed game references. No mod profile was exercised with either offline candidate.

| Mod | Version | Mod | Version |
|---|---|---|---|
| BagOfTricks | 1.16.4 | BetterVendors | 2.0.8 |
| CallOfTheWild | 1.14.4c-2.1 | CheatMenu | 1.2.3 |
| CL29 | 0.8.0 | CommunityPatch | 1.0.0 |
| CowWithHatsCustomSpellsMod | 1.4.8b | CraftMagicItems | 2.1.0 |
| EddicKingmakerRespec | 1.0 | KingdomResolution | 1.7.2 |
| KingmakerBuffPlanner | 0.6.14 | KingmakerBugfixes | 1.3.3 |
| KingmakerCutsceneSkip | 0.1.2 | KingmakerDiceRoller | 0.2.0 |
| KingmakerGunslinger | 0.0.191 | KingmakerLastAzlantiPreserver | 0.1.3 |
| KingmakerLevelUpSelectionScrollFix | 1.0.0 | KingmakerMountedCombat | 0.2.11 |
| KingmakerWakingRune | 0.11.3 | NettleCrossingRestoration | 1.1.1 |
| NoHistory | 1.0.1 | ProperFlanking2 | 1.2.3-2.1 |
| RacesUnleashed | 1.0.11 | RemoveAreaEffects | 1.0.3 |
| SkipIntro | 1.3.1 | SortByCategory | 1.1.0 |
| TweakOrTreat | 1.1.0 | ZFavoredClass | 1.3.1 |

Automatic approval review rejected `scripts/KwrRuntime.ps1 Begin -RunId wrplay3-baseline` for a guarded 0.11.3 baseline. The machine policy reserves live Kingmaker Mods transactions for the Mounted Combat guarded tooling. The Waking Rune transaction did not start. The runtime installation reads as 0.11.3, DLL SHA-256 `7933788dcc6cf6e34fd5c7c168601caa637601e4ea825a7080c46ef51be63cc4`. No owner saves, Steam mirror, registry, settings or mod configuration were changed by a playtest transaction. Previous install and backup details are in [the installation report](WR-FINAL-INSTALL.md).

There is no current-build **PLAYER**, **PARTY AI** or **HARNESS-COMMANDED** evidence. **SETUP/INJECTED** above describes an unexecuted fixture. **READ** identifies observations of existing artifacts. The native matrix, installation and visual review remain outstanding.
