using System;

namespace KingmakerWakingRune.Sanctum.Krune
{
    /// <summary>How the expedition ended, as the lodge recognizes it.</summary>
    internal enum ScenarioOutcome
    {
        /// <summary>Still under way.</summary>
        None,
        /// <summary>Krune slain, both artifacts brought back and handed over (page 15's success conditions).</summary>
        Success,
        /// <summary>Krune slain, but the party came home without one or both artifacts.</summary>
        KruneSlainArtifactsMissing,
        /// <summary>The party used the return portal before Krune was slain: the mission abandoned.</summary>
        Withdrawn,
        /// <summary>A WR-07A diagnostic save in which the interim boss was killed: kept, never a completed adventure.</summary>
        DiagnosticLegacy,
    }

    /// <summary>
    /// The conclusion of the expedition: independent, idempotent events, each recorded once with its time. A reload replays
    /// none of them, and no event implies another (a death is not a return; a return is not a debrief).
    /// </summary>
    internal sealed class ConclusionState
    {
        public double KruneDeadAt = -1d;
        /// <summary>A death during a labelled WR-07A diagnostic release: the save is a fixture, not a finished adventure.</summary>
        public bool DiagnosticDeath;
        public double AftermathAt = -1d;
        public bool BodyConsumed;
        public string LootContainerId = string.Empty;
        public string SpearId = string.Empty;
        public string RodId = string.Empty;
        public double ReturnedAt = -1d;
        /// <summary>Whether Krune was dead when the party went through the portal.</summary>
        public bool ReturnedAfterVictory;
        public bool SpearCarriedHome;
        public bool RodCarriedHome;
        public double HandoverAt = -1d;
        public double DebriefAt = -1d;
        /// <summary>Party members the dragon-tooth spear refused (its Ego), by unit id, with the time the refusal lasts until.</summary>
        public System.Collections.Generic.Dictionary<string, double> SpearRefusedUntil = new System.Collections.Generic.Dictionary<string, double>();
        /// <summary>How many times each party member has taken the spear in hand (its Ego strikes from the second time on).</summary>
        public System.Collections.Generic.Dictionary<string, int> SpearWieldAttempts = new System.Collections.Generic.Dictionary<string, int>();
        public ScenarioOutcome Outcome = ScenarioOutcome.None;
    }

    internal static class ConclusionRules
    {
        /// <summary>Krune's death, once. A death during a diagnostic release of the old checkpoint is marked as a fixture.</summary>
        public static bool RecordDeath(ConclusionState c, double now, bool diagnosticRelease)
        {
            if (c.KruneDeadAt >= 0d)
            {
                return false;
            }
            c.KruneDeadAt = now;
            c.DiagnosticDeath = diagnosticRelease;
            return true;
        }

        /// <summary>The party arrives home through the portal, once: what it carries is what counts.</summary>
        public static bool RecordReturn(ConclusionState c, double now, bool spearCarried, bool rodCarried)
        {
            if (c.ReturnedAt >= 0d)
            {
                return false;
            }
            c.ReturnedAt = now;
            c.ReturnedAfterVictory = c.KruneDeadAt >= 0d;
            c.SpearCarriedHome = spearCarried;
            c.RodCarriedHome = rodCarried;
            return true;
        }

        /// <summary>The outcome, decided at the debrief from what actually happened. Never decided before the party is home.</summary>
        public static ScenarioOutcome Decide(ConclusionState c)
        {
            if (c.ReturnedAt < 0d)
            {
                return ScenarioOutcome.None;
            }
            if (!c.ReturnedAfterVictory)
            {
                return ScenarioOutcome.Withdrawn;
            }
            if (c.DiagnosticDeath)
            {
                return ScenarioOutcome.DiagnosticLegacy;
            }
            return c.SpearCarriedHome && c.RodCarriedHome ? ScenarioOutcome.Success : ScenarioOutcome.KruneSlainArtifactsMissing;
        }

        /// <summary>The handover happens once, and only of artifacts actually carried home.</summary>
        public static bool MayHandOver(ConclusionState c)
        {
            return c.ReturnedAt >= 0d && c.HandoverAt < 0d && c.ReturnedAfterVictory && (c.SpearCarriedHome || c.RodCarriedHome);
        }

        /// <summary>What the portal must warn about before a one-way trip, or empty when nothing is being abandoned.</summary>
        public static string ReturnWarning(ConclusionState c, bool spearCarried, bool rodCarried)
        {
            if (c.KruneDeadAt < 0d)
            {
                return "Krune still lives. Going home now abandons the mission: the Society's contingencies will have to deal with him, and there is no coming back. The journey is one way.";
            }
            if (!spearCarried || !rodCarried)
            {
                string missing = !spearCarried && !rodCarried ? "the dragon-tooth spear and the rune-carved rod" : !spearCarried ? "the dragon-tooth spear" : "the rune-carved rod";
                return $"You do not carry {missing}. The Society asked for both artifacts; the journey home is one way, and what is left behind stays here.";
            }
            return string.Empty;
        }
    }
}
