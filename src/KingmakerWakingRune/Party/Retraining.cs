using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using KingmakerWakingRune.Blueprints;
using KingmakerWakingRune.Lodge;
using KingmakerWakingRune.Sanctum;
using UnityEngine;

namespace KingmakerWakingRune.Party
{
    /// <summary>
    /// The Society Coordinator's free retraining (WR-PLAYTEST-02): the native respec (Player.RespecCompanion), which rebuilds
    /// the chosen character in place from level 1 through the character-build window and keeps its identity, experience,
    /// portrait, inventory and party place. Only the native dialogue action charges a fee, counts uses and advances a day;
    /// this service calls the rebuild itself, so it is free every time and the clock does not move. Nothing global changes:
    /// the native fee schedule, the difficulty's respec setting and other campaigns are untouched, and installed respec or
    /// rolled-score mods act on the same native call exactly as they would on the stock one.
    /// The rebuild is bound to the character the player named (the protagonist option, or the one chosen in the native
    /// selector) when the native rebuild starts; a canceled build changes nothing. On success the character's equipment,
    /// which the native rebuild moves to the shared inventory, is put back where it still fits; an animal companion the new
    /// build no longer has gives its gear to the shared inventory before the native rebuild retires it; and the character's
    /// part of a readied departure is dropped, so the next use of the token readies the rebuilt character in full.
    /// The native character creation behind the rebuild also hands out the new class's starting gold and starting kit; both
    /// are taken back, so the treasury changes only by the native refund for scrolls a caster had copied (the rebuilt
    /// spellbook loses them), and nothing is gained by retraining. As the build window opens, the character's current
    /// gender, race, portrait, name, alignment and voice are preselected; each can still be changed there.
    /// The native rebuild repopulates the unit from a fresh one, which drops every part the scenario keeps on it (on the
    /// protagonist: the departure record, the Quartermaster's sold-charges record and the sanctum's state with the
    /// expedition's outcome). Those part objects are carried across the rebuild unchanged, and the character's departure
    /// entry is dropped, for every native respec of a party member, not only the Coordinator's: the native selector, or
    /// another mod's respec (Eddic's F2 selector) that calls the same native rebuild, is covered too.
    /// </summary>
    internal static class Retraining
    {
        private const float StartSeconds = 5f;

        private static Operation s_Active;

        /// <summary>The scenario's parts of each unit whose native rebuild has started and not yet finished, whoever started it.</summary>
        private static readonly Dictionary<UnitEntityData, List<UnitPart>> s_Carry = new Dictionary<UnitEntityData, List<UnitPart>>();

        private sealed class Operation
        {
            public string Option;
            public UnitEntityData Expected;
            public List<UnitEntityData> Allowed;
            public UnitEntityData Target;
            public Player World;
            public List<KeyValuePair<int, ItemEntity>> Equipped;
            public UnitEntityData Pet;
            public int Experience;
            public long Money;
            public int Items;
            public Dictionary<ItemEntity, int> Owned;
            public long ScrollRefund;
            public string Name;
            public Kingmaker.Blueprints.BlueprintPortrait Portrait;
            public Kingmaker.Enums.Alignment Alignment;
            public Kingmaker.Visual.Sound.BlueprintUnitAsksList Voice;
            public Gender Gender;
            public Kingmaker.Blueprints.Classes.BlueprintRace Race;
            public bool Prefilled;
            public int ShownFrame;
            public float Armed;
            public bool SelectorClosed;
            public bool BuildSeen;
        }

        /// <summary>The protagonist's own option.</summary>
        public static void RequestProtagonist()
        {
            Game.Instance.ScheduleAction(() => Open(true));
        }

        /// <summary>A recruited companion, chosen in the native selector.</summary>
        public static void RequestCompanion()
        {
            Game.Instance.ScheduleAction(() => Open(false));
        }

        /// <summary>True while a retraining is between its request and its end (success or cancel).</summary>
        public static bool InProgress => s_Active != null;

        /// <summary>The pet whose gear must be kept if the native rebuild retires it (see <see cref="Patches.RetrainingPetPatch"/>).</summary>
        public static UnitEntityData RetiringPet => s_Active?.Target != null ? s_Active.Pet : null;

        private static void Open(bool protagonist)
        {
            Game game = Game.Instance;
            string refusal = RetrainingRules.Refusal(
                game.CurrentlyLoadedArea == ScenarioBlueprints.Area,
                game.Player.IsInCombat,
                RefugeTransit.InProgress || PortalTransit.InProgress || CheckpointOperation.AnyPending,
                game.UI.CharacterBuildController != null && game.UI.CharacterBuildController.IsShow,
                LoadingProcess.Instance.IsLoadingInProcess,
                InProgress);
            if (refusal != null)
            {
                Log.Info($"Retraining refused: {refusal}");
                Warn(refusal);
                return;
            }
            UnitEntityData main = game.Player.MainCharacter.Value;
            List<UnitEntityData> eligible = Eligible().Where(u => protagonist ? u == main : u != main).ToList();
            if (eligible.Count == 0)
            {
                string text = protagonist ? "The protagonist cannot be retrained now." : "No recruited companion can be retrained.";
                Log.Info($"Retraining refused: {text}");
                Warn(text);
                return;
            }
            s_Active = new Operation
            {
                Option = protagonist ? "protagonist" : "companion",
                Expected = protagonist ? main : null,
                Allowed = eligible,
                World = game.Player,
                Armed = Time.realtimeSinceStartup,
            };
            Watch.Ensure();
            Log.Info($"Retraining opened ({s_Active.Option}): eligible [{string.Join(", ", eligible.Select(u => u.CharacterName).ToArray())}]; treasury {game.Player.Money} gp.");
            if (protagonist)
            {
                game.Player.RespecCompanion(main, OnSucceeded);
                return;
            }
            EventBus.RaiseEvent(delegate(ICharacterSelectorHandler h) { h.HandleSelectCharacter(eligible, OnSucceeded); });
        }

        /// <summary>Living party members and reserve companions the native respec accepts (its own CanRespec), pets excluded.</summary>
        private static IEnumerable<UnitEntityData> Eligible()
        {
            Player player = Game.Instance.Player;
            return player.PartyCharacters.Concat(player.RemoteCompanions).Select(r => r.Value).Where(u => u != null).Distinct().Where(u =>
                RetrainingRules.CanRetrain(u.Descriptor.State.IsFinallyDead, u.Descriptor.IsPet, u.Descriptor.Progression.CharacterLevel,
                    u.Blueprint.GetComponent<ClassLevelLimit>()?.LevelLimit ?? 0));
        }

        /// <summary>
        /// The native rebuild is starting for <paramref name="unit"/> (Player.RespecCompanion prefix). The open retraining binds
        /// to it only if it is the character the player named; any other respec is left entirely to the game.
        /// </summary>
        public static void BeforeRespec(UnitEntityData unit)
        {
            s_Carry[unit] = Parts(unit.Descriptor).Values.Where(p => p.GetType().Assembly == typeof(Retraining).Assembly).ToList();
            Operation op = s_Active;
            if (op == null || op.Target != null || !op.Allowed.Contains(unit) || (op.Expected != null && op.Expected != unit))
            {
                return;
            }
            op.Target = unit;
            op.Experience = unit.Descriptor.Progression.Experience;
            op.Money = Game.Instance.Player.Money;
            op.Items = Game.Instance.Player.Inventory.Items.Count;
            op.Owned = unit.Inventory.Items.ToDictionary(i => i, i => i.Count);
            op.ScrollRefund = Game.Instance.Player.GetScrollRefundForRespec(unit.Descriptor);
            op.Name = unit.CharacterName;
            op.Portrait = unit.UISettings.PortraitBlueprint;
            op.Alignment = unit.Descriptor.Alignment.Value;
            op.Voice = unit.Descriptor.Asks;
            op.Gender = unit.Descriptor.Gender;
            op.Race = unit.Descriptor.Progression.Race;
            op.Pet = unit.Descriptor.Pet;
            op.Equipped = unit.Body.EquipmentSlots.Select((slot, index) => new KeyValuePair<int, ItemEntity>(index, slot.MaybeItem)).Where(p => p.Value != null).ToList();
            Log.Info($"Retraining started for {Describe(unit)}: xp {op.Experience}, treasury {op.Money} gp (native refund for copied scrolls {op.ScrollRefund} gp), {op.Equipped.Count} equipped items, pet {op.Pet?.CharacterName ?? "none"}.");
        }

        /// <summary>
        /// The native build window has finished opening for the bound rebuild (its own OnShow resets selections, so this runs
        /// a few frames later): the character's current identity is preselected through the window's own setters, so leaving
        /// a step untouched keeps it. Steps a story companion does not get simply refuse the selection.
        /// </summary>
        private static void Prefill(Kingmaker.UI.LevelUp.CharacterBuildController controller)
        {
            Operation op = s_Active;
            if (op?.Target == null || op.Prefilled || controller?.LevelUpController == null)
            {
                return;
            }
            op.Prefilled = true;
            var applied = new List<string>();
            // The race goes to the level-up model directly (the window's own SetRace also redraws a race page that is not built
            // until it is first shown); the gender set after it refreshes the window from the model.
            Try(applied, "race", () => { if (op.Race != null && !controller.LevelUpController.SelectRace(op.Race)) throw new InvalidOperationException("the race step refused it"); });
            Try(applied, "gender", () => controller.SetGender(op.Gender));
            Try(applied, "portrait", () => { if (op.Portrait != null) controller.SetPortrait(op.Portrait); });
            Try(applied, "name", () => { if (!string.IsNullOrEmpty(op.Name)) controller.SetName(op.Name); });
            Try(applied, "alignment", () => controller.SelectAlignment(op.Alignment));
            Try(applied, "voice", () => { if (op.Voice != null) controller.SetVoice(op.Voice); });
            Log.Info($"Retraining of {op.Target.CharacterName}: build window opened with the current {string.Join(", ", applied.ToArray())} preselected ({op.Gender} {op.Race?.name}, portrait {op.Portrait?.name ?? "custom, choose it again"}, {op.Alignment}).");
        }

        private static void Try(List<string> applied, string what, Action set)
        {
            try
            {
                set();
                applied.Add(what);
            }
            catch (Exception ex)
            {
                Log.Error($"Retraining: the current {what} could not be preselected; it is chosen in the window as usual.", ex);
            }
        }

        /// <summary>The native success callback: the character was rebuilt at level 1 and its earned levels are open to take.</summary>
        private static void OnSucceeded()
        {
            Operation op = s_Active;
            s_Active = null;
            if (op?.Target == null)
            {
                Log.Error("Retraining reported success with no bound character; nothing else was changed.");
                return;
            }
            UnitEntityData unit = op.Target;
            string carried = CarryAcross(unit);
            try
            {
                var kept = new List<string>();
                var stored = new List<string>();
                List<ItemSlot> slots = unit.Body.EquipmentSlots.ToList();
                foreach (KeyValuePair<int, ItemEntity> pair in op.Equipped)
                {
                    ItemEntity item = pair.Value;
                    ItemSlot slot = pair.Key < slots.Count ? slots[pair.Key] : null;
                    if (item.Collection != null && item.HoldingSlot == null && slot != null && !slot.HasItem && slot.CanInsertItem(item))
                    {
                        slot.InsertItem(item);
                        kept.Add(item.Name);
                    }
                    else
                    {
                        stored.Add(item.Name);
                    }
                }
                string kit = TakeBackStartingKit(unit, op);
                // The native rebuild retires a former pet either at once (BeforePetRetired, through the Destroy hook) or,
                // when its owner's new build spawns a fresh companion, at a later level-up, after this retraining has
                // ended. A former pet still standing here gives its gear to the shared stash now, so neither path loses it.
                if (op.Pet != null && op.Pet != unit.Descriptor.Pet && Game.Instance.State.Units.Contains(op.Pet))
                {
                    BeforePetRetired(op.Pet);
                }
                Log.Info($"Retraining succeeded for {Describe(unit)}: level {unit.Descriptor.Progression.CharacterLevel} of xp {unit.Descriptor.Progression.Experience} (was {op.Experience}); " +
                         $"treasury {Game.Instance.Player.Money} gp (was {op.Money}; native refund for copied scrolls {op.ScrollRefund} gp); {kit}; inventory {Game.Instance.Player.Inventory.Items.Count} entries (was {op.Items}); " +
                         $"re-equipped [{string.Join(", ", kept.ToArray())}]; left in the shared inventory [{string.Join(", ", stored.ToArray())}]; pet {unit.Descriptor.Pet?.CharacterName ?? "none"} (was {op.Pet?.CharacterName ?? "none"}); scenario records carried across [{carried}].");
                if (stored.Count > 0)
                {
                    Warn($"{unit.CharacterName} is retrained. Items that could not be put back on are in the shared inventory: {string.Join(", ", stored.ToArray())}.");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Retraining of {unit.CharacterName} succeeded natively; restoring its equipment failed, so the items stay in the shared inventory.", ex);
            }
        }

        private static readonly System.Reflection.FieldInfo PartsField = Harmony12.AccessTools.Field(typeof(UnitPartsManager), "m_Parts");
        private static readonly System.Reflection.FieldInfo ManagerField = Harmony12.AccessTools.Field(typeof(UnitDescriptor), "m_Parts");

        private static Dictionary<Type, UnitPart> Parts(UnitDescriptor descriptor)
        {
            return (Dictionary<Type, UnitPart>)PartsField.GetValue(ManagerField.GetValue(descriptor));
        }

        /// <summary>
        /// The native rebuild finished (IUnitChangedAfterRespecHandler, raised by every native respec after its success
        /// callback): a rebuild nobody here started gets the same protection as the Coordinator's.
        /// </summary>
        public static void AfterAnyRespec(UnitEntityData unit)
        {
            if (unit != null && s_Carry.ContainsKey(unit))
            {
                Log.Info($"Respec of {Describe(unit)} outside the Coordinator finished; scenario records carried across [{CarryAcross(unit)}].");
            }
        }

        /// <summary>
        /// Once per finished rebuild: puts the scenario's own parts back on the rebuilt unit, as the same objects, where the
        /// rebuild dropped them, then drops the character's entry from a readied departure.
        /// </summary>
        private static string CarryAcross(UnitEntityData unit)
        {
            if (!s_Carry.TryGetValue(unit, out List<UnitPart> kept))
            {
                return "";
            }
            s_Carry.Remove(unit);
            string names = Restore(unit, kept);
            DeparturePreparation.ForgetMember(unit, "retrained");
            return names;
        }

        /// <summary>Puts the scenario's own parts back on the rebuilt unit, as the same objects, where the rebuild dropped them.</summary>
        private static string Restore(UnitEntityData unit, List<UnitPart> kept)
        {
            var names = new List<string>();
            try
            {
                Dictionary<Type, UnitPart> parts = Parts(unit.Descriptor);
                foreach (UnitPart part in kept ?? new List<UnitPart>())
                {
                    if (!parts.ContainsKey(part.GetType()))
                    {
                        part.SetOwner(unit.Descriptor);
                        parts.Add(part.GetType(), part);
                        EventBus.Subscribe(part);
                    }
                    names.Add(part.GetType().Name);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Retraining of {unit.CharacterName}: the scenario's records could not all be carried across the rebuild.", ex);
            }
            return string.Join(", ", names.ToArray());
        }

        /// <summary>
        /// The class starting gold and starting kit the native character creation added (LevelUpHelper): the gold beyond the
        /// native scroll refund is spent back, and the kit's new items (or the count they added to an existing stack) are
        /// removed. Items the party owned before the rebuild are never touched.
        /// </summary>
        private static string TakeBackStartingKit(UnitEntityData unit, Operation op)
        {
            long gold = Game.Instance.Player.Money - op.Money - op.ScrollRefund;
            if (gold > 0)
            {
                Game.Instance.Player.SpendMoney(gold);
            }
            var removed = new List<string>();
            Kingmaker.Blueprints.Classes.BlueprintCharacterClass main = unit.Descriptor.Progression.GetMaxClass();
            Kingmaker.Blueprints.Classes.BlueprintArchetype replacing = main == null ? null : unit.Descriptor.Progression.GetClassData(main)?.Archetypes.FirstOrDefault(a => a.ReplaceStartingEquipment);
            Kingmaker.Blueprints.Items.BlueprintItem[] starting = replacing?.StartingItems ?? main?.StartingItems ?? new Kingmaker.Blueprints.Items.BlueprintItem[0];
            foreach (IGrouping<Kingmaker.Blueprints.Items.BlueprintItem, Kingmaker.Blueprints.Items.BlueprintItem> line in starting.Where(b => b != null).GroupBy(b => b))
            {
                int left = line.Count();
                foreach (ItemEntity item in unit.Inventory.Items.Where(i => i.Blueprint == line.Key && i.HoldingSlot == null).ToList())
                {
                    int added = item.Count - (op.Owned.TryGetValue(item, out int before) ? before : 0);
                    int take = Math.Min(left, added);
                    if (take > 0)
                    {
                        unit.Inventory.Remove(item, take);
                        left -= take;
                        removed.Add($"{line.Key.name} x{take}");
                    }
                }
            }
            return $"class starting gold taken back {Math.Max(0, gold)} gp, starting kit taken back [{string.Join(", ", removed.ToArray())}]";
        }

        /// <summary>
        /// Before the native rebuild destroys a pet the new build no longer has: its gear goes to the shared inventory. Its
        /// natural attacks (claws, bites) are part of the creature, not gear, and go with it.
        /// </summary>
        public static void BeforePetRetired(UnitEntityData pet)
        {
            var moved = new List<string>();
            foreach (ItemSlot slot in pet.Body.EquipmentSlots.Where(s => s.HasItem && !(s.Item.Blueprint is Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon w && w.IsNatural)).ToList())
            {
                string name = slot.Item.Name;
                if (slot.RemoveItem())
                {
                    moved.Add(name);
                }
            }
            Log.Info($"Retraining: the former pet {pet.CharacterName} ({pet.UniqueId}) is no longer its owner's companion; its gear went to the shared inventory [{string.Join(", ", moved.ToArray())}].");
        }

        /// <summary>A load or new game ends whatever was open; nothing of it applies to the new world.</summary>
        public static void Reset(string reason)
        {
            s_Carry.Clear();
            if (s_Active != null)
            {
                Log.Info($"Retraining ({s_Active.Option}) cleared: {reason}; nothing was changed.");
                s_Active = null;
            }
        }

        private static void Cancel(string why)
        {
            Operation op = s_Active;
            s_Active = null;
            Log.Info($"Retraining ({op.Option}{(op.Target != null ? $", {op.Target.CharacterName}" : "")}) canceled: {why}; the character is unchanged and nothing was spent (treasury {Game.Instance.Player.Money} gp).");
        }

        private static string Describe(UnitEntityData unit)
        {
            return $"{unit.CharacterName} ({unit.UniqueId}{(unit.IsMainCharacter ? ", protagonist" : "")})";
        }

        private static void Warn(string text)
        {
            EventBus.RaiseEvent(delegate(IWarningNotificationUIHandler h) { h.HandleWarning(text); });
        }

        /// <summary>Notes the native selector closing (Patches.RetrainingSelectorPatch).</summary>
        public static void OnSelectorClosed()
        {
            if (s_Active != null)
            {
                s_Active.SelectorClosed = true;
            }
        }

        /// <summary>Ends an open retraining that the player left: the selector closed with nobody chosen, or the build window closed before its first level was confirmed.</summary>
        private sealed class Watch : MonoBehaviour
        {
            private static Watch s_Instance;

            public static void Ensure()
            {
                if (s_Instance == null)
                {
                    var host = new GameObject("KWR_RetrainingWatch");
                    DontDestroyOnLoad(host);
                    s_Instance = host.AddComponent<Watch>();
                }
            }

            private void LateUpdate()
            {
                Operation op = s_Active;
                if (op == null)
                {
                    return;
                }
                if (!ReferenceEquals(Game.Instance.Player, op.World))
                {
                    Reset("another game was loaded");
                    return;
                }
                bool buildOpen = Game.Instance.UI.CharacterBuildController != null && Game.Instance.UI.CharacterBuildController.IsShow;
                if (op.Target == null)
                {
                    if (op.SelectorClosed)
                    {
                        Cancel("the selector was closed without a choice");
                    }
                    else if (op.Expected != null && Time.realtimeSinceStartup - op.Armed > StartSeconds)
                    {
                        Cancel("the native rebuild did not start");
                    }
                    return;
                }
                if (buildOpen)
                {
                    if (!op.BuildSeen)
                    {
                        op.BuildSeen = true;
                        op.ShownFrame = Time.frameCount;
                    }
                    else if (!op.Prefilled && Time.frameCount - op.ShownFrame >= 3)
                    {
                        Prefill(Game.Instance.UI.CharacterBuildController);
                    }
                }
                else if (op.BuildSeen || Time.realtimeSinceStartup - op.Armed > StartSeconds + 60f)
                {
                    Cancel("the character build was closed before its first level was confirmed");
                }
            }
        }
    }
}
