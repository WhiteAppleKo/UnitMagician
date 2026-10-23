using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnitSystem
{
    /// <summary>
    /// IUnitApplicatorRegistry 구현(30번). VContainer가 등록된 IUnitApplicator 전부를 IReadOnlyList로 주입하며,
    /// PureData 서브클래스 타입(DataType)과 대상 속성(UnitType) 두 가지 키로 정확 일치 조회합니다(28번 §6-12와 같은 정확 타입 조회).
    /// </summary>
    public class UnitApplicatorRegistry : IUnitApplicatorRegistry
    {
        private readonly Dictionary<Type, IUnitApplicator> byDataType = new();
        private readonly Dictionary<UnitType, IUnitApplicator> byUnitType = new();

        public UnitApplicatorRegistry(IReadOnlyList<IUnitApplicator> applicators)
        {
            if (applicators == null) return;

            foreach (var applicator in applicators)
            {
                if (applicator == null) continue;

                if (!byDataType.TryAdd(applicator.DataType, applicator))
                {
                    Debug.LogWarning($"[UnitApplicatorRegistry] Duplicate applicator for {applicator.DataType.Name}: {applicator.GetType().Name} ignored.");
                }

                if (!byUnitType.TryAdd(applicator.UnitType, applicator))
                {
                    Debug.LogWarning($"[UnitApplicatorRegistry] Duplicate applicator for unit type {applicator.UnitType}: {applicator.GetType().Name} ignored.");
                }
            }
        }

        public IUnitApplicator GetByDataType(Type dataType)
        {
            if (dataType == null) return null;

            return byDataType.TryGetValue(dataType, out var applicator) ? applicator : null;
        }

        public IUnitApplicator GetByUnitType(UnitType unitType)
        {
            return byUnitType.TryGetValue(unitType, out var applicator) ? applicator : null;
        }
    }
}
