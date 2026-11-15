using System.Collections.Generic;
using System.Linq;
using KingmakerWakingRune.Sanctum;

namespace KingmakerWakingRune.Tests
{
    /// <summary>
    /// The departure record: the expedition is readied once per committed departure; a retry or checkpoint reload readies
    /// only what changed since (a new member, a changed preparation, a level-up); older records migrate quietly.
    /// </summary>
    public static class DepartureRecordTests
    {
        private const string Book = "5a38c9ac8607890409fcb8f6342da6f4";
        private const string Haste = "486eaff58293f6441a5c2759c4872f98";
        private const string Fly = "f7ec0cbc5d2b9d3459ba4be8fd5a42e6";
        private const string Sorc = "b3db3766a4b605040b366265e2af0e50";

        private static DepartureSnapshot Wizard(string slot0, int slots = 2, string unit = "wiz")
        {
            var entries = new Dictionary<string, string>
            {
                [DepartureSnapshot.SlotKey(Book, 3, 0, 0)] = slot0,
                [DepartureSnapshot.SlotKey(Book, 3, 0, 1)] = Haste,
            };
            if (slots > 2)
            {
                entries[DepartureSnapshot.SlotKey(Book, 3, 0, 2)] = "";
                entries[DepartureSnapshot.SlotKey(Book, 4, 0, 0)] = Fly;
            }
            return new DepartureSnapshot(unit, entries);
        }

        private static DepartureSnapshot Sorcerer(int perDay)
        {
            return new DepartureSnapshot("sorc", new Dictionary<string, string> { [DepartureSnapshot.DailyKey(Sorc, 3)] = perDay.ToString() });
        }

        private static List<DepartureAction> Changes(string payload, params DepartureSnapshot[] now)
        {
            return DepartureRecord.Reconcile(DepartureRecord.Parse(payload), now).Where(a => !a.Nothing).ToList();
        }

        public static void TestFirstUseReadiesOnce()
        {
            Assert.Equal(DepartureReadiness.Refresh, DepartureRecord.Classify(null), "no record: ready now");
            string payload = DepartureRecord.Ready(132594.8, new[] { Wizard(Fly), Sorcerer(5) });
            Assert.Equal(DepartureReadiness.Reconcile, DepartureRecord.Classify(payload), "retry or checkpoint reload: reconcile");
            Assert.Equal(true, payload.StartsWith("ready:132595|"), "the record keeps the game time");
        }

        public static void TestUnchangedRetryRefreshesNothing()
        {
            string payload = DepartureRecord.Ready(100, new[] { Wizard(Fly), Sorcerer(5) });
            Assert.Equal(0, Changes(payload, Wizard(Fly), Sorcerer(5)).Count, "an unchanged expedition departs as it stands");
        }

        public static void TestChangedPreparationIsReadied()
        {
            string payload = DepartureRecord.Ready(100, new[] { Wizard(Fly), Sorcerer(5) });
            List<DepartureAction> changes = Changes(payload, Wizard(Haste), Sorcerer(5));
            Assert.Equal(1, changes.Count, "only the changed member");
            Assert.Equal(false, changes[0].Full, "not a whole refresh");
            Assert.Equal(DepartureSnapshot.SlotKey(Book, 3, 0, 0), changes[0].Slots.Single(), "only the changed slot");
            Assert.Equal(0, Changes(payload, Wizard(""), Sorcerer(5)).Count, "a slot emptied is not readied");
        }

        public static void TestSlotPreparedAgainIsReadied()
        {
            string payload = DepartureRecord.Ready(100, new[] { Wizard(Fly) });
            var prepared = new List<string> { DepartureSnapshot.PreparedKey("wiz", DepartureSnapshot.SlotKey(Book, 3, 0, 1)) };
            List<DepartureAction> changes = DepartureRecord.Reconcile(DepartureRecord.Parse(payload), new[] { Wizard(Fly) }, prepared).Where(a => !a.Nothing).ToList();
            Assert.Equal(DepartureSnapshot.SlotKey(Book, 3, 0, 1), changes.Single().Slots.Single(), "the same spell prepared again is readied");
        }

        public static void TestSlotPreparedAgainBelongsToItsCaster()
        {
            // Two wizards with the same book and slot coordinates; A prepares slot 3/0/1 again, B spent its own and changed nothing.
            string slot = DepartureSnapshot.SlotKey(Book, 3, 0, 1);
            string payload = DepartureRecord.Ready(100, new[] { Wizard(Fly, unit: "wizA"), Wizard(Fly, unit: "wizB") });
            var prepared = new List<string> { DepartureSnapshot.PreparedKey("wizA", slot) };
            Dictionary<string, DepartureSnapshot> saved = DepartureRecord.Parse(payload);
            Assert.Equal(2, saved.Count, "both casters round-trip through the saved record");
            List<DepartureAction> changes = DepartureRecord.Reconcile(saved, new[] { Wizard(Fly, unit: "wizA"), Wizard(Fly, unit: "wizB") }, prepared).Where(a => !a.Nothing).ToList();
            Assert.Equal("wizA", changes.Single().Unit, "only the caster who prepared again");
            Assert.Equal(slot, changes.Single().Slots.Single(), "only that slot");
            Assert.Equal(0, DepartureRecord.Reconcile(saved, new[] { Wizard(Fly, unit: "wizA"), Wizard(Fly, unit: "wizB") }, new List<string> { slot }).Count(a => !a.Nothing), "an entry without an owner (an earlier candidate's) readies nobody");
        }

        public static void TestNewRecruitIsReadied()
        {
            string payload = DepartureRecord.Ready(100, new[] { Wizard(Fly) });
            List<DepartureAction> changes = Changes(payload, Wizard(Fly), Sorcerer(5));
            Assert.Equal("sorc", changes.Single().Unit, "the recruit");
            Assert.Equal(true, changes.Single().Full, "readied in full");
        }

        public static void TestLevelUpIsReadied()
        {
            string payload = DepartureRecord.Ready(100, new[] { Wizard(Fly), Sorcerer(5) });
            List<DepartureAction> changes = Changes(payload, Wizard(Fly, 4), Sorcerer(6));
            DepartureAction wizard = changes.Single(a => a.Unit == "wiz");
            Assert.Equal(DepartureSnapshot.SlotKey(Book, 4, 0, 0), wizard.Slots.Single(), "the new slot's chosen spell; the new empty slot stays empty");
            Assert.Equal(1, changes.Single(a => a.Unit == "sorc").Daily[DepartureSnapshot.DailyKey(Sorc, 3)], "one more spontaneous spell");
        }

        public static void TestRecordRoundTrips()
        {
            DepartureSnapshot member = DepartureRecord.Parse(DepartureRecord.Ready(1, new[] { Wizard(Fly + "+4") }))["wiz"];
            Assert.Equal(Fly + "+4", member.Entries[DepartureSnapshot.SlotKey(Book, 3, 0, 0)], "metamagic kept");
            Assert.Equal(2, member.Entries.Count, "every slot");
        }

        public static void TestBareMarkerReadiesEveryone()
        {
            List<DepartureAction> changes = Changes("ready:132595", Wizard(Fly), Sorcerer(5));
            Assert.Equal(2, changes.Count(a => a.Full), "the first candidate's bare marker names nobody, so everyone is readied once");
        }

        public static void TestEarlierTwoUseRecordMigrates()
        {
            Assert.Equal(DepartureReadiness.Legacy, DepartureRecord.Classify("a=| wiz cl 11 3:[fly,-]"), "the old selections record is dropped, not replayed");
        }
    }
}
