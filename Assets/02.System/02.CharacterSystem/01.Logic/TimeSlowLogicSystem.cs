using System;
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

        [Inject]
        public TimeSlowLogicSystem(
            RuntimeDataTimeSlow runtimeData,
            ITimeSlowVisualizer visualizer = null)
        {
            this.runtimeData = runtimeData ?? throw new ArgumentNullException(nameof(runtimeData));
            this.visualizer = visualizer;

            this.runtimeData.OnSlowStateChanged += HandleSlowStateChanged;
            this.runtimeData.OnFocusDepleted += HandleFocusDepleted;
        }

        public void Dispose()
        {
            if (runtimeData != null)
            {
                runtimeData.OnSlowStateChanged -= HandleSlowStateChanged;
                runtimeData.OnFocusDepleted -= HandleFocusDepleted;
                if (runtimeData.IsSlowActive)
                {
                    RestoreTimeScale();
                }
            }
        }

        private void HandleSlowStateChanged(bool isActive)
        {
            float targetScale = isActive ? (runtimeData.PureData != null ? runtimeData.PureData.SlowTimeScale : 0.0f) : 1.0f;
            float defaultFixed = runtimeData.PureData != null ? runtimeData.PureData.DefaultFixedDeltaTime : 0.02f;

            Time.timeScale = targetScale;
            Time.fixedDeltaTime = targetScale > 0f ? (defaultFixed * targetScale) : defaultFixed;

            visualizer?.SetTimeSlowEffect(isActive, targetScale);
        }

        private void HandleFocusDepleted()
        {
            Debug.LogWarning("[TimeSlowLogicSystem] Focus Depleted! Forcing Slow Deactivation.");
            DeactivateSlow();
        }

        public void Tick()
        {
            HandleInput();
            ProcessTimeSlow();
        }

        private void HandleInput()
        {
            if (Keyboard.current == null) return;

            // 키보드 T키를 누를 때마다 시간 정지 On/Off 토글
            if (Keyboard.current.tKey.wasPressedThisFrame)
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
        }

        private void ProcessTimeSlow()
        {
            float unscaledDelta = Time.unscaledDeltaTime;
            var pureData = runtimeData.PureData;

            if (runtimeData.IsSlowActive)
            {
                // 인스펙터에서 실시간으로 조절한 slowTimeScale을 즉각 반영
                float targetScale = pureData != null ? pureData.SlowTimeScale : 0.0f;
                float defaultFixed = pureData != null ? pureData.DefaultFixedDeltaTime : 0.02f;
                Time.timeScale = targetScale;
                Time.fixedDeltaTime = targetScale > 0f ? (defaultFixed * targetScale) : defaultFixed;

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

        private void RestoreTimeScale()
        {
            Time.timeScale = 1.0f;
            float defaultFixed = runtimeData.PureData != null ? runtimeData.PureData.DefaultFixedDeltaTime : 0.02f;
            Time.fixedDeltaTime = defaultFixed;
        }
    }
}
