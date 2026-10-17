using System;

namespace GameEvent
{
    /// <summary>
    /// 완료 조건 SO(EventCompletionSO 파생) 한 종류를 판정하는 평가기의 규약입니다. 판정이 나면 onCompleted(결과)를 호출합니다(성공/실패 모두 가능).
    /// 감시를 중단해야 하는 평가기는 해제 핸들(IDisposable)을 돌려주고, 이미 판정이 났으면 null을 돌려줘도 됩니다.
    /// 판정은 통지(이벤트 구독)로 받는 것이 원칙이며 평가기가 매 프레임 폴링하지 않습니다.
    /// </summary>
    public interface IEventCompletionEvaluator
    {
        /// <summary>이 평가기가 처리하는 완료 조건 SO의 타입.</summary>
        Type CompletionType { get; }

        IDisposable Watch(EventCompletionSO completion, IEventContext context, Action<EventResult> onCompleted);
    }
}
