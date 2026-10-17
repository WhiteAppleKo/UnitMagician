using System;
using Cysharp.Threading.Tasks;
using PipeLine.Combat;
using PipeLine.Contexts;
using PipeLine.PipeLineBase;
using UnitSystem;
using UnityEngine;

namespace PipeLine.CharacterDamage.Steps
{
    /// <summary>
    /// 피격 파이프라인 스텝(1차 게이트): 공격자(투사체)의 Temperature 단위가 피격 대상이 요구하는 온도 조건
    /// (IGateRequirementProvider.RequiredMaxTemperature 미만, 요구값은 대상의 PureData에서 옴)을 만족하는지 확인합니다.
    /// 만족하지 못하면(온도 단위가 없는 경우 포함) context.AttributeGatePassed = false + context.Aborted = true로 설정해
    /// 이후 스텝은 물론 ApplyDamageStep의 실제 데미지 적용도 건너뛰게 합니다(회피와는 무관한 별도 분기 - CombatPipelineManager/ApplyDamageStep 참고).
    /// 만족하면 AttributeGatePassed = true를 기록하고 공격자의 Mass 비율(MassUnitApplicatorSO.CalculateMassRatio 재사용)을
    /// RawDamage에 곱해 FinalDamage를 계산해둡니다. 크기(질량 비율) 조건과 HP/기준 데미지 비교는 이 스텝의 책임이 아니라
    /// SizeGateStep과 피격 대상의 판정 로직(예: DestructibleDoorLogicSystem, CoreHitCompletionEvaluator)이 개별로 담당합니다.
    /// IGateRequirementProvider가 없는 대상은 속성 게이트가 요구되지 않는 대상이므로 통과로 기록합니다.
    /// 상태를 갖지 않는 공유 싱글턴이며, 실제로 기다릴 비동기 작업이 없으므로 즉시 완료된 UniTask를 반환합니다.
    /// </summary>
    [Serializable]
    public class AttributeGateStep : IPipeLineStep<DamageContext>
    {
        public static readonly AttributeGateStep Instance = new AttributeGateStep();

        private AttributeGateStep() { }

        /// <summary>
        /// 속성 데이터는 실제로 충돌한 가해 오브젝트(Source, 예: 투사체)에서 읽는다. Attacker는 owner(투사체를 쏜 NPC/마법
        /// 시전자로 소유권이 바뀔 수도 있다)라 투사체의 온도/질량을 갖고 있지 않기 때문이다. Source에 데이터가 없으면
        /// 기존 동작(Attacker)으로 폴백한다. SizeGateStep도 같은 규칙으로 읽기 위해 공유한다.
        /// </summary>
        internal static RuntimeDataUnitGroup ResolveSourceUnitGroup(in DamageContext context)
        {
            var unitGroup = context.Source != null ? context.Source.GetComponent<RuntimeDataUnitGroup>() : null;
            if (unitGroup == null && context.Attacker != null)
            {
                unitGroup = context.Attacker.GetComponent<RuntimeDataUnitGroup>();
            }

            return unitGroup;
        }

        public UniTask<DamageContext> Execute(DamageContext context)
        {
            var unitGroup = ResolveSourceUnitGroup(context);
            var temperatureUnit = unitGroup != null ? unitGroup.GetMatchingUnitData(UnitType.Temperature) : null;
            string temperatureText = temperatureUnit != null ? temperatureUnit.CurrentValue.ToString("F2") : "N/A";

            // 요구 온도는 피격 대상의 PureData 값(IGateRequirementProvider). 대상이 요구값을 제공하지 않으면 속성 게이트가 요구되지 않는다.
            if (context.Victim != null && context.Victim.TryGetComponent<IGateRequirementProvider>(out var requirement))
            {
                // 온도 단위가 없거나 온도가 요구 최대 온도 미만이 아니면 게이트 실패 - 조용히 중단(회피 연출 없음)
                if (temperatureUnit == null || temperatureUnit.CurrentValue >= requirement.RequiredMaxTemperature)
                {
                    context.AttributeGatePassed = false;
                    context.Aborted = true;
                    Debug.Log($"[AttributeGateStep] Gate FAILED - source '{(context.Source != null ? context.Source.name : context.Attacker?.name)}' (attacker '{context.Attacker?.name}') Temperature: {temperatureText}, Required max (exclusive): {requirement.RequiredMaxTemperature:F2}");
                    return UniTask.FromResult(context);
                }
            }

            context.AttributeGatePassed = true;

            var massUnit = unitGroup != null ? unitGroup.GetMatchingUnitData(UnitType.Mass) : null;
            float massRatio = MassUnitApplicatorSO.CalculateMassRatio(massUnit);
            context.FinalDamage = Mathf.RoundToInt(context.RawDamage * massRatio);

            Debug.Log($"[AttributeGateStep] Gate PASSED - Temperature: {temperatureText}, MassRatio: {massRatio:F2}, RawDamage: {context.RawDamage} => FinalDamage: {context.FinalDamage}");

            return UniTask.FromResult(context);
        }
    }
}
