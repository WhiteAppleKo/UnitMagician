using System;
using Synty.AnimationBaseLocomotion.Samples.InputSystem;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

namespace CharacterSystem
{
    public class TimeSlowLogicSystem : ITickable, IDisposable
    {
        private readonly RuntimeDataTimeSlow runtimeData;
        private readonly ITimeSlowVisualizer visualizer;
        private readonly InputReader inputReader;

        [Inject]
        public TimeSlowLogicSystem(
            RuntimeDataTimeSlow runtimeData,
            ITimeSlowVisualizer visualizer = null,
            InputReader inputReader = null)
        {
            this.runtimeData = runtimeData ?? throw new ArgumentNullException(nameof(runtimeData));
            this.visualizer = visualizer;
            this.inputReader = inputReader;

            this.runtimeData.OnSlowStateChanged += HandleSlowStateChanged;
            this.runtimeData.OnFocusDepleted += HandleFocusDepleted;

            if (this.inputReader != null)
            {
                this.inputReader.onTimeSlowToggled += ToggleSlow;
            }
        }

        public void Dispose()
        {
            if (inputReader != null)
            {
                inputReader.onTimeSlowToggled -= ToggleSlow;
            }

            if (runtimeData != null)
            {
                runtimeData.OnSlowStateChanged -= HandleSlowStateChanged;
                runtimeData.OnFocusDepleted -= HandleFocusDepleted;
                if (runtimeData.IsSlowActive)
                {
                    DeactivateSlow();
                }
            }
        }

        private void HandleSlowStateChanged(bool isActive)
        {
            float targetScale = isActive ? (runtimeData.PureData != null ? runtimeData.PureData.SlowTimeScale : 0.0f) : 1.0f;
            visualizer?.SetTimeSlowEffect(isActive, targetScale);
        }

        private void HandleFocusDepleted()
        {
            Debug.LogWarning("[TimeSlowLogicSystem] Focus Depleted! Forcing Slow Deactivation.");
            DeactivateSlow();
        }

        public void Tick()
        {
            if (inputReader == null)
            {
                HandleInputFallback();
            }
            ProcessTimeSlow();
        }

        private void HandleInputFallback()
        {
            if (Keyboard.current == null) return;

            if (Keyboard.current.tKey.wasPressedThisFrame)
            {
                ToggleSlow();
            }
        }

        private void ProcessTimeSlow()
        {
            float unscaledDelta = Time.unscaledDeltaTime;
            var pureData = runtimeData.PureData;

            if (runtimeData.IsSlowActive)
            {
                float drainRate = pureData != null ? pureData.FocusDrainPerSecond : 20f;
                runtimeData.DrainFocus(drainRate * unscaledDelta);

                if (runtimeData.FocusGauge.CurrentValue <= runtimeData.FocusGauge.MinValue)
                {
                    DeactivateSlow();
                }
            }
            else
            {
                runtimeData.TickRecoveryTimer(unscaledDelta);
                float delay = pureData != null ? pureData.RecoverDelaySeconds : 0.5f;

                if (runtimeData.TimeSinceLastDrain >= delay)
                {
                    float recoverRate = pureData != null ? pureData.FocusRecoverPerSecond : 15f;
                    runtimeData.RecoverFocus(recoverRate * unscaledDelta);
                }
            }
        }

        public void ToggleSlow()
        {
            if (runtimeData.IsSlowActive)
            {
                DeactivateSlow();
            }
            else if (runtimeData.CanActivateSlow())
            {
                ActivateSlow();
            }
        }

        public void ActivateSlow()
        {
            if (runtimeData.IsSlowActive) return;
            runtimeData.SetSlowActive(true);
        }

        public void DeactivateSlow()
        {
            if (!runtimeData.IsSlowActive) return;
            runtimeData.SetSlowActive(false);
        }
    }
}
