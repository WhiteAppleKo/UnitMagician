using System;
using GameEvent;
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
        private readonly RuntimeDataTimeScale timeScaleData;
        private readonly ICharacterStatService characterStatService;
        private readonly ITimeSlowVisualizer visualizer;
        private readonly InputReader inputReader;

        // 이벤트 시스템(GameEvent)은 CharacterLifetimeScope와 별개의 VContainer 스코프 트리에 있어 생성자 주입이
        // 불가능하다(실측: 단일 타입 생성자 주입 시 VContainerException으로 전체 스코프 Build()가 깨짐 — 09번 작업
        // 1차 보고 사항). 그래서 CharacterLifetimeScope(MonoBehaviour)가 두 스코프의 Container가 모두 준비된 뒤(Start())
        // BindEventFlight()로 비행 로직(IEventFlightLogic)을 사후 배선하고, 이벤트 시작/해제 동작은 TimeSlowOverrideActionHandler가 BeginEventSlow/EndEventSlow로 지시한다.
        private IEventFlightLogic eventFlightLogic;

        // 이벤트 전용 감속 수치(최저 timeScale, 접근/이탈 곡선)의 원본은 이벤트 SO(TimeSlowOverrideAction)다(Goal 51). 감속 시작 동작이 BeginEventSlow로 넘겨주며,
        // 구체 SO가 아니라 IEventTimeSlowSettings 인터페이스로만 읽는다.
        private IEventTimeSlowSettings eventSlowSettings;

        [Inject]
        public TimeSlowLogicSystem(
            RuntimeDataTimeSlow runtimeData,
            RuntimeDataTimeScale timeScaleData,
            ICharacterStatService characterStatService,
            ITimeSlowVisualizer visualizer = null,
            InputReader inputReader = null)
        {
            this.runtimeData = runtimeData ?? throw new ArgumentNullException(nameof(runtimeData));
            this.timeScaleData = timeScaleData ?? throw new ArgumentNullException(nameof(timeScaleData));
            this.characterStatService = characterStatService ?? throw new ArgumentNullException(nameof(characterStatService));
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
        /// 이벤트 투사체 비행 로직(IEventFlightLogic)의 구간/진행도 신호를 구독하는 사후 배선. CharacterLifetimeScope가 자신과 EventSystemLifetimeScope의
        /// Container가 모두 빌드된 뒤(Start()) 한 번 호출한다. 재호출 시 기존 구독을 먼저 해제해 중복 구독을 막는다.
        /// </summary>
        public void BindEventFlight(IEventFlightLogic flightLogic)
        {
            UnbindEventFlight();

            eventFlightLogic = flightLogic;
            if (eventFlightLogic != null)
            {
                eventFlightLogic.OnFlightChanged += HandleEventFlightChanged;
            }
        }

        private void UnbindEventFlight()
        {
            if (eventFlightLogic == null) return;

            eventFlightLogic.OnFlightChanged -= HandleEventFlightChanged;
            eventFlightLogic = null;
        }

        /// <summary>이벤트 비행 로직이 방송하는 투사체 비행 구간/진행도를 RuntimeData에 반영합니다(이벤트 감속 중일 때만 유효).</summary>
        private void HandleEventFlightChanged(EventFlightPhase phase, float progress)
        {
            runtimeData.UpdateEventFlight(phase, progress);
        }

        public void Dispose()
        {
            if (inputReader != null)
            {
                inputReader.onTimeSlowToggled -= ToggleSlow;
            }

            UnbindEventFlight();

            if (runtimeData != null)
            {
                runtimeData.OnSlowStateChanged -= HandleSlowStateChanged;
                runtimeData.OnFocusDepleted -= HandleFocusDepleted;
                // 이벤트 감속 중 파괴되면 SetSlowActive(false) 가드가 해제를 거부해 timeScale이 남을 수 있으므로,
                // 이벤트 상태(오버라이드/비행/마나 무제한)를 먼저 정리한 뒤 해제한다.
                if (runtimeData.IsEventTriggered)
                {
                    runtimeData.SetGaugeOverride(false);
                    runtimeData.EndEventFlight();
                    characterStatService.SetManaUnlimited(false);
                }

                if (runtimeData.IsSlowActive)
                {
                    DeactivateSlow();
                }
            }
        }

        private void HandleSlowStateChanged(bool isActive)
        {
            float targetScale = isActive ? (runtimeData.PureData != null ? runtimeData.PureData.SlowTimeScale : 0.0f) : 1.0f;
            ApplyTimeSlowRequest(isActive, targetScale);
        }

        /// <summary>
        /// 시간정지/이벤트 감속의 부수 효과(오디오/VFX/OnSlowStateChanged)는 비주얼에 지시하고, 요청 배율은
        /// RuntimeDataTimeScale에 설정한다(17번 기획서). 실제 Time.timeScale 쓰기는 유효 배율 변경 시
        /// TimeScaleLogicSystem → TimeSlowVisualizer.ApplyTimeScale에서만 일어난다.
        /// </summary>
        private void ApplyTimeSlowRequest(bool isActive, float requestedScale)
        {
            visualizer?.SetTimeSlowEffect(isActive, requestedScale);
            timeScaleData.SetRequestedScale(requestedScale);
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
                // 13번 기획서: timeScale은 시간이 아니라 투사체의 구간/진행도로 구동된다. 이벤트 종료는 이벤트 SO의 감속 해제 동작
                // (TimeSlowOverrideAction release -> EndEventSlow)만 수행하며, 여기서 스스로 종료하지 않는다.
                ApplyTimeSlowRequest(true, EvaluateEventTimeScale());
            }

            // 17번 기획서: UI 일시정지 중에는 unscaled 시간 기반 포커스 게이지 소모/회복 틱을 건너뛴다.
            if (timeScaleData.IsPaused) return;

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

        /// <summary>
        /// 접근 구간 진행도/이탈 구간 진행도로 이벤트 감속의 현재 timeScale을 계산합니다(13번 기획서 §3).
        /// 접근: lerp(1.0, EventSlowMinTimeScale, curve(t)) / 이탈: lerp(EventSlowMinTimeScale, 1.0, curve(u)).
        /// 투사체가 아직 발사되지 않았거나 구간이 없으면 정상 속도(1.0)를 유지합니다.
        /// </summary>
        private float EvaluateEventTimeScale()
        {
            // 이벤트 감속 수치가 아직 넘어오지 않았으면(감속 시작 동작 전) 이벤트 감속은 정상 속도를 유지한다.
            if (eventSlowSettings == null) return 1.0f;

            float minScale = eventSlowSettings.EventSlowMinTimeScale;
            float progress = runtimeData.EventFlightProgress;

            switch (runtimeData.EventFlightPhase)
            {
                case EventFlightPhase.Approach:
                    return Mathf.Lerp(1.0f, minScale, EvaluateCurve(eventSlowSettings.EventApproachCurve, progress));
                case EventFlightPhase.Exit:
                    return Mathf.Lerp(minScale, 1.0f, EvaluateCurve(eventSlowSettings.EventExitCurve, progress));
                default:
                    return 1.0f;
            }
        }

        private static float EvaluateCurve(AnimationCurve curve, float progress)
        {
            if (curve == null || curve.length == 0) return Mathf.Clamp01(progress);
            return Mathf.Clamp01(curve.Evaluate(progress));
        }

        /// <summary>
        /// 이벤트 감속 시작 동작(TimeSlowOverrideAction) 수신 — 이벤트 SO의 감속 수치(최저 timeScale, 접근/이탈 곡선)를 받아 접근 구간 시작(감속 개시)과 마나 무제한 On을 한다.
        /// </summary>
        public void BeginEventSlow(IEventTimeSlowSettings settings)
        {
            eventSlowSettings = settings;
            ActivateEventSlow();
            characterStatService.SetManaUnlimited(true);
        }

        /// <summary>이벤트 감속 해제 동작(TimeSlowOverrideAction release) 수신 — 이벤트 감속과 마나 무제한을 끝낸다.</summary>
        public void EndEventSlow()
        {
            DeactivateEventSlow();
        }

        /// <summary>
        /// 이벤트 전용 감속 모드 발동 경로(접근 구간 시작)입니다. 기존 <see cref="ActivateSlow"/>의 게이지
        /// 게이트와 무관하게 항상 성공합니다 — 게이지 오버라이드를 함께 켜고, 접근 구간 진행도 0(정상 속도)에서 시작해
        /// 이후 구간/진행도 신호에 따라 timeScale이 매 프레임 구동됩니다(09번 기획서 9.8/9.11절).
        /// </summary>
        public void ActivateEventSlow()
        {
            runtimeData.SetGaugeOverride(true);
            runtimeData.BeginEventFlight();
            runtimeData.ForceSlowActive(true);

            // ForceSlowActive가 발행하는 OnSlowStateChanged는 완전 정지용 SlowTimeScale로 시각 효과를 설정하므로,
            // 접근 구간 시작 스케일(정상 속도)로 즉시 덮어씁니다.
            ApplyTimeSlowRequest(true, EvaluateEventTimeScale());
        }

        /// <summary>감속 모드 종료. 이벤트 SO의 감속 해제 동작(EndEventSlow) 수신 시에만 호출됩니다.</summary>
        public void DeactivateEventSlow()
        {
            runtimeData.SetGaugeOverride(false);
            runtimeData.EndEventFlight();
            DeactivateSlow();
            characterStatService.SetManaUnlimited(false);
        }

        public void ToggleSlow()
        {
            // 13번 기획서 §3 "이벤트 중 차단": 이벤트 감속 중에는 T 키 입력 자체를 무시한다(입력 단계 가드).
            // RuntimeDataTimeSlow.SetSlowActive(false)의 거부와 이중으로 막는다.
            if (runtimeData.IsEventTriggered) return;

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
