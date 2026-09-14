using System;
using System.Collections.Generic;
using UnityEngine;
using UnitSystem;

namespace TacticalCommandSystem
{
    /// <summary>
    /// 예약된 단일 전술 명령 항목 데이터입니다.
    /// 불변성을 보장하기 위해 모든 속성은 읽기 전용으로 제공됩니다.
    /// </summary>
    public class TacticalCommandEntry
    {
        public int OrderIndex { get; }
        public GameObject TargetObject { get; }
        public PureDataUnit MagicData { get; }
        public int ManaCost { get; }
        public Vector3 TargetPosition => TargetObject != null ? TargetObject.transform.position : Vector3.zero;

        public TacticalCommandEntry(int orderIndex, GameObject targetObject, PureDataUnit magicData, int manaCost)
        {
            OrderIndex = orderIndex;
            TargetObject = targetObject;
            MagicData = magicData;
            ManaCost = manaCost;
        }
    }

    /// <summary>
    /// 전술 명령 큐의 런타임 상태를 관리하는 순수 C# 모델입니다.
    /// DLV 아키텍처 규칙에 따라 모든 속성은 private set으로 캡슐화되며,
    /// 전용 메서드를 통해서만 수정되고, 상태 변경 시 이벤트를 발행합니다.
    /// </summary>
    public class RuntimeDataTacticalQueue
    {
        private readonly PureDataTacticalCommand pureData;
        private readonly List<TacticalCommandEntry> commandQueue = new();

        public PureDataTacticalCommand PureData => pureData;
        public IReadOnlyList<TacticalCommandEntry> CommandQueue => commandQueue;
        public bool IsTacticalModeActive { get; private set; }
        public bool IsExecuting { get; private set; }
        public int TotalEstimatedManaCost { get; private set; }
        public int QueueCount => commandQueue.Count;
        public int MaxQueueCount => pureData != null ? pureData.MaxQueueCount : 5;

        // C# Events for DLV Observer Pattern
        public event Action<bool> OnTacticalModeStateChanged;
        public event Action<bool> OnExecutingStateChanged;
        public event Action<TacticalCommandEntry> OnCommandEnqueued;
        public event Action<TacticalCommandEntry> OnCommandDequeued;
        public event Action<TacticalCommandEntry> OnCommandRemoved;
        public event Action<IReadOnlyList<TacticalCommandEntry>> OnQueueChanged;
        public event Action<int> OnEstimatedManaCostChanged;
        public event Action OnQueueCleared;

        public RuntimeDataTacticalQueue(PureDataTacticalCommand pureData)
        {
            this.pureData = pureData;
        }

        public void SetTacticalMode(bool active)
        {
            if (IsTacticalModeActive == active) return;

            IsTacticalModeActive = active;
            OnTacticalModeStateChanged?.Invoke(IsTacticalModeActive);
        }

        public void SetExecuting(bool executing)
        {
            if (IsExecuting == executing) return;

            IsExecuting = executing;
            OnExecutingStateChanged?.Invoke(IsExecuting);
        }

        public bool ContainsTarget(GameObject targetObject)
        {
            if (targetObject == null) return false;
            for (int i = 0; i < commandQueue.Count; i++)
            {
                if (commandQueue[i].TargetObject == targetObject)
                {
                    return true;
                }
            }
            return false;
        }

        public bool CanEnqueue(PureDataUnit magicData, int currentAvailableMana)
        {
            if (magicData == null) return false;
            if (commandQueue.Count >= MaxQueueCount) return false;

            int requiredMana = magicData.BaseCost;
            return (TotalEstimatedManaCost + requiredMana) <= currentAvailableMana;
        }

        public bool TryEnqueue(GameObject targetObject, PureDataUnit magicData, int currentAvailableMana, out TacticalCommandEntry newEntry)
        {
            newEntry = null;

            if (targetObject == null || magicData == null) return false;
            
            // 동일 대상 중복 등록 완전 차단
            if (ContainsTarget(targetObject)) return false;

            if (!CanEnqueue(magicData, currentAvailableMana)) return false;

            int nextOrder = commandQueue.Count + 1;
            int cost = magicData.BaseCost;

            newEntry = new TacticalCommandEntry(nextOrder, targetObject, magicData, cost);
            commandQueue.Add(newEntry);
            TotalEstimatedManaCost += cost;

            OnCommandEnqueued?.Invoke(newEntry);
            OnEstimatedManaCostChanged?.Invoke(TotalEstimatedManaCost);
            OnQueueChanged?.Invoke(commandQueue);

            return true;
        }

        public TacticalCommandEntry RemoveLast()
        {
            if (commandQueue.Count == 0) return null;

            int lastIdx = commandQueue.Count - 1;
            var removed = commandQueue[lastIdx];
            commandQueue.RemoveAt(lastIdx);

            TotalEstimatedManaCost -= removed.ManaCost;
            if (TotalEstimatedManaCost < 0) TotalEstimatedManaCost = 0;

            OnCommandRemoved?.Invoke(removed);
            OnEstimatedManaCostChanged?.Invoke(TotalEstimatedManaCost);
            OnQueueChanged?.Invoke(commandQueue);

            return removed;
        }

        public TacticalCommandEntry Dequeue()
        {
            if (commandQueue.Count == 0) return null;

            var entry = commandQueue[0];
            commandQueue.RemoveAt(0);

            TotalEstimatedManaCost -= entry.ManaCost;
            if (TotalEstimatedManaCost < 0) TotalEstimatedManaCost = 0;

            OnCommandDequeued?.Invoke(entry);
            OnEstimatedManaCostChanged?.Invoke(TotalEstimatedManaCost);
            OnQueueChanged?.Invoke(commandQueue);

            return entry;
        }

        public void Clear()
        {
            if (commandQueue.Count == 0 && TotalEstimatedManaCost == 0) return;

            commandQueue.Clear();
            TotalEstimatedManaCost = 0;

            OnQueueCleared?.Invoke();
            OnEstimatedManaCostChanged?.Invoke(0);
            OnQueueChanged?.Invoke(commandQueue);
        }
    }
}
