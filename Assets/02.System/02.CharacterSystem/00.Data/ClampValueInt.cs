using System;
using UnityEngine;

namespace CharacterSystem
{
    [Serializable]
    public class ClampValueInt
    {
        [SerializeField] private int minValue;
        [SerializeField] private int maxValue;
        [SerializeField] private int currentValue;

        public int MinValue => minValue;
        public int MaxValue => maxValue;
        public int CurrentValue => currentValue;

        public event Action<int, int> OnValueChanged;

        public ClampValueInt(int min, int max, int current)
        {
            minValue = min;
            maxValue = max;
            currentValue = Mathf.Clamp(current, min, max);
        }

        public void SetRange(int min, int max)
        {
            minValue = min;
            maxValue = max;
            SetCurrent(currentValue);
        }

        public void Increase(int amount)
        {
            if (amount <= 0) return;
            SetCurrent(currentValue + amount);
        }

        public void Reduce(int amount)
        {
            if (amount <= 0) return;
            SetCurrent(currentValue - amount);
        }

        public void SetCurrent(int value)
        {
            int clamped = Mathf.Clamp(value, minValue, maxValue);
            if (currentValue != clamped)
            {
                currentValue = clamped;
                OnValueChanged?.Invoke(currentValue, maxValue);
            }
        }
    }
}
