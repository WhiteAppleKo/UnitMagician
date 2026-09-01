using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnitSystem
{
    /// <summary>
    /// 1인칭 및 3인칭 숄더뷰에서 시간 정지 중 누적되는 다중 락온 대상 목록 및 실시간 상태를 관리하는 RuntimeData입니다.
    /// </summary>
    public class RuntimeDataMultiLockOn
    {
        private readonly List<RuntimeDataUnitGroup> lockedTargets = new();

        public IReadOnlyList<RuntimeDataUnitGroup> LockedTargets => lockedTargets;
        public int TargetCount => lockedTargets.Count;

        public event Action<RuntimeDataUnitGroup> OnTargetAdded;
        public event Action<RuntimeDataUnitGroup> OnTargetRemoved;
        public event Action OnTargetsCleared;

        public void AddTarget(RuntimeDataUnitGroup target)
        {
            if (target == null) return;

            lockedTargets.Add(target);
            Debug.Log($"<color=magenta>[RuntimeDataMultiLockOn] Target Added:</color> {target.gameObject.name} (Total Locked: {lockedTargets.Count})");
            OnTargetAdded?.Invoke(target);
        }

        public bool RemoveTarget(RuntimeDataUnitGroup target)
        {
            if (target == null) return false;

            bool removed = lockedTargets.Remove(target);
            if (removed)
            {
                Debug.Log($"<color=magenta>[RuntimeDataMultiLockOn] Target Removed:</color> {target.gameObject.name} (Total Locked: {lockedTargets.Count})");
                OnTargetRemoved?.Invoke(target);
            }
            return removed;
        }

        public void ClearTargets()
        {
            if (lockedTargets.Count == 0) return;

            lockedTargets.Clear();
            Debug.Log("<color=magenta>[RuntimeDataMultiLockOn] All Targets Cleared.</color>");
            OnTargetsCleared?.Invoke();
        }
    }
}
