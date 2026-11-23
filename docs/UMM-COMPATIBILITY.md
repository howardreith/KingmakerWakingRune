# UMM compatibility baseline

The requested common baseline is Unity Mod Manager **0.31.1**. `Info.json`, the
compile references and the build/package identity checks agree on that version.
This is a source/build policy change awaiting native qualification, not a record
of a completed gameplay run.

## Build identity

`scripts/loader-reference-pins.json` pins the official UMM 0.31.1 NuGet package,
the upstream Harmony 1.2 adapter and Harmony 2.3.1.1. The same UMM and adapter
hashes are already used by Tabletop Rules Expansion and Cutscene Skip. Waking
Rune also calls Harmony2 directly for dialogue exception finalizers, so its
`0Harmony.dll` reference is pinned as well. The product references all three
with Copy Local disabled; only the mod DLL and Info.json enter its ZIP.

The fetch helper verifies the archive, extracted DLL and assembly identity.
An existing wrong DLL causes a failure rather than being silently reused or
replaced. Direct MSBuild validates the same pins. The package step checks the
compiled assembly's reference versions and includes the exact reference hashes
in its build output.

## Runtime evidence and required checks

| Profile | Current evidence | Remaining qualification |
| --- | --- | --- |
| UMM 0.31.1 / Harmony 2.3.1.1 | Exact upstream references; source API inspection. | Clean Windows build, startup, New Game entry, lodge and Refuge Token travel, Kurshu dialogue finalizers, save/reload. |
| UMM 0.32.4 / Harmony 2.3.6 | Comparison target used by sibling mods; no Waking Rune qualification transferred. | Run the same candidate DLL and scenarios, recording actual loader/Harmony identities. |
| UMM 0.33.0 / Harmony 2.3.6 | Owner-profile comparison target. | Run the same candidate DLL and scenarios with exact identities. |
| UMM 0.33.2 / Harmony 2.3.6 | Historical environment recorded in `docs/RECON.md`. | Requalify the 0.31.1-built candidate; historical results remain bound to their original DLLs. |
| UMM 0.33.4 / Harmony 2.4.3 | Upstream changelog says Harmony changed; not qualified here. | Verify the actual distribution and run the same candidate before claiming support. |

Record the candidate commit, DLL hash and MVID, exact UMM/Harmony hashes, game
version, OS, results, and fresh-process save/reload evidence for each run.
Use a disposable game copy and copied test saves. This policy does not downgrade
the owner's live installation or rewrite historical environment records.
Neither startup alone nor a manifest edit proves gameplay compatibility.

## Sources

- Baseline UMM: https://www.nuget.org/packages/UnityModManager/0.31.1
- Baseline adapter: https://github.com/newman55/unity-mod-manager/tree/10cfb1cce51914a5114928c5bc6a06d1d2c31fe2/lib/Harmony/1.2
- Baseline Harmony: https://www.nuget.org/packages/Lib.Harmony/2.3.1.1
- Current upstream changelog: https://www.nexusmods.com/site/mods/21
