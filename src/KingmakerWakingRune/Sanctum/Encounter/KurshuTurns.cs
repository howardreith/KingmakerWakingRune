using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;

namespace KingmakerWakingRune.Sanctum.Encounter
{
    /// <summary>Turn-based combat's turn start, for the one decision that must come before her brain acts.</summary>
    internal sealed class KurshuTurns : ITurnBasedModeHandler
    {
        public void HandleTurnStarted(UnitEntityData unit)
        {
            KurshuEncounter.OnTurnStarted(unit);
        }

        public void HandleSurpriseRoundStarted()
        {
        }

        public void HandleRoundStarted(int round)
        {
        }

        public void HandleUnitControlChanged(UnitEntityData unit)
        {
        }

        public void HandleUnitNotSurprised(UnitEntityData unit, RuleSkillCheck perceptionCheck)
        {
        }
    }
}