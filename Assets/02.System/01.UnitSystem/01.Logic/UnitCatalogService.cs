using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnitSystem
{
    public class UnitCatalogService : IUnitCatalogService
    {
        public event Action<PureDataUnit> OnUnitAcquired;
        public event Action<PureDataUnit> OnUnitEquipped;
        public event Action<PureDataUnit> OnUnitUnequipped;

        private readonly HashSet<PureDataUnit> possessedUnits = new();
        private readonly HashSet<PureDataUnit> equippedUnits = new();
        private readonly List<PureDataUnit> orderedUnits = new();
        private readonly IUnitEquipRequirementChecker requirementChecker;

        public UnitCatalogService(IReadOnlyList<PureDataUnit> initialUnits, IUnitEquipRequirementChecker requirementChecker)
        {
            this.requirementChecker = requirementChecker;

            if (initialUnits != null)
            {
                foreach (var unitData in initialUnits)
                {
                    if (unitData == null) continue;

                    if (!orderedUnits.Contains(unitData))
                    {
                        orderedUnits.Add(unitData);
                    }
                }
            }

            if (orderedUnits.Count > 0)
            {
                // 씬 시작 시 최소 하나는 바로 쓸 수 있어야 하므로 자동 습득 + 자동 장착(조건 검사는 스텁이라 항상 통과).
                var initialUnit = orderedUnits[0];
                possessedUnits.Add(initialUnit);
                equippedUnits.Add(initialUnit);
            }
        }

        public IReadOnlyList<PureDataUnit> GetPossessedUnits()
        {
            var list = new List<PureDataUnit>();
            foreach (var unitData in orderedUnits)
            {
                if (possessedUnits.Contains(unitData))
                {
                    list.Add(unitData);
                }
            }
            return list;
        }

        public IReadOnlyList<PureDataUnit> GetEquippedUnits()
        {
            var list = new List<PureDataUnit>();
            foreach (var unitData in orderedUnits)
            {
                if (equippedUnits.Contains(unitData))
                {
                    list.Add(unitData);
                }
            }
            return list;
        }

        public IReadOnlyList<PureDataUnit> GetAllUnits()
        {
            return new List<PureDataUnit>(orderedUnits);
        }

        public bool IsPossessed(PureDataUnit unit)
        {
            if (unit == null) return false;
            return possessedUnits.Contains(unit);
        }

        public bool IsEquipped(PureDataUnit unit)
        {
            if (unit == null) return false;
            return equippedUnits.Contains(unit);
        }

        public void AcquireUnit(PureDataUnit unit)
        {
            if (unit == null) return;

            if (!orderedUnits.Contains(unit))
            {
                Debug.LogWarning($"[UnitCatalogService] Cannot acquire unregistered PureDataUnit: {unit.name}");
                return;
            }

            if (possessedUnits.Add(unit))
            {
                Debug.Log($"[UnitCatalogService] Unit acquired: {unit.name}");
                OnUnitAcquired?.Invoke(unit);
            }
        }

        public bool TryEquipUnit(PureDataUnit unit, out string failReason)
        {
            if (unit == null)
            {
                failReason = "Unit is null.";
                return false;
            }

            if (!possessedUnits.Contains(unit))
            {
                failReason = $"Unit {unit.name} is not possessed.";
                return false;
            }

            if (equippedUnits.Contains(unit))
            {
                failReason = string.Empty;
                return true;
            }

            if (requirementChecker != null && !requirementChecker.CanEquip(unit, out failReason))
            {
                return false;
            }

            failReason = string.Empty;

            if (equippedUnits.Add(unit))
            {
                Debug.Log($"[UnitCatalogService] Unit equipped: {unit.name}");
                OnUnitEquipped?.Invoke(unit);
            }

            return true;
        }

        public void UnequipUnit(PureDataUnit unit)
        {
            if (unit == null) return;

            if (equippedUnits.Remove(unit))
            {
                Debug.Log($"[UnitCatalogService] Unit unequipped: {unit.name}");
                OnUnitUnequipped?.Invoke(unit);
            }
        }
    }
}
