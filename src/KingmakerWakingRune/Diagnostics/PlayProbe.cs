using System.Linq;
using Kingmaker;
using UnityEngine;

namespace KingmakerWakingRune.Diagnostics
{
    /// <summary>
    /// Harness-only timeline (runs with the harness probe, inert in a normal install): long frames, pause and unpause,
    /// party combat entry and exit, and the active turn-based unit, each logged at its transition only. Used to tell a frame
    /// stall from a real pause, a turn change or a combat restart. Reads state only.
    /// </summary>
    internal static class PlayProbe
    {
        private const float StallSeconds = 0.15f;
        private static bool s_Paused;
        private static bool s_Combat;
        private static string s_Turn;

        public static void Tick()
        {
            Game game = Game.Instance;
            if (game?.Player == null || game.CurrentlyLoadedArea == null)
            {
                return;
            }
            float dt = Time.unscaledDeltaTime;
            if (dt >= StallSeconds)
            {
                Log.Info($"PROBE stall {dt * 1000f:0} ms at t={game.TimeController.GameTime.TotalSeconds:0.0} paused={game.IsPaused} combat={game.Player.IsInCombat} turn={s_Turn}");
            }
            if (game.IsPaused != s_Paused)
            {
                s_Paused = game.IsPaused;
                Log.Info($"PROBE {(s_Paused ? "paused" : "unpaused")} at t={game.TimeController.GameTime.TotalSeconds:0.0} combat={game.Player.IsInCombat} mode={game.CurrentMode}");
            }
            if (game.Player.IsInCombat != s_Combat)
            {
                s_Combat = game.Player.IsInCombat;
                string hostiles = string.Join(", ", game.State.Units.Where(u => u.IsInCombat && u.IsPlayersEnemy).Select(u => u.CharacterName).ToArray());
                Log.Info($"PROBE combat {(s_Combat ? "started" : "ended")} at t={game.TimeController.GameTime.TotalSeconds:0.0} hostiles in combat=[{hostiles}]");
            }
            string turn = TurnBased.Controllers.CombatController.IsInTurnBasedCombat() ? TurnBased.Controllers.CombatController.CurrentUnit?.CharacterName ?? "none" : null;
            if (turn != s_Turn)
            {
                s_Turn = turn;
                Log.Info($"PROBE turn {(turn ?? "real time")} at t={game.TimeController.GameTime.TotalSeconds:0.0}");
            }
        }
    }
}
