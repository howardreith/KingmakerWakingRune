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
                // A new game is granted the current amount, so the supplement for older lodge saves never applies to it.
                flags.Unlock(ScenarioBlueprints.TreasurySupplementFlag);
                Log.Info($"Party treasury initialized once: {before} -> {player.Money} gp.");
            }
            else
            {
                long supplement = WakingPolicies.TreasurySupplement(true, flags.IsUnlocked(ScenarioBlueprints.TreasurySupplementFlag),
                    flags.IsUnlocked(ScenarioBlueprints.SanctumEnteredFlag), ScenarioIds.PreviousStartingTreasury, ScenarioIds.StartingTreasury);
                if (supplement > 0)
                {
                    long before = player.Money;
                    player.GainMoney(supplement);
                    flags.Unlock(ScenarioBlueprints.TreasurySupplementFlag);
                    Log.Info($"Treasury supplement for a lodge save from before 500,000 gp, applied once: {before} -> {player.Money} gp (+{supplement}).");
                }
            }
        }
    }
}
