using System;
using UnityEngine;
using VContainer.Unity;

namespace CharacterSystem
{
    public class CharacterStatSystem : ICharacterStatService, ITickable
    {
        private readonly RuntimeStatData runtimeData;

        /// <summary>
        /// 런타임 스탯 데이터입니다. HP/MP는 IReadOnlyClampValueInt로 노출되어 외부에서 직접 증감시킬 수 없습니다.
        /// 상태 변경은 반드시 공식 메서드(TakeDamage, Heal, UseMP, RecoverMP)를 통해서만 수행하세요.
        /// </summary>
        public RuntimeStatData RuntimeData => runtimeData;
        public int CurrentMP => runtimeData != null ? runtimeData.MP.CurrentValue : 0;

        public CharacterStatSystem(PureStatData pureData)
        {
            runtimeData = new RuntimeStatData(pureData);
        }

        public CharacterStatSystem(RuntimeStatData existingRuntimeData)
        {
            runtimeData = existingRuntimeData ?? throw new ArgumentNullException(nameof(existingRuntimeData));
        }

        public void Heal(int amount)
        {
            runtimeData.IncreaseHP(amount);
        }

        public void TakeDamage(int amount)
        {
            if (runtimeData.IsInvincible) return;
            runtimeData.ReduceHP(amount);
        }

        public void RecoverMP(int amount)
        {
            runtimeData.IncreaseMP(amount);
        }

        public bool UseMP(int amount)
        {
            if (runtimeData.IsManaUnlimited) return true;
            return runtimeData.TryConsumeMP(amount);
        }

        public void SetInvincible(bool value)
        {
            runtimeData.SetInvincible(value);
        }

        public void SetManaUnlimited(bool value)
        {
            runtimeData.SetManaUnlimited(value);
        }

        public void SetPreviewManaCost(int cost)
        {
            runtimeData.SetPreviewManaCost(cost);
        }

        #region Stamina (19-A)

        public float Stamina => runtimeData != null ? runtimeData.Stamina : 0f;
        public float MaxStamina => runtimeData != null ? runtimeData.MaxStamina : 0f;

        public event Action<float, float> OnStaminaChanged
        {
            add => runtimeData.OnStaminaChanged += value;
            remove => runtimeData.OnStaminaChanged -= value;
        }

        public bool TryUseStamina(float amount)
        {
            return runtimeData.TryUseStamina(amount);
        }

        public void DrainStamina(float amount)
        {
            runtimeData.DrainStamina(amount);
        }

        /// <summary>
        /// 컨테이너가 매 프레임 호출한다. 게임 시간(Time.deltaTime)을 쓰므로 Time.timeScale이 0인
        /// 시간정지/일시정지 중에는 경과 시간도 회복량도 0이라 자동으로 멈춘다.
        /// </summary>
        public void Tick()
        {
            TickStamina(Time.deltaTime);
        }

        /// <summary>
        /// 스태미나 지연 회복 한 틱. 마지막 소모 뒤 staminaRegenDelaySeconds가 지나야 초당 staminaRegenPerSecond로 회복한다.
        /// 지연 구간을 넘긴 프레임에서는 넘긴 만큼의 시간만 회복에 반영해 프레임 레이트와 무관한 결과를 낸다.
        /// </summary>
        public void TickStamina(float deltaTime)
        {
            if (runtimeData == null || deltaTime <= 0f) return;

            float idleBefore = runtimeData.TimeSinceLastStaminaUse;
            runtimeData.AdvanceStaminaIdleTime(deltaTime);

            float regenTime = runtimeData.TimeSinceLastStaminaUse - Mathf.Max(idleBefore, runtimeData.StaminaRegenDelaySeconds);
            if (regenTime <= 0f) return;

            runtimeData.RecoverStamina(runtimeData.StaminaRegenPerSecond * Mathf.Min(regenTime, deltaTime));
        }

        #endregion
    }
}
