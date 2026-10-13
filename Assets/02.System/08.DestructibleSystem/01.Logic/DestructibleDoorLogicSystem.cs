using System;
using UnityEngine;
using PipeLine.Contexts;

namespace DestructibleSystem
{
    /// <summary>
    /// 돌문(파괴 가능 환경 오브젝트)의 순수 C# 로직 구현체([L] 호출형 순수 로직)입니다. MonoBehaviour를 상속하지 않으며,
    /// Transform/Collider/ParticleSystem 등 구체 컴포넌트를 직접 참조하지 않습니다 - 실제 연출/콜라이더 조작은
    /// 전부 IDestructibleDoorVisualizer 인터페이스 호출로 위임합니다("판단"만 이 클래스가 수행).
    ///
    /// 최종 판정(20번 기획서): 피격 파이프라인이 기록한 속성 게이트 결과(AttributeGatePassed), 크기 게이트 결과(SizeGatePassed),
    /// 그리고 전달된 데미지(amount, 속성 확인 + 질량 비율까지 반영된 최종 데미지)를 pureData.Hp와 비교한 결과, 세 조건을 각각 개별
    /// 확인해 모두 충족해야 파괴하고 하나라도 부족하면 금 갔다 복구합니다(속성 게이트 실패는 ApplyDamage가 호출되지 않고 스파크).
    /// 돌문 HP는 절대 누적되지 않으며(RuntimeData 자체가 없음), 매 호출마다 항상 pureData.Hp를 기준으로 독립적으로 재판정합니다 -
    /// 이전 시도의 실패한 타격 데미지는 다음 시도로 전혀 이어지지 않습니다.
    /// </summary>
    public class DestructibleDoorLogicSystem
    {
        private readonly PureDataDestructibleDoor pureData;
        private readonly IDestructibleDoorVisualizer visualizer;

        /// <summary>파괴 판정이 실제로 내려진 시점(PlayDestroy 호출 직전)에 발행됩니다.</summary>
        public event Action OnDestroyed;

        public DestructibleDoorLogicSystem(PureDataDestructibleDoor pureData, IDestructibleDoorVisualizer visualizer)
        {
            this.pureData = pureData ?? throw new ArgumentNullException(nameof(pureData));
            this.visualizer = visualizer ?? throw new ArgumentNullException(nameof(visualizer));
        }

        /// <summary>
        /// 최종 판정: 이 호출로 전달된 amount와 컨텍스트의 게이트 결과만이 판정 대상이며, 이전 호출들의 실패한 데미지는 전혀 누적되지 않습니다
        /// (매 호출이 항상 pureData.Hp 기준의 독립 판정 - "지속 체력"으로 구현하지 말 것).
        /// 속성/크기/데미지 중 실패한 조건은 한 줄 로그로 남깁니다(연출은 변경 없음).
        /// </summary>
        public void ApplyDamage(int amount, in DamageContext context)
        {
            bool attributePassed = context.AttributeGatePassed;
            bool sizePassed = context.SizeGatePassed;
            bool damagePassed = amount >= pureData.Hp;

            if (attributePassed && sizePassed && damagePassed)
            {
                OnDestroyed?.Invoke();
                visualizer.PlayDestroy();
            }
            else
            {
                Debug.Log($"[DestructibleDoorLogicSystem] Judgement NOT destroyed - failed gate(s): {DescribeFailedGates(attributePassed, sizePassed, damagePassed)} (amount: {amount}, required hp: {pureData.Hp})");
                visualizer.PlayCrackAndRepair();
            }
        }

        private static string DescribeFailedGates(bool attributePassed, bool sizePassed, bool damagePassed)
        {
            // 속성 게이트 실패(Aborted)면 이후 크기 게이트는 실행되지 않으므로 속성 게이트만 실패 사유로 남긴다.
            if (!attributePassed) return "속성게이트(attribute)";

            string failed = string.Empty;
            if (!sizePassed) failed += "크기게이트(size) ";
            if (!damagePassed) failed += "데미지(damage) ";
            return failed.TrimEnd();
        }
    }
}
