using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.GameModes;
using Kingmaker.Visual.Animation.Kingmaker;
using Kingmaker.Visual.Animation.Kingmaker.Actions;
using KingmakerWakingRune.Blueprints;
using UnityEngine;

namespace KingmakerWakingRune.Lodge
{
    /// <summary>
    /// Per-frame work while the lodge is loaded: opening the briefing when the UI is free, keeping the warding
    /// agents in their looped casting pose, and an occasional quiet chant from one of them. Destroyed with the area.
    /// </summary>
    internal sealed class LodgeDriver : MonoBehaviour
    {
        private const float AgentRefreshSeconds = 2f;
        private const float MinChantDelaySeconds = 7f;
        private const float MaxChantDelaySeconds = 14f;
        private const float WardingCastSeconds = 240f;

        private static LodgeDriver s_Instance;

        private readonly Dictionary<string, UnitAnimationActionHandle> m_Handles = new Dictionary<string, UnitAnimationActionHandle>();
        private float m_NextRefresh;
        private float m_NextChant;
        private bool m_ChantReported;

        public static void Attach()
        {
            if (s_Instance == null)
            {
                s_Instance = new GameObject("KWR_LodgeDriver").AddComponent<LodgeDriver>();
                DontDestroyOnLoad(s_Instance.gameObject);
            }
        }

        public static void Detach()
        {
            if (s_Instance != null)
            {
                Destroy(s_Instance.gameObject);
                s_Instance = null;
            }
        }

        private void Update()
        {
            if (Game.Instance.CurrentlyLoadedArea != ScenarioBlueprints.Area)
            {
                return;
            }
            BriefingService.TryOpen();
            if (Time.unscaledTime >= m_NextRefresh)
            {
                m_NextRefresh = Time.unscaledTime + AgentRefreshSeconds;
                RefreshAgents();
            }
            if (Time.unscaledTime >= m_NextChant)
            {
                m_NextChant = Time.unscaledTime + Random.Range(MinChantDelaySeconds, MaxChantDelaySeconds);
                ChantOnce();
            }
        }

        private void RefreshAgents()
        {
            foreach (UnitEntityData agent in LodgeSession.CurrentAgents())
            {
                UnitAnimationManager animator = agent.View?.AnimationManager;
                if (animator == null)
                {
                    continue;
                }
                if (m_Handles.TryGetValue(agent.UniqueId, out UnitAnimationActionHandle handle) && !handle.IsReleased)
                {
                    continue;
                }
                UnitAnimationActionHandle cast = animator.CreateHandle(UnitAnimationType.CastSpell, errorOnEmpty: false);
                if (cast == null)
                {
                    continue;
                }
                // A cast with no release: the casting pose holds and no spell, event or combat log entry happens.
                cast.CastingTime = WardingCastSeconds;
                cast.CastStyle = UnitAnimationActionCastSpell.CastAnimationStyle.Omni;
                cast.SpecialCastBehaviour = UnitAnimationActionCastSpell.SpecialBehaviourType.NoCast;
                animator.Execute(cast);
                m_Handles[agent.UniqueId] = cast;
            }
        }

        private void ChantOnce()
        {
            if (Game.Instance.CurrentMode != GameModeType.Default)
            {
                return;
            }
            List<UnitEntityData> agents = LodgeSession.CurrentAgents().Where(a => a.View != null).ToList();
            if (agents.Count == 0)
            {
                return;
            }
            UnitEntityData agent = agents[Random.Range(0, agents.Count)];
            string chant = agent.Descriptor.Gender == Kingmaker.Blueprints.Gender.Female ? "Arcane_Enchantment_Low_Female" : "Arcane_Enchantment_Low_Male";
            uint playing = AkSoundEngine.PostEvent(chant, agent.View.gameObject);
            if (!m_ChantReported)
            {
                m_ChantReported = true;
                Log.Info($"Ambient chant posted: {chant} playingId={playing} (0 means the event did not start).");
            }
        }
    }
}
