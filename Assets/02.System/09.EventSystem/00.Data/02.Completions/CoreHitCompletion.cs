using UnityEngine;

namespace GameEvent
{
    /// <summary>
    /// 이벤트 투사체가 대상(무적 상태의 보스)을 맞힌 결과로 성공/실패를 내는 완료 조건입니다(13번/20번 기획서: 판정은 명중 결과로만, 시간 기반 타임아웃 없음).
    /// 판정 대상은 이벤트 투사체의 피격으로 한정합니다(타이머 샷/직전 재도전 샷/플레이어 일반 공격 등은 HP는 줄이지만 판정하지 않음).
    /// 성공 = (속성 게이트 요구 시 통과) AND (크기 게이트 요구 시 통과) AND FinalDamage가 requiredDamage 이상. 그 외는 모두 실패입니다.
    /// 투사체가 명중 없이 소실되면 소프트락 방지 안전망으로 실패 처리합니다. 게이트가 요구하는 온도/질량 비율 값(아래)은 대상의 게이트 요구값 제공자
    /// (BossGateRequirementComponent)가 읽어 피격 파이프라인의 속성/크기 게이트에 제공합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "CoreHitCompletion_", menuName = "GameEvent/Completion/CoreHit")]
    public class CoreHitCompletion : EventCompletionSO
    {
        [Tooltip("피격을 판정할 이벤트 대상의 id(IEventTarget.TargetId, IEventHealthTarget을 구현한 대상 - 무적 여부를 함께 확인).")]
        [SerializeField] private string targetId;
        [Tooltip("성공에 필요한 최소 FinalDamage(FinalDamage = CollisionDamageTrigger.baseDamage x 질량 비율, AttributeGateStep).")]
        [SerializeField] private int requiredDamage = 100;
        [Tooltip("성공에 속성 게이트 통과를 요구할지.")]
        [SerializeField] private bool requireAttributeGate = true;
        [Tooltip("성공에 크기 게이트 통과를 요구할지.")]
        [SerializeField] private bool requireSizeGate = true;

        [Header("Gate Requirements (20번 기획서 - 대상의 게이트 요구값)")]
        [Tooltip("속성 게이트: 투사체 온도가 이 값 미만이어야 통과(기본 0 = 얼음 계열).")]
        [SerializeField] private float requiredMaxTemperature = 0f;
        [Tooltip("크기 게이트: 투사체 질량 비율(현재/원본)이 이 값 이상이어야 통과.")]
        [SerializeField] private float requiredMassRatio = 1.5f;

        public string TargetId => targetId;
        public int RequiredDamage => requiredDamage;
        public bool RequireAttributeGate => requireAttributeGate;
        public bool RequireSizeGate => requireSizeGate;
        public float RequiredMaxTemperature => requiredMaxTemperature;
        public float RequiredMassRatio => requiredMassRatio;
    }
}
