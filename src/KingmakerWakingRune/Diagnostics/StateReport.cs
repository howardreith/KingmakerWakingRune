using System.Linq;
using System.Text;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;

namespace KingmakerWakingRune.Diagnostics
{
    /// <summary>Read-only snapshot of the facts the acceptance checks need, for the test harness.</summary>
    internal static class StateReport
    {
        public static string Build()
        {
            var sb = new StringBuilder();
            Player player = Game.Instance.Player;
            sb.AppendLine($"mode={Game.Instance.CurrentMode} area={Game.Instance.CurrentlyLoadedArea?.name} gameId={player.GameId}");
            sb.AppendLine($"money={player.Money}");
            var table = Kingmaker.Blueprints.Root.BlueprintRoot.Instance.Progression.XPTable;
            sb.AppendLine($"xpTable: level11={table.GetBonus(11)} level12={table.GetBonus(12)}");
            var flags = player.UnlockableFlags;
            sb.AppendLine("flags: " + string.Join(", ", flags.UnlockedFlags.Keys.Where(f => f.name.StartsWith("KWR_")).Select(f => f.name).ToArray()));
            sb.AppendLine("party (level / xp):");
            foreach (var reference in player.PartyCharacters.Concat(player.RemoteCompanions))
            {
                UnitEntityData unit = reference.Value;
                var progression = unit.Descriptor.Progression;
                string classes = string.Join("/", progression.Classes.Select(c => $"{c.CharacterClass.name}{c.Level}").ToArray());
                bool active = player.PartyCharacters.Contains(reference);
                sb.AppendLine($"  {(active ? "active" : "reserve")} '{unit.CharacterName}' level={progression.CharacterLevel} xp={progression.Experience} canLevel={Kingmaker.UnitLogic.Class.LevelUp.LevelUpController.CanLevelUp(unit.Descriptor)} classes={classes} inGame={unit.IsInGame} custom={unit.IsCustomCompanion()} pos={unit.Position}");
            }
            sb.AppendLine("pets:");
            foreach (UnitEntityData pet in Game.Instance.State.Units.Where(u => u.Descriptor.IsPet))
            {
                sb.AppendLine($"  '{pet.CharacterName}' bp={pet.Blueprint.name} id={pet.UniqueId} master={pet.Descriptor.Master.Value?.CharacterName} level={pet.Descriptor.Progression.CharacterLevel} inGame={pet.IsInGame} pos={pet.Position}");
            }
            UnitEntityData hero = player.MainCharacter.Value;
            sb.AppendLine("belt: " + string.Join(", ", hero.Body.QuickSlots.Select(s => s.HasItem ? $"{s.Item.Blueprint.name}x{s.Item.Count}" : "-").ToArray()));
            sb.AppendLine($"tokens={player.Inventory.Where(i => i.Blueprint.name == "KWR_RefugeToken").Sum(i => i.Count)} lootSetting={Game.Instance.CurrentlyLoadedArea?.LootSetting} indoor={Game.Instance.CurrentlyLoadedArea?.IsIndoor} grid=({hero.Position.x / 1.524f + 13f:0.0},{15.5f - hero.Position.z / 1.524f:0.0})");
            foreach (var reference in player.PartyCharacters)
            {
                UnitEntityData unit = reference.Value;
                var favored = unit.Stats.SkillPerception.Modifiers.Where(m => m.Source != null && m.Source.Blueprint.name.StartsWith("FavoriteTerrain")).Select(m => $"{m.Source.Blueprint.name}+{m.ModValue}").ToArray();
                if (unit.Get<Kingmaker.UnitLogic.Parts.UnitPartFavoredTerrain>() != null)
                {
                    sb.AppendLine($"favored '{unit.CharacterName}' perception={unit.Stats.SkillPerception.ModifiedValue} favoredModifiers=[{string.Join(",", favored)}]");
                }
            }
            sb.AppendLine("authored NPCs:");
            foreach (UnitEntityData unit in Game.Instance.State.Units.Where(u => u.Blueprint.name.StartsWith("KWR_")).OrderBy(u => u.Blueprint.name))
            {
                string portrait = unit.Portrait?.HalfLengthPortrait?.texture != null ? $"{unit.UISettings.PortraitBlueprint?.name} {unit.Portrait.HalfLengthPortrait.texture.width}x{unit.Portrait.HalfLengthPortrait.texture.height}" : "<none>";
                sb.AppendLine($"  '{unit.CharacterName}' bp={unit.Blueprint.name} id={unit.UniqueId} pos={unit.Position} yaw={unit.Orientation:0} portrait={portrait} interactions={unit.Blueprint.ComponentsArray.Length}");
            }
            var vendor = Game.Instance.State.Units.FirstOrDefault(u => u.Blueprint.name == "KWR_quartermaster");
            if (vendor != null)
            {
                var stock = vendor.VendorInventory.Items.ToList();
                sb.AppendLine($"shop: lines={stock.Count} items={stock.Sum(i => i.Count)} sellModifier={Game.Instance.BlueprintRoot.Vendors.SellModifier}");
                foreach (var item in stock.Take(8))
                {
                    sb.AppendLine($"  stock {item.Blueprint.Name} x{item.Count} cost={item.Cost}");
                }
            }
            sb.AppendLine($"inventory lines={player.Inventory.Items.Count}");
            foreach (var item in player.Inventory.Items)
            {
                sb.AppendLine($"  item {item.Blueprint.Name} x{item.Count}");
            }
            return sb.ToString();
        }
    }
}
