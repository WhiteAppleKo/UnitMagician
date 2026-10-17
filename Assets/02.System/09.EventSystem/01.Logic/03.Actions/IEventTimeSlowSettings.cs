using UnityEngine;

namespace GameEvent
{
    /// <summary>
    /// 이벤트(3단계 투사체 비행) 구간의 감속 연출 수치를 읽기 전용으로 노출하는 인터페이스입니다.
    /// 값의 원본은 이벤트 SO(TimeSlowOverrideAction)이며, 감속 시스템(TimeSlowLogicSystem)은 구체 SO가 아니라 이 인터페이스로만 읽습니다
    /// (DIP, 22번 기획서 §11 "조정 값 원칙" - Goal 47). 스코프 트리가 달라 생성자 주입이 불가능하므로 사후 배선(Bind)으로 받습니다.
    /// </summary>
    public interface IEventTimeSlowSettings
    {
        /// <summary>접근 -> 이탈 전환점(투사체-플레이어 최근접 순간)의 최저 TimeScale. 0보다 큰 값.</summary>
        float EventSlowMinTimeScale { get; }

        /// <summary>접근 구간 완급 곡선: x = 접근 진행도(0~1), y = 감속 비율(0 = 정상 속도, 1 = 최저 TimeScale).</summary>
        AnimationCurve EventApproachCurve { get; }

        /// <summary>이탈 구간 완급 곡선: x = 이탈 진행도(0~1), y = 회복 비율(0 = 최저 TimeScale, 1 = 정상 속도).</summary>
        AnimationCurve EventExitCurve { get; }
    }
}
