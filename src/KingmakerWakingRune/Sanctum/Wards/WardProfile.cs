namespace KingmakerWakingRune.Sanctum.Wards
{
    /// <summary>
    /// The frozen Subtier 10-11, six-PC, extra-challenge numbers. Printed values come from the scenario; anything else
    /// is an explicitly labelled adjudication in docs/WR-04-RULES.md. No game types, so tests read these directly.
    /// </summary>
    internal static class WardProfile
    {
        public const string Subtier = "10-11";
        public const int PartyBaseline = 6;
        public const bool ExtraChallenge = true;
        public const bool FourPlayerRelief = false;

        public const double RoundSeconds = 6d;
        public const double MinuteSeconds = 60d;
        public const double HourSeconds = 3600d;
        public const double DaySeconds = 86400d;

        public const int ArcaneCasterLevel = 9;
        public const int DivineCasterLevel = 8;
        public const int FeederStoredCasterLevel = 17;

        public const int AlarmPerceptionDC = 26;
        public const int AlarmDisarmDC = 26;
        public const int AlarmDispelDC = 20;
        public const double AlarmRadiusFeet = 20d;
        public const double AlarmDurationSeconds = 18d * HourSeconds;

        public const int FeederPerceptionDC = 33;
        public const int FeederDisarmDC = 33;
        public const int FeederTouchBonus = 8;
        public const int FeederDice = 10;
        public const double FeederTempHpSeconds = HourSeconds;

        public const int GlyphPerceptionDC = 28;
        public const int GlyphDisarmDC = 28;
        public const int GlyphDispelDC = 19;
        public const int GlyphDice = 4;
        public const int GlyphDie = 8;
        public const int GlyphReflexDC = 17;
        public const double GlyphRadiusFeet = 10d;

        public const int HoundBiteBonus = 10;
        public const int HoundBiteDice = 2;
        public const int HoundBiteDie = 6;
        public const int HoundBiteDamageBonus = 3;
        public const int HoundDispelDC = 20;
        public const double HoundDurationSeconds = 9d * HourSeconds;
        public const double HoundDischargedSeconds = 9d * RoundSeconds;
        public const double HoundNoticeFeet = 30d;
        public const double HoundBiteFeet = 5d;
        public const double HoundCasterSeparationFeet = 100d;

        public const int DoorHardness = 8;
        public const int DoorHitPoints = 75;
        public const int DoorBreakDC = 28;
        public const int DoorClueDC = 22;

        public const int RuneHintDC = 23;
        public const int RuneExactDC = 33;
        public const int RuneHardness = 8;

        public const int CurseSaveDC = 20;
        public const int CurseDamageDie = 6;
        public const int CurseRemoveBaseDC = 20;
        public const int CurseMaxBrands = 7;

        public const int MistSaveDC = 17;
        public const int MistSleepFailures = 3;
        public const int MistKruneSummonResistance = 4;

        public const int GemHardness = 20;
        public const int GemHitPoints = 40;
        public const int GemBreakDC = 40;

        public const double FlailWorkSeconds = 10d * MinuteSeconds;
        /// <summary>CRPG adaptation: copying a visible palm mark with the expedition's field kit.</summary>
        public const double ImpressionWorkSeconds = MinuteSeconds;
        /// <summary>Metres from a fixture's proxy within which a character's body can reach it.</summary>
        public const float ContactReachMeters = 1.6f;
        public const double HuskCrumbleSeconds = MinuteSeconds;
        public const int StoneToFleshSurvivalDC = 15;

        public const double GlobalDeadlineSeconds = 90d * MinuteSeconds;
    }
}
