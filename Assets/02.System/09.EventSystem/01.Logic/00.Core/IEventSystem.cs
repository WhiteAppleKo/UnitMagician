using System;

namespace GameEvent
{
    /// <summary>
    /// 이벤트 시스템(EventLogicSystem)이 외부에 노출하는 규약입니다. 범용 신호(OnEventStarted/OnEventCompleted)와 상태 조회, 그리고
    /// 소유 시스템이 자신의 핸들러/평가기를 등록하는 창구를 제공합니다.
    /// 소유 시스템(NPC/카메라/감속 등)은 이 시스템과 서로 다른 VContainer 스코프 트리에 있어 생성자 주입이 불가능하므로,
    /// 기존의 사후 배선(Bind*) 패턴과 같이 LifetimeScope가 Start()에서 두 컨테이너가 모두 준비된 뒤 Register* 를 사후 호출하는 방식으로 연결합니다.
    /// </summary>
    public interface IEventSystem
    {
        /// <summary>이벤트가 Running으로 전환되고 onStart 동작이 실행된 직후 발행됩니다.</summary>
        event Action<string> OnEventStarted;

        /// <summary>이벤트의 완료 판정이 나고 onSuccess/onFail 동작이 실행된 직후 발행됩니다.</summary>
        event Action<string, EventResult> OnEventCompleted;

        EventState GetState(string eventId);
        EventResult GetResult(string eventId);

        void RegisterActionHandler(IEventActionHandler handler);
        void RegisterTriggerEvaluator(IEventTriggerEvaluator evaluator);
        void RegisterCompletionEvaluator(IEventCompletionEvaluator evaluator);
    }
}
