# WR-02 implementation notes

## Source package
Only `WakingRune_WR02_Claude_Package.zip` (version 1) was present in the lab, so the v1 corrections in the execution prompt apply:
- Portrait folders for Sheila and Ambrus are exchanged in v1. `scripts/ProcessPortraits.ps1` maps each identity to the folder that actually holds that person and crops all three sizes independently from `Original.png`, never changing aspect ratio. Output lives in `assets/portraits/<Id>/` and ships in the mod's `Portraits` folder. Originals are not shipped.
- "Heidm arch" is corrected to "Heidmarch". All other script text is unchanged.

## Native systems used
| Need | Mechanism |
|---|---|
| Portraits | Mod-owned `BlueprintPortrait` with `PortraitData(null, small, medium, full)` sprites loaded from the mod folder; assigned to the unit blueprint's `m_Portrait`. The player's custom portrait folder is untouched. |
| NPCs | Presentation-only clones of base-game units, spawned with `EntityCreator.SpawnUnit` into the area's saved state; `KWR_LodgeCastSetupDone` records it. |
| Briefing | `BlueprintDialog`/`BlueprintCue`/`BlueprintAnswer` blueprints with stable derived GUIDs; started by `DialogController.StartDialogWithUnit` once no loading, mode or chargen window is active. |
| Stage direction | `NoSpeaker` cue; its `OnShow` scrolls the dialogue camera to the masked figure and plays the native `DialogBow` gesture. |
| Follow-ups | `DialogOnClick` on Sheila and Ambrus, gated on `KWR_BriefingComplete`; reuses the hub answers. Starts with Sheila's "Do you have any questions?", a substring of her scripted line. |
| Shop | `BlueprintSharedVendorTable` + `AddSharedVendor`; stock persists in `Player.SharedVendorTables`. Native trade window; native prices. |
| Recruitment | `Player.CreateCustomCompanion(onSuccess, xp)` with the level-11 XP from `BlueprintRoot.Progression.XPTable`. No global state; cancelling never reaches the callback. |
| Party | Recruits join with `Player.AddCompanion` while a slot is free; `ShowPartySelection` (native Group Manager) manages the active six. |
| XP | `AdvanceExperienceTo(XPTable.GetBonus(11))` once for the protagonist, directly on the unit (never party XP). |
| Treasury | Delta to exactly 450,000 gp once per save, after native chargen's class gold. |

## Decisions
- **Two authored service lines** are new (not in the script), so the Quartermaster and Coordinator show a speaker and portrait: "The Society's stores are open to the team. What do you need?" and "Need more hands for this assignment? The Society will sponsor them." Their answer labels are functional ("Show me what you have.", etc.).
- **Recruit starting gold** is refunded immediately: native chargen gives custom companions class starting gold (`LevelUpHelper.AddStartingItems`), and recruitment must not change the treasury. Native starting gear is kept.
- **Warding animation** is a native cast handle (`CastStyle=Omni`, `NoCast`) held for 240 s and re-issued by a 2 s poll. No ability, buff, combat log entry or game event is involved. Agents' facing is set at spawn.
- **Murmuring** uses the game's own quiet positional chant events (`Arcane_Enchantment_Low_Male/Female`, in the always-loaded `Main` bank): one agent at a time, every 7–14 s, skipped during dialogue and other modes. Non-zero Wwise playing IDs are logged; audibility was not measured (no audio capture in the harness).
- **Interrupted briefing** restarts from its first cue on the next load; saving during dialogue is natively disabled, so no state can be half-done.
- **WR-01 saves** (no WR-02 flags) receive the cast, XP, treasury and briefing once on their next lodge load, via the same flag-guarded path; `KWR_LodgeCastSetupDone` keeps the existing Sheila and adds only the missing NPCs.

## Model choices
Ambrus = `CommonBaron` (stern armored veteran), Masked Figure = `Cultist_Conjurer` (red hooded robe with the face hidden), Quartermaster = `BrevoyOfficer`, Coordinator = `KingdomAsCompanion_AndoranNoble`, agents = `DisguisedLanternKing` ×2, `LastwallInquisitor` ×2, `AdventurerRogue`, `Druid`, Sheila unchanged. Models are generic; no bespoke art or animation.
