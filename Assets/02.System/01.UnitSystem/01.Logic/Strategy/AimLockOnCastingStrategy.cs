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
        private readonly LayerMask targetLayer;

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
            CharacterStatSystem playerStatSystem,
            LayerMask targetLayer)
        {
            this.multiLockOnData = multiLockOnData ?? throw new ArgumentNullException(nameof(multiLockOnData));
            this.timeSlowData = timeSlowData;
            this.changeService = changeService;
            this.quickSlotUI = quickSlotUI;
            this.visualizer = visualizer;
            this.playerStatSystem = playerStatSystem;
            this.targetLayer = targetLayer;

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
            // 마우스 좌/우클릭 즉시 시전 및 마나 소모 완전 차단 (숄더뷰/1인칭에서는 클릭 시전 로직 없음)

            if (lockOnCooldownTimer > 0f)
            {
                lockOnCooldownTimer -= Time.unscaledDeltaTime;
            }

            // 시간 정지 중일 때만 시야 내 대상 다중 락온 누적 수행
            if (timeSlowData != null && timeSlowData.IsSlowActive)
            {
                ProcessAimTargeting();
            }
        }

        private void ProcessAimTargeting()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Ray aimRay = new Ray(cam.transform.position, cam.transform.forward);
            float castRadius = 0.6f;
            float maxDistance = 60f;

            RaycastHit[] hits = Physics.SphereCastAll(aimRay, castRadius, maxDistance, targetLayer);
            if (hits == null || hits.Length == 0)
            {
                lastHoveredTarget = null;
                return;
            }

            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                var unitGroup = hit.collider.GetComponentInParent<RuntimeDataUnitGroup>();
                if (unitGroup == null) continue;

                // 1. 사물 타겟팅 불가 대상 제외 (IsTargetable == false)
                if (!unitGroup.IsTargetable) continue;

                // 2. 캐릭터인 경우 적대(Enemy) 진영만 락온 허용 (아군/비적대 제외)
                var charStat = hit.collider.GetComponentInParent<CharacterStatComponent>();
                if (charStat != null && !charStat.IsEnemy())
                {
                    continue;
                }

                // 3. 쿨다운 및 락온 대상 누적 처리
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
