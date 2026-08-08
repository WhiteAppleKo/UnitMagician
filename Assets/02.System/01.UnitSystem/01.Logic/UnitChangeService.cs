using System;
using UnityEngine;

namespace UnitSystem
{
    public class UnitChangeService
    {
        private readonly IUnitCatalogService catalogService;

        public UnitChangeService(IUnitCatalogService catalogService)
        {
            this.catalogService = catalogService;
        }

        public bool ChangeUnit(RuntimeDataUnit targetRuntimeData, PureDataUnit newUnitData, float newValue)
        {
            if (targetRuntimeData == null)
            {
                Debug.LogError("[UnitChangeService] Target RuntimeData is null.");
                return false;
            }

            if (newUnitData == null)
            {
                Debug.LogError("[UnitChangeService] Provided PureDataUnit is null.");
                return false;
            }

            if (!catalogService.IsUnlocked(newUnitData))
            {
                Debug.LogWarning($"[UnitChangeService] Cannot change unit. Unit {newUnitData.name} is locked.");
                return false;
            }

            // 마나 코스트 계산 수식: Cost = BaseCost * |CurrentValue - NewValue|
            int baseCost = newUnitData.BaseCost;
            float valueDiff = Mathf.Abs(targetRuntimeData.CurrentValue - newValue);
            int calculatedCost = Mathf.RoundToInt(baseCost * valueDiff);

            // Debug Console 출력 (자원 시스템 대체)
            Debug.Log($"[UnitChangeService] Mana Cost Calculated: {calculatedCost} (BaseCost: {baseCost}, Diff: {valueDiff})");
            Debug.Log($"[UnitChangeService] Converting Unit: {targetRuntimeData.CurrentUnit}({targetRuntimeData.CurrentValue}) -> {newUnitData.UnitType}({newValue})");

            // 런타임 데이터 업데이트
            targetRuntimeData.UpdateUnitData(newUnitData.UnitType, newValue, newUnitData);

            return true;
        }
    }
}
