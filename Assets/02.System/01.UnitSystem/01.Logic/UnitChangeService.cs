using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using InteractionSystem.Logic;

namespace UnitSystem
{
    public class UnitChangeService
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

            if (!catalogService.IsUnlocked(newUnitData))
            {
                Debug.LogWarning($"[UnitChangeService] Cannot change unit. Unit {newUnitData.name} is locked.");
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
                pipeLine.Run(context).Forget();
                UpdateCollisionTriggerOwner(targetObject, casterGameObject);
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

            // 소유자 권한 시전자(Caster)로 강탈 갱신
            UpdateCollisionTriggerOwner(targetObject, casterGameObject);

            return true;
        }

        private void UpdateCollisionTriggerOwner(GameObject targetObject, GameObject casterGameObject)
        {
            if (targetObject == null || casterGameObject == null) return;

            var parentTriggers = targetObject.GetComponentsInParent<CollisionDamageTrigger>(true);
            foreach (var trigger in parentTriggers)
            {
                trigger.SetOwner(casterGameObject);
                Debug.Log($"<color=cyan>[UnitChangeService]</color> Overtook Parent Owner for {trigger.name} -> New Owner: {casterGameObject.name}");
            }

            var childTriggers = targetObject.GetComponentsInChildren<CollisionDamageTrigger>(true);
            foreach (var trigger in childTriggers)
            {
                trigger.SetOwner(casterGameObject);
                Debug.Log($"<color=cyan>[UnitChangeService]</color> Overtook Child Owner for {trigger.name} -> New Owner: {casterGameObject.name}");
            }
        }
    }
}
