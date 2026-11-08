# Status — The Waking Rune

**Milestone:** WR-01 (New Game → native difficulty/chargen → Magnimar Lodge with Sheila Heidmarch). **Complete.**
**Branch:** `feature/wr-01-lodge-opening` · **Candidate:** `db3352e` (see `docs/ACCEPTANCE-WR-01.md` for hashes).

## Proven
- A1–A8 PASS on the real game (2.1.7b) with real input; layout checked at 1280×720 and 1024×768.
- Build: `scripts/Build.ps1` (MSBuild 17.14, net47, C# 7.3, Harmony 1.2 API) + 8 unit tests.

## Environment state
- Candidate is **not installed**; `Mods/` is empty as before. Profile, Steam save mirror (`userdata/…/640820/remote`),
  registry and UMM `Params.xml` match the pre-session state.
- Test saves and stray autosaves are quarantined under `C:\Dev\KingmakerWakingRuneLab\runtime-backups\*\quarantine`
  and `…\runtime-backups\steam-mirror-repair-20261108\` (mirror cleanup after runs r02/r03, which predate the harness
  covering the mirror).

## How to test again
```powershell
.\scripts\Build.ps1
.\scripts\KwrRuntime.ps1 Begin -RunId <new-id> -PackagePath .\artifacts\KingmakerWakingRune-0.1.0.zip
.\scripts\KwrRuntime.ps1 Launch      # then Shot / Click / Key / Type / Hover, Close
.\scripts\KwrRuntime.ps1 End         # restores profile, Steam mirror, registry, Params, Mods
```
Preconditions: Kingmaker not running; Steam running with Kingmaker cloud sync disabled. The harness takes the
screen (foreground input) while a run is open.

## Next exact action
Owner review of the draft PR; then WR-02 (Sheila's briefing dialogue and the remaining opening cast).
