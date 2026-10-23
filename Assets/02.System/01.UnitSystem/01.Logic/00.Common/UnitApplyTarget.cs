using PipeLine.Combat;

namespace UnitSystem
{
    /// <summary>
    /// Applicator 입력 묶음(28번 §3-1, 가이드라인 Rule 1 "인자 나열 전달 금지")입니다. 대상 오브젝트의 RuntimeDataUnitGroup이
    /// Awake에서 같은 오브젝트의 IUnitTargetVisualizer/IAttackDamageProvider로 한 번 구성해 정적 이벤트에 실어 보냅니다(Logic이 GetComponent하지 않음).
    /// </summary>
    public class UnitApplyTarget
    {
        /// <summary>스케일·머티리얼·이펙트 지시 대상.</summary>
        public IUnitTargetVisualizer Visualizer { get; }

        /// <summary>공격 데미지를 가진 대상(투사체)이면 값, 아니면 null.</summary>
        public RuntimeDataAttackDamage AttackDamage { get; }

        public UnitApplyTarget(IUnitTargetVisualizer visualizer, RuntimeDataAttackDamage attackDamage)
        {
            Visualizer = visualizer;
            AttackDamage = attackDamage;
        }
    }
}
