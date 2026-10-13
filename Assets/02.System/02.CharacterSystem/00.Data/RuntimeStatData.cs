using System;
using UnityEngine;
using PipeLine.Combat;

namespace CharacterSystem
{
    [Serializable]
    public class RuntimeStatData : ISaveableData
    {
        private ClampValueInt hp;
        private ClampValueInt mp;
        private float moveSpeed;
        private FactionType currentFaction;
        private bool isInvincible;
        private bool isManaUnlimited;
        private int previewManaCost;
        private readonly CombatFlagState combatFlags;

        // 스태미나(19-A): 달리기 같은 연속 소모는 프레임당 소수 단위라 정수 클램프 값(ClampValueInt) 대신 float로 보관한다.
        private float stamina;
        private float maxStamina;
        private readonly float staminaRegenPerSecond;
        private readonly float staminaRegenDelaySeconds;
        private float timeSinceLastStaminaUse;

        /// <summary>
        /// 이 캐릭터의 공격/피격 파이프라인 플래그 상태입니다(PureStatData의 기본값으로 초기화). 버프/페이즈 로직이
        /// AddHit/SuppressHit 등 CombatFlagState의 참조 카운트 API로 비트를 켜고 끄며, 직접 값을 대입하는 경로는 없습니다.
        /// </summary>
        public CombatFlagState CombatFlags => combatFlags;

        public IReadOnlyClampValueInt HP => hp;
        public IReadOnlyClampValueInt MP => mp;
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

        public bool IsInvincible => isInvincible;
        public bool IsManaUnlimited => isManaUnlimited;

        public float Stamina => stamina;
        public float MaxStamina => maxStamina;
        public float StaminaRegenPerSecond => staminaRegenPerSecond;
        public float StaminaRegenDelaySeconds => staminaRegenDelaySeconds;

        /// <summary>마지막 스태미나 소모 뒤 흐른 게임 시간(초). 소모가 일어나면 0으로 되돌아간다.</summary>
        public float TimeSinceLastStaminaUse => timeSinceLastStaminaUse;

        /// <summary>
        /// 이번 발동에 쓸 예상 마나 총비용(13-C 적용 전 미리보기). 실제 MP는 줄지 않으며 마나 UI 표시 전용 값입니다.
        /// 0이면 표시할 예상 비용이 없다는 뜻입니다.
        /// </summary>
        public int PreviewManaCost => previewManaCost;

        public event Action OnDeath;
        public event Action OnInsufficientMana;
        public event Action<float> OnMoveSpeedChanged;
        public event Action<FactionType> OnFactionChanged;
        public event Action<bool> OnInvincibleChanged;
        public event Action<bool> OnManaUnlimitedChanged;
        public event Action<int> OnPreviewManaCostChanged;

        /// <summary>스태미나 값이 실제로 바뀔 때만 (현재값, 최대값)으로 발행된다.</summary>
        public event Action<float, float> OnStaminaChanged;

        public RuntimeStatData(PureStatData pureData)
        {
            if (pureData != null)
            {
                hp = new ClampValueInt(0, pureData.MaxHP, pureData.MaxHP);
                mp = new ClampValueInt(0, pureData.MaxMP, pureData.MaxMP);
                moveSpeed = pureData.BaseMoveSpeed;
                maxStamina = pureData.MaxStamina;
                stamina = maxStamina;
                staminaRegenPerSecond = pureData.StaminaRegenPerSecond;
                staminaRegenDelaySeconds = pureData.StaminaRegenDelaySeconds;
                currentFaction = pureData.DefaultFaction;
                combatFlags = new CombatFlagState(pureData.DefaultAttackFlags, pureData.DefaultHitFlags);
            }
            else
            {
                hp = new ClampValueInt(0, 100, 100);
                mp = new ClampValueInt(0, 100, 100);
                moveSpeed = 5.0f;
                maxStamina = 100f;
                stamina = maxStamina;
                staminaRegenPerSecond = 30f;
                staminaRegenDelaySeconds = 1f;
                currentFaction = FactionType.Enemy;
                combatFlags = new CombatFlagState(AttackStepFlags.None, HitStepFlags.Evasion | HitStepFlags.Defense);
            }

            hp.OnValueChanged += HandleHPChanged;
        }

        public bool IsHostile(FactionType other)
        {
            // 적대 판정 규칙은 FactionRelation 한 곳에만 있다(여기서는 위임만 한다).
            return FactionRelation.IsHostile(currentFaction, other);
        }

        private void HandleHPChanged(int current, int max)
        {
            if (current <= hp.MinValue)
            {
                OnDeath?.Invoke();
            }
        }

        /// <summary>
        /// HP/MP 상태 변경 전용 메서드입니다. 외부에서는 ICharacterStatService(Heal/TakeDamage/RecoverMP)를 통해서만 호출해야 합니다.
        /// </summary>
        public void IncreaseHP(int amount) => hp.Increase(amount);
        public void ReduceHP(int amount) => hp.Reduce(amount);
        public void IncreaseMP(int amount) => mp.Increase(amount);

        public void SetInvincible(bool value)
        {
            if (isInvincible == value) return;
            isInvincible = value;
            OnInvincibleChanged?.Invoke(isInvincible);
        }

        public void SetManaUnlimited(bool value)
        {
            if (isManaUnlimited == value) return;
            isManaUnlimited = value;
            OnManaUnlimitedChanged?.Invoke(isManaUnlimited);
        }

        /// <summary>예상 마나 총비용(표시 전용)을 설정한다. 음수는 0으로 보정하며, 값이 바뀔 때만 이벤트를 발행한다.</summary>
        public void SetPreviewManaCost(int cost)
        {
            if (cost < 0) cost = 0;
            if (previewManaCost == cost) return;

            previewManaCost = cost;
            OnPreviewManaCostChanged?.Invoke(previewManaCost);
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

        /// <summary>
        /// 스태미나 소모 시도. 현재 스태미나가 amount 이상이면 정확히 amount만큼 차감하고 true,
        /// 부족하면 아무것도 바꾸지 않고 false를 돌려준다. amount가 0 이하면 소모 없이 true.
        /// 외부에서는 ICharacterStatService.TryUseStamina를 통해서만 호출해야 한다.
        /// </summary>
        public bool TryUseStamina(float amount)
        {
            if (amount <= 0f) return true;
            if (stamina < amount) return false;

            timeSinceLastStaminaUse = 0f;
            SetStamina(stamina - amount);
            return true;
        }

        /// <summary>연속 소모(달리기 등). 0 미만으로 내려가지 않으며, 실제로 소모가 일어나면 회복 지연 타이머를 되돌린다.</summary>
        public void DrainStamina(float amount)
        {
            if (amount <= 0f) return;

            timeSinceLastStaminaUse = 0f;
            SetStamina(stamina - amount);
        }

        /// <summary>스태미나 회복. 최대치를 넘지 않는다.</summary>
        public void RecoverStamina(float amount)
        {
            if (amount <= 0f) return;
            SetStamina(stamina + amount);
        }

        /// <summary>마지막 소모 뒤 경과 시간을 누적한다(음수는 무시). 회복 시작 판단은 로직(CharacterStatSystem)이 한다.</summary>
        public void AdvanceStaminaIdleTime(float deltaTime)
        {
            if (deltaTime <= 0f) return;
            timeSinceLastStaminaUse += deltaTime;
        }

        private void SetStamina(float value)
        {
            float clamped = Mathf.Clamp(value, 0f, maxStamina);
            if (stamina == clamped) return;

            stamina = clamped;
            OnStaminaChanged?.Invoke(stamina, maxStamina);
        }

        public string SaveToJson()
        {
            var dto = new SaveDTO
            {
                currentHP = hp.CurrentValue,
                maxHP = hp.MaxValue,
                currentMP = mp.CurrentValue,
                maxMP = mp.MaxValue,
                currentStamina = stamina,
                maxStamina = maxStamina,
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

            // 스태미나 필드가 없던 이전 세이브(maxStamina == 0)는 현재 스태미나를 건드리지 않는다.
            if (dto.maxStamina > 0f)
            {
                maxStamina = dto.maxStamina;
                SetStamina(dto.currentStamina);
            }
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
            public float currentStamina;
            public float maxStamina;
            public float moveSpeed;
            public int faction;
        }
    }
}
