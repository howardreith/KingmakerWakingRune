using System;

namespace KingmakerWakingRune.Sanctum.Wards
{
    /// <summary>One creature's aggregate Rune of Sloth affliction: brands by plate bit, never a stack of separate curses.</summary>
    internal sealed class CurseRecord
    {
        public string Actor = string.Empty;
        public int PlateMask;
        public double NextProgressionAt = -1d;
        /// <summary>This source's fatigue contribution; unrelated fatigue is the game's own and is never touched.</summary>
        public bool FatigueActive;
        public bool EverCursed;
    }

    internal enum CurseBrandResult
    {
        SavedNoBrand,
        AlreadyBrandedThisPlate,
        FirstBrand,
        AdditionalBrand,
    }

    internal static class CurseRules
    {
        public static int BrandCount(int mask)
        {
            int count = 0;
            for (int m = mask; m != 0; m >>= 1)
            {
                count += m & 1;
            }
            return count;
        }

        /// <summary>The caster-level DC to remove the curse: one higher for each brand after the first, at most 26.</summary>
        public static int RemoveDC(int mask)
        {
            int brands = BrandCount(mask);
            return brands == 0 ? WardProfile.CurseRemoveBaseDC : WardProfile.CurseRemoveBaseDC + Math.Min(brands, WardProfile.CurseMaxBrands) - 1;
        }

        /// <summary>A failed manipulation at one plate. Only a first brand costs Dexterity at once; added brands only raise the DC.</summary>
        public static CurseBrandResult Brand(CurseRecord record, int plateBit, bool savedWill, double now)
        {
            int bit = 1 << plateBit;
            if (savedWill)
            {
                return CurseBrandResult.SavedNoBrand;
            }
            if ((record.PlateMask & bit) != 0)
            {
                return CurseBrandResult.AlreadyBrandedThisPlate;
            }
            bool first = record.PlateMask == 0;
            record.PlateMask |= bit;
            record.EverCursed = true;
            if (first)
            {
                record.NextProgressionAt = now + WardProfile.DaySeconds;
                return CurseBrandResult.FirstBrand;
            }
            return CurseBrandResult.AdditionalBrand;
        }

        /// <summary>How many once-per-day progressions fall due by now, each one 1d6 for the whole affliction. Advances the schedule.</summary>
        public static int DueProgressions(CurseRecord record, double now)
        {
            if (record.PlateMask == 0 || record.NextProgressionAt < 0d)
            {
                return 0;
            }
            int due = 0;
            while (now >= record.NextProgressionAt)
            {
                due++;
                record.NextProgressionAt += WardProfile.DaySeconds;
            }
            return due;
        }

        /// <summary>Removes the affliction. Existing Dexterity damage and its fatigue stay until healed.</summary>
        public static void Remove(CurseRecord record)
        {
            record.PlateMask = 0;
            record.NextProgressionAt = -1d;
        }

        /// <summary>Fatigue lasts exactly while this creature, having been cursed, still has Dexterity damage. Returns whether it changed.</summary>
        public static bool UpdateFatigue(CurseRecord record, int dexterityDamage)
        {
            bool wanted = record.EverCursed && dexterityDamage > 0;
            if (wanted == record.FatigueActive)
            {
                return false;
            }
            record.FatigueActive = wanted;
            return true;
        }
    }
}
