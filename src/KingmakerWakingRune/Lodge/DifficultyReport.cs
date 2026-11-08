using Kingmaker.UI.SettingsUI;

namespace KingmakerWakingRune.Lodge
{
    /// <summary>Compact snapshot of the game-bound difficulty settings, for start/restore logs.</summary>
    internal static class DifficultyReport
    {
        public static string Describe()
        {
            var s = SettingsRoot.Instance;
            return $"stats={s.StatsAdjustments.CurrentValue} enemy={s.EnemyDifficulty.CurrentValue} crits={s.EnemyCriticalHits.CurrentValue} " +
                   $"damageToParty={s.DamageToParty.CurrentValue} deathDoor={s.DeathDoor.CurrentValue} autoLevel={s.AutoLevelup.CurrentValue} " +
                   $"kingdom={s.KingdomDifficulty.CurrentValue} ironman={s.OnlyOneSave.CurrentValue}";
        }
    }
}
