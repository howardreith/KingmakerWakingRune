using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerWakingRune.Sanctum;
using KingmakerWakingRune.Sanctum.Wards;

namespace KingmakerWakingRune.Tests
{
    public static class LayoutMigrationTests
    {
        private static LayoutActor Old(string id, float x, float y, float radius, bool viewless = false)
        {
            var from = new LayoutPosition((x - SanctumPlan.OriginX) * SanctumPlan.PrintedSquareMeters,
                SanctumPlan.HeightAt(x, y).Value, (SanctumPlan.OriginY - y) * SanctumPlan.PrintedSquareMeters);
            return new LayoutActor(id, from, radius, false, viewless, true);
        }

        private static bool Legal(LayoutActor actor, GridPoint p, LayoutPosition world)
        {
            return SanctumPlan.FitsMigration(actor.Logical, p, world.Y,
                actor.Radius.Value / SanctumPlan.MetersPerSquare, SanctumPlan.Doors);
        }

        private static void CheckExact(IEnumerable<LayoutActor> actors)
        {
            var moves = LayoutMigration.Plan(actors, Legal);
            foreach (var actor in actors)
                Assert.Equal(actor.Preferred, moves[actor.Id], actor.Id + " retains logical position");
        }

        public static void TestEastRingPendingActorCannotObstructAnotherWithItsOldWorldPosition()
        {
            var a = Old("A", 21.5f, 12f, 0.5f);
            var b = Old("B", 22.5f, 12f, 0.5f);
            Assert.Equal(true, a.Preferred.Overlaps(b.Original, 1f), "stale position reproduces collision");
            Assert.Equal(false, a.Preferred.Overlaps(b.Preferred, 1f), "both destinations fit");
            CheckExact(new[] { a, b });
            CheckExact(new[] { b, a });
        }

        public static void TestSixPartyMembersAndLargePetMigrateWithoutFallback()
        {
            var party = new List<LayoutActor>();
            for (int i = 0; i < 6; i++) party.Add(Old("PC" + i, 21.5f + i % 2, 10f + i / 2, 0.50f));
            party.Add(Old("Large pet", 22.5f, 13.2f, 0.95f));
            CheckExact(party);
        }

        public static void TestCorpseAndLivingActorKeepSeparateA5Footprints()
        {
            CheckExact(new[] { Old("restored corpse", 9.8f, 11f, 0.49f), Old("living", 10.65f, 11f, 0.49f) });
        }

        public static void TestViewlessReservationSurvivesPartialSaveInEitherEnumerationOrder()
        {
            var waiting = Old("viewless", 22.5f, 12f, 0.6f, true);
            var fixedActor = new LayoutActor("settled", Old("unused", 21.5f, 10f, 0.5f).Preferred,
                0.5f, true, false, true);
            var exact = Old("other pending", 21.5f, 12f, 0.5f);
            var displaced = Old("first pending", 22.5f, 12f, 0.5f);
            var state = new SanctumState { LayoutVersion = 1 };
            state.MigratedActorIds.Add(fixedActor.Id);
            var units = new[] { fixedActor, exact, displaced, waiting };
            state.CaptureLayoutOrigins(units.ToDictionary(a => a.Id, a => a.Original));
            var snapshot = units.Select(a => state.SnapshotOldLayout(a.Id, a.Original, a.Radius, a.AwaitingView)).ToArray();
            var first = LayoutMigration.Plan(snapshot, Legal);
            Assert.Equal(2, first.Count, "only pending actors with views move");
            Assert.Equal(exact.Preferred, first[exact.Id], "second pending actor gets exact");
            Assert.Equal(false, waiting.Preferred.Overlaps(first[displaced.Id], 1.1f), "viewless space reserved");
            Assert.Equal(first[displaced.Id], LayoutMigration.Plan(snapshot.Reverse(), Legal)[displaced.Id],
                "fallback does not depend on native enumeration");

            foreach (var move in first) state.AppliedLayoutPosition(move.Key);
            var saved = WardPayload.Read(WardPayload.Write(state));
            Assert.Equal(1, saved.LayoutVersion, "partial layout persists");
            var current = units.ToDictionary(a => a.Id, a => first.TryGetValue(a.Id, out var applied) ? applied : a.Original);
            current[waiting.Id] = displaced.Preferred; // Its current transform cannot change the saved logical point.
            current["new summon"] = Old("new summon", 22.5f, 10f, 0.5f).Preferred;
            saved.CaptureLayoutOrigins(current);
            var next = current.Reverse().Select(kv => saved.SnapshotOldLayout(kv.Key, kv.Value,
                kv.Key == waiting.Id ? waiting.Radius : 0.5f, false));
            var retry = LayoutMigration.Plan(next, Legal);
            Assert.Equal(1, retry.Count, "settled positions left in place");
            Assert.Equal(waiting.Preferred, retry[waiting.Id], "original reserved destination on retry");
        }

        public static void TestAnUnresolvedPreferredFootprintSurvivesAnotherActorsFallback()
        {
            var a = Old("A", 22.5f, 12f, 0.5f);
            var b = Old("B", 22.5f, 12.6f, 0.5f);
            var moves = LayoutMigration.Plan(new[] { a, b }, (actor, p, world) =>
                actor == a && !world.Equals(a.Preferred) && Legal(actor, p, world));
            Assert.Equal(1, moves.Count, "unplaceable B stays pending");
            Assert.Equal(false, moves[a.Id].Overlaps(b.Preferred, 1f), "B retains its reservation during A's search");
            var unknown = new LayoutActor("missing prefab", b.Original, null, false, true, true);
            Assert.Equal(0, LayoutMigration.Plan(new[] { a, unknown }, Legal).Count, "unknown footprint defers placement");
        }

        public static void TestGenuinePreferredConflictUsesNearestLegalPointBehindClosedDoors()
        {
            foreach (var door in SanctumPlan.Doors)
            {
                // Select a legal approach close to this door while both leaves are closed.
                var approach = SanctumPlan.RingsAround(door.Center.X, door.Center.Y)
                    .Where(p => SanctumPlan.HeightAt(p.X, p.Y).HasValue)
                    .Select(p => Old("A", p.X, p.Y, 0.4f)).First(a => Legal(a, a.Logical, a.Preferred));
                var other = new LayoutActor("B", approach.Original, 0.4f, false, false, true);
                var batch = LayoutMigration.Plan(new[] { other, approach }, Legal);
                Assert.Equal(approach.Preferred, batch["A"], "stable exact winner at " + door.Id);
                var point = SanctumPlan.SavedPoint(batch["B"].X, batch["B"].Z, false);
                Assert.Equal(true, Legal(other, point, batch["B"]), "room, elevation, walls and furniture");
                foreach (var closed in SanctumPlan.Doors)
                    Assert.Equal(true, SanctumPlan.SameDoorSide(closed, approach.Logical, point), closed.Id);
                var legal = SanctumPlan.RingsAround(other.Logical.X, other.Logical.Y)
                    .Where(p => SanctumPlan.HeightAt(p.X, p.Y).HasValue)
                    .Select(p => new { p, world = LayoutPosition.Project(p, SanctumPlan.HeightAt(p.X, p.Y).Value) })
                    .Where(c => Legal(other, c.p, c.world) && !approach.Preferred.Overlaps(c.world, 0.8f));
                Assert.Equal(legal.First().world, batch["B"], "nearest legal fallback at " + door.Id);
                Assert.Equal(batch["B"], LayoutMigration.Plan(new[] { approach, other }, Legal)["B"], "stable tie-break");
            }
        }
    }
}
