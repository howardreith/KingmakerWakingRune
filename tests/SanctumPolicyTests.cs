using KingmakerWakingRune.Sanctum;

namespace KingmakerWakingRune.Tests
{
    public static class SanctumPolicyTests
    {
        private static TravelRejection Check(bool inLodge = true, bool briefing = true, bool combat = false, bool conversation = false, bool build = false, bool departed = false, bool travelling = false, bool destination = true)
        {
            return SanctumPolicy.CheckTravel(inLodge, briefing, combat, conversation, build, departed, travelling, destination);
        }

        public static void TestTokenIsGrantedOnlyOnce()
        {
            Assert.Equal(true, SanctumPolicy.ShouldGrantToken(false, false, false), "fresh or migrating save");
            Assert.Equal(false, SanctumPolicy.ShouldGrantToken(true, false, false), "moved or sold after the grant");
            Assert.Equal(false, SanctumPolicy.ShouldGrantToken(false, true, false), "already departed");
            Assert.Equal(false, SanctumPolicy.ShouldGrantToken(false, false, true), "already owned");
        }

        public static void TestValidRequestIsAccepted()
        {
            Assert.Equal(TravelRejection.None, Check(), "prepared party in the lodge");
        }

        public static void TestEachInvalidContextIsRejected()
        {
            Assert.Equal(TravelRejection.NotInLodge, Check(inLodge: false), "outside the lodge");
            Assert.Equal(TravelRejection.BriefingIncomplete, Check(briefing: false), "before the briefing ends");
            Assert.Equal(TravelRejection.InCombat, Check(combat: true), "in combat");
            Assert.Equal(TravelRejection.InConversation, Check(conversation: true), "in conversation");
            Assert.Equal(TravelRejection.CharacterBuildOpen, Check(build: true), "during character build");
            Assert.Equal(TravelRejection.AlreadyDeparted, Check(departed: true), "after departure");
            Assert.Equal(TravelRejection.DestinationMissing, Check(destination: false), "no destination");
        }

        public static void TestRepeatClickDuringTravelIsRejectedFirst()
        {
            Assert.Equal(TravelRejection.TravelInProgress, Check(travelling: true), "second click");
            Assert.Equal(TravelRejection.TravelInProgress, Check(inLodge: false, travelling: true, departed: true), "takes priority while loading");
        }

        public static void TestRepeatClickHasNoWarningText()
        {
            Assert.Equal(null, SanctumPolicy.RejectionText(TravelRejection.TravelInProgress), "silent while travelling");
            Assert.Equal(null, SanctumPolicy.RejectionText(TravelRejection.None), "nothing to say");
        }

        public static void TestArrivalCommitsOnlyOnce()
        {
            Assert.Equal(true, SanctumPolicy.ShouldCommitArrival(true, true, false), "real transfer arrives");
            Assert.Equal(false, SanctumPolicy.ShouldCommitArrival(false, true, false), "loading a sanctum save is not a transfer");
            Assert.Equal(false, SanctumPolicy.ShouldCommitArrival(true, false, false), "not arrived");
            Assert.Equal(false, SanctumPolicy.ShouldCommitArrival(true, true, true), "already committed");
        }
    }
}
