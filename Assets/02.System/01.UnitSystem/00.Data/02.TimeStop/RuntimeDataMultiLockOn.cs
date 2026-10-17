using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnitSystem
{
    /// <summary>
    /// 시간 정지 중 누적되는 "마법 지정 큐"를 관리하는 RuntimeData입니다(13번 기획서 §3).
    /// 락온한 대상은 먼저 PendingTarget(마법 미지정)이 되고, 팝업에서 마법을 고르면(CommitPending)
    /// (대상, 마법) 쌍인 LockOnCastEntry로 Entries에 쌓입니다. 같은 대상이 여러 번 쌓일 수 있습니다.
    /// 시간 정지 해제(또는 이벤트 전환점) 시 UnitCasterSystem이 Entries 전체를 한꺼번에 발동합니다.
    /// 마나 비용 같은 파생 계산은 이 클래스가 들고 있지 않습니다(IUnitBatchCastingService가 단일 규칙).
    /// </summary>
    public class RuntimeDataMultiLockOn
    {
        private readonly List<LockOnCastEntry> entries = new();

        /// <summary>지정이 끝난 (대상, 마법) 쌍의 큐. 같은 대상이 여러 번 들어갈 수 있다.</summary>
        public IReadOnlyList<LockOnCastEntry> Entries => entries;
        public int EntryCount => entries.Count;

        /// <summary>락온은 했지만 아직 마법을 고르지 않은 대상(팝업이 열려 있는 동안).</summary>
        public RuntimeDataUnitGroup PendingTarget { get; private set; }

        /// <summary>
        /// 지정 대기 중인지 여부. 대상이 파괴되어 Unity null이 되었더라도 참조가 남아 있으면 "대기 중"으로 보아
        /// CancelPending이 정상 취소 + 이벤트 발행을 하게 한다(15-A §3-C).
        /// </summary>
        public bool HasPending => !ReferenceEquals(PendingTarget, null);

        /// <summary>
        /// 마법 지정 창이 닫혔는지 여부(13번 기획서 §3 - 3단계 전환점 이후). 닫힌 동안은 새 락온(BeginPending)을
        /// 받지 않는다. 이벤트 종료/재시작 시 OpenDesignation으로 다시 연다.
        /// </summary>
        public bool IsDesignationClosed { get; private set; }

        public event Action<bool> OnDesignationClosedChanged;
        public event Action OnPendingChanged;
        public event Action<LockOnCastEntry> OnEntryAdded;
        public event Action<LockOnCastEntry> OnEntryRemoved;
        public event Action OnEntriesCleared;
        public event Action OnBatchCastRequested;

        public void RequestBatchCast() => OnBatchCastRequested?.Invoke();

        /// <summary>지정 창을 닫는다(이후 BeginPending 무시). 지정 대기/큐 정리는 하지 않는다 — 발동(RequestBatchCast)이 처리한다.</summary>
        public void CloseDesignation() => SetDesignationClosed(true);

        /// <summary>지정 창을 다시 연다(이벤트 종료/재시작).</summary>
        public void OpenDesignation() => SetDesignationClosed(false);

        private void SetDesignationClosed(bool closed)
        {
            if (IsDesignationClosed == closed) return;

            IsDesignationClosed = closed;
            Debug.Log($"<color=magenta>[RuntimeDataMultiLockOn] Designation {(closed ? "Closed" : "Opened")}.</color>");
            OnDesignationClosedChanged?.Invoke(closed);
        }

        /// <summary>대상이 이미 지정 큐(Entries) 또는 지정 대기(PendingTarget)에 있는지 여부.</summary>
        public bool Contains(RuntimeDataUnitGroup target)
        {
            if (target == null) return false;
            if (PendingTarget == target) return true;
            return ContainsInEntries(target);
        }

        public bool ContainsInEntries(RuntimeDataUnitGroup target)
        {
            if (target == null) return false;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Target == target) return true;
            }
            return false;
        }

        /// <summary>락온 시 호출. 이미 Entries에 있는 대상이어도 허용한다(한 대상에 여러 마법).</summary>
        public void BeginPending(RuntimeDataUnitGroup target)
        {
            if (target == null) return;
            if (IsDesignationClosed) return;
            if (PendingTarget == target) return;

            PendingTarget = target;

            Debug.Log($"<color=magenta>[RuntimeDataMultiLockOn] Pending Begin:</color> {target.gameObject.name} (Queued: {entries.Count})");
            OnPendingChanged?.Invoke();
        }

        /// <summary>팝업에서 마법을 고르면 호출. PendingTarget + spell을 쌍으로 큐에 추가하고 PendingTarget을 비운다.</summary>
        public bool CommitPending(PureDataUnit spell)
        {
            if (PendingTarget == null || spell == null) return false;

            var entry = new LockOnCastEntry(PendingTarget, spell);
            PendingTarget = null;
            entries.Add(entry);

            Debug.Log($"<color=magenta>[RuntimeDataMultiLockOn] Entry Added:</color> {DescribeTarget(entry.Target)} <- {spell.name} (Queued: {entries.Count})");
            OnEntryAdded?.Invoke(entry);
            OnPendingChanged?.Invoke();
            return true;
        }

        /// <summary>지정 취소(팝업 열림 중 취소 입력 / 대기 대상 소실). Entries에는 아무 영향이 없다.</summary>
        public void CancelPending()
        {
            // Unity의 == null 비교는 파괴된 대상도 null로 보므로, 참조 기준으로 판정해야
            // 대상이 파괴된 지정 대기도 취소 + OnPendingChanged 발행이 누락되지 않는다.
            if (ReferenceEquals(PendingTarget, null)) return;

            var pending = PendingTarget;
            Debug.Log($"<color=magenta>[RuntimeDataMultiLockOn] Pending Cancelled:</color> {DescribeTarget(pending)}");
            PendingTarget = null;
            OnPendingChanged?.Invoke();
        }

        /// <summary>가장 최근에 쌓인 쌍 1개를 취소한다(기존 "우클릭으로 최근 락온 취소"의 대체).</summary>
        public bool RemoveLastEntry()
        {
            if (entries.Count == 0) return false;

            var last = entries[entries.Count - 1];
            entries.RemoveAt(entries.Count - 1);

            Debug.Log($"<color=magenta>[RuntimeDataMultiLockOn] Entry Removed:</color> {DescribeTarget(last.Target)} <- {(last.Spell != null ? last.Spell.name : "null")} (Queued: {entries.Count})");
            OnEntryRemoved?.Invoke(last);
            return true;
        }

        /// <summary>쌍 큐와 지정 대기를 모두 비운다(발동 직전/시간정지 해제/이벤트 종료 시).</summary>
        public void ClearEntries()
        {
            CancelPending();

            if (entries.Count == 0) return;

            entries.Clear();
            Debug.Log("<color=magenta>[RuntimeDataMultiLockOn] All Entries Cleared.</color>");
            OnEntriesCleared?.Invoke();
        }

        /// <summary>
        /// 소실된 대상(파괴됨 또는 계층 비활성/풀 반환됨 — RuntimeDataUnitGroup.IsValidTarget 한 곳의 판정)을 정리한다(15-A §3-C).
        /// 지정 대기 대상이 소실되면 CancelPending(→ OnPendingChanged), 큐의 소실 대상 엔트리는 모두 제거하며 엔트리마다
        /// OnEntryRemoved를 발행한다(→ 팝업 닫힘, 13-C 미리보기/마나 UI 갱신). 풀링된 객체가 재사용돼도 이전 큐 엔트리가 남지 않는다.
        /// 데이터는 스스로 갱신하지 않으므로 주기 호출은 LockOnTargetLossLogicSystem이 맡는다. 정리한 항목이 있으면 true.
        /// </summary>
        public bool RemoveLostTargets()
        {
            return RemoveTargets(null);
        }

        /// <summary>
        /// 특정 대상 하나를 지정 대기/큐에서 즉시 제거한다(15-B). 풀 반환(Release) 경계에서 호출되는 직접 통지 경로다 —
        /// 대상이 아직 활성 계층이어도(반환과 재사용이 같은 프레임에 일어나 IsValidTarget이 이미 true여도) 제거한다.
        /// 지정 대기면 CancelPending(→ OnPendingChanged), 그 대상의 큐 엔트리는 모두 OnEntryRemoved와 함께 제거하고 락온 마커를 끈다.
        /// 정리한 항목이 있으면 true.
        /// </summary>
        public bool RemoveTarget(RuntimeDataUnitGroup target)
        {
            if (ReferenceEquals(target, null)) return false;

            return RemoveTargets(target);
        }

        /// <summary>specificTarget이 null이면 소실(IsValidTarget == false)된 모든 대상을, 아니면 그 대상 하나만 정리한다.</summary>
        private bool RemoveTargets(RuntimeDataUnitGroup specificTarget)
        {
            bool changed = false;
            bool specific = !ReferenceEquals(specificTarget, null);

            // 소실 대상의 락온 마커(TargetHighlight)를 끄기 위해 모아 둔다. 풀링된 객체가 재활성화될 때 마커가 켜진 채 나타나지 않게 한다.
            List<RuntimeDataUnitGroup> lostTargets = null;

            if (HasPending && IsRemovable(PendingTarget, specificTarget, specific))
            {
                (lostTargets ??= new List<RuntimeDataUnitGroup>()).Add(PendingTarget);
                CancelPending();
                changed = true;
            }

            List<LockOnCastEntry> removed = null;
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                if (!IsRemovable(entries[i].Target, specificTarget, specific)) continue;

                removed ??= new List<LockOnCastEntry>();
                removed.Add(entries[i]);
                (lostTargets ??= new List<RuntimeDataUnitGroup>()).Add(entries[i].Target);
                entries.RemoveAt(i);
            }

            ReleaseLostTargetMarkers(lostTargets);

            if (removed != null)
            {
                changed = true;
                Debug.Log($"<color=magenta>[RuntimeDataMultiLockOn] Lost Targets Removed:</color> {removed.Count} entries (Queued: {entries.Count})");

                // 제거는 뒤에서부터 했으므로, 큐 순서대로 통지하도록 뒤집어 발행한다.
                for (int i = removed.Count - 1; i >= 0; i--)
                {
                    OnEntryRemoved?.Invoke(removed[i]);
                }
            }

            return changed;
        }

        private static bool IsRemovable(RuntimeDataUnitGroup candidate, RuntimeDataUnitGroup specificTarget, bool specific)
        {
            return specific
                ? ReferenceEquals(candidate, specificTarget)
                : !RuntimeDataUnitGroup.IsValidTarget(candidate);
        }

        /// <summary>
        /// 소실로 제거된 대상의 락온 마커를 끈다. 파괴된 대상(Unity null)은 건너뛰고, 같은 대상이 큐/대기에 다른 쌍으로
        /// 아직 남아 있으면 마커를 유지한다(한 대상에 여러 쌍 가능). 비활성 계층의 대상도 Highlight는 안전하게 동작한다.
        /// </summary>
        private void ReleaseLostTargetMarkers(List<RuntimeDataUnitGroup> lostTargets)
        {
            if (lostTargets == null) return;

            for (int i = 0; i < lostTargets.Count; i++)
            {
                var target = lostTargets[i];
                if (target == null) continue;
                if (Contains(target)) continue;

                target.Highlight(false, false);
            }
        }

        private static string DescribeTarget(RuntimeDataUnitGroup target)
        {
            return target != null ? target.gameObject.name : "null";
        }
    }
}
