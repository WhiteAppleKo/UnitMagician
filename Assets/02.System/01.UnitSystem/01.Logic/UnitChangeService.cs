using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace UnitSystem
{
    public class UnitChangeService : IUnitChangeService
    {
        private readonly IUnitCatalogService catalogService;

        public UnitChangeService(IUnitCatalogService catalogService)
        {
            this.catalogService = catalogService;
        }

        public bool ChangeUnit(GameObject targetObject, RuntimeDataUnit targetRuntimeData, PureDataUnit newUnitData, float newValue, CharacterSystem.RuntimeStatData casterStatData = null, PipeLine.UnitMagic.UnitMagicPipeLine pipeLine = null, GameObject casterGameObject = null)
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

            if (!catalogService.IsEquipped(newUnitData))
            {
                Debug.LogWarning($"[UnitChangeService] Cannot change unit. Unit {newUnitData.name} is not equipped.");
                return false;
            }

            // 마나 코스트 계산 수식: Cost = BaseCost * |CurrentValue - NewValue|
            int baseCost = newUnitData.BaseCost > 0 ? newUnitData.BaseCost : 10;
            float valueDiff = Mathf.Abs(targetRuntimeData.CurrentValue - newValue);
            int calculatedCost = Mathf.Max(Mathf.RoundToInt(baseCost * valueDiff), baseCost);

            var context = new PipeLine.Contexts.UnitMagicContext
            {
                Caster = casterGameObject,
                CasterStatData = casterStatData,
                TargetObject = targetObject,
                TargetRuntimeData = targetRuntimeData,
                SelectedPureData = newUnitData,
                NewValue = newValue,
                RequiredMana = calculatedCost
            };

            if (pipeLine != null)
            {
                // 진영 변경은 ApplyUnitChangeStep이 단위 변경이 실제로 적용된 경우에만 처리한다(여기서 호출하지 않는다).
                pipeLine.Run(context).Forget();
                return true;
            }

            // 마나 수치 검증 및 차감 연동 (Fallback)
            if (casterStatData != null)
            {
                if (!casterStatData.TryConsumeMP(calculatedCost))
                {
                    Debug.LogWarning($"[UnitChangeService] Insufficient Mana! Required: {calculatedCost}, Current MP: {casterStatData.MP.CurrentValue}");
                    return false;
                }
            }

            Debug.Log($"[UnitChangeService] Mana Cost Consumed: {calculatedCost}. Unit Converting: {targetRuntimeData.CurrentUnit}({targetRuntimeData.CurrentValue}) -> {newUnitData.UnitType}({newValue})");

            // 런타임 데이터 업데이트
            targetRuntimeData.UpdateUnitData(newUnitData.UnitType, newValue, newUnitData);
            if (targetObject != null && newUnitData.Applicator != null)
            {
                newUnitData.Applicator.Apply(targetObject, targetRuntimeData);
            }

            // 마나 소모가 성공한 뒤에만 대상 투사체의 진영을 시전자 진영으로 바꾼다(적 투사체 빼앗기)
            PipeLine.Combat.FactionChange.ApplyCasterFaction(targetObject, casterGameObject);

            return true;
        }
    }
}
