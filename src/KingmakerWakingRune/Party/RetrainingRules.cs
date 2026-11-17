namespace KingmakerWakingRune.Party
{
    /// <summary>Pure decisions for the Coordinator's free retraining (WR-PLAYTEST-02). No game types, so they are tested directly.</summary>
    internal static class RetrainingRules
    {
        /// <summary>Why retraining cannot start now, or null when it can. Nothing else may be moving the party or its builds.</summary>
        public static string Refusal(bool inLodge, bool inCombat, bool travelOrCheckpoint, bool characterBuildOpen, bool loading, bool alreadyRetraining)
        {
            if (!inLodge)
            {
                return "Retraining is offered only at the Magnimar lodge.";
            }
            if (inCombat)
            {
                return "Not while fighting.";
            }
            if (travelOrCheckpoint || loading)
            {
                return "Not while the expedition is leaving or the game is saving.";
            }
            if (characterBuildOpen || alreadyRetraining)
            {
                return "Finish the character build already open first.";
            }
            return null;
        }

        /// <summary>
        /// The native respec's own eligibility (RespecCompanion.CanRespec): living, not an animal companion, and above the
        /// blueprint's class-level limit. A pet is rebuilt with its owner, never on its own. The protagonist has its own option.
        /// </summary>
        public static bool CanRetrain(bool finallyDead, bool isPet, int characterLevel, int classLevelLimit)
        {
            return !finallyDead && !isPet && characterLevel > classLevelLimit;
        }
    }
}
