using System;
using UnityEngine;

namespace GameEvent
{
    /// <summary>
    /// TargetDestroyedCompletion 평가기입니다. 대상(IEventTarget)의 OnDestroyed 통지를 구독해 파괴되는 순간 Success를 냅니다(IsDestroyed 폴링 없음).
    /// 이미 파괴된 대상이면 Watch 호출 즉시 Success입니다. 대상 id를 찾지 못하면 에러 로그만 남기고 감시하지 않습니다.
    /// </summary>
    public class TargetDestroyedCompletionEvaluator : IEventCompletionEvaluator
    {
        public Type CompletionType => typeof(TargetDestroyedCompletion);

        public IDisposable Watch(EventCompletionSO completion, IEventContext context, Action<EventResult> onCompleted)
        {
            var spec = (TargetDestroyedCompletion)completion;
            if (!context.TryGetTarget(spec.TargetId, out var target))
            {
                Debug.LogError($"[TargetDestroyedCompletionEvaluator] 이벤트 '{context.EventId}': 대상 '{spec.TargetId}'를 찾을 수 없어 완료 판정을 할 수 없습니다.");
                return null;
            }

            if (target.IsDestroyed)
            {
                onCompleted(EventResult.Success);
                return null;
            }

            return new DestroyedSubscription(target, onCompleted);
        }

        private sealed class DestroyedSubscription : IDisposable
        {
            private readonly IEventTarget target;
            private readonly Action<EventResult> onCompleted;

            public DestroyedSubscription(IEventTarget target, Action<EventResult> onCompleted)
            {
                this.target = target;
                this.onCompleted = onCompleted;
                target.OnDestroyed += HandleDestroyed;
            }

            private void HandleDestroyed()
            {
                onCompleted(EventResult.Success);
            }

            public void Dispose()
            {
                target.OnDestroyed -= HandleDestroyed;
            }
        }
    }
}
