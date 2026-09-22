namespace PipeLine.Combat
{
    /// <summary>
    /// 피격 대상(victim)이 자신에게 필요한 피격 파이프라인 플래그(HitStepFlags)를 스스로 노출할 수 있게 하는
    /// 보조 인터페이스입니다. IDamageable을 직접 확장하지 않고 별도 인터페이스로 분리한 이유는, 기존 IDamageable
    /// 구현체(예: CharacterStatComponent)가 이 인터페이스를 몰라도 계속 컴파일/동작해야 하기 때문입니다
    /// (하위 호환 - 구현하지 않으면 InteractionSystem이 기존 캐릭터 기본값(Evasion|Defense)을 사용).
    /// 돌문처럼 회피/방어 판정이 필요 없고 속성 게이트만 필요한 비-캐릭터 오브젝트가 이 인터페이스를 구현합니다.
    /// </summary>
    public interface IHitStepFlagsProvider
    {
        /// <summary>이 대상에게 적용되어야 하는 피격 파이프라인 플래그 조합입니다.</summary>
        HitStepFlags CurrentHitFlags { get; }
    }
}
