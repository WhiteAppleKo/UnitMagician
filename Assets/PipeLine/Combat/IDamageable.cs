namespace PipeLine.Combat
{
    /// <summary>
    /// 피격 파이프라인의 최종 결과(FinalDamage)를 전달받아 실제로 HP 등을 감소시키는,
    /// 피격 가능 대상이 구현하는 얇은 인터페이스입니다.
    /// ApplyDamageStep(파이프라인 종료 고정 스텝)은 이 인터페이스를 통해서만 데미지를 적용하므로,
    /// PipeLine 계층이 CharacterStatSystem 등 구체적인 스탯 시스템 구현을 몰라도 됩니다.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// 피격 파이프라인이 산출한 최종 데미지를 적용합니다.
        /// </summary>
        void ApplyDamage(int amount);
    }
}
