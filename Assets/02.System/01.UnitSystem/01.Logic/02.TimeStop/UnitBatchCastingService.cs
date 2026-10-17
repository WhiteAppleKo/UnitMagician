using System;
using System.Collections.Generic;
using UnityEngine;
using CharacterSystem;

namespace UnitSystem
{
    /// <summary>
    /// 시간 정지 락온 큐 일괄 시전(유효 쌍 판정, 총 마나 한 번 차감, 쌍별 ChangeUnit)을 제공하는 서비스 인터페이스입니다.
    /// 값 변환·마나 비용 규칙은 시전 방식과 무관한 공용 규칙이므로 IUnitCastRuleService가 맡고, 이 서비스는 그 규칙에 위임합니다.
    /// </summary>
    public interface IUnitBatchCastingService
    {
        int CalculateTotalCost(IReadOnlyList<RuntimeDataUnitGroup> targets, PureDataUnit selectedUnit);
        bool ExecuteBatchCast(IReadOnlyList<RuntimeDataUnitGroup> targets, PureDataUnit selectedUnit, ICharacterStatService statService, GameObject casterObject = null);
        bool ExecuteBatchCast(IReadOnlyList<RuntimeDataUnitGroup> targets, PureDataUnit selectedUnit, CharacterStatSystem casterStat, GameObject casterObject = null);
        bool ExecuteBatchCast(IReadOnlyList<RuntimeDataUnitGroup> targets, PureDataUnit selectedUnit, RuntimeStatData casterStatData, GameObject casterObject = null);

        // --- 13번 기획서: (대상, 마법) 쌍 큐 기반 시전 ---
        /// <summary>
        /// 실제 시전/비용 계산/미리보기가 공통으로 처리하는 "유효 쌍"만 남긴다(15-A 단일 규칙).
        /// 유효 쌍 = 대상이 유효(파괴/비활성 아님)하고, 마법이 있고, 그 마법이 장착된 쌍. 큐 순서를 유지한다.
        /// </summary>
        IReadOnlyList<LockOnCastEntry> GetEffectiveEntries(IReadOnlyList<LockOnCastEntry> entries);

        /// <summary>
        /// 마나 비용의 단일 규칙(15-A): 유효 쌍 중 대상이 해당 마법 유형의 유닛을 가진 쌍의 마법 BaseCost 합.
        /// 실제 차감(ExecuteBatchCast)과 HUD 예상 비용(LockOnManaPreviewLogicSystem)이 모두 이 값을 쓴다.
        /// </summary>
        int CalculateTotalCost(IReadOnlyList<LockOnCastEntry> entries);

        /// <summary>
        /// 큐를 쌍마다 자기 마법으로 처리한다. 유효 쌍만 대상으로 총비용을 UseMP 한 번으로 차감하며, 부족 시 전부 실패(하이라이트 해제, 아무 변환도 하지 않음).
        /// 성공 시 쌍을 큐 순서대로 ChangeUnit 한다.
        /// </summary>
        bool ExecuteBatchCast(IReadOnlyList<LockOnCastEntry> entries, ICharacterStatService statService, GameObject casterObject = null);
    }

    /// <summary>
    /// 시간 정지 락온 큐 일괄 시전 서비스 클래스입니다. 값 변환·마나 비용 규칙은 IUnitCastRuleService에 위임합니다.
    /// </summary>
    public class UnitBatchCastingService : IUnitBatchCastingService
    {
        private readonly IUnitCastRuleService ruleService;
        private readonly IUnitChangeService changeService;
        private readonly IUnitCatalogService catalogService;

        /// <param name="catalogService">장착 여부 판정용. null이면(DI 없는 폴백 경로) 장착 필터를 적용하지 않는다.</param>
        public UnitBatchCastingService(IUnitCastRuleService ruleService, IUnitChangeService changeService, IUnitCatalogService catalogService = null)
        {
            this.ruleService = ruleService;
            this.changeService = changeService;
            this.catalogService = catalogService;
        }

        public int CalculateTotalCost(IReadOnlyList<RuntimeDataUnitGroup> targets, PureDataUnit selectedUnit)
        {
            if (targets == null || targets.Count == 0 || selectedUnit == null) return 0;

            int totalCost = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                totalCost += ruleService.CostOfPair(targets[i], selectedUnit);
            }

            return totalCost;
        }

        public bool ExecuteBatchCast(IReadOnlyList<RuntimeDataUnitGroup> targets, PureDataUnit selectedUnit, ICharacterStatService statService, GameObject casterObject = null)
        {
            if (targets == null || targets.Count == 0) return false;

            if (selectedUnit == null)
            {
                Debug.LogWarning("[UnitBatchCastingService] Batch Cast Aborted: No Unit selected.");
                return false;
            }

            int totalCost = CalculateTotalCost(targets, selectedUnit);

            // ICharacterStatService.UseMP() 경유 마나 검증 및 소모 (사전 MP 체크는 UseMP 자체가 처리 — 부족 시 false, 무제한이면 체크 없이 true)
            if (statService != null)
            {
                if (!statService.UseMP(totalCost))
                {
                    Debug.LogWarning($"[UnitBatchCastingService] Insufficient Mana. (Required: {totalCost})");
                    for (int i = 0; i < targets.Count; i++)
                    {
                        var target = targets[i];
                        if (target != null) target.Highlight(false, false);
                    }
                    return false;
                }
                Debug.Log($"<color=green>[UnitBatchCastingService] Consumed {totalCost} MP!</color>");
            }
            else
            {
                Debug.LogWarning("[UnitBatchCastingService] Caster Stat Service is null! Mana not consumed.");
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
                    float newValue = ruleService.CalculateNewValue(matchingUnitData, selectedUnit);
                    changeService?.ChangeUnit(targetGroup.gameObject, matchingUnitData, selectedUnit, newValue, null, null, casterObject);
                }
            }

            return true;
        }

        public IReadOnlyList<LockOnCastEntry> GetEffectiveEntries(IReadOnlyList<LockOnCastEntry> entries)
        {
            var effective = new List<LockOnCastEntry>();
            if (entries == null) return effective;

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null || entry.Spell == null) continue;
                if (!RuntimeDataUnitGroup.IsValidTarget(entry.Target)) continue;
                if (catalogService != null && !catalogService.IsEquipped(entry.Spell)) continue;

                effective.Add(entry);
            }

            return effective;
        }

        public int CalculateTotalCost(IReadOnlyList<LockOnCastEntry> entries)
        {
            return SumEffectiveCost(GetEffectiveEntries(entries));
        }

        private int SumEffectiveCost(IReadOnlyList<LockOnCastEntry> effective)
        {
            int totalCost = 0;
            for (int i = 0; i < effective.Count; i++)
            {
                totalCost += ruleService.CostOfPair(effective[i].Target, effective[i].Spell);
            }

            return totalCost;
        }

        public bool ExecuteBatchCast(IReadOnlyList<LockOnCastEntry> entries, ICharacterStatService statService, GameObject casterObject = null)
        {
            if (entries == null || entries.Count == 0) return false;

            // 큐에 있던 모든 대상의 락온 마커를 먼저 끈다(제외되는 쌍/마나 부족 실패 포함).
            for (int i = 0; i < entries.Count; i++)
            {
                var markedTarget = entries[i]?.Target;
                if (markedTarget != null) markedTarget.Highlight(false, false);
            }

            // 지정 후 발동 전에 장착 해제되었거나 대상이 소실된 쌍은 비용 계산/시전에서 제외한다
            // (ChangeUnit이 IsEquipped로 조용히 거부하는데 마나만 차감되는 불일치 방지).
            var effective = GetEffectiveEntries(entries);
            if (effective.Count < entries.Count)
            {
                Debug.LogWarning($"[UnitBatchCastingService] 장착 해제되었거나 대상이 소실된 쌍 {entries.Count - effective.Count}개를 제외합니다.");
            }
            if (effective.Count == 0) return false;

            entries = effective;
            int totalCost = SumEffectiveCost(effective);   // HUD 예상 비용(CalculateTotalCost)과 같은 한 규칙

            // 총 마나 UseMP 한 번 (부족 시 false, 무제한이면 체크 없이 true) — 실패 시 하이라이트 전부 해제 + 아무 변환도 하지 않음
            if (statService != null)
            {
                if (!statService.UseMP(totalCost))
                {
                    Debug.LogWarning($"[UnitBatchCastingService] Insufficient Mana. (Required: {totalCost})");
                    return false;
                }
                Debug.Log($"<color=green>[UnitBatchCastingService] Consumed {totalCost} MP!</color>");
            }
            else
            {
                Debug.LogWarning("[UnitBatchCastingService] Caster Stat Service is null! Mana not consumed.");
            }

            Debug.Log($"<color=green>[UnitBatchCastingService] Executing Queued Cast: {entries.Count} entries! Total Mana Cost: {totalCost}</color>");

            // 쌍을 큐 순서대로 자기 마법으로 변환 (총 마나는 이미 차감했으므로 ChangeUnit에는 null 전달해 중복 차감 방지)
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null || entry.Target == null) continue;

                var matchingUnitData = entry.Target.GetMatchingUnitData(entry.Spell.UnitType);
                if (matchingUnitData != null)
                {
                    float newValue = ruleService.CalculateNewValue(matchingUnitData, entry.Spell);
                    changeService?.ChangeUnit(entry.Target.gameObject, matchingUnitData, entry.Spell, newValue, null, null, casterObject);
                }
            }

            return true;
        }

        public bool ExecuteBatchCast(IReadOnlyList<RuntimeDataUnitGroup> targets, PureDataUnit selectedUnit, CharacterStatSystem casterStat, GameObject casterObject = null)
        {
            return ExecuteBatchCast(targets, selectedUnit, (ICharacterStatService)casterStat, casterObject);
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
                    float newValue = ruleService.CalculateNewValue(matchingUnitData, selectedUnit);
                    changeService?.ChangeUnit(targetGroup.gameObject, matchingUnitData, selectedUnit, newValue, null, null, casterObject);
                }
            }

            return true;
        }
    }
}
