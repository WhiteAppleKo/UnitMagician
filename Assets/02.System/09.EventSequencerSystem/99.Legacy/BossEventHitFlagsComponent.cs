using UnityEngine;
using CharacterSystem;
using PipeLine.Combat;

namespace EventSequencerSystem
{
    /// <summary>
    /// [Legacy - 16-A로 대체됨] 보스 피격 플래그는 이제 보스의 CombatFlagState(RuntimeStatData.CombatFlags)가 들고 있고,
    /// 회복(무적) 페이즈 진입/해제 때 BossPhaseLogicSystem이 AddHit/SuppressHit로 갱신합니다. 이 컴포넌트는 더 이상 씬/프리팹에서 쓰이지 않습니다.
    ///
    /// 보스(06번 시퀀서의 3단계 결정타 대상)의 피격 파이프라인 플래그를 상황에 맞게 노출하는 [V] 컴포넌트였습니다
    /// (IHitStepFlagsProvider 구현 - 돌문 DestructibleDoorComponent와 같은 패턴).
    ///
    /// 보스는 평소 일반 캐릭터 기본값(Evasion|Defense)으로 피격 판정을 받지만, 회복(무적) 페이즈에서는 투사체의 속성(얼음) 게이트와
    /// 크기 확대에 의한 데미지(FinalDamage = RawDamage x 질량 비율)를 검사해야 06이 "얼음 게이트 통과 AND FinalDamage >= 기준치"로
    /// 성공/실패를 판정할 수 있다(13번 기획서 §3 "3단계 판정"). 그래서 무적 상태(RuntimeStatData.IsInvincible)인 동안에만
    /// AttributeGate 단독 플래그를 반환한다 - 방어력 스텝(DefenseStep)은 FinalDamage를 RawDamage - Defense로 덮어써서
    /// 질량 비율을 지우므로, 이 구간에서는 함께 쓰지 않는다. 무적이 아닐 때는 기존 기본값 그대로(회귀 없음).
    /// </summary>
    [RequireComponent(typeof(CharacterStatComponent))]
    public class BossEventHitFlagsComponent : MonoBehaviour, IHitStepFlagsProvider
    {
        private const HitStepFlags NormalFlags = HitStepFlags.Evasion | HitStepFlags.Defense;

        private CharacterStatComponent statComponent;

        public HitStepFlags CurrentHitFlags
        {
            get
            {
                if (statComponent == null) statComponent = GetComponent<CharacterStatComponent>();

                var runtimeData = statComponent != null && statComponent.StatService != null
                    ? statComponent.StatService.RuntimeData
                    : null;

                return runtimeData != null && runtimeData.IsInvincible ? HitStepFlags.AttributeGate : NormalFlags;
            }
        }
    }
}
