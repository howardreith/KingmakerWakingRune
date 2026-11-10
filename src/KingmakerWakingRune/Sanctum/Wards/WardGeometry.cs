namespace KingmakerWakingRune.Sanctum.Wards
{
    /// <summary>Measured ward placements, in plan squares; kept here so the unit tests can check coverage against the plan.</summary>
    internal static class WardGeometry
    {
        public const float SquaresPerFiveFeet = 1f;
        public static readonly GridPoint AlarmCenter = new GridPoint(13.15f, 22.6f);
        public const float AlarmRadiusSquares = (float)(WardProfile.AlarmRadiusFeet / 5d);
        public static readonly GridPoint GlyphCenter = new GridPoint(19.6f, 14.65f);
        public const float GlyphTriggerSquares = 1.1f;
        public const float GlyphBurstSquares = (float)(WardProfile.GlyphRadiusFeet / 5d);
        public static readonly GridPoint HoundPoint = new GridPoint(15.6f, 9.6f);
        public static readonly GridPoint MistCenter = SanctumPlan.SarcophagusCenter;
        public const float MistHalfWidthSquares = 8.2f / 2f / SanctumPlan.MetersPerSquare;
        public const float MistHalfDepthSquares = 6.2f / 2f / SanctumPlan.MetersPerSquare;

        public static bool InAlarmZone(float x, float y, float height)
        {
            return Distance(x, y, AlarmCenter) <= AlarmRadiusSquares && height < 7f;
        }

        public static bool InGlyphTrigger(float x, float y)
        {
            return Distance(x, y, GlyphCenter) <= GlyphTriggerSquares;
        }

        public static bool InGlyphBurst(float x, float y)
        {
            return Distance(x, y, GlyphCenter) <= GlyphBurstSquares;
        }

        /// <summary>The visible floor mist's own rectangle, and only inside the A4/A5 chamber.</summary>
        public static bool InMist(float x, float y)
        {
            return System.Math.Abs(x - MistCenter.X) <= MistHalfWidthSquares
                   && System.Math.Abs(y - MistCenter.Y) <= MistHalfDepthSquares
                   && SanctumPlan.SpaceAt(x, y) == SanctumSpace.Atrium;
        }

        private static float Distance(float x, float y, GridPoint point)
        {
            float dx = x - point.X, dy = y - point.Y;
            return (float)System.Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
