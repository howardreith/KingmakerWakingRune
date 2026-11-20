using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Controllers;
using Kingmaker.Controllers.Combat;
using Kingmaker.Controllers.Dialog;
using Kingmaker.Controllers.Projectiles;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.GameModes;
using Kingmaker.UnitLogic.Commands;
using UnityEngine;

namespace KingmakerWakingRune.Sanctum.Wards
{
    internal static class KurshuNotice
    {
        private static BlueprintDialog s_Notice;
        private static readonly Encounter.NoticeClock Clock = new Encounter.NoticeClock();
        private static bool s_Closing;
        internal static bool Active => s_Notice != null;

        public static void Install(string id)
        {
            var harmony = new Harmony(id);
            HarmonyMethod Hook(string name) => new HarmonyMethod(typeof(KurshuNotice), name);
            foreach (string method in new[] { "Start", "AddInvolvedUnit" })
                harmony.Patch(AccessTools.Method(typeof(DialogController), method), transpiler: Hook(nameof(ProtectCommands)),
                    finalizer: Hook(nameof(Failed)));
            harmony.Patch(AccessTools.Method(typeof(Game), "DoStartMode"), prefix: Hook(nameof(Entering)), postfix: Hook(nameof(Entered)), transpiler: Hook(nameof(KeepPauseMode)), finalizer: Hook(nameof(Failed)));
            harmony.Patch(AccessTools.Method(typeof(Game), "DoStopMode"), postfix: Hook(nameof(Exited)), finalizer: Hook(nameof(Failed)));
            harmony.Patch(AccessTools.Method(typeof(TurnBased.Controllers.CombatController), "Deactivate"),
                prefix: Hook(nameof(PreserveTurnWait)));
            // These shared controllers' Stop handlers clear active projectiles and remove area effects from their subjects.
            // They must survive the reader's mode pop. A true area unload retains its own teardown.
            foreach (var controller in new[] { typeof(ProjectileController), typeof(AreaEffectsController) })
                harmony.Patch(AccessTools.Method(controller, "Stop"), prefix: Hook(nameof(KeepCombatEffects)));
            harmony.Patch(AccessTools.Method(typeof(GameMode), nameof(GameMode.Tick)), transpiler: Hook(nameof(GuardSimulation)));
            harmony.Patch(AccessTools.Method(typeof(DialogController), "StartScheduledDialogImmediately"),
                transpiler: Hook(nameof(KeepProjectiles)), finalizer: Hook(nameof(Failed)));
            foreach (string method in new[] { "SelectAnswer", "Tick", "StopDialog", "Stop" })
                harmony.Patch(AccessTools.Method(typeof(DialogController), method), finalizer: Hook(nameof(Failed)));
            harmony.Patch(AccessTools.Method(typeof(DialogController), "Clear"), postfix: Hook(nameof(Cleared)));
        }

        // The only operation in this native deactivation is WaitingForUI.Reset(). Its outstanding
        // turn-based guard must survive the temporary switch to the reader.
        private static bool PreserveTurnWait() => !Active;
        private static bool KeepCombatEffects() => !Active || Game.Instance.CurrentMode != GameModeType.Dialog;

        private static void Entering(GameModeType type)
        {
            if (type != GameModeType.Dialog || !WardDialogs.IsCombatStory(Game.Instance.DialogController.Dialog)) return;
            if (s_Notice != null) Release();
            Clock.Capture(Game.Instance.IsPaused, Time.timeScale);
            s_Notice = Game.Instance.DialogController.Dialog;
        }

        // The stock DoStartMode pops every Pause before pushing Dialog. Keep that exact pause on
        // the stack so nested mode changes and unloading do not need to synthesize a new pause.
        private static IEnumerable<CodeInstruction> KeepPauseMode(IEnumerable<CodeInstruction> instructions)
        {
            var code = new List<CodeInstruction>(instructions);
            var stop = AccessTools.Method(typeof(Game), "DoStopMode");
            var current = AccessTools.PropertyGetter(typeof(Game), nameof(Game.CurrentMode));
            for (int i = 0; i < code.Count; i++)
            {
                var instruction = code[i];
                if (i >= 2 && code[i-2].Calls(stop) && code[i-1].opcode == OpCodes.Ldarg_0 && instruction.Calls(current))
                {
                    instruction.opcode = OpCodes.Ldarg_1;
                    instruction.operand = null;
                    yield return instruction;
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(KurshuNotice), nameof(PauseBeforeEntry)));
                }
                else yield return instruction;
            }
        }

        private static GameModeType PauseBeforeEntry(Game game, GameModeType entering)
        {
            var current = game.CurrentMode;
            return entering == GameModeType.Dialog && current == GameModeType.Pause && s_Notice != null
                ? GameModeType.Default : current;
        }

        private static void Entered(GameModeType type)
        {
            if (type == GameModeType.Dialog && Active && Game.Instance.CurrentMode == type)
            {
                Game.Instance.TimeController.Suspend();
                Game.Instance.TimeController.SetGameDeltaTime(0f);
            }
        }

        private static void Exited(GameModeType type)
        {
            // The next mode has reactivated its native clock by this point.
            if (type == GameModeType.Dialog && Game.Instance.CurrentMode != GameModeType.Dialog) Release();
        }

        private static void Cleared()
        {
            if (s_Notice == null) return;
            if (Game.Instance.CurrentMode != GameModeType.Dialog) Release();
            else Game.Instance.ScheduleAction(() =>
            {
                if (s_Notice != null && (Game.Instance.CurrentMode != GameModeType.Dialog ||
                    Game.Instance.DialogController.Dialog != null && Game.Instance.DialogController.Dialog != s_Notice)) Release();
            });
        }

        internal static void Release()
        {
            if (s_Notice == null) return;
            s_Notice = null;
            s_Closing = false;
            Clock.Restore((paused, scale) =>
            {
                if (Game.Instance.CurrentMode == GameModeType.Default || Game.Instance.CurrentMode == GameModeType.Pause)
                    Game.Instance.IsPaused = paused;
                Time.timeScale = scale;
            });
        }

        private static void Failed(Exception __exception)
        {
            if (__exception == null || s_Notice == null || s_Closing) return;
            s_Closing = true;
            var failed = s_Notice;
            Game.Instance.ScheduleAction(() =>
            {
                if (s_Notice != failed) return;
                try
                {
                    if (Game.Instance.CurrentMode == GameModeType.Dialog)
                    {
                        if (Game.Instance.DialogController.Dialog == failed) Game.Instance.DialogController.StopDialog();
                        else if (Game.Instance.DialogController.Dialog == null) Game.Instance.StopMode(GameModeType.Dialog);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Kurshu dialogue teardown failed.", ex);
                }
                finally
                {
                    // StopMode may be queued while the game is switching modes. Its postfix owns the last restore.
                    if (Game.Instance.CurrentMode == GameModeType.Dialog && s_Notice == failed &&
                        (Game.Instance.DialogController.Dialog == null || Game.Instance.DialogController.Dialog == failed))
                    {
                        try { Game.Instance.StopMode(GameModeType.Dialog); }
                        catch (Exception ex) { Log.Error("Could not pop Kurshu dialogue mode.", ex); Release(); }
                    }
                    else if (s_Notice == failed) Release();
                }
            });
        }

        private static IEnumerable<CodeInstruction> ProtectCommands(IEnumerable<CodeInstruction> instructions)
        {
            var interrupt = AccessTools.Method(typeof(UnitCommands), "InterruptAll", new[] { typeof(bool) });
            var leave = AccessTools.Method(typeof(UnitCombatLeaveController), "TickGroups");
            foreach (var instruction in instructions)
            {
                if (instruction.Calls(interrupt) || instruction.Calls(leave))
                {
                    instruction.operand = AccessTools.Method(typeof(KurshuNotice), instruction.Calls(leave) ? nameof(TickGroups) : nameof(Interrupt));
                    instruction.opcode = OpCodes.Call;
                }
                yield return instruction;
            }
        }

        private static IEnumerable<CodeInstruction> KeepProjectiles(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            // The native method already moved the scheduled dialog into a local before clearing projectiles.
            int scheduled = original.GetMethodBody().LocalVariables.Single(local => local.LocalType == typeof(StartDialogData)).LocalIndex;
            var clear = AccessTools.Method(typeof(ProjectileController), nameof(ProjectileController.Clear));
            foreach (var instruction in instructions)
            {
                if (instruction.Calls(clear))
                {
                    instruction.opcode = OpCodes.Ldloc;
                    instruction.operand = (short)scheduled;
                    yield return instruction;
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(KurshuNotice), nameof(ClearProjectiles)));
                }
                else yield return instruction;
            }
        }

        private static void ClearProjectiles(ProjectileController projectiles, StartDialogData scheduled)
        {
            if (!WardDialogs.IsCombatStory(scheduled.Dialog)) projectiles.Clear();
        }

        private static void Interrupt(UnitCommands commands, bool raiseEvent)
        {
            if (!WardDialogs.IsCombatStory(Game.Instance.DialogController.Dialog)) commands.InterruptAll(raiseEvent);
        }

        private static void TickGroups(bool ignoreTimer)
        {
            if (!WardDialogs.IsCombatStory(Game.Instance.DialogController.Dialog)) UnitCombatLeaveController.TickGroups(ignoreTimer);
        }

        private static IEnumerable<CodeInstruction> GuardSimulation(IEnumerable<CodeInstruction> instructions)
        {
            var tick = AccessTools.Method(typeof(IController), "Tick");
            foreach (var instruction in instructions)
            {
                if (instruction.Calls(tick))
                {
                    instruction.operand = AccessTools.Method(typeof(KurshuNotice), nameof(TickController));
                    instruction.opcode = OpCodes.Call;
                }
                yield return instruction;
            }
        }

        private static void TickController(IController controller)
        {
            if (s_Notice == null) { controller.Tick(); return; }
            var game = Game.Instance;
            if (game.CurrentMode != GameModeType.Dialog)
            {
                controller.Tick();
                return;
            }
            try
            {
                if (controller == game.TimeController)
                {
                    if (!s_Closing) game.TimeController.Suspend();
                    game.TimeController.SetGameDeltaTime(0f);
                }
                else if (controller == game.DialogController || controller == game.Keyboard || controller == game.CameraController)
                    controller.Tick();
            }
            catch (Exception ex)
            {
                Failed(ex);
                throw;
            }
        }
    }
}
