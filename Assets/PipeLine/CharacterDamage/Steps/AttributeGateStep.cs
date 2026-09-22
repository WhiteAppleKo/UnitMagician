using System;
using Cysharp.Threading.Tasks;
using PipeLine.Contexts;
using PipeLine.PipeLineBase;
using UnitSystem;
using UnityEngine;

namespace PipeLine.CharacterDamage.Steps
{
    /// <summary>
    /// 피격 파이프라인 스텝(1차 게이트): 공격자(투사체)의 Temperature 단위가 얼음 속성(음수)인지 확인합니다.
    /// 얼음 속성이 아니면(단위가 없거나 0 이상이면) context.Aborted = true로 설정해 이후 스텝은 물론
    /// ApplyDamageStep의 실제 데미지 적용도 건너뛰게 합니다(회피와는 무관한 별도 분기 - CombatPipelineManager/ApplyDamageStep 참고).
    /// 얼음 속성이면 공격자의 Mass 비율(MassUnitApplicatorSO.CalculateMassRatio 재사용)을 RawDamage에 곱해
    /// FinalDamage를 계산해둡니다. HP와 비교하는 2차 게이트는 이 스텝의 책임이 아니라 victim의 IDamageable
    /// 구현체(예: DestructibleDoorLogicSystem)가 담당합니다.
    /// 상태를 갖지 않는 공유 싱글턴이며, 실제로 기다릴 비동기 작업이 없으므로 즉시 완료된 UniTask를 반환합니다.
    /// </summary>
    [Serializable]
    public class AttributeGateStep : IPipeLineStep<DamageContext>
    {
        public static readonly AttributeGateStep Instance = new AttributeGateStep();

        private AttributeGateStep() { }

        public UniTask<DamageContext> Execute(DamageContext context)
        {
            var unitGroup = context.Attacker != null ? context.Attacker.GetComponent<RuntimeDataUnitGroup>() : null;
            var temperatureUnit = unitGroup != null ? unitGroup.GetMatchingUnitData(UnitType.Temperature) : null;

            // 온도 단위가 없거나 음수(얼음 속성)가 아니면 게이트 실패 - 조용히 중단(회피 연출 없음)
            if (temperatureUnit == null || temperatureUnit.CurrentValue >= 0f)
            {
                context.Aborted = true;
                string temperatureText = temperatureUnit != null ? temperatureUnit.CurrentValue.ToString("F2") : "N/A";
                Debug.Log($"[AttributeGateStep] Gate FAILED - attacker '{context.Attacker?.name}' is not ice-attributed (Temperature: {temperatureText})");
                return UniTask.FromResult(context);
            }

            var massUnit = unitGroup.GetMatchingUnitData(UnitType.Mass);
            float massRatio = MassUnitApplicatorSO.CalculateMassRatio(massUnit);
            context.FinalDamage = Mathf.RoundToInt(context.RawDamage * massRatio);

            Debug.Log($"[AttributeGateStep] Gate PASSED - Temperature: {temperatureUnit.CurrentValue:F2}, MassRatio: {massRatio:F2}, RawDamage: {context.RawDamage} => FinalDamage: {context.FinalDamage}");

            return UniTask.FromResult(context);
        }
    }
}
