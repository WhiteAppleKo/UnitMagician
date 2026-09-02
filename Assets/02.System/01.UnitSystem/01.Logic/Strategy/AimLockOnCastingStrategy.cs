using System;
using UnityEngine;
using CharacterSystem;

namespace UnitSystem
{
    /// <summary>
    /// 1인칭 및 3인칭 숄더뷰(FirstPerson, ThirdPersonShoulder) 시점 전용 마법 조작 전략입니다.
    /// 화면 중심에 크로스헤어를 표출하고, 마우스 클릭 즉시 시전을 완전 차단하며,
    /// 시간 정지(T키) 중 화면 중심 시야 내 유효 대상을 다중 락온하여 시간 정지 해제 시 일괄 마법 변환을 실행합니다.
    /// </summary>
    public class AimLockOnCastingStrategy : IUnitCastingStrategy
    {
        private readonly RuntimeDataMultiLockOn multiLockOnData;
        private readonly RuntimeDataTimeSlow timeSlowData;
        private readonly UnitChangeService changeService;
        private readonly UnitQuickSlotUIComponent quickSlotUI;
        private readonly IMultiLockOnVisualizer visualizer;
        private readonly CharacterStatSystem playerStatSystem;

        private float lockOnCooldownTimer = 0f;
        private const float LOCK_ON_INTERVAL = 0.2f;
        private RuntimeDataUnitGroup lastHoveredTarget = null;
        private bool isDisposed = false;

        public AimLockOnCastingStrategy(
            RuntimeDataMultiLockOn multiLockOnData,
            RuntimeDataTimeSlow timeSlowData,
            UnitChangeService changeService,
            UnitQuickSlotUIComponent quickSlotUI,
            IMultiLockOnVisualizer visualizer,
            CharacterStatSystem playerStatSystem)
        {
            this.multiLockOnData = multiLockOnData ?? throw new ArgumentNullException(nameof(multiLockOnData));
            this.timeSlowData = timeSlowData;
            this.changeService = changeService;
            this.quickSlotUI = quickSlotUI;
            this.visualizer = visualizer;
            this.playerStatSystem = playerStatSystem;

            if (this.timeSlowData != null)
            {
                this.timeSlowData.OnSlowStateChanged += HandleSlowStateChanged;
            }
        }

        public void Enter()
        {
            visualizer?.SetCrosshairVisible(true);
            ClearAllLockOns();
        }

        public void Exit()
        {
            visualizer?.SetCrosshairVisible(false);
            ClearAllLockOns();
        }

        public void Dispose()
        {
            if (isDisposed) return;
            isDisposed = true;

            if (timeSlowData != null)
            {
                timeSlowData.OnSlowStateChanged -= HandleSlowStateChanged;
            }

            visualizer?.SetCrosshairVisible(false);
            ClearAllLockOns();
        }

        public void Update()
        {
            // 전략 패턴: Update에서는 조건 분기 없이 순수 마법 조준 및 락온만 실행
            if (lockOnCooldownTimer > 0f)
            {
                lockOnCooldownTimer -= Time.unscaledDeltaTime;
            }

            ProcessAimTargeting();
        }

        private void ProcessAimTargeting()
        {
            PureDataUnit selectedUnitData = quickSlotUI != null ? quickSlotUI.CurrentSelectedUnit : null;
            if (selectedUnitData == null)
            {
                var catalogService = UnityEngine.Object.FindFirstObjectByType<UnitQuickSlotUIComponent>();
                if (catalogService != null)
                {
                    selectedUnitData = catalogService.CurrentSelectedUnit;
                }
            }

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

                // 위 조건들을 모두 만족하는 유효 마법 대상만 락온 목록에 누적 및 하이라이트
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
                // 시간 정지 해제 순간 일괄 마법 변환 실행
                if (multiLockOnData.TargetCount > 0)
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
            if (targetCount == 0)
            {
                ClearAllLockOns();
                return;
            }

            PureDataUnit selectedUnitData = quickSlotUI != null ? quickSlotUI.CurrentSelectedUnit : null;
            if (selectedUnitData == null)
            {
                Debug.LogWarning("[AimLockOnCastingStrategy] Batch Cast Aborted: No Unit selected in Quick Slots.");
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

            // 2. 마나 검증 및 부족 시 안전 예외 처리
            if (casterStat != null && casterStat.MP.CurrentValue < totalCost)
            {
                Debug.LogWarning($"[AimLockOnCastingStrategy] Batch Cast Failed! Insufficient Mana. (Required: {totalCost}, Current MP: {casterStat.MP.CurrentValue})");
                casterStat.TryConsumeMP(totalCost); // Trigger OnInsufficientMana event & log
                ClearAllLockOns();
                return;
            }

            Debug.Log($"<color=green>[AimLockOnCastingStrategy] Executing Batch Cast on {targetCount} targets! Total Mana Cost: {totalCost}</color>");

            // 3. 일괄 마법 변환 실행
            for (int i = 0; i < targets.Count; i++)
            {
                var targetGroup = targets[i];
                if (targetGroup == null) continue;

                var matchingUnitData = targetGroup.GetMatchingUnitData(selectedUnitData.UnitType);
                if (matchingUnitData != null)
                {
                    float newValue = CalculateNewValueForUnit(matchingUnitData, selectedUnitData);
                    if (changeService != null)
                    {
                        changeService.ChangeUnit(targetGroup.gameObject, matchingUnitData, selectedUnitData, newValue, casterStat);
                    }
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
