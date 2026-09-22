using System;

namespace DestructibleSystem
{
    /// <summary>
    /// 돌문(파괴 가능 환경 오브젝트)의 순수 C# 로직 구현체([L] 호출형 순수 로직)입니다. MonoBehaviour를 상속하지 않으며,
    /// Transform/Collider/ParticleSystem 등 구체 컴포넌트를 직접 참조하지 않습니다 - 실제 연출/콜라이더 조작은
    /// 전부 IDestructibleDoorVisualizer 인터페이스 호출로 위임합니다("판단"만 이 클래스가 수행).
    ///
    /// 2차 게이트: ApplyDamage(int amount)로 전달된 데미지(이미 1차 게이트인 AttributeGateStep에서
    /// 속성 확인 + 질량 비율까지 반영된 최종 데미지)를 pureData.Hp와 비교합니다. 돌문 HP는 절대 누적되지 않으며
    /// (RuntimeData 자체가 없음), 매 호출마다 항상 pureData.Hp를 기준으로 독립적으로 재판정합니다 - 이전 시도의
    /// 실패한 타격 데미지는 다음 시도로 전혀 이어지지 않습니다.
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
        /// 2차 게이트: 이 호출로 전달된 amount만이 판정 대상이며, 이전 호출들의 실패한 데미지는 전혀 누적되지 않습니다
        /// (매 호출이 항상 pureData.Hp 기준의 독립 판정 - "지속 체력"으로 구현하지 말 것).
        /// </summary>
        public void ApplyDamage(int amount)
        {
            if (amount >= pureData.Hp)
            {
                OnDestroyed?.Invoke();
                visualizer.PlayDestroy();
            }
            else
            {
                visualizer.PlayCrackAndRepair();
            }
        }
    }
}
