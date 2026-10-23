using System;

namespace PipeLine.Combat
{
    /// <summary>
    /// BaseDamage를 쓰는 모든 런타임 주체(충돌 데미지 오브젝트, NPC 전투 주체)의 공용 데미지 데이터([RuntimeData])입니다(28번 §6-9).
    /// BaseDamage가 있는 모든 RuntimeData는 CurrentDamage를 가진다 - 각 주체는 BaseDamage를 별도 필드로 두지 않고 이 인스턴스를 보관합니다.
    /// BaseDamage는 PureData나 발사 값에서 Initialize로만 정해지고, CurrentDamage는 마법 등 런타임 변화(Applicator의 데미지 계산 자리)가
    /// SetCurrentDamage로 저장합니다. 이 주체가 데미지를 줄 때는 CurrentDamage를 씁니다. 직렬화하지 않습니다(§6-10).
    /// </summary>
    public class RuntimeDataAttackDamage
    {
        public int BaseDamage { get; private set; }
        public int CurrentDamage { get; private set; }

        /// <summary>BaseDamage/CurrentDamage가 바뀔 때 발행됩니다.</summary>
        public event Action<RuntimeDataAttackDamage> OnDamageChanged;

        public RuntimeDataAttackDamage(int baseDamage = 0)
        {
            BaseDamage = baseDamage;
            CurrentDamage = baseDamage;
        }

        /// <summary>생성·발사·풀 재사용 시: BaseDamage = CurrentDamage = baseDamage(이전 런타임 변화는 모두 지워진다).</summary>
        public void Initialize(int baseDamage)
        {
            BaseDamage = baseDamage;
            CurrentDamage = baseDamage;
            OnDamageChanged?.Invoke(this);
        }

        /// <summary>런타임 변화(마법 적용 등)의 결과를 저장합니다.</summary>
        public void SetCurrentDamage(int value)
        {
            if (CurrentDamage == value) return;

            CurrentDamage = value;
            OnDamageChanged?.Invoke(this);
        }
    }
}
