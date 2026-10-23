using UnityEngine;

namespace UnitSystem
{
    /// <summary>
    /// IUnitTargetStateCalculator 구현(31번 §3-2 3, 32번 §3-2 1). 재계산 공식은 이 함수 한 곳에만 있습니다.
    ///   1) state.ResetToData() - 상태를 초기 상태(질량 배율 1, 온도 = 대상 PureData 값)로 되돌린다.
    ///   2) 새 누적기(UnitStateAccumulator)에 AppliedUnits를 처음부터 순서대로 돌며 각 항목 속성의 Applicator.Accumulate를 적용한다.
    ///   3) 확정 - 질량 배율 = Mass 항목이 있으면 배율 합, 없으면 1 / 온도 = 가장 나중 항목 값, 없으면 원본 온도.
    /// 즉시 마법(Vector)은 목록에 없으므로 재계산 대상이 아닙니다(32번).
    /// </summary>
    public class UnitTargetStateCalculator : IUnitTargetStateCalculator
    {
        private readonly IUnitApplicatorRegistry registry;

        public UnitTargetStateCalculator(IUnitApplicatorRegistry registry)
        {
            this.registry = registry;
        }

        public void Recalculate(RuntimeDataUnitTarget state)
        {
            if (state == null) return;

            state.ResetToData();

            var data = state.Data;
            if (data == null) return; // 지원 속성 없는 대상: 초기 상태 그대로

            var acc = new UnitStateAccumulator();
            var applied = state.AppliedUnits;
            for (int i = 0; i < applied.Count; i++)
            {
                var unit = applied[i];
                if (unit == null) continue;

                var applicator = registry?.GetByUnitType(unit.UnitType);
                if (applicator == null)
                {
                    Debug.LogWarning($"[UnitTargetStateCalculator] No applicator registered for unit type {unit.UnitType}. Entry skipped.");
                    continue;
                }

                applicator.Accumulate(acc, unit);
            }

            state.SetMass(acc.MassEntryCount > 0 ? acc.MassMultiplierSum : RuntimeDataUnitTarget.DefaultMassMultiplier);
            state.SetTemperature(acc.Temperature ?? data.BaseTemperature);
        }
    }
}
