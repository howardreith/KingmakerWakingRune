namespace KingmakerWakingRune.Sanctum
{
    internal enum TravelRejection
    {
        None,
        NotInLodge,
        BriefingIncomplete,
        InCombat,
        InConversation,
        CharacterBuildOpen,
        AlreadyDeparted,
        TravelInProgress,
        DestinationMissing,
    }

    /// <summary>Pure decisions for the token and transit. No game types, so they are tested directly.</summary>
    internal static class SanctumPolicy
    {
        public static bool ShouldGrantToken(bool granted, bool departed, bool alreadyOwned)
        {
            return !granted && !departed && !alreadyOwned;
        }

        public static TravelRejection CheckTravel(bool inLodge, bool briefingComplete, bool inCombat, bool inConversation, bool characterBuildOpen, bool departed, bool travelInProgress, bool destinationReady)
        {
            if (travelInProgress)
            {
                return TravelRejection.TravelInProgress;
            }
            if (departed)
            {
                return TravelRejection.AlreadyDeparted;
            }
            if (!inLodge)
            {
                return TravelRejection.NotInLodge;
            }
            if (!destinationReady)
            {
                return TravelRejection.DestinationMissing;
            }
            if (inCombat)
            {
                return TravelRejection.InCombat;
            }
            if (inConversation)
            {
                return TravelRejection.InConversation;
            }
            if (characterBuildOpen)
            {
                return TravelRejection.CharacterBuildOpen;
            }
            return briefingComplete ? TravelRejection.None : TravelRejection.BriefingIncomplete;
        }

        /// <summary>Arrival commits only for a transfer this session actually started and into the right area.</summary>
        public static bool ShouldCommitArrival(bool travelInProgress, bool arrivedInSanctum, bool alreadyCommitted)
        {
            return travelInProgress && arrivedInSanctum && !alreadyCommitted;
        }

        public static string RejectionText(TravelRejection rejection)
        {
            switch (rejection)
            {
                case TravelRejection.NotInLodge:
                    return "The token can only be broken in the Magnimar Lodge.";
                case TravelRejection.BriefingIncomplete:
                    return "Hear out the briefing before breaking the token.";
                case TravelRejection.InCombat:
                    return "The token cannot be used in combat.";
                case TravelRejection.InConversation:
                    return "Finish the conversation first.";
                case TravelRejection.CharacterBuildOpen:
                    return "Finish character creation first.";
                case TravelRejection.AlreadyDeparted:
                    return "The token has already been spent.";
                case TravelRejection.DestinationMissing:
                    return "The token's magic finds nothing to reach.";
                default:
                    return null;
            }
        }
    }
}
