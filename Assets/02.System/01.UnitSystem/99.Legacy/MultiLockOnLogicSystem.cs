#pragma warning disable CS0618 // 숄더뷰 고정(22번 기획서)으로 CameraMode 비숄더 멤버가 [Obsolete] — 레거시 격리 파일이므로 경고 억제
using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using CameraMovement;
using CharacterSystem;

namespace UnitSystem
{
    /// <summary>
    /// 1인칭 및 3인칭 숄더뷰 시점에서 시간 정지(T키) 중 화면 시야 내 대상을 다중 락온하고,
    /// 시간 정지 해제 시 일괄 마법 변환 및 마나를 소모하는 DLV Logic System입니다.
    /// </summary>
    public class MultiLockOnLogicSystem : ITickable, IDisposable
    {
        private readonly RuntimeDataMultiLockOn multiLockOnData;
        private readonly RuntimeDataTimeSlow timeSlowData;
        private readonly ICameraFollowService cameraFollowService;
        private readonly UnitChangeService changeService;
        private readonly IUnitBatchCastingService batchCastingService;
        private readonly UnitQuickSlotUIComponent quickSlotUI;
        private readonly IMultiLockOnVisualizer visualizer;
        private readonly CharacterStatSystem playerStatSystem;

        private float lockOnCooldownTimer = 0f;
        private const float LOCK_ON_INTERVAL = 0.2f;
        private RuntimeDataUnitGroup lastHoveredTarget = null;

        [Inject]
        public MultiLockOnLogicSystem(
            RuntimeDataMultiLockOn multiLockOnData,
            RuntimeDataTimeSlow timeSlowData,
            ICameraFollowService cameraFollowService,
            UnitChangeService changeService,
            IMultiLockOnVisualizer visualizer,
            UnitQuickSlotUIComponent quickSlotUI = null,
            CharacterStatSystem playerStatSystem = null,
            IUnitBatchCastingService batchCastingService = null)
        {
            this.multiLockOnData = multiLockOnData ?? throw new ArgumentNullException(nameof(multiLockOnData));
            this.timeSlowData = timeSlowData ?? throw new ArgumentNullException(nameof(timeSlowData));
            this.cameraFollowService = cameraFollowService;
            this.changeService = changeService;
            this.visualizer = visualizer;
            this.quickSlotUI = quickSlotUI;
            this.playerStatSystem = playerStatSystem;
            this.batchCastingService = batchCastingService ?? (changeService != null ? new UnitBatchCastingService(new UnitCastRuleService(), changeService) : null);

            this.timeSlowData.OnSlowStateChanged += HandleSlowStateChanged;
        }

        public void Dispose()
        {
            if (timeSlowData != null)
            {
                timeSlowData.OnSlowStateChanged -= HandleSlowStateChanged;
            }

            ClearAllLockOns();
        }

        private bool IsAimLockOnViewMode()
        {
            // 숄더뷰 고정(22번 기획서) — ICameraFollowService의 카메라 모드 조회가 제거되어 항상 숄더뷰로 간주한다.
            return cameraFollowService != null;
        }

        public void Tick()
        {
            if (!IsAimLockOnViewMode()) return;

            if (lockOnCooldownTimer > 0f)
            {
                lockOnCooldownTimer -= Time.unscaledDeltaTime;
            }

            // 시간 정지 중일 때만 다중 락온 누적 수행
            if (timeSlowData.IsSlowActive)
            {
                ProcessAimTargeting();
            }
        }

        private void ProcessAimTargeting()
        {
            PureDataUnit selectedUnitData = quickSlotUI != null ? quickSlotUI.CurrentSelectedUnit : null;
            if (selectedUnitData == null)
            {
                lastHoveredTarget = null;
                return;
            }

            if (visualizer == null)
            {
                lastHoveredTarget = null;
                return;
            }

            float castRadius = 0.6f;
            float maxDistance = 60f;

            var detectedColliders = visualizer.DetectAimTargets(castRadius, maxDistance);
            if (detectedColliders == null || detectedColliders.Count == 0)
            {
                lastHoveredTarget = null;
                return;
            }

            for (int i = 0; i < detectedColliders.Count; i++)
            {
                var col = detectedColliders[i];
                if (col == null) continue;

                var unitGroup = col.GetComponentInParent<RuntimeDataUnitGroup>();

                // 1. RuntimeDataUnitGroup이 없는 단순 적/아군 캐릭터는 마법 락온에서 완전히 배제
                if (unitGroup == null)
                {
                    continue;
                }

                // 2. 타겟팅 불가 사물(IsTargetable == false) 제외
                if (!unitGroup.IsTargetable)
                {
                    continue;
                }

                // 3. 현재 선택된 마법 단위를 지원하지 않는 사물/적 배제
                if (!unitGroup.HasUnit(selectedUnitData.UnitType))
                {
                    continue;
                }

                // 4. 쿨다운 및 중복 락온 처리
                if (unitGroup != lastHoveredTarget || lockOnCooldownTimer <= 0f)
                {
                    multiLockOnData.BeginPending(unitGroup);
                    multiLockOnData.CommitPending(selectedUnitData);
                    unitGroup.Highlight(true, true);
                    lastHoveredTarget = unitGroup;
                    lockOnCooldownTimer = LOCK_ON_INTERVAL;

                    visualizer.UpdateLockOnCount(multiLockOnData.EntryCount);
                }
                return;
            }

            lastHoveredTarget = null;
        }

        private void HandleSlowStateChanged(bool isSlowActive)
        {
            if (!isSlowActive)
            {
                // 시간 정지 해제 순간 일괄 마법 발동
                if (IsAimLockOnViewMode() && multiLockOnData.EntryCount > 0)
                {
                    ExecuteBatchCast();
                }
                else
                {
                    ClearAllLockOns();
                }
            }
        }

        private void ExecuteBatchCast()
        {
            int targetCount = multiLockOnData.EntryCount;
            if (targetCount == 0) return;

            PureDataUnit selectedUnitData = quickSlotUI != null ? quickSlotUI.CurrentSelectedUnit : null;
            if (selectedUnitData == null)
            {
                Debug.LogWarning("[MultiLockOnLogicSystem] Batch Cast Aborted: No Unit selected in Quick Slots.");
                ClearAllLockOns();
                return;
            }

            var entries = multiLockOnData.Entries;

            bool success = batchCastingService.ExecuteBatchCast(entries, playerStatSystem, null);
            if (success)
            {
                visualizer?.PlayBatchCastEffect();
            }

            ClearAllLockOns();
        }

        private void ClearAllLockOns()
        {
            foreach (var entry in multiLockOnData.Entries)
            {
                if (entry.Target != null)
                {
                    entry.Target.Highlight(false, false);
                }
            }

            multiLockOnData.ClearEntries();
            visualizer?.UpdateLockOnCount(0);
            lastHoveredTarget = null;
        }
    }
}
