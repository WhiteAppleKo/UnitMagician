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
    }
}
