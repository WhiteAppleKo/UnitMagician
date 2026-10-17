using System.Collections.Generic;
using UnityEngine;

namespace GameEvent
{
    /// <summary>
    /// 씬에 등록된 이벤트 대상 목록(IEventTarget[])을 id로 조회해 주는 [L] 호출형 순수 로직입니다.
    /// 목록은 EventSystemLifetimeScope가 인스펙터 등록분을 넘겨주며, id가 비었거나 중복이면 에러 로그 후 그 항목을 건너뜁니다.
    /// </summary>
    public class EventTargetRegistry : IEventTargetProvider
    {
        private readonly Dictionary<string, IEventTarget> targets = new Dictionary<string, IEventTarget>();

        public EventTargetRegistry(IEventTarget[] eventTargets)
        {
            if (eventTargets == null) return;

            foreach (var target in eventTargets)
            {
                if (target == null) continue;

                if (string.IsNullOrEmpty(target.TargetId))
                {
                    Debug.LogError($"[EventTargetRegistry] TargetId가 비어 있어 등록하지 않았습니다: {target.TargetObject}");
                    continue;
                }

                if (!targets.TryAdd(target.TargetId, target))
                {
                    Debug.LogError($"[EventTargetRegistry] TargetId '{target.TargetId}'가 중복되어 등록하지 않았습니다: {target.TargetObject}");
                }
            }
        }

        public bool TryGetTarget(string targetId, out IEventTarget target)
        {
            target = null;
            return !string.IsNullOrEmpty(targetId) && targets.TryGetValue(targetId, out target);
        }
    }
}
