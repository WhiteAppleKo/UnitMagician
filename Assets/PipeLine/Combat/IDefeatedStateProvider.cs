namespace PipeLine.Combat
{
    /// <summary>
    /// 피격 대상(victim)이 "이미 쓰러졌는지(HP 0 이하 / 파괴됨)"를 스스로 노출할 수 있게 하는 선택적 보조 인터페이스입니다.
    /// IHitStepFlagsProvider와 같은 이유로 IDamageable을 확장하지 않고 분리했습니다(구현하지 않은 IDamageable은
    /// 기존처럼 항상 "살아 있음"으로 취급되어 하위 호환). CollisionDamageTrigger는 쓰러진 대상과의 충돌을
    /// 피격으로 인정하지 않고(singleImpact를 소비하지 않고) 그대로 통과시킵니다(15-B 후속).
    /// </summary>
    public interface IDefeatedStateProvider
    {
        /// <summary>HP가 0 이하이거나 파괴되어 더 이상 피격 판정을 받을 이유가 없으면 true.</summary>
        bool IsDefeated { get; }
    }
}
