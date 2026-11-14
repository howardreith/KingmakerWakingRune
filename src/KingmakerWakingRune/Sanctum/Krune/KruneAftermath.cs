using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using KingmakerWakingRune.Sanctum.Wards;
using UnityEngine;

namespace KingmakerWakingRune.Sanctum.Krune
{
    /// <summary>
    /// What Krune's death does to his sanctum (page 15, adapted): the sarcophagus, the empty soul gem and the remaining rune
    /// foci crumble; the revival's blue-green light fades; the preservation mist dissipates and exposes no one again; his body
    /// is consumed by its runes after a short presentation, his gear first set down as recoverable loot. No collapse, no
    /// countdown. Each step is recorded once in ConclusionState and re-applied on every load, so nothing comes back.
    /// Other enemies, summons and spells follow their own rules: his death removes none of them.
    /// </summary>
    internal static class KruneAftermath
    {
        /// <summary>How long his body stands, consumed by its runes, before it is gone.</summary>
        public const double ConsumeSeconds = 4d;

        public static void Tick(SanctumState state, double now)
        {
            KruneState krune = state.Krune;
            ConclusionState c = state.Conclusion;
            if (!KruneAwakening.Authoritative)
            {
                return;
            }
            UnitEntityData actor = KruneAwakening.Actor;
            if (c.KruneDeadAt < 0d)
            {
                if (actor == null || !actor.Descriptor.State.IsDead)
                {
                    return;
                }
                ConclusionRules.RecordDeath(c, now, krune.DiagnosticRelease);
                KruneAwakening.Note($"KRUNE SLAIN at {now:0}{(c.DiagnosticDeath ? " (during a WR-07A diagnostic release: a fixture, not a finished adventure)" : string.Empty)}.");
            }
            if (c.AftermathAt < 0d)
            {
                c.AftermathAt = now;
                KruneLoot.Secure(state, actor);
                Apply(state);
                SanctumFlash.Surge(2.5f);
                WardRuntime.Say("The runes on Krune's body flare and burn; the sarcophagus, the soul emerald and the rune foci crumble, and the mist thins away.");
                KruneAwakening.Note("aftermath applied: sarcophagus, gem and foci crumbled; light faded; mist dissipated.");
            }
            if (!c.BodyConsumed && now - c.AftermathAt >= ConsumeSeconds)
            {
                KruneLoot.Secure(state, actor);
                if (actor != null && actor.IsInGame)
                {
                    actor.IsInGame = false;
                }
                c.BodyConsumed = true;
                KruneAwakening.Note("his body is consumed by its runes; his gear lies where he fell.");
            }
        }

        /// <summary>The changed sanctum as the save records it, on every load (and once at the death).</summary>
        public static void Apply(SanctumState state)
        {
            ConclusionState c = state.Conclusion;
            SanctumMist.Dissipated = c.AftermathAt >= 0d;
            if (c.AftermathAt < 0d)
            {
                return;
            }
            // The already-empty soul gem crumbles with the rest. This is not the early-release break: Gem.Destroyed (which
            // means "broken to wake him") is left as it was; only what is shown changes.
            if (!state.Gem.Destroyed)
            {
                foreach (GameObject part in SanctumDressing.EmeraldIntact.Where(p => p != null))
                {
                    part.SetActive(false);
                }
                foreach (GameObject part in SanctumDressing.EmeraldBroken.Where(p => p != null))
                {
                    part.SetActive(true);
                }
                UnitEntityData gem = WardFixtures.Gem;
                if (gem != null && gem.IsInGame)
                {
                    gem.IsInGame = false;
                }
            }
            foreach (GameObject part in SanctumDressing.Sarcophagus.Where(p => p != null))
            {
                part.SetActive(false);
            }
            foreach (GameObject part in SanctumDressing.SarcophagusRubble.Where(p => p != null))
            {
                part.SetActive(true);
            }
            foreach (RuneFocus focus in KruneProjection.Foci)
            {
                if (SanctumDressing.BrokenRunes.TryGetValue(focus.Id, out GameObject[] parts))
                {
                    foreach (GameObject part in parts.Where(p => p != null))
                    {
                        part.SetActive(true);
                    }
                }
                if (SanctumDressing.RuneLights.TryGetValue(focus.Id, out Light light) && light != null)
                {
                    light.enabled = false;
                }
                // A crumbled focus has no plate to touch, study or strike: no curse or other effect can come from it.
                UnitEntityData plate = WardFixtures.Find(focus.Id);
                if (plate != null && plate.IsInGame)
                {
                    plate.IsInGame = false;
                }
            }
            if (SanctumLighting.Glow != null)
            {
                SanctumLighting.Glow.enabled = false;
            }
            if (SanctumLighting.Flash != null)
            {
                SanctumLighting.Flash.enabled = false;
            }
            SanctumMist.Dissipated = true;
            if (SanctumMist.Effects != null)
            {
                SanctumMist.Effects.SetActive(false);
            }
            UnitEntityData actor = KruneAwakening.Actor;
            if (c.BodyConsumed && actor != null && actor.IsInGame)
            {
                actor.IsInGame = false;
            }
        }
    }
}
