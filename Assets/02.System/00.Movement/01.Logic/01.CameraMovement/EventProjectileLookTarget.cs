using GameEvent;
using UnityEngine;

namespace CameraMovement
{
    /// <summary>
    /// IEventCameraLookTarget의 구현체([L] 호출형 순수 로직)입니다. 09(GameEvent)의 IEventFlightTracker가 주는
    /// ProjectilePosition(투사체 획득 전에는 발사 지점, 소실 후에는 마지막 관측 위치)을 그대로 카메라 시선 대상으로 넘깁니다.
    ///
    /// 09는 LocomotionLifetimeScope와 다른 VContainer 스코프 트리(EventSystemLifetimeScope)라 생성자 주입이 불가능하므로,
    /// 다른 시스템과 같은 사후 배선 패턴을 씁니다 — LifetimeScope가 두 컨테이너가 모두
    /// 준비된 뒤(Start) Bind를 호출합니다. 이 클래스는 Transform/MonoBehaviour를 참조하지 않고 인터페이스만 보관합니다.
    /// </summary>
    public class EventProjectileLookTarget : IEventCameraLookTarget
    {
        private IEventFlightTracker m_flightTracker;

        /// <summary>09의 비행 추적기를 연결한다. 재호출하면 새 추적기로 교체된다.</summary>
        public void Bind(IEventFlightTracker flightTracker)
        {
            m_flightTracker = flightTracker;
        }

        public bool TryGetPosition(out Vector3 position)
        {
            if (m_flightTracker == null)
            {
                position = default;
                return false;
            }

            position = m_flightTracker.ProjectilePosition;
            return true;
        }
    }
}
