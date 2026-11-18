# WR-PLAYTEST-03 — owner playtest follow-up

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

The candidate binds a directed plan and rod offer to the actual command. Its end callback records a cast, rejection, interruption, lost action, invalid target or range, unavailable resource, or native failure based on the execution state available there. It releases the matching rod offer and excludes a failed ability, metamagic, action type and unit or rounded landing point for that round. This lets a failed swift plan fall through to another option. A declined rod offer still permits the spell. Slots and actions are spent by native execution. Summon selection resolves the child ability and checks the landing footprint. The watchdog checks twice per real second and rechecks an empty actionable queue after two unpaused seconds on Krune's turn. It can dispatch a different plan, pursue a previously seen position or finish a turn.

The offline regression queries the same rejected swift plan 1,000 times without advancing the round, checks that a standard plan stays available, then advances the round and checks reconsideration. The harness command `krunerejectsummon` arms a **SETUP/INJECTED** rejection after native Init and before Start, only when the quickened V summon has an Extend offer and a living summoned Frost Giant is present. No run of that fixture is recorded. It exercises recovery even if the original profile-specific rejection no longer happens naturally.

## Owner notes and results

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

| Restoration timing | 0.11.3 owner log | 0.12.0 |
|---|---|---|
| A1 cultist | Lines 11525 and 11536 record successful Fortitude saves. No frame duration accompanies either. | NOT RUN |
| A3 dead husk | No attributable frame timing | NOT RUN |
| Native spell control | No attributable frame timing | NOT RUN |

The owner log has zero harness frame-stall markers. A frame-time delta or a claim that the hitch is gone would require measurements. The supplied before image is `evidence/wall_issue.png` in the owner archive, with a local copy at `artifacts/wr03-owner-wall.png`. It shows the southwest black surface intruding on the corridor. No candidate screenshot, wall comparison, traversal or performance video was captured.

## Runtime profile and installation

**READ** from the owner’s baseline, Kingmaker 2.1.7b and UMM 0.33.0.0. The log reports the following mods loading. The candidate was built against the installed game references. No mod profile was exercised with 0.12.0.

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
