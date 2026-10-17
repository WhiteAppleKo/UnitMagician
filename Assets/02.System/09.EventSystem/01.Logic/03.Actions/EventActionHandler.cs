using System;

namespace GameEvent
{
    /// <summary>IEventActionHandler의 형 변환 보일러플레이트를 줄이는 기반 클래스입니다. 파생 핸들러는 구체 동작 SO 타입(TAction)만 받는 Execute를 구현합니다.</summary>
    public abstract class EventActionHandler<TAction> : IEventActionHandler where TAction : EventActionSO
    {
        public Type ActionType => typeof(TAction);

        public void Execute(EventActionSO action, IEventContext context)
        {
            Execute((TAction)action, context);
        }

        protected abstract void Execute(TAction action, IEventContext context);
    }
}
