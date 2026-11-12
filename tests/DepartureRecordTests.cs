using System.Collections.Generic;
using KingmakerWakingRune.Sanctum;

namespace KingmakerWakingRune.Tests
{
    /// <summary>The departure rest's record: what makes the token ask again, and what preparation after the rest never does.</summary>
    public static class DepartureRecordTests
    {
        private static string Name(string id)
        {
            return id == "a" ? "Ilsa" : id == "b" ? "Brena" : "Tessa";
        }

        public static void TestTheRecordSurvivesItsSavedForm()
        {
            var rested = new Dictionary<string, string> { { "b", "| book cl 11 1:[x,y]" }, { "a", "| wiz cl 11 3:[fly,-]" } };
            Dictionary<string, string> read = DepartureRecord.Read(DepartureRecord.Write(rested));
            Assert.Equal(2, read.Count, "both members");
            Assert.Equal("| wiz cl 11 3:[fly,-]", read["a"], "selections kept exactly");
            Assert.Equal(0, DepartureRecord.Read(null).Count, "no record");
        }

        public static void TestSpendingAfterTheRestChangesNothing()
        {
            // The selections leave out what has been spent: a cast protection, a potion or a wound since the rest is not a change.
            var rested = new Dictionary<string, string> { { "a", "| wiz cl 11 4:[fom,fom]" }, { "b", "| clr cl 11 2:[bless]" } };
            var now = new Dictionary<string, string> { { "a", "| wiz cl 11 4:[fom,fom]" }, { "b", "| clr cl 11 2:[bless]" } };
            Assert.Equal(0, DepartureRecord.Changes(rested, now, Name).Count, "the prepared party departs without a second rest");
        }

        public static void TestNewSelectionsAndPartyChangesAreNamed()
        {
            var rested = new Dictionary<string, string> { { "a", "| wiz cl 11 4:[fom,fom]" }, { "b", "| clr cl 11 2:[bless]" } };
            var now = new Dictionary<string, string> { { "a", "| wiz cl 11 4:[fom,haste]" }, { "c", "| -" } };
            List<string> changes = DepartureRecord.Changes(rested, now, Name);
            Assert.Equal(3, changes.Count, "selection, join, leave");
            Assert.Equal("Ilsa's prepared spells or caster level changed", changes[0], "a newly chosen, unprepared spell");
            Assert.Equal("Tessa joined", changes[1], "a new member");
            Assert.Equal("a member left", changes[2], "a member gone");
        }
    }
}
