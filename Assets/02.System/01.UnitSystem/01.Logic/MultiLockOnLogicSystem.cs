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
            IObjectResolver resolver)
        {
            this.multiLockOnData = multiLockOnData ?? throw new ArgumentNullException(nameof(multiLockOnData));
            this.timeSlowData = timeSlowData ?? throw new ArgumentNullException(nameof(timeSlowData));
            this.cameraFollowService = cameraFollowService;
            this.changeService = changeService;

            if (resolver != null)
            {
                resolver.TryResolve(out this.quickSlotUI);
                resolver.TryResolve(out this.visualizer);
                resolver.TryResolve(out this.playerStatSystem);
            }

            this.timeSlowData.OnSlowStateChanged += HandleSlowStateChanged;
            if (this.cameraFollowService != null)
            {
                this.cameraFollowService.OnCameraModeChanged += HandleCameraModeChanged;
            }
        }

        public void Dispose()
        {
            if (timeSlowData != null)
            {
                timeSlowData.OnSlowStateChanged -= HandleSlowStateChanged;
            }

            if (cameraFollowService != null)
            {
                cameraFollowService.OnCameraModeChanged -= HandleCameraModeChanged;
            }

            ClearAllLockOns();
        }

        private bool IsAimLockOnViewMode()
        {
            if (cameraFollowService == null) return false;
            return cameraFollowService.CurrentMode == CameraMode.FirstPerson ||
                   cameraFollowService.CurrentMode == CameraMode.ThirdPersonShoulder;
        }

        private void HandleCameraModeChanged(CameraMode mode)
        {
            bool isAimMode = IsAimLockOnViewMode();
            visualizer?.SetCrosshairVisible(isAimMode);

            if (!isAimMode && multiLockOnData.TargetCount > 0)
            {
                ClearAllLockOns();
            }
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

            Camera cam = Camera.main;
            if (cam == null) return;

            Ray aimRay = new Ray(cam.transform.position, cam.transform.forward);
            float castRadius = 0.6f;
            float maxDistance = 60f;

            RaycastHit[] hits = Physics.SphereCastAll(aimRay, castRadius, maxDistance);
            if (hits == null || hits.Length == 0)
            {
                lastHoveredTarget = null;
                return;
            }

            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                // 플레이어 본인 제외
                if (hit.collider.transform.root == cam.transform.root) continue;

                var unitGroup = hit.collider.GetComponentInParent<RuntimeDataUnitGroup>();

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
                    multiLockOnData.AddTarget(unitGroup);
                    unitGroup.Highlight(true, true);
                    lastHoveredTarget = unitGroup;
                    lockOnCooldownTimer = LOCK_ON_INTERVAL;

                    visualizer?.UpdateLockOnCount(multiLockOnData.TargetCount);
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
                if (IsAimLockOnViewMode() && multiLockOnData.TargetCount > 0)
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
            int targetCount = multiLockOnData.TargetCount;
            if (targetCount == 0) return;

            PureDataUnit selectedUnitData = quickSlotUI != null ? quickSlotUI.CurrentSelectedUnit : null;
            if (selectedUnitData == null)
            {
                Debug.LogWarning("[MultiLockOnLogicSystem] Batch Cast Aborted: No Unit selected in Quick Slots.");
                ClearAllLockOns();
                return;
            }

            var targets = multiLockOnData.LockedTargets;
            RuntimeStatData casterStat = playerStatSystem?.RuntimeData;

            // 1. 총 필요 마나 사전 계산
            int totalCost = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                var targetGroup = targets[i];
                if (targetGroup == null) continue;

                var matchingUnitData = targetGroup.GetMatchingUnitData(selectedUnitData.UnitType);
                if (matchingUnitData != null)
                {
                    float newValue = CalculateNewValueForUnit(matchingUnitData, selectedUnitData);
                    float diff = Mathf.Abs(matchingUnitData.CurrentValue - newValue);
                    totalCost += Mathf.RoundToInt(selectedUnitData.BaseCost * diff);
                }
            }

            // 2. 마나 검증
            if (casterStat != null && casterStat.MP.CurrentValue < totalCost)
            {
                Debug.LogWarning($"[MultiLockOnLogicSystem] Batch Cast Failed! Insufficient Mana. (Required: {totalCost}, Current MP: {casterStat.MP.CurrentValue})");
                casterStat.TryConsumeMP(totalCost); // Trigger OnInsufficientMana event & log
                ClearAllLockOns();
                return;
            }

            Debug.Log($"<color=green>[MultiLockOnLogicSystem] Executing Batch Cast on {targetCount} targets! Total Mana Cost: {totalCost}</color>");

            // 3. 일괄 마법 변환 실행
            for (int i = 0; i < targets.Count; i++)
            {
                var targetGroup = targets[i];
                if (targetGroup == null) continue;

                var matchingUnitData = targetGroup.GetMatchingUnitData(selectedUnitData.UnitType);
                if (matchingUnitData != null)
                {
                    float newValue = CalculateNewValueForUnit(matchingUnitData, selectedUnitData);
                    changeService.ChangeUnit(targetGroup.gameObject, matchingUnitData, selectedUnitData, newValue, casterStat);
                }
            }

            visualizer?.PlayBatchCastEffect();
            ClearAllLockOns();
        }

        private float CalculateNewValueForUnit(RuntimeDataUnit targetUnit, PureDataUnit spellUnit)
        {
            if (targetUnit == null || spellUnit == null) return 0f;

            float originalVal = targetUnit.OriginalValue > 0f ? targetUnit.OriginalValue : 1.0f;

            switch (spellUnit.UnitType)
            {
                case UnitType.Mass:
                    return originalVal * spellUnit.MassScaleMultiplier;
                case UnitType.Volume:
                    return originalVal;
                case UnitType.Vector:
                    return -targetUnit.CurrentValue;
                default:
                    return targetUnit.CurrentValue;
            }
        }

        private void ClearAllLockOns()
        {
            foreach (var target in multiLockOnData.LockedTargets)
            {
                if (target != null)
                {
                    target.Highlight(false, false);
                }
            }

            multiLockOnData.ClearTargets();
            visualizer?.UpdateLockOnCount(0);
            lastHoveredTarget = null;
        }
    }
}
