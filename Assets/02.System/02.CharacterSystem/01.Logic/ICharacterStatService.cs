namespace CharacterSystem
{
    /// <summary>
    /// 캐릭터 스탯 조작(체력/마나 회복 및 소모, 피격 연산)을 추상화한 DIP 인터페이스입니다.
    /// </summary>
    public interface ICharacterStatService
    {
        RuntimeStatData RuntimeData { get; }
        void Heal(int amount);
        void TakeDamage(int amount);
        void RecoverMP(int amount);
        bool UseMP(int amount);
    }
}
