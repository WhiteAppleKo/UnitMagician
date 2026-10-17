using UnityEngine;

namespace GameEvent
{
    /// <summary>
    /// 강 이벤트(보스 50%)의 투사체 발사 동작입니다. 이벤트 시작 시점의 플레이어 P와 보스 B로 발사 지점 L을 정합니다:
    /// L = P + R(+launchAngleDegrees, 위 축) x normalize(수평(B - P)) x launchDistance (위에서 볼 때 시계 방향 = 플레이어가 보스를 볼 때의 오른쪽).
    /// 특수 NPC가 L에서 보스를 향해 투사체를 1발 쏘고, 이 투사체의 비행(접근/이탈 구간, 전환점)이 감속/카메라/마법 발동을 구동합니다.
    /// 아군 NPC 자체의 투사체 속도/데미지는 이 이벤트에서 의미가 없고 아래 이벤트 투사체 전용 값이 쓰입니다(이벤트 투사체 속도는 곧 이벤트 진행 시간).
    /// </summary>
    [CreateAssetMenu(fileName = "EventProjectileLaunchAction_", menuName = "GameEvent/Action/EventProjectileLaunch")]
    public class EventProjectileLaunchAction : EventActionSO, IEventLaunchSettings
    {
        [Header("Launch Point (22번 기획서 §11 - 플레이어 우측 N도)")]
        [Tooltip("발사 지점 L의 방향 각도(도). P에서 B로 향하는 수평 방향을 0도로 보고, 위에서 내려다볼 때 오른쪽(시계 방향)으로 이 각도만큼 회전한 방향에 L을 둡니다. 음수면 왼쪽입니다.")]
        [SerializeField] private float launchAngleDegrees = 120f;
        [Tooltip("발사 지점 L이 플레이어 P에서 떨어지는 수평 거리(m). 0 이하이면 비행 구간 계산을 시작하지 않습니다.")]
        [SerializeField] private float launchDistance = 12f;
        [Tooltip("투사체가 지나는 플레이어 기준점 높이 보정(플레이어 루트 위치에 더해짐). 발사 지점 L의 높이와 접근/이탈 판정 평면 기준 P에 쓰입니다.")]
        [SerializeField] private Vector3 trackingHeightOffset = new Vector3(0f, 1f, 0f);

        [Header("Event Projectile (이벤트 전용 값 - 돌문 샷/보스 일반 공격 값과 별개)")]
        [Tooltip("이벤트 투사체 속도(m/s). 이벤트 진행 시간(판단 시간)을 정합니다. 0 이하이면 발사 NPC 데이터의 속도를 씁니다.")]
        [SerializeField] private float projectileSpeed = 12f;
        [Tooltip("이벤트 투사체 데미지. 0 이하이면 발사 NPC 데이터의 데미지를 씁니다.")]
        [SerializeField] private int projectileDamage = 60;

        public float LaunchAngleDegrees => launchAngleDegrees;
        public float LaunchDistance => launchDistance;
        public Vector3 TrackingHeightOffset => trackingHeightOffset;
        public float ProjectileSpeed => projectileSpeed;
        public int ProjectileDamage => projectileDamage;
    }
}
