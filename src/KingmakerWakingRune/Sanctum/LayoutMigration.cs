using System;
using System.Collections.Generic;
using System.Linq;

namespace KingmakerWakingRune.Sanctum
{
    // Metres in the Unity world. The original coordinates persist for an unresolved actor across saves.
    internal struct LayoutPosition
    {
        public float X, Y, Z;

        public LayoutPosition(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static LayoutPosition Project(GridPoint point, float height)
        {
            return new LayoutPosition(SanctumPlan.ToWorldX(point.X), height, SanctumPlan.ToWorldZ(point.Y));
        }

        public bool Overlaps(LayoutPosition other, float combinedRadius)
        {
            if (Math.Abs(Y - other.Y) > SanctumPlan.GroundTolerance) return false;
            float dx = X - other.X, dz = Z - other.Z;
            return dx * dx + dz * dz < combinedRadius * combinedRadius;
        }
    }

    internal sealed class LayoutActor
    {
        public readonly string Id;
        public readonly LayoutPosition Original, Preferred;
        public readonly GridPoint Logical;
        public readonly float? Radius;
        public readonly bool Settled, AwaitingView;

        public LayoutActor(string id, LayoutPosition original, float? radius, bool settled, bool awaitingView, bool previousLayout)
        {
            Id = id;
            Original = original;
            Radius = radius;
            Settled = settled;
            AwaitingView = awaitingView;
            Logical = SanctumPlan.SavedPoint(original.X, original.Z, previousLayout && !settled);
            Preferred = settled ? original : LayoutPosition.Project(Logical, SanctumPlan.HeightAt(Logical.X, Logical.Y) ?? original.Y);
        }
    }

    internal static class LayoutMigration
    {
        public static Dictionary<string, LayoutPosition> Plan(IEnumerable<LayoutActor> snapshot,
            Func<LayoutActor, GridPoint, LayoutPosition, bool> legal)
        {
            var actors = snapshot.OrderBy(actor => actor.Id, StringComparer.Ordinal).ToList();
            var moves = new Dictionary<string, LayoutPosition>();
            // Without even a prefab radius we cannot rule out an overlap. Retry when that view is available.
            if (actors.Any(actor => !actor.Radius.HasValue)) return moves;
            var occupied = actors.Where(a => a.Settled || a.AwaitingView).ToDictionary(a => a, a => a.Preferred);
            var blocked = new List<LayoutActor>();

            bool Reserve(LayoutActor actor, GridPoint point, LayoutPosition position)
            {
                if (!legal(actor, point, position) || occupied.Any(other =>
                    position.Overlaps(other.Value, actor.Radius.Value + other.Key.Radius.Value))) return false;
                occupied.Add(actor, position);
                moves.Add(actor.Id, position);
                return true;
            }

            foreach (var actor in actors)
                if (!actor.Settled && !actor.AwaitingView && !Reserve(actor, actor.Logical, actor.Preferred))
                    blocked.Add(actor);

            // A fallback may not take another pending actor's reservation, even if that actor must retry on load.
            foreach (var actor in blocked) occupied.Add(actor, actor.Preferred);
            foreach (var actor in blocked)
            {
                occupied.Remove(actor);
                foreach (var point in SanctumPlan.RingsAround(actor.Logical.X, actor.Logical.Y))
                {
                    var height = SanctumPlan.HeightAt(point.X, point.Y);
                    if (height.HasValue && Reserve(actor, point, LayoutPosition.Project(point, height.Value))) break;
                }
                if (!moves.ContainsKey(actor.Id)) occupied.Add(actor, actor.Preferred);
            }
            return moves;
        }
    }
}
