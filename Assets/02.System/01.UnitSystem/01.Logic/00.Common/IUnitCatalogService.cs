using System;
using System.Collections.Generic;

namespace UnitSystem
{
    public interface IUnitCatalogService
    {
        event Action<PureDataUnit> OnUnitAcquired;
        event Action<PureDataUnit> OnUnitEquipped;
        event Action<PureDataUnit> OnUnitUnequipped;

        IReadOnlyList<PureDataUnit> GetPossessedUnits();
        IReadOnlyList<PureDataUnit> GetEquippedUnits();
        IReadOnlyList<PureDataUnit> GetAllUnits();
        bool IsPossessed(PureDataUnit unit);
        bool IsEquipped(PureDataUnit unit);

        void AcquireUnit(PureDataUnit unit);
        bool TryEquipUnit(PureDataUnit unit, out string failReason);
        void UnequipUnit(PureDataUnit unit);
    }
}
