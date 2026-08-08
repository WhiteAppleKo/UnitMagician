using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnitSystem
{
    public class UnitCatalogService : IUnitCatalogService
    {
        public event Action<PureDataUnit> OnUnitUnlocked;

        private readonly HashSet<PureDataUnit> unlockedUnits = new();
        private readonly List<PureDataUnit> orderedUnits = new();

        public UnitCatalogService(IReadOnlyList<PureDataUnit> initialUnits)
        {
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
                unlockedUnits.Add(orderedUnits[0]);
            }
        }



        public IReadOnlyList<PureDataUnit> GetUnlockedUnits()
        {
            var list = new List<PureDataUnit>();
            foreach (var unitData in orderedUnits)
            {
                if (unlockedUnits.Contains(unitData))
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

        public bool IsUnlocked(PureDataUnit unit)
        {
            if (unit == null) return false;
            return unlockedUnits.Contains(unit);
        }

        public void UnlockUnit(PureDataUnit unit)
        {
            if (unit == null) return;
            
            if (!orderedUnits.Contains(unit))
            {
                Debug.LogWarning($"[UnitCatalogService] Cannot unlock unregistered PureDataUnit: {unit.name}");
                return;
            }

            if (unlockedUnits.Add(unit))
            {
                Debug.Log($"[UnitCatalogService] Unit unlocked: {unit.name}");
                OnUnitUnlocked?.Invoke(unit);
            }
        }
    }
}
