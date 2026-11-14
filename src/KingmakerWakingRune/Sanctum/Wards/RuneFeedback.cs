using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using UnityEngine;

namespace KingmakerWakingRune.Sanctum.Wards
{
    /// <summary>
    /// What the party sees of a rune's state (WR-PLAYTEST-01): a restrained line when a qualifying hold begins or breaks,
    /// one clear notice, a local pulse and a short sound at the moment it is disabled, and afterwards a hover name and an
    /// inspection that open with the current state. Each message belongs to one state transition, so a load never replays
    /// the celebration. The rune is named only once the party has identified it; otherwise it is "the rune plate".
    /// </summary>
    internal static class RuneFeedback
    {
        private const string PlateName = "Thassilonian rune plate";
        private const string DisableSound = "DispelMagic";
        private const float PulseSeconds = 2.5f;

        public static void Begun(RuneFocus focus)
        {
            WardRuntime.Say("The rune's motion falters. Keep it bound.");
        }

        public static void Broken(RuneFocus focus)
        {
            WardRuntime.Say("The binding slips too soon, and the rune's motion returns.");
        }

        public static void Disabled(RuneFocus focus)
        {
            string line = $"{Title(focus)} goes dark. Its link to Krune is broken.";
            WardRuntime.Say(line);
            EventBus.RaiseEvent(delegate(IWarningNotificationUIHandler h) { h.HandleWarning(line); });
            UnitEntityData proxy = WardFixtures.Find(focus.Id);
            if (proxy?.View != null)
            {
                AkSoundEngine.PostEvent(DisableSound, proxy.View.gameObject);
            }
            Vector3 at = SanctumDressing.RuneLights.TryGetValue(focus.Id, out Light light) && light != null ? light.transform.position : proxy?.Position ?? Vector3.zero;
            RunePulse.Spawn(at);
            WardInteract.Refresh(focus.Id);
        }

        /// <summary>The first line of a disabled plate's inspection: its state, which needs no roll to see.</summary>
        public static string DisabledStatus(RuneFocus focus, bool identified)
        {
            return identified
                ? $"This Rune of {focus.Kind} is disabled. Its link to Krune has been broken."
                : "This rune is disabled. Its link to Krune has been broken.";
        }

        /// <summary>The plate's hover name follows its saved state, on every load: "(disabled)" once it is.</summary>
        public static void ApplyHoverName(RuneFocus focus, bool disabled)
        {
            UnitEntityData proxy = WardFixtures.Find(focus.Id);
            if (proxy == null)
            {
                return;
            }
            string wanted = disabled ? (Identified(focus) ? $"Rune of {focus.Kind} (disabled)" : PlateName + " (disabled)") : string.Empty;
            if (proxy.Descriptor.CustomName != wanted)
            {
                // The native rename (ChangeUnitName): the overtip and hover name listen for this event.
                proxy.Descriptor.CustomName = wanted;
                EventBus.RaiseEvent(delegate(IUnitNameHandler h) { h.OnUnitNameChanged(proxy); });
            }
        }

        private static bool Identified(RuneFocus focus)
        {
            return WardRuntime.State != null && WardInteract.RuneIdentified(focus.Id, WardRuntime.State);
        }

        private static string Title(RuneFocus focus)
        {
            return Identified(focus) ? $"The Rune of {focus.Kind}" : "The rune plate";
        }

        /// <summary>A brief cold flare at the plate that fades out and removes itself: the shutdown, seen where it happens.</summary>
        private sealed class RunePulse : MonoBehaviour
        {
            private Light m_Light;
            private float m_Started;

            public static void Spawn(Vector3 at)
            {
                var host = new GameObject("KWR_RunePulse");
                host.transform.position = at + Vector3.up * 0.3f;
                RunePulse pulse = host.AddComponent<RunePulse>();
                pulse.m_Light = host.AddComponent<Light>();
                pulse.m_Light.type = LightType.Point;
                pulse.m_Light.color = new Color(0.55f, 0.85f, 1f);
                pulse.m_Light.range = 6f;
                pulse.m_Light.intensity = 6f;
                pulse.m_Started = Time.unscaledTime;
            }

            private void Update()
            {
                float t = (Time.unscaledTime - m_Started) / PulseSeconds;
                if (t >= 1f)
                {
                    Destroy(gameObject);
                    return;
                }
                m_Light.intensity = 6f * (1f - t) * (1f - t);
            }
        }
    }
}
