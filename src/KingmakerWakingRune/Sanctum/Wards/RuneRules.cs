using System;
using System.Collections.Generic;
using System.Linq;

namespace KingmakerWakingRune.Sanctum.Wards
{
    internal enum RuneDamageType
    {
        Bludgeoning,
        Piercing,
        Slashing,
        Fire,
        Cold,
        Acid,
        Electricity,
        Sonic,
        Force,
        NegativeEnergy,
        Other,
    }

    /// <summary>One resolved component of a damaging hit, at the amount the native pipeline delivered.</summary>
    internal struct RuneDamageComponent
    {
        public RuneDamageType Type;
        public int Amount;

        public RuneDamageComponent(RuneDamageType type, int amount)
        {
            Type = type;
            Amount = amount;
        }
    }

    internal enum RuneCondition
    {
        Entangled,
        Staggered,
        Paralyzed,
    }

    internal enum RuneRoute
    {
        NegativeEnergyDamage,
        NegativeLevels,
        PoisonDiseaseDamage,
        DeathSpell,
        EntangledDuration,
        StaggerParalysisDuration,
        ColdDamage,
        EnergyDamage,
        BludgeoningSingleHit,
        Dispel,
        AcidFireDamage,
        TrickerySkill,
        PiercingSlashingSingleHit,
        CompulsionSpell,
        SummonedDamage,
        UseMagicDeviceSkill,
    }

    internal enum RuneSkill
    {
        Trickery,
        UseMagicDevice,
    }

    /// <summary>An open legal attempt on a focus. Every field is saved, so a window outlives a reload or a combat-mode change.</summary>
    internal sealed class RuneWindow
    {
        public RuneRoute Route;
        /// <summary>The ability score a poison or disease window tracks, or empty.</summary>
        public string Ability = string.Empty;
        public double OpenedAt;
        public double ClosesAt;
        public int Accumulated;
        public List<string> Contributors = new List<string>();
    }

    /// <summary>A status route: continuous from <see cref="StartedAt"/> while the status holds.</summary>
    internal sealed class RuneSustain
    {
        public RuneCondition Condition;
        public double StartedAt;
        public string Actor = string.Empty;
    }

    internal sealed class RuneNegativeLevel
    {
        public string Actor = string.Empty;
        public int Count;
        /// <summary>Simulation seconds at which these levels end, or below zero if they do not.</summary>
        public double ExpiresAt = -1d;
    }

    internal sealed class RunePendingCast
    {
        public string Actor = string.Empty;
        public double ResolvesAt;
    }

    internal sealed class RuneFocusState
    {
        public bool Disabled;
        public string DisabledBy = string.Empty;
        public string DisabledRoute = string.Empty;
        public double DisabledAt = -1d;
        public List<RuneWindow> Windows = new List<RuneWindow>();
        public List<RuneSustain> Sustains = new List<RuneSustain>();
        public List<RuneNegativeLevel> NegativeLevels = new List<RuneNegativeLevel>();
        public List<RunePendingCast> PendingCasts = new List<RunePendingCast>();
    }

    /// <summary>What an observed effect did to a focus. Failures name every creature that must face the curse.</summary>
    internal sealed class RuneOutcome
    {
        public bool Disabled;
        public RuneRoute? Route;
        public bool Pending;
        public readonly List<string> FailedActors = new List<string>();
        public string Detail = string.Empty;

        public static RuneOutcome None => new RuneOutcome();
    }

    /// <summary>
    /// The seven foci at Subtier 10-11. Hardness applies once per independently resolved hit, before thresholds; only the
    /// components a route names are counted, and listed energy takes no generic object halving. The native proxy has no
    /// hardness or resistance of its own, so nothing is subtracted twice.
    /// </summary>
    internal static class RuneRules
    {
        public const int NegativeEnergyMinimum = 27;
        public const int NegativeLevelMinimum = 2;
        public const int AbilityDamageMinimum = 3;
        public const int MinimumSpellLevel = 4;
        public const int EnergyMinimum = 35;
        public const int SingleHitMinimum = 35;
        public const int SummonedDamageMinimum = 50;
        public const int SkillDC = 35;
        public const int DispelCasterLevel = 14;
        public const int EntangleRounds = 10;
        public const int StaggerRounds = 5;

        private sealed class DamageRoute
        {
            public RuneRoute Route;
            public RuneDamageType[] Types;
            public int Minimum;
            public bool SingleHit;
        }

        private static readonly Dictionary<RuneKind, DamageRoute[]> DamageRoutes = new Dictionary<RuneKind, DamageRoute[]>
        {
            { RuneKind.Life, new[] { Windowed(RuneRoute.NegativeEnergyDamage, NegativeEnergyMinimum, RuneDamageType.NegativeEnergy) } },
            { RuneKind.Alacrity, new[] { Windowed(RuneRoute.ColdDamage, EnergyMinimum, RuneDamageType.Cold) } },
            { RuneKind.Defense, new[] { Windowed(RuneRoute.EnergyDamage, EnergyMinimum, RuneDamageType.Electricity, RuneDamageType.Force, RuneDamageType.Sonic), Single(RuneRoute.BludgeoningSingleHit, RuneDamageType.Bludgeoning) } },
            { RuneKind.Armor, new[] { Windowed(RuneRoute.AcidFireDamage, EnergyMinimum, RuneDamageType.Acid, RuneDamageType.Fire), Single(RuneRoute.PiercingSlashingSingleHit, RuneDamageType.Piercing, RuneDamageType.Slashing) } },
            { RuneKind.Control, new DamageRoute[0] },
        };

        private static DamageRoute Windowed(RuneRoute route, int minimum, params RuneDamageType[] types)
        {
            return new DamageRoute { Route = route, Types = types, Minimum = minimum };
        }

        private static DamageRoute Single(RuneRoute route, params RuneDamageType[] types)
        {
            return new DamageRoute { Route = route, Types = types, Minimum = SingleHitMinimum, SingleHit = true };
        }

        /// <summary>
        /// One independently resolved hit. <paramref name="summoned"/> marks an attacker that is a summoned or called
        /// creature (never merely a pet); only it feeds Control's source-based route.
        /// </summary>
        public static RuneOutcome ApplyDamage(RuneFocus focus, RuneFocusState state, double now, string actor, bool summoned, bool bypassHardness, IList<RuneDamageComponent> components)
        {
            var outcome = new RuneOutcome();
            if (state.Disabled)
            {
                return outcome;
            }
            bool contributed = false;
            bool relevant = false;
            int hardness = bypassHardness ? 0 : WardProfile.RuneHardness;
            foreach (DamageRoute route in DamageRoutes[focus.Kind])
            {
                int qualifying = components.Where(c => route.Types.Contains(c.Type)).Sum(c => c.Amount);
                if (qualifying <= 0)
                {
                    continue;
                }
                relevant = true;
                int effective = Math.Max(0, qualifying - hardness);
                if (route.SingleHit)
                {
                    if (effective >= route.Minimum)
                    {
                        return Disable(state, now, actor, route.Route, $"{route.Route}: {qualifying} qualifying - hardness {hardness} = {effective} (needs {route.Minimum}).", outcome);
                    }
                    outcome.Detail = $"{route.Route}: {qualifying} - {hardness} = {effective}, short of {route.Minimum} in one hit.";
                    continue;
                }
                RuneWindow window = OpenWindow(state, route.Route, string.Empty, now, WardProfile.RoundSeconds, actor);
                window.Accumulated += effective;
                contributed = true;
                outcome.Detail = $"{route.Route}: {window.Accumulated}/{route.Minimum} after hardness {hardness}, window closes at {window.ClosesAt:0}.";
                if (window.Accumulated >= route.Minimum)
                {
                    return Disable(state, now, actor, route.Route, outcome.Detail, outcome);
                }
            }
            if (focus.Kind == RuneKind.Control && summoned)
            {
                int total = components.Sum(c => c.Amount);
                if (total > 0)
                {
                    relevant = true;
                    RuneWindow window = OpenWindow(state, RuneRoute.SummonedDamage, string.Empty, now, 10d * WardProfile.RoundSeconds, actor);
                    window.Accumulated += Math.Max(0, total - hardness);
                    contributed = true;
                    outcome.Detail = $"SummonedDamage: {window.Accumulated}/{SummonedDamageMinimum}, window closes at {window.ClosesAt:0}.";
                    if (window.Accumulated >= SummonedDamageMinimum)
                    {
                        return Disable(state, now, actor, RuneRoute.SummonedDamage, outcome.Detail, outcome);
                    }
                }
            }
            state.PendingCasts.RemoveAll(p => p.Actor == actor);
            if (contributed)
            {
                outcome.Pending = true;
            }
            else
            {
                outcome.FailedActors.Add(actor);
                if (!relevant)
                {
                    outcome.Detail = "No route of this focus answers that damage.";
                }
            }
            return outcome;
        }

        /// <summary>Real negative levels on the focus. Totals only what is still unexpired; the printed route has no one-round limit.</summary>
        public static RuneOutcome ApplyNegativeLevels(RuneFocus focus, RuneFocusState state, double now, string actor, int count, double expiresAt)
        {
            var outcome = new RuneOutcome();
            if (state.Disabled || count <= 0)
            {
                return outcome;
            }
            state.PendingCasts.RemoveAll(p => p.Actor == actor);
            if (focus.Kind != RuneKind.Life)
            {
                outcome.FailedActors.Add(actor);
                outcome.Detail = "No route of this focus answers negative levels.";
                return outcome;
            }
            state.NegativeLevels.Add(new RuneNegativeLevel { Actor = actor, Count = count, ExpiresAt = expiresAt });
            int total = state.NegativeLevels.Sum(l => l.Count);
            outcome.Detail = $"NegativeLevels: {total}/{NegativeLevelMinimum}.";
            if (total >= NegativeLevelMinimum)
            {
                return Disable(state, now, actor, RuneRoute.NegativeLevels, outcome.Detail, outcome);
            }
            outcome.Pending = true;
            return outcome;
        }

        /// <summary>Ability damage from a poison or disease. The adapter excludes drain and penalties; one score, one round.</summary>
        public static RuneOutcome ApplyAbilityDamage(RuneFocus focus, RuneFocusState state, double now, string actor, string ability, int amount)
        {
            var outcome = new RuneOutcome();
            if (state.Disabled || amount <= 0)
            {
                return outcome;
            }
            state.PendingCasts.RemoveAll(p => p.Actor == actor);
            if (focus.Kind != RuneKind.Life)
            {
                outcome.FailedActors.Add(actor);
                outcome.Detail = "No route of this focus answers ability damage.";
                return outcome;
            }
            RuneWindow window = OpenWindow(state, RuneRoute.PoisonDiseaseDamage, ability, now, WardProfile.RoundSeconds, actor);
            window.Accumulated += amount;
            outcome.Detail = $"PoisonDiseaseDamage {ability}: {window.Accumulated}/{AbilityDamageMinimum}, window closes at {window.ClosesAt:0}.";
            if (window.Accumulated >= AbilityDamageMinimum)
            {
                return Disable(state, now, actor, RuneRoute.PoisonDiseaseDamage, outcome.Detail, outcome);
            }
            outcome.Pending = true;
            return outcome;
        }

        /// <summary>A spell completed at the focus. Descriptor routes judge effective spell level; any other cast waits a round for its real effects.</summary>
        public static RuneOutcome ApplySpell(RuneFocus focus, RuneFocusState state, double now, string actor, bool death, bool compulsion, int effectiveLevel)
        {
            var outcome = new RuneOutcome();
            if (state.Disabled)
            {
                return outcome;
            }
            RuneRoute? route = focus.Kind == RuneKind.Life && death ? RuneRoute.DeathSpell : focus.Kind == RuneKind.Control && compulsion ? RuneRoute.CompulsionSpell : (RuneRoute?)null;
            if (route.HasValue)
            {
                if (effectiveLevel >= MinimumSpellLevel)
                {
                    return Disable(state, now, actor, route.Value, $"{route.Value}: effective spell level {effectiveLevel}.", outcome);
                }
                outcome.FailedActors.Add(actor);
                outcome.Detail = $"{route.Value}: effective spell level {effectiveLevel} is below {MinimumSpellLevel}.";
                return outcome;
            }
            state.PendingCasts.RemoveAll(p => p.Actor == actor);
            state.PendingCasts.Add(new RunePendingCast { Actor = actor, ResolvesAt = now + WardProfile.RoundSeconds });
            outcome.Pending = true;
            outcome.Detail = "Spell delivered; awaiting its resolved effects.";
            return outcome;
        }

        /// <summary>A real targeted dispel against the Defense focus. The caster-level check itself is the native one, against CL 14.</summary>
        public static RuneOutcome ApplyDispel(RuneFocus focus, RuneFocusState state, double now, string actor, bool success, int checkResult, int dc)
        {
            var outcome = new RuneOutcome();
            if (state.Disabled)
            {
                return outcome;
            }
            state.PendingCasts.RemoveAll(p => p.Actor == actor);
            if (focus.Kind == RuneKind.Defense && success)
            {
                return Disable(state, now, actor, RuneRoute.Dispel, $"Dispel check {checkResult} against DC {dc}.", outcome);
            }
            outcome.FailedActors.Add(actor);
            outcome.Detail = focus.Kind == RuneKind.Defense ? $"Dispel check {checkResult} failed DC {dc}." : "No route of this focus answers dispelling.";
            return outcome;
        }

        public static RuneOutcome ApplySkill(RuneFocus focus, RuneFocusState state, double now, string actor, RuneSkill skill, int result)
        {
            var outcome = new RuneOutcome();
            if (state.Disabled)
            {
                return outcome;
            }
            RuneRoute? route = focus.Kind == RuneKind.Armor && skill == RuneSkill.Trickery ? RuneRoute.TrickerySkill : focus.Kind == RuneKind.Control && skill == RuneSkill.UseMagicDevice ? RuneRoute.UseMagicDeviceSkill : (RuneRoute?)null;
            if (route.HasValue && result >= SkillDC)
            {
                return Disable(state, now, actor, route.Value, $"{skill} {result} against DC {SkillDC}.", outcome);
            }
            outcome.FailedActors.Add(actor);
            outcome.Detail = route.HasValue ? $"{skill} {result} is short of DC {SkillDC}." : $"{skill} does not reach this focus.";
            return outcome;
        }

        /// <summary>
        /// Reports whether a status is currently on the focus, from whichever effect applies it. A sustained attempt succeeds
        /// at its full continuous duration, and fails once when the status lapses early.
        /// </summary>
        public static RuneOutcome ObserveCondition(RuneFocus focus, RuneFocusState state, double now, RuneCondition condition, bool active, string actor, bool originQualifies)
        {
            var outcome = new RuneOutcome();
            if (state.Disabled || focus.Kind != RuneKind.Alacrity)
            {
                return outcome;
            }
            RuneSustain sustain = state.Sustains.FirstOrDefault(s => s.Condition == condition);
            if (active && sustain == null)
            {
                if (condition == RuneCondition.Entangled && !originQualifies)
                {
                    return outcome;
                }
                state.PendingCasts.RemoveAll(p => p.Actor == actor);
                state.Sustains.Add(new RuneSustain { Condition = condition, StartedAt = now, Actor = actor });
                outcome.Pending = true;
                outcome.Detail = $"{condition} begun at {now:0}.";
                return outcome;
            }
            if (sustain == null)
            {
                return outcome;
            }
            int required = condition == RuneCondition.Entangled ? EntangleRounds : StaggerRounds;
            double continuous = now - sustain.StartedAt;
            if (continuous >= required * WardProfile.RoundSeconds)
            {
                state.Sustains.Remove(sustain);
                return Disable(state, now, sustain.Actor, condition == RuneCondition.Entangled ? RuneRoute.EntangledDuration : RuneRoute.StaggerParalysisDuration, $"{condition} held {continuous / WardProfile.RoundSeconds:0.0} rounds.", outcome);
            }
            if (!active)
            {
                state.Sustains.Remove(sustain);
                outcome.FailedActors.Add(sustain.Actor);
                outcome.Detail = $"{condition} ended after {continuous / WardProfile.RoundSeconds:0.0} of {required} rounds.";
            }
            else
            {
                outcome.Pending = true;
            }
            return outcome;
        }

        /// <summary>Closes elapsed windows and expired negative levels. Each participant is named at most once per call.</summary>
        public static RuneOutcome Tick(RuneFocus focus, RuneFocusState state, double now)
        {
            var outcome = new RuneOutcome();
            if (state.Disabled)
            {
                return outcome;
            }
            foreach (RuneWindow window in state.Windows.Where(w => now >= w.ClosesAt).ToList())
            {
                state.Windows.Remove(window);
                foreach (string actor in window.Contributors)
                {
                    AddFailure(outcome, actor);
                }
                outcome.Detail = $"{window.Route}: {window.Accumulated} when its window closed.";
            }
            foreach (RuneNegativeLevel level in state.NegativeLevels.Where(l => l.ExpiresAt >= 0d && now >= l.ExpiresAt).ToList())
            {
                state.NegativeLevels.Remove(level);
                AddFailure(outcome, level.Actor);
                outcome.Detail = "Negative levels expired short of the route.";
            }
            foreach (RunePendingCast cast in state.PendingCasts.Where(p => now >= p.ResolvesAt).ToList())
            {
                state.PendingCasts.Remove(cast);
                AddFailure(outcome, cast.Actor);
                outcome.Detail = "A spell delivered at the focus changed nothing.";
            }
            return outcome;
        }

        private static void AddFailure(RuneOutcome outcome, string actor)
        {
            if (!outcome.FailedActors.Contains(actor))
            {
                outcome.FailedActors.Add(actor);
            }
        }

        private static RuneWindow OpenWindow(RuneFocusState state, RuneRoute route, string ability, double now, double seconds, string actor)
        {
            RuneWindow window = state.Windows.FirstOrDefault(w => w.Route == route && w.Ability == ability);
            if (window == null)
            {
                window = new RuneWindow { Route = route, Ability = ability, OpenedAt = now, ClosesAt = now + seconds };
                state.Windows.Add(window);
            }
            if (!window.Contributors.Contains(actor))
            {
                window.Contributors.Add(actor);
            }
            return window;
        }

        private static RuneOutcome Disable(RuneFocusState state, double now, string actor, RuneRoute route, string detail, RuneOutcome outcome)
        {
            state.Disabled = true;
            state.DisabledBy = actor;
            state.DisabledRoute = route.ToString();
            state.DisabledAt = now;
            state.Windows.Clear();
            state.Sustains.Clear();
            state.PendingCasts.Clear();
            state.NegativeLevels.Clear();
            outcome.Disabled = true;
            outcome.Route = route;
            outcome.Pending = false;
            outcome.FailedActors.Clear();
            outcome.Detail = detail;
            return outcome;
        }
    }
}
