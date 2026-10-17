using UnityEngine;

namespace GameEvent
{
    /// <summary>
    /// 이벤트 투사체 발사 수치를 읽기 전용으로 노출하는 인터페이스입니다. 값의 원본은 이벤트 SO(EventProjectileLaunchAction)이고,
    /// 비행 계산 로직(EventFlightLogicSystem)은 구체 SO가 아니라 이 인터페이스로만 읽습니다(DIP).
    /// </summary>
    public interface IEventLaunchSettings
    {
        /// <summary>발사 지점 L의 방향 각도(도). P-&gt;B 수평 방향 기준 위에서 볼 때 오른쪽(시계 방향) 회전.</summary>
        float LaunchAngleDegrees { get; }

        /// <summary>발사 지점 L이 플레이어 P에서 떨어지는 수평 거리(m). 0 이하이면 비행 구간 계산을 시작하지 않습니다.</summary>
        float LaunchDistance { get; }

        /// <summary>이벤트 투사체 속도(m/s) = 이벤트 진행 시간(판단 시간). 0 이하이면 발사 NPC 데이터의 속도를 씁니다.</summary>
        float ProjectileSpeed { get; }

        /// <summary>이벤트 투사체 데미지. 0 이하이면 발사 NPC 데이터의 데미지를 씁니다.</summary>
        int ProjectileDamage { get; }

        /// <summary>투사체가 지나는 플레이어 기준점 높이 보정(플레이어 루트 위치에 더해지는 오프셋). 발사 지점 L의 높이와 접근/이탈 판정 평면 기준 P에 쓰입니다.</summary>
        Vector3 TrackingHeightOffset { get; }
    }
}
