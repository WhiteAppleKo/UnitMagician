using System;
using UnityEngine;

namespace GameEvent
{
    /// <summary>
    /// BossHpRatioTrigger 평가기입니다. 대상(IEventHealthTarget)의 HP 변경 통지(OnHpRatioChanged)를 구독해 HP 비율이 트리거의 ratio 이하가 되는 순간 충족시킵니다(폴링 없음).
    /// 평가 시작 시점에 이미 ratio 이하이면 Watch 호출 즉시 충족됩니다(예: 선행 이벤트가 끝난 뒤 이미 HP가 낮은 경우). 대상 id를 찾지 못하거나 체력 대상이 아니면
    /// 에러 로그만 남기고 감시하지 않습니다.
    /// </summary>
    public class BossHpRatioTriggerEvaluator : IEventTriggerEvaluator
    {
        public Type TriggerType => typeof(BossHpRatioTrigger);

        public IDisposable Watch(EventTriggerSO trigger, IEventContext context, Action onSatisfied)
        {
            var spec = (BossHpRatioTrigger)trigger;
            if (!context.TryGetTarget(spec.TargetId, out var target) || !(target is IEventHealthTarget healthTarget))
            {
                Debug.LogError($"[BossHpRatioTriggerEvaluator] 이벤트 '{context.EventId}': 체력 대상 '{spec.TargetId}'를 찾을 수 없어 시작 조건을 감시할 수 없습니다.");
                return null;
            }

            if (healthTarget.HpRatio <= spec.Ratio)
            {
                onSatisfied();
                return null;
            }

            return new HpSubscription(healthTarget, spec.Ratio, onSatisfied);
        }

        private sealed class HpSubscription : IDisposable
        {
            private readonly IEventHealthTarget target;
            private readonly float ratio;
            private readonly Action onSatisfied;

            public HpSubscription(IEventHealthTarget target, float ratio, Action onSatisfied)
            {
                this.target = target;
                this.ratio = ratio;
                this.onSatisfied = onSatisfied;
                target.OnHpRatioChanged += HandleHpRatioChanged;
            }

            private void HandleHpRatioChanged(float hpRatio)
            {
                if (hpRatio <= ratio) onSatisfied();
            }

            public void Dispose()
            {
                target.OnHpRatioChanged -= HandleHpRatioChanged;
            }
        }
    }
}
