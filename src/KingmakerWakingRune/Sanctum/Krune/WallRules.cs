using System;
using System.Collections.Generic;
using System.Linq;

namespace KingmakerWakingRune.Sanctum.Krune
{
    /// <summary>One 5-foot section of a wall of stone, in source grid squares.</summary>
    internal sealed class WallSection
    {
        public int Index;
        public float X;
        public float Y;
        public int HitPoints = WallRules.SectionHitPoints;
    }

    /// <summary>A standing wall of stone: its proxy's identity, its line and its sections. Saved; rebuilt on every load.</summary>
    internal sealed class WallRecord
    {
        public string Id = string.Empty;
        public float CenterX;
        public float CenterY;
        /// <summary>The wall's direction along its length (unit vector, grid squares).</summary>
        public float DirX = 1f;
        public float DirY;
        public double RaisedAt;
        public string Caster = string.Empty;
        public List<WallSection> Sections = new List<WallSection>();
    }

    /// <summary>
    /// Wall of stone at caster level 17 (core spell), as laid out here: a straight wall across the chosen point, at right angles
    /// to the caster's line to it, up to 8 sections of 5 feet (10 feet high: 16 of the 17 squares the level allows). It is 4
    /// inches thick (1 per 4 levels): each section has hardness 8 and 15 hit points per inch, 60. A section cannot form where a
    /// creature stands or off the floor; the wall needs at least one section. Instantaneous: never dispelled, only broken.
    /// </summary>
    internal static class WallRules
    {
        public const int MaxSections = 8;
        public const int Hardness = 8;
        public const int SectionHitPoints = 60;

        public static WallRecord Layout(string id, float casterX, float casterY, float pointX, float pointY, int maxSections, Func<float, float, bool> clear)
        {
            float dx = pointX - casterX;
            float dy = pointY - casterY;
            float length = (float)Math.Sqrt(dx * dx + dy * dy);
            // Across the caster's line to the point; straight east-west if he aims at his own feet.
            float dirX = length > 0.01f ? -dy / length : 1f;
            float dirY = length > 0.01f ? dx / length : 0f;
            var wall = new WallRecord { Id = id, CenterX = pointX, CenterY = pointY, DirX = dirX, DirY = dirY };
            int count = Math.Max(1, Math.Min(MaxSections, maxSections));
            for (int i = 0; i < count; i++)
            {
                float offset = i - (count - 1) / 2f;
                float x = pointX + dirX * offset;
                float y = pointY + dirY * offset;
                if (clear(x, y))
                {
                    wall.Sections.Add(new WallSection { Index = i, X = x, Y = y });
                }
            }
            return wall;
        }

        /// <summary>A hit lands on the standing section nearest the attacker: hardness once, then that section's hit points.</summary>
        public static WallSection Hit(WallRecord wall, float fromX, float fromY, int amount, out int dealt)
        {
            WallSection section = wall.Sections.Where(s => s.HitPoints > 0).OrderBy(s => (s.X - fromX) * (s.X - fromX) + (s.Y - fromY) * (s.Y - fromY)).FirstOrDefault();
            dealt = Math.Max(0, amount - Hardness);
            if (section != null)
            {
                section.HitPoints = Math.Max(0, section.HitPoints - dealt);
            }
            return section;
        }

        /// <summary>Disintegrate removes a 10-foot cube: the two standing sections nearest the point it struck.</summary>
        public static List<WallSection> Disintegrate(WallRecord wall, float atX, float atY)
        {
            var gone = wall.Sections.Where(s => s.HitPoints > 0).OrderBy(s => (s.X - atX) * (s.X - atX) + (s.Y - atY) * (s.Y - atY)).Take(2).ToList();
            foreach (WallSection s in gone)
            {
                s.HitPoints = 0;
            }
            return gone;
        }

        public static bool Standing(WallRecord wall)
        {
            return wall.Sections.Any(s => s.HitPoints > 0);
        }
    }
}
