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

        public PureDataTimeSlow PureData => pureData;
        public ClampValueInt FocusGauge => focusGauge;
        public bool IsSlowActive => isSlowActive;
        public float TimeSinceLastDrain => timeSinceLastDrain;

        public event Action<bool> OnSlowStateChanged;
        public event Action OnFocusDepleted;

        public RuntimeDataTimeSlow(PureDataTimeSlow pureData)
        {
            this.pureData = pureData;
            int max = pureData != null ? pureData.MaxFocus : 100;
            int min = pureData != null ? pureData.MinFocus : 0;
            focusGauge = new ClampValueInt(min, max, max);
        }

        public bool CanActivateSlow()
        {
            int minRequired = pureData != null ? pureData.MinFocusToActivate : 5;
            return focusGauge.CurrentValue >= minRequired;
        }

        public void SetSlowActive(bool active)
        {
            if (isSlowActive == active) return;

            if (active && !CanActivateSlow())
            {
                return;
            }

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
