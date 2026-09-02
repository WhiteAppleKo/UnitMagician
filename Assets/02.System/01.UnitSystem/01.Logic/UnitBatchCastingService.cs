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
        bool ExecuteBatchCast(IReadOnlyList<RuntimeDataUnitGroup> targets, PureDataUnit selectedUnit, ICharacterStatService statService, GameObject casterObject = null);
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
            if (spellUnit == null) return 0;
            return spellUnit.BaseCost;
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

        public bool ExecuteBatchCast(IReadOnlyList<RuntimeDataUnitGroup> targets, PureDataUnit selectedUnit, ICharacterStatService statService, GameObject casterObject = null)
        {
            return ExecuteBatchCast(targets, selectedUnit, statService?.RuntimeData, casterObject);
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
            if (casterStatData != null)
            {
                if (casterStatData.MP.CurrentValue < totalCost)
                {
                    Debug.LogWarning($"[UnitBatchCastingService] Insufficient Mana. (Required: {totalCost}, Current MP: {casterStatData.MP.CurrentValue})");
                    for (int i = 0; i < targets.Count; i++)
                    {
                        var target = targets[i];
                        if (target != null) target.Highlight(false, false);
                    }
                    return false;
                }

                // 총 필요 마나 1회 일괄 차감
                casterStatData.TryConsumeMP(totalCost);
                Debug.Log($"<color=green>[UnitBatchCastingService] Consumed {totalCost} MP! Remaining MP: {casterStatData.MP.CurrentValue}</color>");
            }
            else
            {
                Debug.LogWarning("[UnitBatchCastingService] Caster Stat Data is null! Mana not consumed.");
            }

            Debug.Log($"<color=green>[UnitBatchCastingService] Executing Batch Cast on {targets.Count} targets! Total Mana Cost: {totalCost}</color>");

            // 타겟 순차 변환 및 마커 소등 (이미 총 마나를 차감했으므로 개별 ChangeUnit에는 null 전달하여 중복 차감 방지)
            for (int i = 0; i < targets.Count; i++)
            {
                var targetGroup = targets[i];
                if (targetGroup == null) continue;

                targetGroup.Highlight(false, false);

                var matchingUnitData = targetGroup.GetMatchingUnitData(selectedUnit.UnitType);
                if (matchingUnitData != null)
                {
                    float newValue = CalculateNewValue(matchingUnitData, selectedUnit);
                    changeService?.ChangeUnit(targetGroup.gameObject, matchingUnitData, selectedUnit, newValue, null, null, casterObject);
                }
            }

            return true;
        }
    }
}
