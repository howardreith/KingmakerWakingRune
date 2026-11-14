using System;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.Visual.Animation.Kingmaker;
using UnityEngine;

namespace KingmakerWakingRune.Sanctum.Wards
{
    /// <summary>
    /// One physical action on a fixture by one body. The executor walks into reach, plays the use animation, and only then
    /// does the action resolve, against that same unit. Interrupting it beforehand, by any new order, does nothing at all.
    /// </summary>
    internal sealed class KwrFixtureCommand : UnitCommand
    {
        private readonly string m_Label;
        private readonly bool m_OutOfCombatOnly;
        private readonly Func<UnitEntityData, bool> m_Valid;
        private readonly Action<UnitEntityData> m_Body;

        private KwrFixtureCommand(TargetWrapperProxy target, float reach, string label, bool outOfCombatOnly, Func<UnitEntityData, bool> valid, Action<UnitEntityData> body)
            : base(CommandType.Standard, target.Wrapper)
        {
            m_Label = label;
            m_OutOfCombatOnly = outOfCombatOnly;
            m_Valid = valid;
            m_Body = body;
            ApproachRadius = reach;
            NeedLoS = false;
        }

        public static KwrFixtureCommand AtUnit(UnitEntityData target, float reach, string label, bool outOfCombatOnly, Func<UnitEntityData, bool> valid, Action<UnitEntityData> body)
        {
            return new KwrFixtureCommand(new TargetWrapperProxy(target), reach, label, outOfCombatOnly, valid, body);
        }

        public static KwrFixtureCommand AtPoint(Vector3 point, float reach, string label, bool outOfCombatOnly, Func<UnitEntityData, bool> valid, Action<UnitEntityData> body)
        {
            return new KwrFixtureCommand(new TargetWrapperProxy(point), reach, label, outOfCombatOnly, valid, body);
        }

        /// <summary>Issues the action on its actor, replacing whatever it was doing, as a player's order would.</summary>
        public static void Issue(UnitEntityData actor, KwrFixtureCommand command)
        {
            command.CreatedByPlayer = actor.IsDirectlyControllable;
            actor.Commands.Run(command);
            Log.Info($"Physical action '{command.m_Label}' ordered for {actor.CharacterName}.");
        }

        public override bool ShouldBeInterrupted => m_OutOfCombatOnly && Executor.IsInCombat;

        /// <summary>
        /// An unseen actor's hands guard (a weapon being drawn or put away) can stay set until it is seen again, and the native
        /// controller starts no command that waits for hands: a fleeing cultist out of sight stood at a closed door
        /// indefinitely (WR-PLAYTEST-01 review). Out of sight, the action does not wait for the hands; in sight it does.
        /// </summary>
        public override bool DontWaitForHands => !Executor.IsVisibleForPlayer;

        protected override void TriggerAnimation()
        {
            base.TriggerAnimation();
            StartAnimation(UnitAnimationType.Interact, h => h.InteractionType = UnitAnimationInteractionType.UseObject);
        }

        protected override ResultType OnAction()
        {
            if ((m_OutOfCombatOnly && Executor.IsInCombat) || !Executor.Descriptor.State.CanAct || (m_Valid != null && !m_Valid(Executor)))
            {
                Log.Info($"Physical action '{m_Label}' by {Executor.CharacterName} no longer valid; nothing happens.");
                return ResultType.Fail;
            }
            Log.Info($"Physical action '{m_Label}' delivered by {Executor.CharacterName}.");
            m_Body(Executor);
            return ResultType.Success;
        }

        protected override string GetInnerDataDescription()
        {
            return m_Label;
        }

        /// <summary>Lets the two constructors share one base call without an ambiguous overload.</summary>
        private struct TargetWrapperProxy
        {
            public readonly Kingmaker.Utility.TargetWrapper Wrapper;

            public TargetWrapperProxy(UnitEntityData unit)
            {
                Wrapper = unit;
            }

            public TargetWrapperProxy(Vector3 point)
            {
                Wrapper = point;
            }
        }
    }
}
