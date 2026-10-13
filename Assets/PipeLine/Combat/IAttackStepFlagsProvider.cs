namespace PipeLine.Combat
{
    /// <summary>
    /// 공격 주체(캐릭터/NPC/투사체)가 자신이 쓰는 공격 파이프라인 플래그(AttackStepFlags)를 스스로 노출할 수 있게 하는
    /// 보조 인터페이스입니다. IHitStepFlagsProvider(피격 쪽)와 같은 패턴으로, 소비자는 TryGetComponent로만 조회합니다.
    /// 구현하지 않은 대상은 공격 플래그 None으로 취급됩니다.
    /// </summary>
    public interface IAttackStepFlagsProvider
    {
        /// <summary>이 공격 주체가 공격할 때 적용되어야 하는 공격 파이프라인 플래그 조합입니다.</summary>
        AttackStepFlags CurrentAttackFlags { get; }
    }
}
