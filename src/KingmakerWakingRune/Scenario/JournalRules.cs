using System.Collections.Generic;

namespace KingmakerWakingRune.Scenario
{
    /// <summary>One journal objective's state, as the game's quest book knows it.</summary>
    internal enum JournalMark
    {
        None,
        Started,
        Completed,
        Failed,
    }

    /// <summary>What the journal is derived from: recorded scenario facts only (never a dialogue visit or a journal state).</summary>
    internal sealed class JournalFacts
    {
        public bool SanctumEntered;
        public bool DefendersDefeated;
        public bool KruneAwake;
        public bool KruneSlain;
        public bool Returned;
        public bool SpearHome;
        public bool RodHome;
        public bool Debriefed;
        public bool Withdrawn;
    }

    /// <summary>
    /// The adventure's journal: one quest, six objectives, each derived from the recorded facts so it can be brought up to
    /// date on any load (older saves included). The game's quest book only moves forward, so the runtime applies only the
    /// steps that advance an objective; the last objective finishes the quest (failed on a withdrawal). No objective names
    /// a hidden room or a rune's solution.
    /// </summary>
    internal static class JournalRules
    {
        public const string Prepare = "Prepare";
        public const string Defenses = "Defenses";
        public const string Revival = "Revival";
        public const string Krune = "Krune";
        public const string Artifacts = "Artifacts";
        public const string Report = "Report";

        /// <summary>The objectives in journal order (the order they are applied in).</summary>
        public static readonly string[] Order = { Prepare, Defenses, Revival, Krune, Artifacts, Report };

        public static Dictionary<string, JournalMark> Marks(JournalFacts f)
        {
            bool reachedTomb = f.DefendersDefeated || f.KruneAwake;
            return new Dictionary<string, JournalMark>
            {
                [Prepare] = f.SanctumEntered ? JournalMark.Completed : JournalMark.Started,
                [Defenses] = reachedTomb ? JournalMark.Completed : f.Returned ? JournalMark.Failed : f.SanctumEntered ? JournalMark.Started : JournalMark.None,
                [Revival] = f.KruneAwake ? JournalMark.Completed : f.Returned ? JournalMark.Failed : f.DefendersDefeated ? JournalMark.Started : JournalMark.None,
                [Krune] = f.KruneSlain ? JournalMark.Completed : f.Returned ? JournalMark.Failed : f.KruneAwake ? JournalMark.Started : JournalMark.None,
                [Artifacts] = !f.KruneSlain ? JournalMark.None : !f.Returned ? JournalMark.Started : f.SpearHome && f.RodHome ? JournalMark.Completed : JournalMark.Failed,
                [Report] = f.Debriefed ? (f.Withdrawn ? JournalMark.Failed : JournalMark.Completed) : f.Returned ? JournalMark.Started : JournalMark.None,
            };
        }

        /// <summary>Whether moving from <paramref name="current"/> to <paramref name="wanted"/> is a step forward (never back).</summary>
        public static bool Advances(JournalMark current, JournalMark wanted)
        {
            switch (current)
            {
                case JournalMark.None:
                    return wanted != JournalMark.None;
                case JournalMark.Started:
                    return wanted == JournalMark.Completed || wanted == JournalMark.Failed;
                default:
                    return false;
            }
        }
    }
}
