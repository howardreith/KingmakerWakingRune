using System;
using System.Collections.Generic;
using System.Linq;

// Saved fields: some are only ever written by the native adapters or the JSON reader.
#pragma warning disable CS0649

namespace KingmakerWakingRune.Sanctum.Wards
{
    internal enum CredentialKind
    {
        Impression,
        Hand,
    }

    /// <summary>One credential item the party really holds, tied to the marked figure it came from.</summary>
    internal sealed class CredentialRecord
    {
        public string ItemId = string.Empty;
        public CredentialKind Kind;
        public string SourceSlot = string.Empty;
        public string MadeBy = string.Empty;
        public double MadeAt = -1d;
    }

    /// <summary>One authorised contact with A2's handprint.</summary>
    internal sealed class DoorPresentation
    {
        public string Actor = string.Empty;
        public string Credential = string.Empty;
        public string SourceSlot = string.Empty;
        public double At = -1d;
    }

    internal sealed class CredentialState
    {
        /// <summary>Marked palms the party has looked at; a note only, never a key.</summary>
        public List<string> MarksSeen = new List<string>();
        public List<CredentialRecord> Items = new List<CredentialRecord>();
        public List<string> HandsRecovered = new List<string>();
        public List<DoorPresentation> Presentations = new List<DoorPresentation>();
        /// <summary>Old saves whose marked bodies are gone get one remnant at the figure's place, recorded so it never repeats.</summary>
        public List<string> RemnantSlots = new List<string>();
    }

    /// <summary>
    /// The standalone A2 adaptation (docs/WR-04-RULES.md, not a printed rule): two A1 cultists carry an authentic Sihedron
    /// palm mark, and a precise impression of one, or the marked hand itself, satisfies the door's old sign-based mechanism.
    /// It is a local identity check and grants nothing else.
    /// </summary>
    internal static class CredentialRules
    {
        public static readonly IReadOnlyList<string> MarkedSources = new[] { "Support-N1", "Support-N4" };

        public static bool IsMarked(string slot)
        {
            return MarkedSources.Contains(slot);
        }

        /// <summary>A visible mark on a petrified figure can be copied while the party holds no impression already.</summary>
        public static bool CanTakeImpression(string slot, StatueOutcome outcome, bool partyHoldsImpression)
        {
            return IsMarked(slot) && outcome == StatueOutcome.Stone && !partyHoldsImpression;
        }

        /// <summary>Only the actual dead body of a marked cultist, once per figure.</summary>
        public static bool CanRecoverHand(CredentialState state, string slot, bool bodyIsCorpse)
        {
            return IsMarked(slot) && bodyIsCorpse && !state.HandsRecovered.Contains(slot);
        }

        public static void RecordImpression(CredentialState state, string itemId, string slot, string actor, double now)
        {
            state.Items.Add(new CredentialRecord { ItemId = itemId, Kind = CredentialKind.Impression, SourceSlot = slot, MadeBy = actor, MadeAt = now });
        }

        /// <summary>False if this figure's hand was already taken; the caller then grants nothing.</summary>
        public static bool RecordHand(CredentialState state, string itemId, string slot, string actor, double now)
        {
            if (state.HandsRecovered.Contains(slot))
            {
                return false;
            }
            state.HandsRecovered.Add(slot);
            state.Items.Add(new CredentialRecord { ItemId = itemId, Kind = CredentialKind.Hand, SourceSlot = slot, MadeBy = actor, MadeAt = now });
            return true;
        }

        /// <summary>Forgets records for items no longer held, so a lost impression can be remade from a still-valid source.</summary>
        public static void Reconcile(CredentialState state, ICollection<string> heldItemIds)
        {
            state.Items.RemoveAll(r => r.Kind == CredentialKind.Impression && !heldItemIds.Contains(r.ItemId));
        }

        public static void RecordPresentation(CredentialState state, string actor, CredentialRecord credential, double now)
        {
            state.Presentations.Add(new DoorPresentation { Actor = actor, Credential = credential.Kind.ToString(), SourceSlot = credential.SourceSlot, At = now });
        }
    }

    /// <summary>The leaf's own durability: hardness once per independently resolved hit, never on a running total.</summary>
    internal static class DoorRules
    {
        public static int HitDamage(int amount, int hardness, bool bypassHardness)
        {
            return bypassHardness ? Math.Max(0, amount) : Math.Max(0, amount - hardness);
        }

        /// <summary>
        /// An energy component's share against stone, before hardness: the five energies are halved (core object rule) and
        /// life energy does not harm stone at all. The game's other energy kinds are not energy under that rule: force and
        /// untyped magic (Magic Missile, Disintegrate) and divine, holy or unholy power apply in full.
        /// </summary>
        public static int EnergyShare(string energyType, int amount)
        {
            switch (energyType)
            {
                case "Fire":
                case "Cold":
                case "Sonic":
                case "Electricity":
                case "Acid":
                    return amount / 2;
                case "NegativeEnergy":
                case "PositiveEnergy":
                    return 0;
                default:
                    return amount;
            }
        }

        /// <summary>Restrained feedback for a hit, from what it did and what is left.</summary>
        public static string Describe(int dealt, int left, int full)
        {
            if (dealt <= 0)
            {
                return "The blow leaves no more than a scratch.";
            }
            if (left <= 0)
            {
                return "The stone door breaks and falls.";
            }
            if (left * 4 <= full)
            {
                return "Cracks run right through the door; it cannot take much more.";
            }
            if (left * 2 <= full)
            {
                return "Chips fly and a crack opens across the door's face.";
            }
            return "Stone chips from the door's face.";
        }
    }

    internal struct WorkPlan
    {
        public double Advance;
        public bool Completed;
    }

    /// <summary>
    /// Long work is one bounded jump. It is refused outright where intermediate events would be lost (combat, someone in
    /// a recurring hazard, a sentry about to bite); a deadline inside the interval ends it there, with no reward.
    /// </summary>
    internal static class WorkRules
    {
        public static WorkPlan Plan(double now, double seconds, double? deadline)
        {
            if (deadline.HasValue && deadline.Value <= now + seconds)
            {
                return new WorkPlan { Advance = Math.Max(0d, deadline.Value - now), Completed = false };
            }
            return new WorkPlan { Advance = seconds, Completed = true };
        }
    }
}
