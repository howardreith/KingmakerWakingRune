# WR-03 progress record

Branch `feature/wr-03-sanctum` from accepted `main` (`bc248fb`). Package SHA-256 `878F53D3…C43AD45A`, extracted to `analysis\wr03-package`.

## Findings that cost time; do not rediscover
- **Terrain:** Favored Terrain reads `BlueprintArea.LootSetting`. Underground = NaturalCaves, RichHuman, Cyclops, Kobolds, Dwarves. Urban = PoorHuman, RichHuman, Barbarians, Goblins, Lizards. The lodge is `RichHuman`, so it matches both and is **not** a control.
- **Navmesh at runtime** needs all of: a `RecastMeshObj` on an object with an *enabled* renderer (this build's `RasterizeCollider` ignores `MeshCollider`; disabled renderers report empty bounds), the donor `NavmeshMasks` removed, a `ValidNavmeshArea` marker on the floor plan (the scan culls areas not connected to one), and `walkableClimb` 1.5 (the donor's 0.5 makes the ledge filter reject steep flights).
- **Screenshots** must be in-engine (`Shot`); the game presents directly to the display.
- `Continue` on the main menu loads the newest save, usually the automatic pre-departure save during iteration.

- **Arrival** must use `AreaEnterPoint` child transforms: the centred native formation stands a six-person party on the front stairs.
- **Level-up** reopens the next level after Complete; queue nothing behind it. The load list is newest-first, so row positions shift after every save.
- **Portrait double-click** recentres the camera only when both clicks are fast (`DoubleClick`). A world click on a party member selects it instead of moving.

## State
Done: all WR-03 code, acceptance on `ab56249`, reports. Awaiting owner review of the draft PR.
