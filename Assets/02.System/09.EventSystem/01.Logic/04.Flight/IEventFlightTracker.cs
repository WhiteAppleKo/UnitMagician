using UnityEngine;

namespace GameEvent
{
    /// <summary>
    /// 3단계 이벤트의 비행 구간 계산 로직(EventFlightLogicSystem)이 위치 값(Vector3)만으로 투사체/플레이어/보스를
    /// 읽을 수 있게 하는 Visual Interface입니다(13번 기획서 §3). Logic은 Transform에 직접 접근하지 않고,
    /// Transform 접근은 이 인터페이스의 구현체(EventFlightTrackerComponent)에서만 일어납니다.
    ///
    /// P(플레이어)/B(보스)는 이벤트 진입 시점(CaptureEndpoints)에 스냅샷으로 고정되고, L(발사 지점)은 로직이 결정해
    /// BeginFlight로 내려보냅니다(구현체는 아군이 그 지점에서 투사체를 발사하도록 준비하고, 발사된 투사체의 참조를 받습니다).
    /// </summary>
    public interface IEventFlightTracker
    {
        /// <summary>이번 이벤트에서 추적 중인 투사체가 존재(파괴/비활성화되지 않고 생존)하는지 여부.</summary>
        bool HasProjectile { get; }

        /// <summary>이번 비행(BeginFlight 이후)에서 이벤트 투사체 참조를 한 번이라도 받았는지 여부. 발사 전 상태와 소실 상태를 구분하는 데 쓰입니다.</summary>
        bool HasAcquiredProjectile { get; }

        /// <summary>
        /// 주어진 가해 오브젝트(<c>DamageContext.Source</c>)가 이번 이벤트에서 추적 중인 이벤트 투사체(또는 그 자식)인지 판정합니다.
        /// 아직 투사체를 받기 전이거나 이미 소실/종료된 뒤에는 항상 false입니다. Transform 계층 비교는 구현체 안에서만 수행합니다.
        /// </summary>
        bool IsEventProjectile(GameObject source);

        /// <summary>BeginFlight로 지정된, 이벤트 진입 시 고정된 발사 지점 L.</summary>
        Vector3 LaunchPosition { get; }

        /// <summary>CaptureEndpoints 시점에 고정된 플레이어 위치 P.</summary>
        Vector3 PlayerPosition { get; }

        /// <summary>CaptureEndpoints 시점에 고정된 보스 위치 B.</summary>
        Vector3 BossPosition { get; }

        /// <summary>추적 중인 투사체의 현재 위치 X(매 프레임 값). 투사체가 없으면 마지막으로 관측한 위치.</summary>
        Vector3 ProjectilePosition { get; }

        /// <summary>
        /// 이벤트 진입 시 플레이어/보스의 현재 위치를 P/B로 스냅샷합니다. 플레이어 위치에는 투사체가 지나는 높이를 맞추기 위한
        /// playerAnchorOffset(이벤트 SO의 추적 높이 오프셋)을 더합니다.
        /// </summary>
        void CaptureEndpoints(Vector3 playerAnchorOffset);

        /// <summary>
        /// 발사 지점 L을 확정하고, 아군이 그 지점에서 투사체를 발사해 그 참조를 이 추적기가 받도록 준비합니다.
        /// projectileSpeed/projectileDamage는 이 발사 1회에만 적용되는 이벤트 투사체 전용 값입니다(0 이하이면 NPC 데이터 값을 씀).
        /// </summary>
        void BeginFlight(Vector3 launchPosition, float projectileSpeed, int projectileDamage);

        /// <summary>
        /// 발사 직후 호출됩니다. 아직 소모되지 않은 발사 지점 예약과 스폰 구독을 해제해,
        /// 이후 다른 발사(예: 돌문 반복 사격)가 이벤트 투사체로 오인되지 않게 합니다(이미 받은 투사체 참조는 유지).
        /// </summary>
        void CloseLaunchWindow();

        /// <summary>추적을 끝내고 아군 발사 지점 오버라이드/투사체 참조를 해제합니다.</summary>
        void EndFlight();
    }
}
