using System;

namespace GameEvent
{
    /// <summary>
    /// 시작 조건 SO(EventTriggerSO 파생) 한 종류를 감시하는 평가기의 규약입니다. 조건이 충족되면 onSatisfied를 호출합니다.
    /// 감시를 중단해야 하는 평가기는 해제 핸들(IDisposable)을 돌려주고, 즉시 충족되어 감시할 것이 없으면 null을 돌려줘도 됩니다.
    /// 조건 충족은 통지(이벤트 구독)로 받는 것이 원칙이며 평가기가 매 프레임 폴링하지 않습니다.
    /// </summary>
    public interface IEventTriggerEvaluator
    {
        /// <summary>이 평가기가 처리하는 시작 조건 SO의 타입.</summary>
        Type TriggerType { get; }

        IDisposable Watch(EventTriggerSO trigger, IEventContext context, Action onSatisfied);
    }
}
