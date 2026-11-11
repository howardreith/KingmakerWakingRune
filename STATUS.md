# Status: The Waking Rune

**Milestone:** WR-04-R4 player-facing gaps, on top of WR-04, R1, R2 and R3. Candidate 0.5.5 is ready for owner review.

**Branch:** `feature/wr-04-sanctum-wards` (draft PR #4). Do not merge, tag or release without owner review.

Results:
- `docs/ACCEPTANCE-WR-04-R4.md`: this cycle (reporters, summon orders, departure readiness, shop, inspection).
- `docs/ACCEPTANCE-WR-04-R3.md`, `-R2.md`, `-R1.md` and `ACCEPTANCE-WR-04.md`: earlier rows.

Rules, adaptations and migration are in `docs/WR-04-RULES.md`; the progress record is in `docs/WR-04-PROGRESS.md`.

## Candidate
- Code `5e05519`, version 0.5.5. DLL SHA-256 `56bb6091267a5f657fe722460d8821c02723768e8c803a637c14cf486c582587`.
- Zip `artifacts\KingmakerWakingRune-0.5.5.zip`, SHA-256 `a3a524fe1671e51e33e71886e342019237f947aae56eceb82cd1b95409b445ef`. This is the package tested last and now installed. Later commits change documentation only.

## What changed in R4
- **Reporters** are hostile and can be targeted, but flee with their own AI off:
  - flight continues in combat;
  - in turn-based combat they move only in their own turn;
  - they yield to charm, domination, confusion and panic.
- **Summon orders:** any active rune offers "Direct my summoned creatures to attack this rune", and "Call the summoned creatures off" once an order exists.
- **Departure** includes a native night's rest:
  - a notice first, then spellbook review and a warning listing empty prepared slots;
  - the rest happens once, before the checkpoint, and cancelling spends nothing.
- **Shop:** fauchards and ten more exotic families, +3 kama and nunchaku, six Stone to Flesh scrolls, two +3 daggers. Existing lodges reconcile natively.
- **Inspection:** a magnifying-glass cursor on scenery, and the deeper rune reading told as discoveries, with no numbers.
- **Fixture fix:** ability damage on a fixture no longer kills it. One point of native poison had killed the Life plate.
- **Build:** a working-tree build writes its own `-dirty` zip and never replaces a tested package.

## Proven this cycle
- A bought Stone to Flesh scroll restored the marked reporter. Hold Person, cast normally, stopped it, and no messenger was raised.
- An unhindered reporter fled in real time and in turn-based combat, opened A2 with its own brand, and reported only on arrival. That held across a mid-route save and reload.
- A summoned Axiomite, ordered at Control, disabled it by actual summoned damage. Calling the summons off worked, and a monitor lizard's native poison rider reached the Life rule.
- A party with empty slots departed: the warning appeared, Summon Monster VI chosen during review was ready on arrival, and no sanctum load rested.
- Shop stock reconciled on two existing lodge saves (before and after R3's purchases), purchases went through, and nothing refilled on reload.
- The magnifying-glass cursor appears on statues and plates, while spells keep their cast cursor.

## Not done or not proven
- A fresh new game's shop; turn-based interception; an unmarked reporter at a latched A2.
- The monitor lizard's poison cannot reach the Life threshold by itself (it restacks one buff); the route is proven, not a disable by it.
- From R3: glyph spell-resistance and evasion; Krune-summon mist bonus; hound see-invisibility, wall and tether; Trickery, UMD, bludgeoning and negative-level routes.
- Door audio by ear.

## Owner decisions
- None blocking. The reporter ruling (hostile, fleeing) followed the review.
- Python 3.15 removal remains yours to authorize (below).

## Environment state
- Candidate 0.5.5 is installed, per the owner's standing preference.
- The 0.4.9 install it replaced is backed up at `C:\Dev\KingmakerWakingRuneLab\runtime-backups\wr04r4-a\mod-existing` (DLL `7191e0cb…`).
- Earlier installs:
  - 0.4.2 at `runtime-backups\wr04r3-a\mod-existing`;
  - 0.4.1 at `runtime-backups\wr04r2-a\mod-existing`;
  - 0.4.0 at `runtime-backups\wr04r1-final\mod-existing`.
- **The 0.4.9 zip was overwritten** by an unfinished working-tree build early in R4 and was rebuilt from the installed 0.4.9 files.
  - Its DLL is the tested `7191e0cb…`, but the zip's hash is now `10a74a99…`, not R3's `247f0360…`.
  - The build script no longer allows this.
- Transaction `wr04r4-a` restored and verified:
  - the profile (26 differences reverted, added files quarantined);
  - the Steam mirror (12);
  - the registry key (the turn-based option was switched on for a test and restored);
  - UMM `Params.xml`.
- No owner save was opened this cycle.

### Python, installed by accident in R1
- **Cause.** On 2026-11-10 at 04:14:01 UTC, during R1, a `python - <<'EOF'` heredoc was sent through the Git Bash tool. It reached the Windows `python.exe` alias of the Python install manager that was already present. The manager updated itself to 26.4 and then installed Python 3.15.2.
- **Added files:** `%LOCALAPPDATA%\Python\pythoncore-3.15-64`, `%LOCALAPPDATA%\Python\bin` and `%LOCALAPPDATA%\Python\_cache`.
- **Registry:** `HKCU\Software\Python\PythonCore\3.15`.
- **PATH and aliases:** unchanged; the aliases now resolve to 3.15.2.
- **Used once in R4.** A `python3 --version` inside a shell command printed `Python 3.15.2` and changed nothing. Nothing else has used it.
- **Not reversed.** A narrow reversal would be the manager's own uninstall of 3.15.

## How to test again
```powershell
.\scripts\Build.ps1
.\scripts\KwrRuntime.ps1 Begin -RunId <new-id> -PackagePath .\artifacts\KingmakerWakingRune-0.5.5.zip
.\scripts\KwrRuntime.ps1 Launch      # then Shot / State / Command / Click / DoubleClick / Drag / Key / Hover / Scroll, Close, Collect
.\scripts\KwrRuntime.ps1 End         # restores profile, Steam mirror, registry, Params, and the previous Mods install
```

The party seeds are under `runtime-evidence\wr04r3-a\saves`, and the R4 saves under `runtime-evidence\wr04r4-a\saves`. Copy one into the profile before `Launch`; `End` removes it.

Harness commands:
- `party`, `fixtures`, `units`, `state`, `wardpart`, `move`, `teleport`, `cast`, `castpoint`, `attack`, `spawnfoe`, `condition`, `uncondition`, `save`, `load`, and the R1/R2 set;
- new and read-only: `stock`, `slots <member>`, `cursor`.

## Next exact action
Owner review of draft PR #4. Do not merge, tag or release from this branch without that review.
