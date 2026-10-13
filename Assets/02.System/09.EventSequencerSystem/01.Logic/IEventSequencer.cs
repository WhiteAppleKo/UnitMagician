using System;

namespace EventSequencerSystem
{
    /// <summary>
    /// TutorialSequencerLogicSystem이 방송하는 신호만 노출하는 최소 인터페이스입니다.
    /// 06번 신호(보스 50% 도달, 4단계 결정타 성공/실패) 외에, 13번 기획서(3단계 접근/이탈 곡선)에서
    /// 아군 투사체의 비행 구간/진행도 변경과 접근->이탈 전환점 신호가 추가되었습니다.
    /// VContainer_가이드라인상 구체 클래스 직접 주입이 금지되어 있어, 09번(시간 정지 마법 시스템 확장)이
    /// 06번의 이벤트를 구독하기 위해 추가되었습니다(09번 기획서 9.8절).
    /// 기존 이벤트 시그니처는 변경하지 않고 이벤트 추가만 했습니다.
    /// </summary>
    public interface IEventSequencer
    {
        event Action OnBoss50Percent;
        event Action OnCoreHitSuccess;
        event Action OnCoreHitFail;

        /// <summary>3단계 투사체의 구간(접근/이탈) 또는 구간 내 진행도(0~1)가 바뀔 때 발행됩니다.</summary>
        event Action<EventFlightPhase, float> OnEventFlightChanged;

        /// <summary>투사체가 플레이어를 지나치는 전환점(접근 -> 이탈)에서 이벤트 사이클당 정확히 한 번 발행됩니다.</summary>
        event Action OnEventFlightTurnaround;
    }
}
