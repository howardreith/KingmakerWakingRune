using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Commands.Base;

namespace KingmakerWakingRune.Sanctum.Wards
{
    /// <summary>Opens the fixture's own inspection dialogue, chosen from the unit's saved identity when clicked.</summary>
    public sealed class KwrFixtureInteraction : UnitInteractionComponent
    {
        public override UnitCommand.ResultType Interact(UnitEntityData user, UnitEntityData target)
        {
            string id = WardFixtures.IdOf(target);
            if (id == null)
            {
                return UnitCommand.ResultType.Fail;
            }
            WardInteract.Open(id, user);
            return UnitCommand.ResultType.Success;
        }
    }
}

namespace KingmakerWakingRune.Sanctum.Wards
{
    /// <summary>Clears the mist's sleep from the ally this touch reached.</summary>
    public sealed class KwrWakeAction : Kingmaker.UnitLogic.Mechanics.Actions.ContextAction
    {
        public override string GetCaption()
        {
            return "Wake a sleeper";
        }

        public override void RunAction()
        {
            Kingmaker.EntitySystem.Entities.UnitEntityData unit = Target.Unit;
            if (unit != null)
            {
                WardRuntime.WakeSleeper(unit);
            }
        }
    }
}
