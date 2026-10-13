using System.Collections.Generic;
using UnityEngine;

namespace InteractionSystem.Data
{
    /// <summary>피격자 한 명의 연속 피격 기록입니다(값 타입, 불변).</summary>
    public readonly struct HostilityState
    {
        public int HitCount { get; }
        public float LastHitTime { get; }

        public HostilityState(int hitCount, float lastHitTime)
        {
            HitCount = hitCount;
            LastHitTime = lastHitTime;
        }
    }

    /// <summary>
    /// 중립 피격자별 연속 피격 기록(21번)을 보관하는 Runtime Data입니다. 판정 규칙은 갖지 않으며(NeutralHostilityLogicSystem이 판단),
    /// 기록 설정/제거/정리 전용 메서드만 제공합니다. 피격자가 파괴되거나 비활성화되면 PruneInvalid로 기록을 정리해 누수를 막습니다.
    /// </summary>
    public class RuntimeDataHostility
    {
        private readonly Dictionary<GameObject, HostilityState> states = new Dictionary<GameObject, HostilityState>();
        private readonly List<GameObject> pruneCache = new List<GameObject>();

        /// <summary>현재 보관 중인 기록 수입니다.</summary>
        public int Count => states.Count;

        public bool TryGetState(GameObject victim, out HostilityState state)
        {
            state = default;
            return victim != null && states.TryGetValue(victim, out state);
        }

        public void SetState(GameObject victim, int hitCount, float lastHitTime)
        {
            if (victim == null) return;
            states[victim] = new HostilityState(hitCount, lastHitTime);
        }

        public bool Remove(GameObject victim)
        {
            return victim != null && states.Remove(victim);
        }

        /// <summary>파괴되었거나 비활성화된 피격자의 기록을 제거하고, 제거한 수를 돌려준다.</summary>
        public int PruneInvalid()
        {
            if (states.Count == 0) return 0;

            pruneCache.Clear();
            foreach (var key in states.Keys)
            {
                if (key == null || !key.activeInHierarchy)
                {
                    pruneCache.Add(key);
                }
            }

            for (int i = 0; i < pruneCache.Count; i++)
            {
                states.Remove(pruneCache[i]);
            }

            int removed = pruneCache.Count;
            pruneCache.Clear();
            return removed;
        }

        public void Clear()
        {
            states.Clear();
        }
    }
}
