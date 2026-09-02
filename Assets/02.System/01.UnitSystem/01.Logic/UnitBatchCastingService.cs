using System;
using System.Collections.Generic;
using UnityEngine;
using CharacterSystem;

namespace UnitSystem
{
    /// <summary>
    /// 단위 마법 일괄 시전 시 총 필요 마나 계산, 마나 검증 및 유닛 변환 공통 비즈니스 로직을 제공하는 서비스 인터페이스입니다.
    /// </summary>
    public interface IUnitBatchCastingService
    {
        float CalculateNewValue(RuntimeDataUnit targetUnit, PureDataUnit spellUnit);
        int CalculateUnitCost(RuntimeDataUnit targetUnit, PureDataUnit spellUnit);
        int CalculateTotalCost(IReadOnlyList<RuntimeDataUnitGroup> targets, PureDataUnit selectedUnit);
        bool ExecuteBatchCast(IReadOnlyList<RuntimeDataUnitGroup> targets, PureDataUnit selectedUnit, CharacterStatSystem casterStat, GameObject casterObject = null);
        bool ExecuteBatchCast(IReadOnlyList<RuntimeDataUnitGroup> targets, PureDataUnit selectedUnit, RuntimeStatData casterStatData, GameObject casterObject = null);
    }

    /// <summary>
    /// 단위 마법 일괄 시전 공통 비즈니스 로직을 구현한 서비스 클래스입니다.
    /// </summary>
    public class UnitBatchCastingService : IUnitBatchCastingService
    {
        private readonly UnitChangeService changeService;

        public UnitBatchCastingService(UnitChangeService changeService)
        {
            this.changeService = changeService;
        }

        public float CalculateNewValue(RuntimeDataUnit targetUnit, PureDataUnit spellUnit)
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

        public int CalculateUnitCost(RuntimeDataUnit targetUnit, PureDataUnit spellUnit)
        {
            if (targetUnit == null || spellUnit == null) return 0;

            float newValue = CalculateNewValue(targetUnit, spellUnit);
            float diff = Mathf.Abs(targetUnit.CurrentValue - newValue);
            int baseCost = spellUnit.BaseCost > 0 ? spellUnit.BaseCost : 10;
            return Mathf.Max(Mathf.RoundToInt(baseCost * diff), baseCost);
        }

        public int CalculateTotalCost(IReadOnlyList<RuntimeDataUnitGroup> targets, PureDataUnit selectedUnit)
        {
            if (targets == null || targets.Count == 0 || selectedUnit == null) return 0;

            int totalCost = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                var targetGroup = targets[i];
                if (targetGroup == null) continue;

                var matchingUnitData = targetGroup.GetMatchingUnitData(selectedUnit.UnitType);
                if (matchingUnitData != null)
                {
                    totalCost += CalculateUnitCost(matchingUnitData, selectedUnit);
                }
            }

            return totalCost;
        }

        public bool ExecuteBatchCast(IReadOnlyList<RuntimeDataUnitGroup> targets, PureDataUnit selectedUnit, CharacterStatSystem casterStat, GameObject casterObject = null)
        {
            return ExecuteBatchCast(targets, selectedUnit, casterStat?.RuntimeData, casterObject);
        }

        public bool ExecuteBatchCast(IReadOnlyList<RuntimeDataUnitGroup> targets, PureDataUnit selectedUnit, RuntimeStatData casterStatData, GameObject casterObject = null)
        {
            if (targets == null || targets.Count == 0) return false;

            if (selectedUnit == null)
            {
                Debug.LogWarning("[UnitBatchCastingService] Batch Cast Aborted: No Unit selected.");
                return false;
            }

            int totalCost = CalculateTotalCost(targets, selectedUnit);

            // 마나 검증 및 부족 시 안전 예외 처리
            if (casterStatData != null && casterStatData.MP.CurrentValue < totalCost)
            {
                Debug.LogWarning($"[UnitBatchCastingService] Insufficient Mana. (Required: {totalCost}, Current MP: {casterStatData.MP.CurrentValue})");
                casterStatData.TryConsumeMP(totalCost);
                for (int i = 0; i < targets.Count; i++)
                {
                    var target = targets[i];
                    if (target != null) target.Highlight(false, false);
                }
                return false;
            }

            Debug.Log($"<color=green>[UnitBatchCastingService] Executing Batch Cast on {targets.Count} targets! Total Mana Cost: {totalCost}</color>");

            // 타겟 순차 변환 및 마커 소등
            for (int i = 0; i < targets.Count; i++)
            {
                var targetGroup = targets[i];
                if (targetGroup == null) continue;

                targetGroup.Highlight(false, false);

                var matchingUnitData = targetGroup.GetMatchingUnitData(selectedUnit.UnitType);
                if (matchingUnitData != null)
                {
                    float newValue = CalculateNewValue(matchingUnitData, selectedUnit);
                    changeService?.ChangeUnit(targetGroup.gameObject, matchingUnitData, selectedUnit, newValue, casterStatData, null, casterObject);
                }
            }

            return true;
        }
    }
}
