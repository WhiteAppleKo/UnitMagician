using System.Collections.Generic;
using System.Text;

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
    /// ILockOnPreviewCalculator 구현(31번 §3-2 8). 대상마다 큐에 있는 마법의 임시 인스턴스(Applicator.CreateInstance)를 큐 순서대로 모아,
    /// "현재 적용 목록 + 임시 인스턴스"를 가진 이벤트 없는 복사본 상태(RuntimeDataUnitTarget.CreateSimulation)를 만들고
    /// 실제 적용과 같은 재계산(IUnitTargetStateCalculator.Recalculate)으로 예상 질량 배율·온도를 읽습니다. 예상 스케일 배율 = 복사본 CurrentMass(질량 배율 그대로, 32번).
    /// 즉시 마법(Vector, IsInstant)은 실제로도 목록에 남지 않으므로 복사본 목록에 넣지 않고, 변화 예정 속성으로만 표시합니다(32번).
    /// 유효 쌍 판정은 UnitBatchCastingService(실제 시전과 같은 GetEffectiveEntries)를 호출해 재사용하며 규칙을 복제하지 않습니다.
    /// 읽기 전용 — 실제 대상의 상태와 적용 목록은 바뀌지 않습니다.
    /// </summary>
    public class LockOnPreviewCalculator : ILockOnPreviewCalculator
    {
        private readonly IUnitApplicatorRegistry applicatorRegistry;
        private readonly IUnitBatchCastingService batchCastingService;
        private readonly IUnitTargetStateCalculator stateCalculator;

        public LockOnPreviewCalculator(IUnitApplicatorRegistry applicatorRegistry, IUnitBatchCastingService batchCastingService, IUnitTargetStateCalculator stateCalculator)
        {
            this.applicatorRegistry = applicatorRegistry;
            this.batchCastingService = batchCastingService;
            this.stateCalculator = stateCalculator;
        }

        public IReadOnlyList<LockOnTargetPreview> BuildTargetPreviews(IReadOnlyList<LockOnCastEntry> entries)
        {
            var previews = new List<LockOnTargetPreview>();
            if (batchCastingService == null || applicatorRegistry == null || stateCalculator == null) return previews;

            // 시전자 영향 계산 자리는 아직 비어 있어 시전자 정보 없이 만든 임시 인스턴스도 실제 시전과 같은 값을 캐싱한다.
            // 시전자 영향이 구현되면 실제 시전자 정보를 받아 넘겨야 한다(30번 §6-6).
            var castSource = UnitCastSource.None;
            var effective = batchCastingService.GetEffectiveEntries(entries);

            // 대상별로 큐 임시 인스턴스를 큐 순서대로 쌓는다. 실제 시전은 쌍마다 ChangeUnit이 목록 끝에 추가하므로 결과가 같다.
            var queued = new Dictionary<RuntimeDataUnitGroup, QueuedUnits>();
            var order = new List<RuntimeDataUnitGroup>();

            for (int i = 0; i < effective.Count; i++)
            {
                var entry = effective[i];
                var state = entry.Target.UnitTarget;
                if (state == null || !state.HasUnit(entry.Spell.UnitType)) continue;

                var applicator = applicatorRegistry.GetByDataType(entry.Spell.GetType());
                if (applicator == null) continue;

                var instance = applicator.CreateInstance(entry.Spell, castSource);
                if (instance == null) continue;

                if (!queued.TryGetValue(entry.Target, out var units))
                {
                    units = new QueuedUnits();
                    queued.Add(entry.Target, units);
                    order.Add(entry.Target);
                }

                units.Add(instance, applicator.IsInstant);
            }

            for (int i = 0; i < order.Count; i++)
            {
                var target = order[i];
                previews.Add(BuildPreview(target, queued[target]));
            }

            return previews;
        }

        private LockOnTargetPreview BuildPreview(RuntimeDataUnitGroup target, QueuedUnits units)
        {
            // 실제 상태·목록은 건드리지 않는 복사본에서 실제 적용과 같은 재계산을 돌린다.
            var simulation = target.UnitTarget.CreateSimulation(units.LastingInstances);
            stateCalculator.Recalculate(simulation);

            UnitType changed = UnitType.None;
            float predictedMass = 0f;
            bool hasScaleChange = false;
            float scaleMultiplier = 1f;
            float predictedTemperature = 0f;
            var sb = new StringBuilder();

            for (int i = 0; i < units.Types.Count; i++)
            {
                var unitType = units.Types[i];
                changed |= unitType;
                if (sb.Length > 0) sb.Append(", ");

                // 속성의 현재 값 읽기는 그 속성의 Applicator(ReadValue) 한 곳이 정한다(비용 계산과 같은 함수).
                // 아래 분기는 값을 미리보기 데이터(LockOnTargetPreview)의 어느 칸에 담을지만 정한다.
                var applicator = applicatorRegistry.GetByUnitType(unitType);
                float predicted = applicator != null ? applicator.ReadValue(simulation) : 0f;

                switch (unitType)
                {
                    case UnitType.Mass:
                        // 크기 변화는 Mass 마법(MassUnitApplicator)이 큐에 있을 때만 일어난다: 예상 스케일 배율 = 예상 질량 배율(32번, 실제 적용과 같은 값).
                        predictedMass = predicted;
                        hasScaleChange = true;
                        scaleMultiplier = predictedMass;
                        sb.Append($"Mass x{predictedMass:0.##}");
                        break;
                    case UnitType.Temperature:
                        predictedTemperature = predicted;
                        sb.Append($"Temp {predictedTemperature:0.##}{(predictedTemperature < 0f ? " (Ice)" : string.Empty)}");
                        break;
                    case UnitType.Vector:
                        // 즉시 마법: 상태에 남는 값이 없어 변화 예정 표시만 한다.
                        sb.Append("Vector (instant)");
                        break;
                    default:
                        sb.Append(unitType);
                        break;
                }
            }

            return new LockOnTargetPreview(target, changed, predictedMass, hasScaleChange, scaleMultiplier, predictedTemperature, units.EntryCount, sb.ToString());
        }

        /// <summary>
        /// 한 대상에 쌓인 큐 임시 인스턴스 중 지속 마법(큐 순서, 복사본 목록에 들어감)과, 처음 나온 순서대로의 속성 종류(즉시 마법 포함), 유효 쌍 수.
        /// </summary>
        private sealed class QueuedUnits
        {
            public readonly List<RuntimeDataUnit> LastingInstances = new();
            public readonly List<UnitType> Types = new();
            public int EntryCount { get; private set; }

            public void Add(RuntimeDataUnit instance, bool isInstant)
            {
                EntryCount++;
                if (!isInstant) LastingInstances.Add(instance);
                if (!Types.Contains(instance.UnitType)) Types.Add(instance.UnitType);
            }
        }
    }
}
