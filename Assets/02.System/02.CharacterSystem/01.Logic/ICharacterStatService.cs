namespace CharacterSystem
{
    /// <summary>
    /// 캐릭터 스탯 조작(체력/마나 회복 및 소모, 피격 연산)을 추상화한 DIP 인터페이스입니다.
    /// </summary>
    public interface ICharacterStatService
    {
        /// <summary>
        /// 런타임 스탯 데이터입니다. HP/MP는 IReadOnlyClampValueInt로 노출되어 외부에서 직접 증감시킬 수 없습니다.
        /// 상태 변경은 반드시 공식 메서드(TakeDamage, Heal, UseMP, RecoverMP)를 통해서만 수행하세요.
        /// </summary>
        RuntimeStatData RuntimeData { get; }
        int CurrentMP { get; }
        void Heal(int amount);
        void TakeDamage(int amount);
        void RecoverMP(int amount);
        bool UseMP(int amount);
        void SetInvincible(bool value);
        void SetManaUnlimited(bool value);

        /// <summary>적용 전 미리보기(13-C)의 예상 마나 총비용을 마나 UI에 알린다(표시 전용, 실제 MP는 변하지 않음). 0이면 표시 해제.</summary>
        void SetPreviewManaCost(int cost);

        /// <summary>현재 스태미나(19-A). 연속 소모를 위해 float이다.</summary>
        float Stamina { get; }

        /// <summary>최대 스태미나(19-A).</summary>
        float MaxStamina { get; }

        /// <summary>스태미나가 바뀔 때만 (현재값, 최대값)으로 발행된다(19-A).</summary>
        event System.Action<float, float> OnStaminaChanged;

        /// <summary>스태미나를 amount만큼 한 번에 소모한다(구르기/공격 등). 부족하면 소모 없이 false.</summary>
        bool TryUseStamina(float amount);

        /// <summary>스태미나를 연속 소모한다(달리기 등). 0 미만으로 내려가지 않는다.</summary>
        void DrainStamina(float amount);
    }
}
