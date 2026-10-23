using System;
using System.Collections.Generic;

namespace UnitSystem
{
    /// <summary>
    /// 마법 대상의 RuntimeData입니다(31번, 32번). 대상 PureData(PureDataUnitTarget)를 읽기 참조하고, 실제 상태(질량 배율·현재 온도)와
    /// 적용된 지속 마법 인스턴스를 적용 순서대로 누적하는 적용 마법 목록(AppliedUnits)을 가집니다.
    /// - 초기 상태 = 질량 배율 1, 온도 = PureData 값. 목록이 바뀔 때마다 Logic(IUnitTargetStateCalculator.Recalculate)이 초기 상태로 되돌린 뒤(ResetToData)
    ///   목록을 처음부터 순서대로 순회 적용해 확정한 값을 전용 변경 메서드(SetMass/SetTemperature)로 기록합니다.
    ///   이 클래스는 계산하지 않으며, 계산 중간값(배율 합 등)도 갖지 않습니다(Logic의 UnitStateAccumulator 소관).
    /// - 지속 마법(Mass·Temperature): 같은 속성이어도 교체하지 않고 목록 끝에 하나씩 추가합니다(Mass 3번 → Mass 항목 3개).
    ///   해제: 조건에 맞는 항목을 전부 빼고, 남은 항목은 상대 순서를 유지한 채 앞으로 당겨집니다(빈칸 없음).
    ///   목록이 바뀌면 바뀐 속성마다 OnAppliedUnitsChanged를 발행합니다.
    /// - 즉시 마법(Vector): 목록에 넣지 않고 OnInstantUnitApplied만 발행합니다(ApplyInstant). 되돌릴 필요가 없으므로 상태에도 남기지 않습니다.
    /// 미리보기용 복사본(CreateSimulation)은 이벤트를 발행하지 않습니다.
    /// </summary>
    public class RuntimeDataUnitTarget
    {
        /// <summary>질량 배율의 초기·리셋 값(Mass 항목이 없을 때의 배율).</summary>
        public const float DefaultMassMultiplier = 1f;

        private readonly List<RuntimeDataUnit> appliedUnits = new();
        private readonly bool raiseEvents;

        /// <summary>목록이 바뀐 속성과, 추가였다면 추가된 인스턴스(해제면 null).</summary>
        public event Action<UnitType, RuntimeDataUnit> OnAppliedUnitsChanged;

        /// <summary>즉시 마법이 적용됐을 때 그 인스턴스(목록에는 남지 않음).</summary>
        public event Action<RuntimeDataUnit> OnInstantUnitApplied;

        /// <summary>대상 원본(읽기 참조). 비어 있으면 지원 속성이 없는 대상으로 동작합니다.</summary>
        public PureDataUnitTarget Data { get; }

        /// <summary>
        /// 현재 질량 배율(스케일 배율). 초기 1이며, Mass 항목이 있으면 그 항목들의 배율 합입니다(×2 3개 → 6).
        /// 크기(프리팹 기본 Scale × 배율)·Rigidbody 질량·데미지·크기 게이트·미리보기가 이 값을 그대로 읽습니다.
        /// </summary>
        public float CurrentMass { get; private set; }

        /// <summary>현재 온도.</summary>
        public float CurrentTemperature { get; private set; }

        /// <summary>적용 마법 목록(적용 순서대로, 지속 마법만).</summary>
        public IReadOnlyList<RuntimeDataUnit> AppliedUnits => appliedUnits;

        public RuntimeDataUnitTarget(PureDataUnitTarget data) : this(data, null, true)
        {
        }

        private RuntimeDataUnitTarget(PureDataUnitTarget data, IEnumerable<RuntimeDataUnit> applied, bool raiseEvents)
        {
            Data = data;
            this.raiseEvents = raiseEvents;
            if (applied != null)
            {
                foreach (var unit in applied)
                {
                    if (unit != null && HasUnit(unit.UnitType)) appliedUnits.Add(unit);
                }
            }

            ResetToData();
        }

        /// <summary>대상이 그 마법 속성을 받을 수 있는지(Data.SupportedUnits 기준, Data가 비어 있으면 false).</summary>
        public bool HasUnit(UnitType unitType)
        {
            return Data != null && Data.HasUnit(unitType);
        }

        /// <summary>
        /// 지속 마법 인스턴스를 목록 끝에 추가하고 OnAppliedUnitsChanged(속성, 추가된 인스턴스)를 발행합니다.
        /// 대상이 그 속성을 받을 수 없으면 추가하지 않고 false를 반환합니다.
        /// </summary>
        public bool AddApplied(RuntimeDataUnit instance)
        {
            if (instance == null || !HasUnit(instance.UnitType)) return false;

            appliedUnits.Add(instance);
            Raise(instance.UnitType, instance);
            return true;
        }

        /// <summary>
        /// 즉시 마법 인스턴스를 적용합니다: 목록에 넣지 않고 OnInstantUnitApplied(인스턴스)만 발행합니다(32번).
        /// 대상이 그 속성을 받을 수 없으면 발행하지 않고 false를 반환합니다. 미리보기 복사본은 발행하지 않습니다.
        /// </summary>
        public bool ApplyInstant(RuntimeDataUnit instance)
        {
            if (instance == null || !HasUnit(instance.UnitType)) return false;

            if (raiseEvents) OnInstantUnitApplied?.Invoke(instance);
            return true;
        }

        /// <summary>
        /// 조건에 맞는 항목을 전부 제거합니다. 남은 항목은 순서를 유지한 채 앞으로 당겨지며,
        /// 바뀐 속성마다(처음 제거된 순서대로) OnAppliedUnitsChanged(속성, null)를 발행합니다. 제거 개수를 반환합니다.
        /// </summary>
        public int RemoveApplied(Predicate<RuntimeDataUnit> match)
        {
            if (match == null || appliedUnits.Count == 0) return 0;

            // 한 번 순회하며 조건을 한 번만 평가한다: 남길 항목은 쓰기 위치(write)로 당기고(상대 순서 유지, 빈칸 없음),
            // 제거 항목의 속성은 처음 제거된 순서대로 모은다.
            var changedTypes = new List<UnitType>();
            int write = 0;
            for (int read = 0; read < appliedUnits.Count; read++)
            {
                var unit = appliedUnits[read];
                if (match(unit))
                {
                    if (!changedTypes.Contains(unit.UnitType)) changedTypes.Add(unit.UnitType);
                    continue;
                }

                appliedUnits[write++] = unit;
            }

            int removed = appliedUnits.Count - write;
            if (removed == 0) return 0;

            appliedUnits.RemoveRange(write, removed);

            for (int i = 0; i < changedTypes.Count; i++)
            {
                Raise(changedTypes[i], null);
            }

            return removed;
        }

        /// <summary>그 속성 항목을 전부 해제합니다(RemoveApplied(u => u.UnitType == type)).</summary>
        public int RemoveAllOfType(UnitType unitType)
        {
            return RemoveApplied(u => u.UnitType == unitType);
        }

        /// <summary>상태 필드를 초기 상태로 되돌립니다(질량 배율 1, 온도 = PureData 값, Data가 없으면 0). 목록은 유지하며 재계산의 첫 단계입니다.</summary>
        public void ResetToData()
        {
            CurrentMass = DefaultMassMultiplier;
            CurrentTemperature = Data != null ? Data.BaseTemperature : 0f;
        }

        /// <summary>Logic이 순회 적용 결과(질량 배율)를 기록합니다.</summary>
        public void SetMass(float massMultiplier)
        {
            CurrentMass = massMultiplier;
        }

        /// <summary>Logic이 순회 적용 결과(온도)를 기록합니다.</summary>
        public void SetTemperature(float temperature)
        {
            CurrentTemperature = temperature;
        }

        /// <summary>풀 재사용 시: 목록을 비우고 상태를 초기 상태로 되돌립니다. 이벤트는 발행하지 않습니다(시각 복원은 각 IPoolReusable 소관).</summary>
        public void Reset()
        {
            appliedUnits.Clear();
            ResetToData();
        }

        /// <summary>
        /// 미리보기용 복사본: 같은 PureData와 "현재 목록 + extra"(대상이 받을 수 없는 속성은 제외) 목록을 가진, 이벤트를 발행하지 않는 상태입니다.
        /// 상태 값은 초기 상태에서 시작하므로, 실제 적용과 같은 재계산(IUnitTargetStateCalculator.Recalculate)을 거쳐 읽습니다. 원본 상태와 목록은 바뀌지 않습니다.
        /// </summary>
        public RuntimeDataUnitTarget CreateSimulation(IEnumerable<RuntimeDataUnit> extra)
        {
            var applied = new List<RuntimeDataUnit>(appliedUnits);
            if (extra != null) applied.AddRange(extra);
            return new RuntimeDataUnitTarget(Data, applied, false);
        }

        private void Raise(UnitType unitType, RuntimeDataUnit added)
        {
            if (raiseEvents) OnAppliedUnitsChanged?.Invoke(unitType, added);
        }
    }
}
