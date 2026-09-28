using System;

namespace EventSequencerSystem
{
    /// <summary>
    /// TutorialSequencerLogicSystem이 방송하는 세 신호(보스 50% 도달, 4단계 결정타 성공/실패)만 노출하는
    /// 최소 인터페이스입니다. VContainer_가이드라인상 구체 클래스 직접 주입이 금지되어 있어, 09번(시간 정지
    /// 마법 시스템 확장)이 06번의 이벤트를 구독하기 위해 추가되었습니다(09번 기획서 9.8절).
    /// 기존 TutorialSequencerLogicSystem의 로직/이벤트 시그니처는 변경하지 않고 인터페이스만 씌웁니다.
    /// </summary>
    public interface IEventSequencer
    {
        event Action OnBoss50Percent;
        event Action OnCoreHitSuccess;
        event Action OnCoreHitFail;
    }
}
