using System;

namespace UnitSystem
{
    public class RuntimeDataUnit
    {
        public event Action<RuntimeDataUnit> OnUnitChanged;

        public UnitType CurrentUnit { get; private set; }
        public float CurrentValue { get; private set; }
        public float OriginalValue { get; private set; }
        public PureDataUnit CurrentUnitData { get; private set; }

        public RuntimeDataUnit(UnitType initialUnit, float initialValue, PureDataUnit initialUnitData)
        {
            CurrentUnit = initialUnit;
            CurrentValue = initialValue;
            OriginalValue = initialValue;
            CurrentUnitData = initialUnitData;
        }

        public void UpdateUnitData(UnitType newUnit, float newValue, PureDataUnit newUnitData)
        {
            CurrentUnit = newUnit;
            CurrentValue = newValue;
            CurrentUnitData = newUnitData;

            OnUnitChanged?.Invoke(this);
        }
    }
}
