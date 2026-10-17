using System;

namespace GameEvent
{
    /// <summary>
    /// SceneStartTrigger 평가기입니다. 감시할 대상이 없으므로 Watch가 불리는 즉시(= 실행기가 이벤트를 Armed로 만든 시점, 씬 시작) 충족시킵니다.
    /// </summary>
    public class SceneStartTriggerEvaluator : IEventTriggerEvaluator
    {
        public Type TriggerType => typeof(SceneStartTrigger);

        public IDisposable Watch(EventTriggerSO trigger, IEventContext context, Action onSatisfied)
        {
            onSatisfied();
            return null;
        }
    }
}
