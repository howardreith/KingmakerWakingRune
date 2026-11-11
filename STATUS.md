# Status: The Waking Rune

**Milestone:** WR-04-R5 corrections, on top of WR-04, R1, R2, R3 and R4. Candidate 0.5.15 is ready for owner review.

**Branch:** `feature/wr-04-sanctum-wards` (draft PR #4). Do not merge, tag or release without owner review.

Results:
- `docs/ACCEPTANCE-WR-04-R5.md`: this cycle.
- `docs/WR-04-LEDGER.md`: the cumulative ledger of the whole WR-04 scope.
- `docs/ACCEPTANCE-WR-04-R4.md`, `-R3.md`, `-R2.md`, `-R1.md` and `ACCEPTANCE-WR-04.md`: earlier rows.

Rules, adaptations and migration are in `docs/WR-04-RULES.md`; the progress record is in `docs/WR-04-PROGRESS.md`.

## Candidate
- Version 0.5.15, commit `0a7407b`. Gameplay code last changed in 0.5.11 (`4fbef87`); 0.5.12 to 0.5.15 changed harness commands only.
- DLL SHA-256 `462a2ab28ea532a8d5313afd91285b267ff8dfe2d912f36861db14eccb23bced`.
- Zip `artifacts\KingmakerWakingRune-0.5.15.zip`, SHA-256 `b80c26174ed86aa07bd719f4203e29d0230ca74275744d83cfba917bfb209ec8`. This is the package tested last and now installed. Later commits change documentation only, plus a byte-order mark removed from a harness file; the rebuilt DLL is byte-identical.
- 151 unit tests pass.

## What changed in R5
- **Legacy reporters.** A reporter an older build restored as a neutral is migrated before the control test, once, and only when no charm, domination or other faction effect holds it. Its group's cached hostility is rebuilt at the same time (a defect found in the run).
- **Departure retries.** Readiness is a record of each member when the rest finished. A retry after a failed checkpoint re-rests if anyone joined or left, or if prepared slots, resources or health changed, and the notice says what changed. An unchanged retry departs without a second rest.
- **Summon orders.**
  - Saved by summon, summoner and rune identity, checked again on every load, then resumed or removed (with the attack in hand and the manual target).
  - Ownership is rechecked on every renewal: the summon must not be hostile to its summoner, under a faction effect, or confused or panicked.
  - Two defects found in the run are fixed. After a reload, a summon's unsaved group id made a valid order look like changed allegiance. And redirecting an attacking summon renewed its own interruption forever, hanging the game.
- **Blocked turn-based runner.** A move that gets nowhere ends the runner's turn.
- **Build.** An existing clean package is never replaced unless `-ReplacePackage` is passed with the owner's authorization.

## Proven this cycle
In-world mouse input failed this run, so these are "with setup" (see below):
- The legacy neutral runner from a real 0.4.9 save became hostile, could be targeted, resumed at leg 3 and reported only on arrival.
- Departure: a changed preparation re-rested with a notice naming the member; the unchanged retry departed without resting.
- A summon order at Control, saved at 42/50 and reloaded after a full game restart: it was found valid, resumed, and finished the rune without counting damage twice. Recall after a reload worked. A confused summon was released while the other kept its order. Expiry ended an order. Redirecting no longer hangs.
- Turn-based Hold Person interception (held in round 1, killed in round 2). An unmarked reporter stopped by the latched A2. A blocked runner ending its turns.
- The Trickery route (35) and the Use Magic Device route (41).

## Not done or not proven
- A controlled legacy reporter (it cannot arise in an older save) and a dominated summon.
- A recruit joining between departure attempts.
- A fresh new game's shop.
- The open ledger rows: glyph spell resistance, evasion and dispel; hound checks; Defense's single bludgeoning hit; and the rest in `WR-04-LEDGER.md`.
- Door audio by ear.

## Owner decisions
- None blocking.
- Python 3.15 removal remains yours to authorize (below).
- In-world mouse input failed this run. If it fails for you as well, that is worth knowing before the next run.

## Environment state
- Candidate 0.5.15 is installed, per the owner's standing preference.
- The 0.5.5 install it replaced is backed up at `C:\Dev\KingmakerWakingRuneLab\runtime-backups\wr04r5-a\mod-existing` (DLL `56bb6091…`).
- Earlier installs:
  - 0.4.9 at `runtime-backups\wr04r4-a\mod-existing`;
  - 0.4.2 at `wr04r3-a`;
  - 0.4.1 at `wr04r2-a`;
  - 0.4.0 at `wr04r1-final`.
- Transaction `wr04r5-a` was restored and verified:
  - the profile: 22 differences reverted; added test saves, autosaves and the pre-departure checkpoint quarantined;
  - the Steam mirror: 6;
  - the registry key, which restores the turn-based setting the harness switched on;
  - UMM `Params.xml`.
- One hung game process (0.5.10) was terminated by `Close`; it was the run's own process only.
- No owner save was opened.

### Python, installed by accident in R1
- **Cause.** On 2026-11-10 at 04:14:01 UTC, a `python - <<'EOF'` heredoc reached the Python install manager's `python.exe` alias, which updated itself and installed Python 3.15.2.
- **Added files:** `%LOCALAPPDATA%\Python\pythoncore-3.15-64`, `%LOCALAPPDATA%\Python\bin` and `%LOCALAPPDATA%\Python\_cache`.
- **Registry:** `HKCU\Software\Python\PythonCore\3.15`.
- **PATH and aliases:** unchanged.
- **Use since:** once in R4 (`python3 --version`), and not in R5.
- **Not reversed.** A narrow reversal would be the manager's own uninstall of 3.15.

## How to test again
```powershell
.\scripts\Build.ps1                  # refuses to replace an existing clean package
.\scripts\KwrRuntime.ps1 Begin -RunId <new-id> -PackagePath .\artifacts\KingmakerWakingRune-0.5.15.zip
.\scripts\KwrRuntime.ps1 Launch      # then Shot / Command / Click / Key / Fault ..., Close, Collect
.\scripts\KwrRuntime.ps1 End -KeepInstalled
```

Seeds are under `runtime-evidence\wr04r3-a\saves`, `wr04r4-a\saves` and `wr04r5-a\saves`. Copy one into the profile before `Launch`: the game only sees saves present at start.

Harness commands:
- Read-only: `party`, `units`, `fixtures`, `state`, `stock`, `slots <member>`, `cursor`, `cantarget <member> <ability> <target>`, `screen <target>`, `turn`.
- Labelled SETUP: `move`, `teleport`, `look` (camera only), `perform <fixture> <key> [member]`, `spell <member> <spell or variant> <target | x y>`, `token`, `answer yes|no`, `memorize <member> <spell> <level>`, `tbm`, `endturn`, `attack`, `save`, `load`.
- INJECTED: `cast` and `castpoint` (granted abilities), `condition`, `advance`, faults.

Targets are a fixture id, `party:<i>`, or `body:<statue>`; members are `<i>` or `pet:<owner i>`.

## Next exact action
Owner review of draft PR #4. Do not merge, tag or release from this branch without that review.
