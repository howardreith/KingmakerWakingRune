using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem;
using Kingmaker.View;
using KingmakerWakingRune.Blueprints;

namespace KingmakerWakingRune.Lodge
{
    /// <summary>
    /// Isolates the donor room for the Waking Rune area only. When the native loader matches scene
    /// entity views against this area's persistent state, it receives no donor views, so donor NPCs,
    /// doors, exits, loot, script zones and cutscene players are never created (their startup logic
    /// never runs) and the native "destroy unattached views" step removes the inert views.
    /// Entities owned by the save (the player, Sheila) are still restored with fresh views.
    /// </summary>
    internal static class LodgeScope
    {
        public static bool IsLodgeLoading(SceneEntitiesState state)
        {
            var loaded = Game.Instance.State.LoadedAreaState;
            return ScenarioBlueprints.IsRegistered
                   && loaded != null
                   && loaded.Area.Blueprint == ScenarioBlueprints.Area
                   && state != null
                   && state != Game.Instance.State.PlayerState.CrossSceneState;
        }

        public static IEnumerable<EntityViewBase> Filter(SceneEntitiesState state, IEnumerable<EntityViewBase> views)
        {
            if (!IsLodgeLoading(state))
            {
                return views;
            }
            List<EntityViewBase> suppressed = views.ToList();
            Log.Info($"Donor entity views suppressed for lodge: scene={state.SceneName} count={suppressed.Count} " +
                     $"kinds=[{string.Join(",", suppressed.GroupBy(v => v.GetType().Name).Select(g => $"{g.Key}x{g.Count()}").ToArray())}]");
            return Enumerable.Empty<EntityViewBase>();
        }
    }
}
