using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Mechanics;

namespace KingmakerWakingRune.Sanctum.Wards
{
    /// <summary>The authored object rules (ObjectRules) applied to the live proxies, by identity, on every load alike.</summary>
    internal static class WardObjects
    {
        /// <summary>The core Disintegrate spell; its scrolls and wands carry the same ability.</summary>
        private const string DisintegrateGuid = "4aa7942c3e62a164387a73184bca3fc1";

        public static ObjectSave SaveOf(UnitEntityData unit)
        {
            string id = WardFixtures.IdOf(unit);
            SanctumState state = WardRuntime.State;
            if (id == null || state == null)
            {
                return ObjectSave.NotAnObject;
            }
            bool feederActive = !state.Door.Destroyed && FeederRules.IsArmed(state.Door.Feeder, WardRuntime.Now);
            return ObjectRules.SaveOf(id, feederActive);
        }

        public static bool IsDisintegrate(MechanicsContext context)
        {
            return context?.SourceAbility != null && context.SourceAbility.AssetGuid == DisintegrateGuid;
        }
    }
}
