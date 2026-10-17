using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace UnitSystem
{
    /// <summary>
    /// 락온 큐(Entries)의 "적용 전 변화 예측"을 계산하는 호출형 순수 로직 인터페이스입니다(13-C).
    /// 마나 비용은 여기서 계산하지 않습니다 — 비용 규칙은 IUnitBatchCastingService.CalculateTotalCost 한 곳이 정본입니다(15-A).
    /// </summary>
    public interface ILockOnPreviewCalculator
    {
        /// <summary>대상별로 큐 순서대로 마법을 누적 적용한 예측 결과를 만든다(마법이 하나라도 유효하게 적용되는 대상만 포함).</summary>
        IReadOnlyList<LockOnTargetPreview> BuildTargetPreviews(IReadOnlyList<LockOnCastEntry> entries);
    }

    /// <summary>
    /// ILockOnPreviewCalculator 구현. 값 계산은 UnitCastRuleService(실제 시전과 같은 CalculateNewValue),
    /// 유효 쌍 판정은 UnitBatchCastingService(실제 시전과 같은 GetEffectiveEntries)를 호출해 재사용하며 규칙을 복제하지 않습니다.
    /// 읽기 전용 — RuntimeData를 변경하지 않습니다.
    /// </summary>
    public class LockOnPreviewCalculator : ILockOnPreviewCalculator
    {
        private readonly IUnitCastRuleService castRuleService;
        private readonly IUnitBatchCastingService batchCastingService;

        public LockOnPreviewCalculator(IUnitCastRuleService castRuleService, IUnitBatchCastingService batchCastingService)
        {
            this.castRuleService = castRuleService;
            this.batchCastingService = batchCastingService;
        }

        public IReadOnlyList<LockOnTargetPreview> BuildTargetPreviews(IReadOnlyList<LockOnCastEntry> entries)
        {
            var previews = new List<LockOnTargetPreview>();
            if (batchCastingService == null) return previews;

            var effective = batchCastingService.GetEffectiveEntries(entries);

            // 대상별로 (유닛 종류 → 누적 상태)를 큐 순서대로 시뮬레이션한다. 실제 시전은 쌍마다 ChangeUnit이
            // RuntimeDataUnit.CurrentValue를 갱신한 뒤 다음 쌍을 처리하므로, 여기서도 "현재값"을 이어받아 계산한다.
            var states = new Dictionary<RuntimeDataUnitGroup, TargetState>();
            var order = new List<RuntimeDataUnitGroup>();

            for (int i = 0; i < effective.Count; i++)
            {
                var entry = effective[i];
                var unitData = entry.Target.GetMatchingUnitData(entry.Spell.UnitType);
                if (unitData == null) continue;

                if (!states.TryGetValue(entry.Target, out var state))
                {
                    state = new TargetState();
                    states.Add(entry.Target, state);
                    order.Add(entry.Target);
                }

                state.Apply(castRuleService, unitData, entry.Spell);
            }

            for (int i = 0; i < order.Count; i++)
            {
                var target = order[i];
                previews.Add(states[target].ToPreview(target));
            }

            return previews;
        }

        private sealed class UnitState
        {
            public float Original;
            public float Current;
            public int Count;

            // Mass 전용: 크기 변화는 MassUnitApplicatorSO가 적용될 때만 일어난다(그 시점의 질량비 → 스케일 배율, 마지막 것이 유효).
            public bool HasScaleChange;
            public float ScaleMultiplier = 1f;
        }

        private sealed class TargetState
        {
            private readonly Dictionary<UnitType, UnitState> units = new();
            private int entryCount;

            public void Apply(IUnitCastRuleService service, RuntimeDataUnit unitData, PureDataUnit spell)
            {
                if (!units.TryGetValue(spell.UnitType, out var unit))
                {
                    unit = new UnitState { Original = unitData.OriginalValue, Current = unitData.CurrentValue };
                    units.Add(spell.UnitType, unit);
                }

                unit.Current = service.CalculateNewValue(unit.Original, unit.Current, spell);
                unit.Count++;
                entryCount++;

                if (spell.UnitType == UnitType.Mass && spell.Applicator is MassUnitApplicatorSO)
                {
                    float ratio = MassUnitApplicatorSO.CalculateMassRatio(unit.Current, unit.Original);
                    unit.ScaleMultiplier = MassUnitApplicatorSO.CalculateScaleRatio(ratio);
                    unit.HasScaleChange = true;
                }
            }

            public LockOnTargetPreview ToPreview(RuntimeDataUnitGroup target)
            {
                UnitType changed = UnitType.None;
                float predictedMass = 0f;
                bool hasScaleChange = false;
                float scaleMultiplier = 1f;
                float predictedTemperature = 0f;
                var sb = new StringBuilder();

                foreach (var pair in units)
                {
                    changed |= pair.Key;
                    var unit = pair.Value;
                    if (sb.Length > 0) sb.Append(", ");

                    switch (pair.Key)
                    {
                        case UnitType.Mass:
                            predictedMass = unit.Current;
                            hasScaleChange = unit.HasScaleChange;
                            scaleMultiplier = unit.ScaleMultiplier;
                            sb.Append(hasScaleChange
                                ? $"Mass {unit.Current:0.##} (scale x{scaleMultiplier:0.###})"
                                : $"Mass {unit.Current:0.##} (no scale change)");
                            break;
                        case UnitType.Temperature:
                            predictedTemperature = unit.Current;
                            sb.Append($"Temp {unit.Current:0.##}{(unit.Current < 0f ? " (Ice)" : string.Empty)}");
                            break;
                        case UnitType.Volume:
                            sb.Append("Volume");
                            break;
                        case UnitType.Vector:
                            sb.Append($"Vector {unit.Current:0.##}");
                            break;
                        default:
                            sb.Append(pair.Key);
                            break;
                    }
                }

                return new LockOnTargetPreview(target, changed, predictedMass, hasScaleChange, scaleMultiplier, predictedTemperature, entryCount, sb.ToString());
            }
        }
    }
}
