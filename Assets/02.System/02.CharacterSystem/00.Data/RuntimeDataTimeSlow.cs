using System;
using UnityEngine;
using EventSequencerSystem;

namespace CharacterSystem
{
    [Serializable]
    public class RuntimeDataTimeSlow : ISaveableData
    {
        private readonly PureDataTimeSlow pureData;
        private readonly ClampValueInt focusGauge;
        private bool isSlowActive;

        private float drainBuffer;
        private float recoverBuffer;
        private float timeSinceLastDrain;

        // 09번(시간 정지 마법 시스템 확장) 신규 필드 — 게이지 오버라이드 + 이벤트 전용 감속
        // 13번 기획서: 시간 기반 램프 대신 아군 투사체의 구간(접근/이탈) + 구간 내 진행도(0~1) 모델로 감속을 구동한다.
        private bool isGaugeOverride;
        private bool isEventTriggered;
        private EventFlightPhase eventFlightPhase;
        private float eventFlightProgress;

        public PureDataTimeSlow PureData => pureData;
        public ClampValueInt FocusGauge => focusGauge;
        public bool IsSlowActive => isSlowActive;
        public float TimeSinceLastDrain => timeSinceLastDrain;
        public bool IsGaugeOverride => isGaugeOverride;
        /// <summary>이벤트 감속 진행 중(접근 시작부터 이벤트 종료까지 true).</summary>
        public bool IsEventTriggered => isEventTriggered;
        public EventFlightPhase EventFlightPhase => eventFlightPhase;
        /// <summary>현재 구간(접근/이탈)의 진행도 0~1.</summary>
        public float EventFlightProgress => eventFlightProgress;

        public event Action<bool> OnSlowStateChanged;
        public event Action OnFocusDepleted;
        public event Action<bool> OnGaugeOverrideChanged;
        /// <summary>이벤트 비행 구간 또는 진행도가 바뀔 때 발행됩니다(시작/종료 포함).</summary>
        public event Action<EventFlightPhase, float> OnEventFlightChanged;

        public RuntimeDataTimeSlow(PureDataTimeSlow pureData)
        {
            this.pureData = pureData;
            int max = pureData != null ? pureData.MaxFocus : 100;
            int min = pureData != null ? pureData.MinFocus : 0;
            focusGauge = new ClampValueInt(min, max, max);
        }

        public bool CanActivateSlow()
        {
            int minRequired = pureData != null ? pureData.MinFocusToActivate : 0;
            return focusGauge.CurrentValue >= minRequired;
        }

        public void SetSlowActive(bool active)
        {
            if (active && !CanActivateSlow())
            {
                return;
            }

            // 13번 기획서 §3 "이벤트 중 차단": 이벤트 감속(IsEventTriggered) 진행 중에는 수동 해제를 단일 관문에서 거부한다
            // (T 키 등 모든 SetSlowActive(false) 경로). 이벤트 종료는 TimeSlowLogicSystem.DeactivateEventSlow()만 수행하며,
            // 그 메서드는 EndEventFlight()로 IsEventTriggered를 먼저 끈 뒤 해제하므로 이 가드에 걸리지 않는다.
            // 활성화 경로(SetSlowActive(true)/ForceSlowActive)는 영향받지 않는다.
            if (!active && isEventTriggered)
            {
                return;
            }

            ApplySlowActive(active);
        }

        /// <summary>
        /// 이벤트 시퀀서(06번) 전용 강제 발동 경로입니다. <see cref="CanActivateSlow"/> 게이트를 우회합니다 —
        /// 이벤트 발동은 게이지 상태와 무관하게 항상 성공해야 합니다(09번 기획서 9.8절, Goal 프롬프트 완료 기준).
        /// 일반 유저 수동 발동(<see cref="SetSlowActive"/>)에는 영향을 주지 않습니다.
        /// </summary>
        public void ForceSlowActive(bool active)
        {
            ApplySlowActive(active);
        }

        private void ApplySlowActive(bool active)
        {
            if (isSlowActive == active) return;

            isSlowActive = active;
            if (isSlowActive)
            {
                drainBuffer = 0f;
                timeSinceLastDrain = 0f;
            }
            else
            {
                recoverBuffer = 0f;
            }

            OnSlowStateChanged?.Invoke(isSlowActive);
        }

        /// <summary>
        /// 게이지 오버라이드 On/Off. true면 <see cref="DrainFocus"/> 억제(호출부 책임, 09번 기획서 9.8절).
        /// 값이 바뀔 때만 <see cref="OnGaugeOverrideChanged"/>를 발행합니다(기존 SetSlowActive와 동일한 변경 감지 패턴).
        /// </summary>
        public void SetGaugeOverride(bool value)
        {
            if (isGaugeOverride == value) return;
            isGaugeOverride = value;
            OnGaugeOverrideChanged?.Invoke(isGaugeOverride);
        }

        /// <summary>감속 모드(이벤트 전용) 진입 — 접근 구간 시작(진행도 0)으로 초기화합니다.</summary>
        public void BeginEventFlight()
        {
            isEventTriggered = true;
            ApplyEventFlight(EventFlightPhase.Approach, 0f);
        }

        /// <summary>
        /// 비행 구간/진행도를 갱신합니다(진행도는 0~1로 클램프). 이벤트 감속 중이 아니면 무시하고,
        /// 값이 실제로 바뀔 때만 <see cref="OnEventFlightChanged"/>를 발행합니다.
        /// </summary>
        public void UpdateEventFlight(EventFlightPhase phase, float progress)
        {
            if (!isEventTriggered) return;
            ApplyEventFlight(phase, Mathf.Clamp01(progress));
        }

        /// <summary>감속 모드 종료 — 구간/진행도를 초기화하고 IsEventTriggered를 끕니다.</summary>
        public void EndEventFlight()
        {
            isEventTriggered = false;
            ApplyEventFlight(EventFlightPhase.None, 0f);
        }

        private void ApplyEventFlight(EventFlightPhase phase, float progress)
        {
            if (eventFlightPhase == phase && Mathf.Approximately(eventFlightProgress, progress)) return;

            eventFlightPhase = phase;
            eventFlightProgress = progress;
            OnEventFlightChanged?.Invoke(eventFlightPhase, eventFlightProgress);
        }

        public void DrainFocus(float amount)
        {
            if (amount <= 0f) return;

            timeSinceLastDrain = 0f;
            drainBuffer += amount;
            int intToReduce = Mathf.FloorToInt(drainBuffer);
            if (intToReduce > 0)
            {
                drainBuffer -= intToReduce;
                focusGauge.Reduce(intToReduce);

                if (focusGauge.CurrentValue <= focusGauge.MinValue)
                {
                    OnFocusDepleted?.Invoke();
                }
            }
        }

        public void RecoverFocus(float amount)
        {
            if (amount <= 0f) return;

            recoverBuffer += amount;
            int intToIncrease = Mathf.FloorToInt(recoverBuffer);
            if (intToIncrease > 0)
            {
                recoverBuffer -= intToIncrease;
                focusGauge.Increase(intToIncrease);
            }
        }

        public void TickRecoveryTimer(float deltaTime)
        {
            timeSinceLastDrain += deltaTime;
        }

        public string SaveToJson()
        {
            var dto = new SaveDTO
            {
                currentFocus = focusGauge.CurrentValue,
                maxFocus = focusGauge.MaxValue,
                minFocus = focusGauge.MinValue,
                isSlowActive = isSlowActive
            };
            return JsonUtility.ToJson(dto);
        }

        public void LoadFromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return;
            var dto = JsonUtility.FromJson<SaveDTO>(json);
            if (dto == null) return;

            focusGauge.SetRange(dto.minFocus, dto.maxFocus);
            focusGauge.SetCurrent(dto.currentFocus);
            isSlowActive = dto.isSlowActive;
        }

        [Serializable]
        private class SaveDTO
        {
            public int currentFocus;
            public int maxFocus;
            public int minFocus;
            public bool isSlowActive;
        }
    }
}
