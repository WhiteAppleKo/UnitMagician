using PipeLine.Combat;
using UnityEngine;

namespace UnitSystem
{
    /// <summary>
    /// 질량(Mass) 마법 적용 로직입니다(지속 마법).
    /// - 순회 적용(31번): 항목의 곱 배율(ScaleMultiplier)을 누적기의 배율 합에 더한다. 확정(UnitTargetStateCalculator)은 질량 배율 = 배율 합
    ///   (×2 3개 → 6, ×8 아님), 항목이 없으면 1(32번 - CurrentMass는 스케일 배율).
    /// - 시각: 질량 배율(RuntimeDataUnitTarget.CurrentMass)을 그대로 넘겨 크기 = 프리팹 기본 Scale × 배율, Rigidbody 질량 = 원래 질량 × 배율로 바꾸고(32번),
    ///   이 타입 마법이 추가될 때만 마법 에셋의 VFX를 스폰한다.
    /// - 데미지 계산 자리: 입력 데미지(BaseDamage에서 시작) × 질량 배율을 반환한다. 저장은 UnitApplyLogicSystem이 최종값으로 한 번만 한다
    ///   (매 재계산마다 BaseDamage에서 다시 시작하므로 중첩 곱셈이 생기지 않음).
    /// </summary>
    public class MassUnitApplicator : UnitApplicator<PureDataMassUnit>
    {
        public override UnitType UnitType => UnitType.Mass;

        protected override RuntimeDataUnit CreateInstance(PureDataMassUnit data, UnitCastSource source)
        {
            return new RuntimeDataMassUnit(data, ApplyCasterInfluence(data, data.MassScaleMultiplier, source));
        }

        protected override void AccumulateInstance(UnitStateAccumulator acc, RuntimeDataUnit instance)
        {
            if (instance is RuntimeDataMassUnit mass)
            {
                acc.AddMassMultiplier(mass.ScaleMultiplier);
            }
        }

        protected override void ApplyVisualToTarget(UnitApplyTarget target, RuntimeDataUnitTarget state, RuntimeDataUnit added)
        {
            float multiplier = state.CurrentMass;

            if (target.Visualizer != null)
            {
                target.Visualizer.ApplyMassScale(multiplier);

                // VFX 훅: Mass 마법이 추가되어 스케일 변화가 일어나는 시점에만 마법 링 회전 + 팽창 이펙트 스폰(해제면 스폰 안 함, 비어 있으면 스폰 안 함)
                if (added is RuntimeDataMassUnit addedMass && addedMass.Data.ScaleChangeVfxPrefab != null)
                {
                    target.Visualizer.SpawnEffect(addedMass.Data.ScaleChangeVfxPrefab);
                }
            }

            Debug.Log($"[MassUnitApplicator] Applied Mass & Scale ({(added != null ? added.Source.name : "recalculated")}). Multiplier: {multiplier}, Entries: {CountOf<RuntimeDataMassUnit>(state.AppliedUnits)}");
        }

        protected override int CalculateDamage(int currentDamage, RuntimeDataAttackDamage damage, RuntimeDataUnitTarget state)
        {
            int result = Mathf.RoundToInt(currentDamage * state.CurrentMass);
            Debug.Log($"[MassUnitApplicator] Damage {currentDamage} (BaseDamage {damage.BaseDamage}) x Mass {state.CurrentMass:F2} => {result}");
            return result;
        }

        protected override float ReadStateValue(RuntimeDataUnitTarget state) => state.CurrentMass;
    }
}
