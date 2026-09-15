using System;

namespace CharacterSystem
{
    public class CharacterStatSystem : ICharacterStatService
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
            runtimeData.ReduceHP(amount);
        }

        public void RecoverMP(int amount)
        {
            runtimeData.IncreaseMP(amount);
        }

        public bool UseMP(int amount)
        {
            return runtimeData.TryConsumeMP(amount);
        }
    }
}
