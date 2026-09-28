using System;
using UnityEngine;

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

        // 09번(시간 정지 마법 시스템 확장) 신규 필드 — 게이지 오버라이드 + 이벤트 전용 감속 램프
        private bool isGaugeOverride;
        private bool isEventTriggered;
        private float eventRampElapsedSeconds;
        private float eventRampDurationSeconds;

        public PureDataTimeSlow PureData => pureData;
        public ClampValueInt FocusGauge => focusGauge;
        public bool IsSlowActive => isSlowActive;
        public float TimeSinceLastDrain => timeSinceLastDrain;
        public bool IsGaugeOverride => isGaugeOverride;
        public bool IsEventTriggered => isEventTriggered;
        public float EventRampElapsedSeconds => eventRampElapsedSeconds;
        public float EventRampDurationSeconds => eventRampDurationSeconds;

        public event Action<bool> OnSlowStateChanged;
        public event Action OnFocusDepleted;
        public event Action<bool> OnGaugeOverrideChanged;

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

        /// <summary>감속 모드(이벤트 전용) 진입 — 램프 경과/총 시간을 초기화합니다.</summary>
        public void BeginEventRamp(float durationSeconds)
        {
            isEventTriggered = true;
            eventRampElapsedSeconds = 0f;
            eventRampDurationSeconds = durationSeconds;
        }

        /// <summary>감속 모드 종료.</summary>
        public void EndEventRamp()
        {
            isEventTriggered = false;
        }

        /// <summary>
        /// 램프 경과 시간을 누적하고 진행률(0~1)을 반환합니다. durationSeconds가 0 이하이면 0으로 나누기를
        /// 피하기 위해 즉시 1을 반환합니다(호출부에서 즉시 DeactivateEventSlow 트리거 가능).
        /// </summary>
        public float TickEventRamp(float deltaTime)
        {
            eventRampElapsedSeconds += deltaTime;
            if (eventRampDurationSeconds <= 0f) return 1f;
            return Mathf.Clamp01(eventRampElapsedSeconds / eventRampDurationSeconds);
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
