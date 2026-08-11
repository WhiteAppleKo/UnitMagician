using System;
using UnityEngine;

namespace UnitSystem
{
    public class RuntimeDataUnit
    {
        public event Action<RuntimeDataUnit> OnUnitChanged;

        public UnitType CurrentUnit { get; private set; }
        public float CurrentValue { get; private set; }
        public float OriginalValue { get; private set; }
        public PureDataUnit CurrentUnitData { get; private set; }

        public Vector3 VectorDirection { get; private set; } = Vector3.forward;
        public float VectorSpeed { get; private set; } = 0f;

        public RuntimeDataUnit(UnitType initialUnit, float initialValue, PureDataUnit initialUnitData)
        {
            CurrentUnit = initialUnit;
            CurrentValue = initialValue;
            OriginalValue = initialValue;
            CurrentUnitData = initialUnitData;
            VectorDirection = Vector3.forward;
            VectorSpeed = initialValue;
        }

        public void UpdateUnitData(UnitType newUnit, float newValue, PureDataUnit newUnitData)
        {
            CurrentUnit = newUnit;
            CurrentValue = newValue;
            CurrentUnitData = newUnitData;

            OnUnitChanged?.Invoke(this);
        }

        public void SetVectorData(Vector3 direction, float speed)
        {
            VectorDirection = direction.normalized;
            VectorSpeed = speed;
            CurrentValue = speed;

            OnUnitChanged?.Invoke(this);
        }
    }
}
