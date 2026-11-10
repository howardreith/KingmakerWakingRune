# Status — The Waking Rune

**Milestone:** WR-04-R2 (review repairs for the R1 candidate) on top of WR-04 and WR-04-R1; candidate 0.4.2 ready for owner review.
**Branch:** `feature/wr-04-sanctum-wards` (draft PR #4). Do not merge, tag or release without owner review.
Results:
- `docs/ACCEPTANCE-WR-04-R2.md`: this cycle, answering the review point by point.
- `docs/ACCEPTANCE-WR-04-R1.md` and `docs/ACCEPTANCE-WR-04.md`: earlier rows.

Rules, adaptations and migration are in `docs/WR-04-RULES.md`; the progress record is in `docs/WR-04-PROGRESS.md`.

## Candidate
- Code `44a4b78`, version 0.4.2. DLL SHA-256 `68db679cd292a19ee57ef3fc65b8c324428670a07c7daf56cab3d97e1eefefdc`.
- Zip `artifacts\KingmakerWakingRune-0.4.2.zip`, SHA-256 `8f9bd28621af8df3a22c3af594472630f9ebf4b734c3e36a0136bc30f78faa06`: the package tested last and now installed. Later commits change documentation only.

## What changed in R2
- Scenario state is created only when the sanctum loads; ordinary turn-based campaigns are untouched (native regression in the owner's stock campaign).
- Long work is refused while a rune attempt is timed, and sustained routes credit only observed duration.
- Rune plates deny saves by what an effect does as well as by descriptor (Harm against Life, natively).
- Mist sleep:
  - its lifecycle is traced;
  - any end of the sleep is a wake that resets the streak;
  - a sleeper rolls no save.
  - The R1 anomaly is the game's `ExtraDecline` three-round cap on negative effects.
- Restored cultists match GMG p.278 except AC, which carries the game's global difficulty modifier: human, printed scores, Con 12, hp 16.
- The hound's bite is a native attack roll from an invisible attacker.
- A spent feeder buff is removed.

## Not done or not proven
- Integrated six-character Subtier 10–11 walkthrough: no valid level 10–11 party save exists. The six-character WR-03 test party has one level 11 character.
- Armor success (attempted, 28/35); Control by an obtainable route; poison summon, entangle, cold and energy routes.
- Runner interception; glyph penetration, evasion and disarm; alarm disarm; Krune-summon mist bonus natively.
- Door audio by ear.

Owner decisions:
- Should the mist's sleep ignore the `ExtraDecline` preset's three-round cap? Today it obeys it.
- Is the global `DifficultOpponent` AC modifier on restored cultists acceptable? It is left as the game applies it.

## Environment state
- Candidate 0.4.2 is installed per the owner's standing preference. The 0.4.1 install it replaced is backed up at `C:\Dev\KingmakerWakingRuneLab\runtime-backups\wr04r2-a\mod-existing`, and the 0.4.0 install at `runtime-backups\wr04r1-final\mod-existing`.
- Transaction `wr04r2-a` restored and verified the profile (20 differences reverted), the Steam mirror (6), the registry key (the turn-based option was changed during testing and restored) and UMM `Params.xml`. The owner's `Manual_303` is byte-identical to its snapshot.

### Python, installed by accident in R1
- **Cause.** On 2026-11-10 at 04:14:01 UTC, during R1, a `python - <<'EOF'` heredoc sent through the Git Bash tool (to edit `SanctumState.cs`) reached the Windows `python.exe` app-execution alias of the already-present Python install manager. The manager first updated itself to 26.4 (Store package `PythonSoftwareFoundation.PythonManager` 26.4.240.0) and then installed Python 3.15.2.
- **Added files.**
  - `%LOCALAPPDATA%\Python\pythoncore-3.15-64` (122.8 MB, created 23:14:13 local).
  - `%LOCALAPPDATA%\Python\bin`: `python`, `python3`, `python3.15`, `pythonw*` and `pip`/`pip3` shims, created 23:14:15.
  - `%LOCALAPPDATA%\Python\_cache`, created 23:14:01.
- **Registry.** It added the registration key `HKCU\Software\Python\PythonCore\3.15` (`Help`, `InstallPath`).
- **PATH and aliases.** Unchanged: `%LOCALAPPDATA%\Python\bin` is not on the user or machine PATH, the machine PATH has no Python entry, and the aliases in `WindowsApps` (`python`, `python3`, `py`, `pymanager`, `pywmanager`) are the manager's own and existed before.
- **Interpreter selection.** Changed: those aliases now resolve to the installed 3.15.2 instead of offering an install.
- **Not reversed.** Nothing has been removed without the owner's authority. A narrow reversal would be the manager's own uninstall of 3.15 (removing the `pythoncore-3.15-64` runtime and its registration). The manager's self-update cannot be rolled back that way.
- No Python was used afterwards. Commands use verified tools (PowerShell, the repo scripts).

## How to test again
```powershell
.\scripts\Build.ps1
.\scripts\KwrRuntime.ps1 Begin -RunId <new-id> -PackagePath .\artifacts\KingmakerWakingRune-0.4.2.zip
.\scripts\KwrRuntime.ps1 Launch      # then Shot / State / Command / Click / DoubleClick / Drag / Key / Hover / Scroll, Close, Collect
.\scripts\KwrRuntime.ps1 End         # restores profile, Steam mirror, registry, Params, and the previous Mods install
```
New harness commands: `wardpart` (is the part present, read without creating it) and `spawnfoe <unit guid> <metres>`. The R1 commands are still there: `items`, `units`, `hitdoor`, `damagekrune`, `grantfeeder`, `shield`, `move`. Disposable test saves from this run are under `runtime-evidence\wr04r2-a\saves`. Copy one into the profile after `Begin`; `End` removes it.

## Next exact action
Owner review of draft PR #4. Do not merge, tag or release from this branch without that review.
