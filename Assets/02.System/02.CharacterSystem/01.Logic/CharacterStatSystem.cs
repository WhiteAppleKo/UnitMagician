using System;

namespace CharacterSystem
{
    public class CharacterStatSystem
    {
        private readonly RuntimeStatData runtimeData;

        public RuntimeStatData RuntimeData => runtimeData;

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
            runtimeData.HP.Increase(amount);
        }

        public void TakeDamage(int amount)
        {
            runtimeData.HP.Reduce(amount);
        }

        public void RecoverMP(int amount)
        {
            runtimeData.MP.Increase(amount);
        }

        public bool UseMP(int amount)
        {
            return runtimeData.TryConsumeMP(amount);
        }
    }
}
