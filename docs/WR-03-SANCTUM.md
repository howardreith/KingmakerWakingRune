# WR-03 source-to-built notes

The Sanctum of Sloth is built at runtime from the page-6 plan of *The Waking Rune*. `Sanctum/SanctumPlan.cs` is the single authority for coordinates and is unit-tested without the game. `data/sanctum_manifest.json` and `data/token_contract.json` in the mission package are design records; nothing loads them.

## Engine integration points
| Concern | Native route | Code |
|---|---|---|
| Area | Own `BlueprintArea` cloned from `DunswardTomb` scenes, parts and components cleared | `ScenarioBlueprints.CreateSanctumArea` |
| Donor contents | `SceneLoader.CreateSceneEntities` / `LoadSceneEntities` prefix drops every donor entity view (175: 51 spawners, 14 unit groups, 13 traps, 79 map objects, 16 script zones, 2 locators) | `LodgeScope.Filter` |
| Geometry | Donor static roots hidden; floors, walls, ramps and scenery generated in `OnAreaDidLoad` | `SanctumScope`, `SanctumBuilder`, `SanctumDressing` |
| Navigation | Runtime Recast scan on a mod-owned `RecastMeshObj`, forced bounds, own `ValidNavmeshArea` | `SanctumScope.ScanNavigation` |
| Arrival | `AreaEnterPoint` with child transforms selects the native `PositionCharactersByPreset` | `SanctumScope.EnsureEnterMarker` |
| Token item | `BlueprintItemEquipmentUsable` + `BlueprintAbility` with `AbilityEffectRunAction` | `RefugeTokenBlueprints` |
| Transfer | `SaveManager.CreateNewSave`, `FadeCanvas`, `Game.LoadArea(enterPoint, AutoSaveMode.None)` | `RefugeTransit` |
| Lighting | Donor `!LIGHT_SETUP` hidden by a poller because the light scene loads after `OnAreaDidLoad`; own directional fills and three point lights; ambient and light probes restored on unload | `SanctumLighting`, `SanctumScope` |

No global item, spell, mercenary or area-terrain behaviour is patched. All Harmony hooks are listed in `src/KingmakerWakingRune/Patches`; each returns early outside the Waking Rune areas or enter points.

## Stable identities
GUIDs come from `ScenarioIds.Derive(name)` (MD5 of a fixed name), so they stay the same across builds.
- Area `area.sanctum` = `f3feae225b609cb25e7dd8d1810a4260` (asset name `KWR_SanctumOfSloth`); enter point `enter.sanctum` (`KWR_Sanctum_Enter`).
- Item `item.refugetoken` (`KWR_RefugeToken`), ability `ability.refugetoken` (`KWR_RefugeTokenAbility`).
- Save-bound flags `KWR_RefugeTokenGranted`, `KWR_SanctumEntered`.

## Donor and asset provenance
- Materials cloned from the donor scene: `cyclops_dungeon_floor_tile_02_opaque_d`, `cyclops_dungeon_rock_opaque_d`, `black_cut_opaque_d`. No game asset is shipped or modified on disk.
- Scenery is Unity primitives without colliders, tinted by 2×2 textures. Figures are deliberately simple stone shapes.
- `art/RefugeToken_master.png` (512×512, SHA-256 `8799CBD1…CDA6`) and `assets/icons/RefugeToken.png` (128×128, 18,160 bytes, SHA-256 `D04275AF…3E9C`) are original, painted by `scripts/MakeRefugeTokenIcon.ps1`. Native item icons are 128×128 sprites at 66.667 ppu. The file ships as `Icons/RefugeToken.png`.

## Grid transform
One square is 5 ft = 1.524 m. `worldX = (gridX − 13) × 1.524`; `worldZ = (15.5 − gridY) × 1.524`. North on the page is +Z.

## Plan decisions
| Item | Source | Built |
|---|---|---|
| A2 direction | p.9 text says north; p.6 map draws it south | **Map followed.** A2 falls south from the ziggurat's first tier to A3. |
| Arrival | Rectangle north of the front steps | Rectangle at x 11.09–15.16, y 13.9–15.4 (cut at the built stairs). Native preset slots: main character at (13.1, 15.1), first eight others inside the rectangle, up to four more on the same floor for a very large party. Facing south, so the ziggurat is ahead. |
| Ziggurat height | Apex 20 ft; lower tiers unstated | Apex 20 ft (6.1 m); first tier **6 ft, inferred** so the front flight stays short and readable. |
| A1 ledges | Narrow on the map | Widened to about 6.5 ft so a 0.5 m agent can pass. Walkable area otherwise matches: 2,412 sampled cells on plan, 1 stray navmesh-only cell; plan-only cells are the agent-radius margin. |
| Ceilings | 20 ft, A1 30 ft | Cutaway only: walls in front of a floor stay low, walls behind rise only as far as they can without hiding the next walkable floor. No ceiling is rendered. |
| Dome rim, A3 plates | Dome over A4; plates wall-mounted | The A4 dome rim and the tipped plates on plinths exist only because of the cutaway camera. The rim is visible as a grey arc across the room. |
| Doors | A2 Sihedron door; A4 approach | **Open, passable and inert.** Leaves are held open, with no door entities. |
| Camera | North-up map | `CameraRotation` 0 looks south (so the arrival view faces the ziggurat). The native local map is therefore rotated 180° from page 6. |

## Terrain
`BlueprintArea.LootSetting` drives Favored Terrain. The sanctum uses `Cyclops`, which the native `FavoriteTerrainUnderground` feature matches and Urban, Forest, Plains, Highlands and First World do not. `IsIndoors` is inherited from the donor (true). The lodge is `RichHuman`, which matches both Underground and Urban, so it is **not** a control for Underground. Observed: `ACCEPTANCE-WR-03.md` S01.

## Token semantics
One mod-owned usable item, one charge, not chargeable. Native use does not consume it (`SpendCharges=false`); `RefugeToken.Consume()` removes it after the arrival commit. Free action, personal range, no UMD or class gates. The action routes through `RefugeTransit.Request`, the single entry for belt use and inventory Use.

Rejected, with a warning and no consumption: already departed, travel in progress, not in the lodge, destination missing, in combat, in conversation, character build open, briefing incomplete. A successful request closes native windows, writes a new `Before the Sanctum of Sloth` save (the game appends a number on a name collision, so it never overwrites), fades out, loads the area, and on arrival consumes the token, sets `KWR_SanctumEntered` and fades in. Runs on unscaled time so paused UI cannot stall it. If arrival does not happen within 240 s the screen fades back in and the token stays; loading any save cancels an in-flight transfer.

No functional return route exists. Teleport VFX is **omitted**.

## Migration
`RefugeToken.EnsureGranted` runs once per save in the lodge when `KWR_RefugeTokenGranted` and `KWR_SanctumEntered` are unset and none is owned. It places the token in the main character's belt, using a free slot if there is one; if the belt is full it displaces the last slot's stack to the stash. It sets the granted flag, so a token later moved to inventory stays there and is never regranted. Recruiting adds no token.

## Known limitations
- The token can be sold to a vendor for 0 gp (native behaviour; by code reading only, not tested). The tooltip shows native DC 11 and caster level 1 lines.
- Rest and camping are disabled in the sanctum so a donor rest encounter cannot introduce enemies.
- The turn-based grid graph is not resized (no combat in this milestone).
