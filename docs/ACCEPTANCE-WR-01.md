# WR-01 acceptance report

**Candidate:** commit `db3352e` · package `KingmakerWakingRune-0.1.0.zip` SHA-256 `0b8b9dca74f35c773f2835cb77cace033c02bd4bc507066671c0db6484703b9b` ·
DLL SHA-256 `72a011ddec696ef9688b328efbddb5347c21ebb86aa1494409c0c615c0319bb7`.
**Environment:** Kingmaker 2.1.7b (Steam, cloud sync disabled/offline), UMM 0.33.2, Harmony 1.2.0.1 API; render
resolution 1280×720 borderless (owner's saved setting) on a 2560×1440 display; plus 1024×768.
**Method:** real mouse/keyboard input to the owned game process (`scripts/KwrRuntime.ps1`), screenshots inspected,
mod and game logs. Every run was a guarded transaction; profile, Steam save mirror, registry, UMM params and Mods
were restored and verified afterwards.

Evidence lives outside the repo (screenshots contain proprietary game imagery):
`C:\Dev\KingmakerWakingRuneLab\runtime-evidence\r03-final-db3352e\` (final candidate) and
`...\r04-res720-db3352e\` (resolution check). Earlier runs `r01-recon`, `r02-accept` are development history.

| ID | Result | Evidence (r03 unless noted) |
|---|---|---|
| A1 | **PASS** | `*-A1-open1.png`, `*-A1-open4-waking-selected.png`: Main Story, Varnhold's Lot, Beneath the Stolen Lands, then **The Waking Rune**, all visible without scrolling, native styling/highlight, Back/Next clear. Four opens in one process each log `Scenario list frame grown by 62` from native values (no accumulation, no duplicate entries, entry still clickable). 1024×768: r04 `*-res1024-selector.png`, `*-res1024-waking-selected.png`. |
| A2 | **PASS** | `*-A2-difficulty-custom.png` (preset → Custom after a changed option), native chargen through Portrait/Race/Class/Skills/Abilities/Character (`*-A2-appearance.png`, `*-A2-feats.png`, `*-A2-name-alignment.png`), `*-A2-total.png` (Ysolde Kestrel, human female Barbarian 1, CG, red hair, chosen portrait), `*-A2-loading.png` (native loading screen), `*-A2-lodge-arrival.png`. Log: `Character creation complete; starting Waking Rune: player='Ysolde Kestrel' … difficulty=stats=Improvement …` and `Skipped main-campaign BlueprintRoot.StartGameActions`. Inventory/sheet match creation choices. |
| A3 | **PASS** | `*-A3-sheila-nameplate.png` (native nameplate "Sheila Heidmarch", non-hostile outline, idle at desk), `*-A3-walked-around-desk.png`, `*-A3-walked-aisle.png`, `*-A3-walk-doorway-blocked.png` (ordering through the open doorway stops at the threshold landing; no area transition exists), `*-A3-inventory.png`, `*-A3-charsheet.png`, `*-A3-paused.png`, `*-A3-escmenu.png`. Log: `Donor entity views suppressed … count=113` — no donor NPCs, doors, loot or exits are created. |
| A4 | **PASS** | Manual save `KWR_AUTOMATION_A4_FINAL` (`*-A4-save-named.png`, area name shown natively as "Magnimar Lodge — Sheila's Study"), full process exit and Steam relaunch, `*-A4-load-list-after-restart.png`, `*-A4-restored-sheila.png`. Log after reload: `decision=Restored (setup previously recorded; one Sheila present)`, same player id `fd278611…`, moved position (-17.4, 0.1, -7.8), same Sheila id `fe128d49…`, difficulty `stats=Improvement … deathDoor=False`. No respawn, no chargen. |
| A5 | **PASS** | Back at difficulty → scenario (`*-A5-back-at-difficulty.png`); chargen cancel → native "Are you sure?" → New Game window (`*-A5-chargen-cancel-dialog.png`); closing the window logs `Pending Waking Rune selection cleared (new game flow abandoned)`. Stock Main Story then starts natively (`*-A7-main-hall.png`, no mod log activity); save loads log `Loading save … area=…` with selection cleared. |
| A6 | **PASS** | Return to menu from Ysolde's session, new Waking Rune game with Melaku (`*-A6-second-character-lodge.png`): new `gameId 90023602…`, new player id, `decision=CreateInitialSetup`, exactly one new Sheila id `ce5b73f6…`. |
| A7 | **PASS** | Main Story start (`*-A7-main-hall.png`), Varnhold's Lot native pregens + its own import prompt + chargen/level-up (`*-A7-varnhold-*.png`), Beneath the Stolen Lands native settings, its own DLC import prompt, start in Tenebrous Depths (`*-A7-btsl-started.png`), and a disposable stock autosave load (`*-A7-stock-save-loaded.png`, log `Loading save 'Autosave1' area=JamandisMansionThroneroom`). All three are owned; no entitlement checks were touched. Varnhold's Lot start was taken to its native level-up screen and cancelled (not played into the world). |
| A8 | **PASS** | Mod folder removed, game relaunched: three-entry native selector (`*-A8-selector-mod-absent.png`), zero `KingmakerWakingRune` log lines, stock Main Story start (`*-A8-mainstory-start-mod-absent.png`) and stock save load into the banquet hall (`*-A8-stock-world-mod-absent.png`). |

Unit tests: 8/8 (`LodgePolicy`: stock area ignored, first entry creates once, reload verifies only, missing/duplicate Sheila reported not repaired, missing marker / unsupported version rejected).

## Limitations
- Sheila uses the generic `AdventurerRanger` model; no portrait (nameplate/inspection show the name).
- The room is the base-game Capital Library: the whole library interior is walkable; its exits are inert because no
  donor map objects exist. The local map is the library's.
- Like any NPC in Kingmaker, Sheila is hidden by fog of war while out of the party's line of sight.
- Saves from this mod require the mod. Two test saves from build `d85bfc5` (before the campaign-type fix) recorded
  `CampaignDLC=None`; they were disposable and have been quarantined.
