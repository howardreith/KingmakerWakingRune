namespace KingmakerWakingRune.Sanctum.Wards
{
    /// <summary>How an authored breakable object makes saving throws. The proxy unit's own statistics never decide it.</summary>
    internal enum ObjectSave
    {
        /// <summary>Not an authored breakable object: the unit saves with its own statistics (rune plates, statues, props).</summary>
        NotAnObject,
        /// <summary>An unattended nonmagical object: it never makes a saving throw (core magic-item rules, "Saving Throws").</summary>
        Never,
        /// <summary>A magical object: 2 + half its caster level on every save (core magic-item rules, "Saving Throws").</summary>
        MagicItem,
    }

    /// <summary>What a Disintegrate that reached an authored object does to it.</summary>
    internal enum DisintegrateOutcome
    {
        /// <summary>A failed save or no save: the object is disintegrated outright (it is well within one 10-foot cube).</summary>
        Disintegrated,
        /// <summary>A successful save: only the reduced damage, through hardness like any other hit.</summary>
        ReducedDamage,
    }

    /// <summary>
    /// The breakable objects' authored rules (project adjudications from the core magic-item and Disintegrate rules; the
    /// scenario prints only hardness, hit points and break DCs).
    /// - The soul emerald is an active CL 17 receptacle: a magical object, save +10.
    /// - The A2 leaf is stone. While its feeder is armed and not suppressed it carries that CL 17 device, so it saves as
    ///   a magical object; disarmed, suppressed or spent, it is an unattended nonmagical object and never saves.
    /// - Rune plates keep their own printed no-save rule against their vulnerabilities; they are not covered here.
    /// </summary>
    internal static class ObjectRules
    {
        public const int GemCasterLevel = 17;

        public static int MagicItemSaveBonus(int casterLevel)
        {
            return 2 + casterLevel / 2;
        }

        public static ObjectSave SaveOf(string fixtureId, bool doorFeederActive)
        {
            switch (fixtureId)
            {
                case WardText.GemId:
                    return ObjectSave.MagicItem;
                case WardText.DoorId:
                    return doorFeederActive ? ObjectSave.MagicItem : ObjectSave.Never;
                default:
                    // A wall of stone is an instantaneous conjuration: once raised it is ordinary stone.
                    return fixtureId != null && fixtureId.StartsWith("Wall-", System.StringComparison.Ordinal) ? ObjectSave.Never : ObjectSave.NotAnObject;
            }
        }

        public static int SaveBonus(string fixtureId)
        {
            return MagicItemSaveBonus(fixtureId == WardText.DoorId ? WardProfile.FeederStoredCasterLevel : GemCasterLevel);
        }

        /// <summary>
        /// Disintegrate against an object: a save that was not made (or that the object could not make) disintegrates
        /// it; a successful save leaves only the reduced damage, which then meets hardness as one hit.
        /// </summary>
        public static DisintegrateOutcome Disintegrate(bool savePassed)
        {
            return savePassed ? DisintegrateOutcome.ReducedDamage : DisintegrateOutcome.Disintegrated;
        }
    }
}
