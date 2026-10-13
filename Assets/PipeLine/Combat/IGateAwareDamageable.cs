using PipeLine.Contexts;

namespace PipeLine.Combat
{
    /// <summary>
    /// 피격 파이프라인의 게이트 결과(DamageContext.AttributeGatePassed/SizeGatePassed)까지 받아 스스로 최종 판정하는
    /// 피격 가능 대상이 구현하는 선택 인터페이스입니다(20번 기획서 §3-1). ApplyDamageStep은 이 인터페이스를 구현한 대상에는
    /// 컨텍스트를 함께 전달하고, 구현하지 않은 일반 IDamageable(예: CharacterStatComponent)에는 기존 ApplyDamage(int)를 그대로 호출합니다.
    /// </summary>
    public interface IGateAwareDamageable : IDamageable
    {
        /// <summary>
        /// 최종 데미지와 게이트 결과를 함께 전달받아 적용합니다. 속성 게이트 실패(Aborted)는 ApplyDamageStep이 호출 자체를 하지 않습니다.
        /// </summary>
        void ApplyDamage(int amount, in DamageContext context);
    }
}
