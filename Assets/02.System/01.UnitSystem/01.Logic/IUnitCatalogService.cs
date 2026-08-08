using System;
using System.Collections.Generic;

namespace UnitSystem
{
    public interface IUnitCatalogService
    {
        event Action<PureDataUnit> OnUnitUnlocked;

        IReadOnlyList<PureDataUnit> GetUnlockedUnits();
        IReadOnlyList<PureDataUnit> GetAllUnits();
        bool IsUnlocked(PureDataUnit unit);
        void UnlockUnit(PureDataUnit unit);
    }
}
