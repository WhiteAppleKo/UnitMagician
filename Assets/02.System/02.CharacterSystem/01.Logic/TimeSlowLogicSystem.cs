using System;
using EventSequencerSystem;
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

        // 06(EventSequencerSystem)은 CharacterLifetimeScope와 별개의 VContainer 스코프 트리에 있어 생성자 주입이
        // 불가능하다(실측: 단일 타입 생성자 주입 시 VContainerException으로 전체 스코프 Build()가 깨짐 — 09번 작업
        // 1차 보고 사항). 조정자 지시에 따라 06의 bossStatComponent 패턴과 동일하게, CharacterLifetimeScope(MonoBehaviour)가
        // 두 스코프의 Container가 모두 준비된 뒤(Start()) BindEventSequencer()로 사후 배선한다.
        private PureDataTutorialSequence pureDataTutorialSequence;
        private IEventSequencer eventSequencer;

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

        /// <summary>
        /// 06(이벤트 시퀀서) 연동 사후 배선. CharacterLifetimeScope가 자신과 EventSequencerSystemLifetimeScope의
        /// Container가 모두 빌드된 뒤(Start()) 한 번 호출한다. 재호출 시 기존 구독을 먼저 해제해 중복 구독을 막는다.
        /// </summary>
        public void BindEventSequencer(IEventSequencer sequencer, PureDataTutorialSequence pureDataTutorialSequence)
        {
            UnbindEventSequencer();

            this.eventSequencer = sequencer;
            this.pureDataTutorialSequence = pureDataTutorialSequence;

            if (this.eventSequencer != null)
            {
                this.eventSequencer.OnBoss50Percent += HandleBoss50Percent;
                this.eventSequencer.OnCoreHitSuccess += DeactivateEventSlow;
                this.eventSequencer.OnCoreHitFail += DeactivateEventSlow;
            }
        }

        private void UnbindEventSequencer()
        {
            if (eventSequencer != null)
            {
                eventSequencer.OnBoss50Percent -= HandleBoss50Percent;
                eventSequencer.OnCoreHitSuccess -= DeactivateEventSlow;
                eventSequencer.OnCoreHitFail -= DeactivateEventSlow;
            }
        }

        public void Dispose()
        {
            if (inputReader != null)
            {
                inputReader.onTimeSlowToggled -= ToggleSlow;
            }

            UnbindEventSequencer();

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

            if (runtimeData.IsEventTriggered)
            {
                float t = runtimeData.TickEventRamp(unscaledDelta);
                float startScale = pureData != null ? pureData.EventSlowTimeScale : 0f;
                float currentTimeScale = Mathf.Lerp(startScale, 1.0f, t);
                visualizer?.SetTimeSlowEffect(true, currentTimeScale);

                if (t >= 1f)
                {
                    // 램프 완료 = 06의 타임아웃 실패 판정과 동일 시점 (09번 기획서 9.8절)
                    DeactivateEventSlow();
                    return;
                }
            }

            if (runtimeData.IsSlowActive)
            {
                // 게이지 오버라이드 중에는 초당 게이지 자연 감소를 완전히 스킵 (09번 기획서 9.7절)
                if (!runtimeData.IsGaugeOverride)
                {
                    float drainRate = pureData != null ? pureData.FocusDrainPerSecond : 0f;
                    if (drainRate > 0f)
                    {
                        runtimeData.DrainFocus(drainRate * unscaledDelta);

                        if (runtimeData.FocusGauge.CurrentValue <= runtimeData.FocusGauge.MinValue)
                        {
                            DeactivateSlow();
                        }
                    }
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

        /// <summary>06의 OnBoss50Percent 수신 — 06의 CoreHitTimeoutSeconds와 정확히 같은 램프 지속시간으로 감속을 시작합니다.</summary>
        private void HandleBoss50Percent()
        {
            float rampDuration = pureDataTutorialSequence != null ? pureDataTutorialSequence.CoreHitTimeoutSeconds : 0f;
            ActivateEventSlow(rampDuration);
        }

        /// <summary>
        /// 06(이벤트 시퀀서) 전용 감속 모드 발동 경로입니다. 기존 <see cref="ActivateSlow"/>의 게이지 게이트와
        /// 무관하게 항상 성공합니다 — 게이지 오버라이드를 함께 켜고, rampDurationSeconds에 걸쳐 자동으로
        /// 1.0배까지 복귀하는 램프를 시작합니다(09번 기획서 9.8절).
        /// </summary>
        public void ActivateEventSlow(float rampDurationSeconds)
        {
            runtimeData.SetGaugeOverride(true);
            runtimeData.BeginEventRamp(rampDurationSeconds);
            runtimeData.ForceSlowActive(true);

            // ForceSlowActive가 발행하는 OnSlowStateChanged는 완전 정지용 SlowTimeScale로 시각 효과를 설정하므로,
            // 이벤트 감속의 초기 스케일(EventSlowTimeScale)로 즉시 덮어씁니다.
            float initialScale = runtimeData.PureData != null ? runtimeData.PureData.EventSlowTimeScale : 0f;
            visualizer?.SetTimeSlowEffect(true, initialScale);
        }

        /// <summary>감속 모드 조기 종료(06의 성공/실패 판정 수신) 또는 램프 완료 시 호출됩니다.</summary>
        public void DeactivateEventSlow()
        {
            runtimeData.SetGaugeOverride(false);
            runtimeData.EndEventRamp();
            DeactivateSlow();
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
