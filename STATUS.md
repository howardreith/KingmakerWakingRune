# Status: The Waking Rune

**Milestone:** WR-04-R3 completion run, on top of WR-04, R1 and R2. Candidate 0.4.9 is ready for owner review.

**Branch:** `feature/wr-04-sanctum-wards` (draft PR #4). Do not merge, tag or release without owner review.

Results:
- `docs/ACCEPTANCE-WR-04-R3.md`: this cycle (prepared party, journey, routes, timing boundary, turn-based, restart, difficulty).
- `docs/ACCEPTANCE-WR-04-R2.md`, `-R1.md` and `ACCEPTANCE-WR-04.md`: earlier rows.

Rules, adaptations and migration are in `docs/WR-04-RULES.md`; the progress record is in `docs/WR-04-PROGRESS.md`.

## Candidate
- Code `94c820d`, version 0.4.9. DLL SHA-256 `7191e0cbf0e9d38230c47afecabb1794648a0d2f30780526806c2b59a8dd68d1`.
- Zip `artifacts\KingmakerWakingRune-0.4.9.zip`, SHA-256 `247f0360b1f1fb0edeb94de7bcbefcae05b0d5d60d23d1327bc718fc15d89571`. This is the package tested last and now installed. Later commits change documentation only.

## What changed in R3
- **Sustained rune routes** are timed by the effect's own native lifecycle. An effect lasting exactly five rounds succeeds; one removed 1.5 s short fails. Both were shown natively, along with a restart mid-effect and a turn-based run.
- **Rune plates** make no break-free attempt against effects their routes name. Native Entangle had let the Alacrity plate escape.
- **Alarm and glyph** are disarmed from the edge of their own areas. Previously the disarm walk set the alarm off.
- **The Quartermaster offers a native night's rest**, so a party levelled in the lodge can ready its spells.
- **Harness:** `condition` and `uncondition` apply or remove a native buff of exact length.

## Proven this cycle
- A six-character level 11 party built through the scenario's own flow, saved as reusable seeds `KWR_AUTOMATION_R3_PARTY_LEVELED`, `_SEED` and `_RESTED`. It was built on the Challenging preset, with premade builds, Coordinator recruits and the Quartermaster.
- The ordinary journey: token, A1 study and impression, alarm found and disarmed, A2 opened by the credential, all seven runes dark, glyph disarmed, hound and mist met, and a full restart mid-scenario.
- Routes:
  - Life by death spell, Harm and poison (poison by HARNESS grant);
  - Alacrity by Entangle (10 rounds) and by cold;
  - Defense by energy (HARNESS grant);
  - Armor by empowered Fireball (one caster);
  - Control by Serenity, an obtainable compulsion spell.
- Mist under ExtraDecline: three-round sleep, ended by the engine and reconciled as a wake with the streak reset.

## Not done or not proven
- **Reporter interception: not achieved.** The runner outran melee, and Neutral reporters cannot be targeted by enemy-only spells. Needs an owner ruling (below).
- Unmarked reporter at a latched A2; glyph spell-resistance penetration and evasion; Krune-summon mist bonus; hound see-invisibility, wall and tether controls.
- Trickery, UMD, summoned-damage, bludgeoning and negative-level routes on this build.
- Door audio by ear.

## Owner decisions
- **Reporters:** should a restored reporter count as the party's enemy, so that Hold Person and Magic Missile can stop it? Its AI might then fight instead of running.
- **Difficulty:** the scenario respects the selected preset (the review's recommendation, now a ruling in WR-04-RULES). Confirm, or ask for the hardcore baseline to be enforced.
- **Python 3.15** removal: still yours to authorize (below).

## Environment state
- Candidate 0.4.9 is installed, per the owner's standing preference.
- The 0.4.2 install it replaced is backed up at `C:\Dev\KingmakerWakingRuneLab\runtime-backups\wr04r3-a\mod-existing` (DLL `68db679c…`).
- Earlier installs: 0.4.1 at `runtime-backups\wr04r2-a\mod-existing`, 0.4.0 at `runtime-backups\wr04r1-final\mod-existing`.
- The local 0.4.2 zip was overwritten by a mislabelled dirty build early in this cycle and was deleted. Its DLL survives in the backup above.
- Transaction `wr04r3-a` restored and verified:
  - the profile (35 differences reverted, added files quarantined);
  - the Steam mirror (23);
  - the registry key (the turn-based option was switched on for a test and restored);
  - UMM `Params.xml`.
- No owner save was opened this cycle.

### Python, installed by accident in R1 (unchanged this cycle)
- **Cause.** On 2026-11-10 at 04:14:01 UTC, during R1, a `python - <<'EOF'` heredoc was sent through the Git Bash tool (to edit `SanctumState.cs`). It reached the Windows `python.exe` app-execution alias of the Python install manager that was already present. The manager updated itself to 26.4 (Store package `PythonSoftwareFoundation.PythonManager` 26.4.240.0) and then installed Python 3.15.2.
- **Added files:**
  - `%LOCALAPPDATA%\Python\pythoncore-3.15-64` (122.8 MB);
  - `%LOCALAPPDATA%\Python\bin` (`python`, `python3`, `python3.15`, `pythonw*`, `pip`/`pip3` shims);
  - `%LOCALAPPDATA%\Python\_cache`.
- **Registry:** `HKCU\Software\Python\PythonCore\3.15` (`Help`, `InstallPath`).
- **PATH and aliases:** unchanged. The aliases in `WindowsApps` existed before.
- **Interpreter selection:** changed. The aliases now resolve to 3.15.2 instead of offering an install.
- **Not reversed.** A narrow reversal would be the manager's own uninstall of 3.15; the manager's self-update cannot be rolled back that way. No Python has been used since; commands use verified tools (PowerShell, the repo scripts).

## How to test again
```powershell
.\scripts\Build.ps1
.\scripts\KwrRuntime.ps1 Begin -RunId <new-id> -PackagePath .\artifacts\KingmakerWakingRune-0.4.9.zip
.\scripts\KwrRuntime.ps1 Launch      # then Shot / State / Command / Click / DoubleClick / Drag / Key / Hover / Scroll, Close, Collect
.\scripts\KwrRuntime.ps1 End         # restores profile, Steam mirror, registry, Params, and the previous Mods install
```

The party seeds are under `runtime-evidence\wr04r3-a\saves`. Copy one into the profile before `Launch` (the game scans saves at start); `End` removes it.

Harness commands: `party`, `fixtures`, `units`, `state`, `wardpart`, `move`, `teleport`, `cast`, `castpoint`, `attack`, `spawnfoe`, `condition`, `uncondition`, `save`, `load`, and the R1/R2 set.

## Next exact action
Owner review of draft PR #4. Do not merge, tag or release from this branch without that review.
