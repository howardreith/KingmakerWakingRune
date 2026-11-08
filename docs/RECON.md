# WR-01 reconnaissance (installed Kingmaker 2.1.7b)

Verified against the installed `Assembly-CSharp.dll` (decompiled with ilspycmd into the lab, never committed)
and live runtime dumps. Only facts used by the implementation are recorded.

## Environment
| Item | Verified value |
|---|---|
| Game | Pathfinder: Kingmaker 2.1.7b (UMM detects `2.1.7b`), Steam App 640820, buildid 6757524 |
| UMM | 0.33.2 (doorstop `winhttp.dll`), entry `MainMenu.Start:After` |
| Harmony | `0Harmony12.dll` 1.2.0.1 (legacy API used by this mod); UMM also ships Harmony 2.3.6 |
| Build | MSBuild 17.14 (VS 2022 Build Tools), .NET Framework 4.7, C# 7.3 |
| Saves/profile | `%USERPROFILE%\AppData\LocalLow\Owlcat Games\Pathfinder Kingmaker`, registry `HKCU\Software\Owlcat Games\Pathfinder Kingmaker` |
| Steam Cloud | client in offline mode, App 640820 `Sync Disabled` (checked before every launch) |

## Integration points
| Concern | Native seam | Use |
|---|---|---|
| Blueprint registration | `LibraryScriptableObject.LoadDictionary` (called once by `ResourcesLibrary.InitializeLibraryAsync` at boot, before any save is read; no hyper-blueprint files in this build) | Postfix registers mod blueprints in `BlueprintsByAssetId` + `GetAllBlueprints()` |
| Strings | `LocalizationManager.LoadPack(Locale)` (private static) | Postfix adds mod keys to every loaded pack |
| Adventure selector | `NewGameWinPhaseStory.Initialize` builds one `CharBuildSelectorStory` widget per `BlueprintRoot.NewGameSettings.StoryList` entry; `Dispose` destroys them | Append one `StoryEntity` to the list once; widgets/handlers stay native |
| Story → preset | `NewGameWinPhaseStory.SetStory` and `NewGameWindowController.SetStory` resolve `DlcRoot.Get(type).StartGamePreset` and set `Game.NewGamePreset` | Out-of-range `DlcType` (0x4B57) resolved by a `DlcRoot.Get` postfix to mod `DlcData` (enabled, no import) |
| Why not `DlcType.None` | `SceneLoader` main-menu immediate start does `StoryList.Single(s => s.Type == preset.DlcCampaign)` | A second `None` entry would throw |
| Difficulty / chargen | `NewGameWindowController.ToNextPhase` → Difficulty → Pregen → save injector (skips: no DLC match) → boon (skips: not Endless) → `MainMenu.HandleStartChargen` → `CharacterBuildController` | Unchanged |
| Chargen finalization | `CharacterBuildController.Commit` → `MainMenu.HandleLevelUpComplete` → `EnterGame(Game.LoadNewGame)` | Prefix on `Game.LoadNewGame(preset, import)` logs only |
| Game start | `Game.LoadNewGame(preset)`: `Player.Initialize`, copy game-bound settings (difficulty) from defaults, `preset.SetupState()` (sets `Player.StartPreset`, unlocks `UnlockedFlags`), `LoadArea(preset.EnterPoint)`, then `BlueprintRoot.StartGameActions` + `preset.StartGameActions` | Mod preset: `CharGen=true`, XP 0, no companions/quests/kingdom/autosave, empty actions, session flag in `UnlockedFlags` |
| Abandon | `NewGameWindowController.OnHide` runs only on Esc/Back/close (`EnterGameCoroutine` disposes, never hides) | Postfix restores `BlueprintRoot.NewGamePreset` |
| Save load | `Game.LoadGame(SaveInfo)` (all UI load paths) | Prefix clears any pending selection |
| Area scenes | `BlueprintAreaPart`: static scene (geometry), dynamic/mechanics scene (entity views → persistent entities on first visit), light/audio variants. `AreaPersistentState.MainState` is keyed by the mechanics scene name | Mod area copies donor static/light/audio; mechanics = runtime-created empty scene |
| Mechanics scene supply | `SceneLoader.LoadSceneCoroutine` skips scenes already loaded | Prefix creates `KWR_MagnimarLodge_Mechanics` via `SceneManager.CreateScene` with an `AreaEnterPoint` |
| Party placement | `Player.MoveCharacters` → `AreaEnterPoint.FindAreaEnterPointOnScene` → preset placement clamps to nearest navmesh node; `UnitsPlacer.MovePartyToNavmesh` at end of `OnAreaLoaded` | Enter point with one child slot |
| Area-loaded hook | `ISceneHandler.OnAreaDidLoad` (before `EntityCreator.Tick`) | Sheila spawn/verify |
| NPC spawn/persistence | `EntityCreationController.SpawnUnit(bp, pos, rot, LoadedAreaState.MainState)`; saved with the area state, view recreated on load | Spawn once, guarded by save-bound setup flag |
| Save marker | `Player.UnlockableFlags` (`BlueprintUnlockableFlag`, value = schema version) | `KWR_WakingRuneSession` (v1), `KWR_LodgeInitialSetupDone` |

| Main-campaign start actions | `BlueprintRoot.StartGameActions` (36 actions: companion romance flags, camping encounters) runs for every preset | Skipped (prefix on `ActionList.Run`) only when `Player.StartPreset` is the Waking Rune preset |
| Save header campaign | `SaveManager` writes `SaveInfo.CampaignDLC = StartPreset.DlcCampaign`; DLC importers list `CampaignDLC == None` saves as main-story saves | Preset `DlcCampaign = 0x4B57`, so Waking Rune saves are never offered as main-story imports |

## Donor choices (verified in game)
| Need | Choice | Why |
|---|---|---|
| Room | `CapitalLibrary` area (`e100798af67576b459fb345cdd4e77ce`): `CapitalLibrary_Static` + `_Light` + `_Audio_Day` + `_Mechanics` | Self-contained area with its own navmesh (`AstarPath`) and `LocalMapArea` in the mechanics scene; reading desk, bookshelves, candelabra. `RestovThroneRoom_JamandiOffice` was tried first: it is an area *part* without its own navmesh (`GetNearestNode` NRE). |
| Donor isolation | All 113 entity views of `CapitalLibrary_Mechanics` (57 map objects incl. doors/exits/loot, 2 unit spawners, 54 waypoints) are withheld from `CreateSceneEntities`/`LoadSceneEntities` for the lodge only; native "destroy unattached views" removes them | No donor entity is ever created, so none of its logic runs. The donor area's blueprint components (area scripts) are not copied. Donor blueprints are never mutated. |
| Entry | Donor marker `CapitalLibrary_Enter` (`e2a1da2a…`) pose at (-21.9, 0.1, -0.7), yaw 89 | Mod enter point placed at that pose right before party placement |
| Sheila model | `AdventurerRanger` (`66f9f99225680ea4dac138bbb54b9c28`, generic neutral human female adventurer, fixed prefab) | Cloned to `KWR_SheilaHeidmarch` with no components/facts, `FactionNeutrals`, name "Sheila Heidmarch"; spawned 6 m in front of the entry at the reading desk (-16.2, 0, -0.6) |

## Runtime notes
- Kingmaker hides NPCs outside the party's line of sight; Sheila is not drawn while the hero stands behind a bookcase (native fog-of-war behavior).
- The game mirrors saves through Steam remote storage (`userdata/<id>/640820/remote`) and restores missing local saves from it at launch. Test harnesses must snapshot/restore that mirror as well as the profile.
