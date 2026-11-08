using Kingmaker;
using Kingmaker.Blueprints.Root;
using Kingmaker.EntitySystem.Entities;
using KingmakerWakingRune.Blueprints;
using KingmakerWakingRune.Lodge;

namespace KingmakerWakingRune.Party
{
    /// <summary>The once-per-save protagonist XP and party treasury, guarded by save-bound flags.</summary>
    internal static class StartingGrants
    {
        public static int LevelTargetXp => BlueprintRoot.Instance.Progression.XPTable.GetBonus(ScenarioIds.PartyLevelTarget);

        public static void ApplyOnce()
        {
            Player player = Game.Instance.Player;
            var flags = player.UnlockableFlags;

            if (WakingPolicies.ShouldGrant(flags.IsUnlocked(ScenarioBlueprints.ProtagonistXpFlag)))
            {
                UnitEntityData main = player.MainCharacter.Value;
                int before = main.Descriptor.Progression.Experience;
                if (WakingPolicies.ProtagonistXpDelta(before, LevelTargetXp) > 0)
                {
                    main.Descriptor.Progression.AdvanceExperienceTo(LevelTargetXp, log: false);
                }
                flags.Unlock(ScenarioBlueprints.ProtagonistXpFlag);
                Log.Info($"Protagonist XP target applied: xp {before} -> {main.Descriptor.Progression.Experience} (level {ScenarioIds.PartyLevelTarget} threshold {LevelTargetXp}, next level at {BlueprintRoot.Instance.Progression.XPTable.GetBonus(ScenarioIds.PartyLevelTarget + 1)}).");
            }

            if (WakingPolicies.ShouldGrant(flags.IsUnlocked(ScenarioBlueprints.TreasuryFlag)))
            {
                long before = player.Money;
                long delta = WakingPolicies.TreasuryDelta(before, ScenarioIds.StartingTreasury);
                if (delta > 0)
                {
                    player.GainMoney(delta);
                }
                else if (delta < 0)
                {
                    player.SpendMoney(-delta);
                }
                flags.Unlock(ScenarioBlueprints.TreasuryFlag);
                Log.Info($"Party treasury initialized once: {before} -> {player.Money} gp.");
            }
        }
    }
}
