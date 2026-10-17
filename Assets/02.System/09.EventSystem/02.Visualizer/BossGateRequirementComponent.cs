using UnityEngine;
using PipeLine.Combat;

namespace GameEvent
{
    /// <summary>
    /// 보스(보스 50% 이벤트의 결정타 대상) GameObject에 붙는 얇은 [V] 컴포넌트로, 회복(무적) 페이즈에서 피격 대상으로서
    /// 요구되는 게이트 조건 값(속성 최대 온도, 크기 질량 비율)을 IGateRequirementProvider로 제공합니다(20번 기획서).
    /// 값은 이벤트 SO의 완료 조건(CoreHitCompletion)에서 읽기만 하며(성공 판정 기준 requiredDamage와 같은 에셋) 코드에 기준 숫자는 없습니다.
    /// 일반 캐릭터(CharacterStatComponent)가 아니라 이 컴포넌트를 두는 이유: 요구값의 소유자가 이벤트 SO이고,
    /// CharacterStatComponent(일반 IDamageable 구현체)는 변경 없이 동작해야 하기 때문입니다(하위 호환). 게이트 비트(AttributeGate|SizeGate)는
    /// 회복 페이즈 동안에만 BossPhaseLogicSystem이 보스의 CombatFlags에 켜므로 평소 피격에는 이 값이 쓰이지 않습니다.
    /// </summary>
    public class BossGateRequirementComponent : MonoBehaviour, IGateRequirementProvider
    {
        [Tooltip("보스 이벤트의 완료 조건 SO. 속성 게이트 최대 온도 / 크기 게이트 질량 비율 요구값을 여기서 읽습니다.")]
        [SerializeField] private CoreHitCompletion coreHitCompletion;

        /// <summary>IGateRequirementProvider 구현: 속성 게이트 요구 최대 온도(이벤트 SO).</summary>
        public float RequiredMaxTemperature => coreHitCompletion != null ? coreHitCompletion.RequiredMaxTemperature : 0f;

        /// <summary>IGateRequirementProvider 구현: 크기 게이트 요구 질량 비율(이벤트 SO).</summary>
        public float RequiredMassRatio => coreHitCompletion != null ? coreHitCompletion.RequiredMassRatio : 0f;
    }
}
