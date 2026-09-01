using System;
using UnityEngine;

namespace CharacterSystem
{
    [Serializable]
    public class RuntimeStatData : ISaveableData
    {
        private ClampValueInt hp;
        private ClampValueInt mp;
        private float moveSpeed;
        private FactionType currentFaction;

        public ClampValueInt HP => hp;
        public ClampValueInt MP => mp;
        public float MoveSpeed
        {
            get => moveSpeed;
            set
            {
                if (Mathf.Approximately(moveSpeed, value)) return;
                moveSpeed = value;
                OnMoveSpeedChanged?.Invoke(moveSpeed);
            }
        }

        public FactionType CurrentFaction
        {
            get => currentFaction;
            set
            {
                if (currentFaction == value) return;
                currentFaction = value;
                OnFactionChanged?.Invoke(currentFaction);
            }
        }

        public event Action OnDeath;
        public event Action OnInsufficientMana;
        public event Action<float> OnMoveSpeedChanged;
        public event Action<FactionType> OnFactionChanged;

        public RuntimeStatData(PureStatData pureData)
        {
            if (pureData != null)
            {
                hp = new ClampValueInt(0, pureData.MaxHP, pureData.MaxHP);
                mp = new ClampValueInt(0, pureData.MaxMP, pureData.MaxMP);
                moveSpeed = pureData.BaseMoveSpeed;
                currentFaction = pureData.DefaultFaction;
            }
            else
            {
                hp = new ClampValueInt(0, 100, 100);
                mp = new ClampValueInt(0, 100, 100);
                moveSpeed = 5.0f;
                currentFaction = FactionType.Enemy;
            }

            hp.OnValueChanged += HandleHPChanged;
        }

        public bool IsHostile(FactionType other)
        {
            if (currentFaction == FactionType.Player || currentFaction == FactionType.Ally)
            {
                return other == FactionType.Enemy;
            }
            if (currentFaction == FactionType.Enemy)
            {
                return other == FactionType.Player || other == FactionType.Ally;
            }
            return false;
        }

        private void HandleHPChanged(int current, int max)
        {
            if (current <= hp.MinValue)
            {
                OnDeath?.Invoke();
            }
        }

        public bool TryConsumeMP(int amount)
        {
            if (mp.CurrentValue < amount)
            {
                Debug.LogWarning($"[RuntimeStatData] MP Consume Failed! Insufficient MP. (Required: {amount}, Current MP: {mp.CurrentValue} / {mp.MaxValue})");
                OnInsufficientMana?.Invoke();
                return false;
            }
            mp.Reduce(amount);
            Debug.Log($"<color=cyan>[RuntimeStatData] MP Consumed: {amount}. Remaining MP: {mp.CurrentValue} / {mp.MaxValue}</color>");
            return true;
        }

        public string SaveToJson()
        {
            var dto = new SaveDTO
            {
                currentHP = hp.CurrentValue,
                maxHP = hp.MaxValue,
                currentMP = mp.CurrentValue,
                maxMP = mp.MaxValue,
                moveSpeed = moveSpeed,
                faction = (int)currentFaction
            };
            return JsonUtility.ToJson(dto);
        }

        public void LoadFromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return;
            var dto = JsonUtility.FromJson<SaveDTO>(json);
            if (dto == null) return;

            hp.SetRange(0, dto.maxHP);
            hp.SetCurrent(dto.currentHP);
            mp.SetRange(0, dto.maxMP);
            mp.SetCurrent(dto.currentMP);
            MoveSpeed = dto.moveSpeed;
            CurrentFaction = (FactionType)dto.faction;
        }

        [Serializable]
        private class SaveDTO
        {
            public int currentHP;
            public int maxHP;
            public int currentMP;
            public int maxMP;
            public float moveSpeed;
            public int faction;
        }
    }
}
