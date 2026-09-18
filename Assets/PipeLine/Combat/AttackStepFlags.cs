using System;

namespace PipeLine.Combat
{
    /// <summary>
    /// 공격자 스탯 기반 "공격 파이프라인"에서 어떤 스텝을 적용할지 결정하는 플래그입니다.
    /// CombatPipelineManager는 이 플래그 조합을 키로 삼아 조립된 스텝 배열을 캐싱(메모이제이션)합니다.
    ///
    /// 확장 가이드: 새 비트를 추가할 때는
    ///   1) 여기에 다음 비트(1 &lt;&lt; N)를 추가하고
    ///   2) CombatPipelineManager의 정준 순서(canonical order) 테이블에 해당 스텝을 등록해야
    ///      실제로 파이프라인에 반영됩니다. (예: 향후 속성 공격/관통 공격 등)
    /// </summary>
    [Flags]
    public enum AttackStepFlags
    {
        None = 0,
        Critical = 1 << 0,

        // 향후 확장 예시(비트 자리만 예약):
        // ElementAttack = 1 << 1,
        // PierceAttack  = 1 << 2,
    }
}
