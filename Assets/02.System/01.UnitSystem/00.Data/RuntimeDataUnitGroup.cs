using System;
using System.Collections.Generic;
using UnityEngine;
using Synty.AnimationBaseLocomotion.Samples;

namespace UnitSystem
{
    public class RuntimeDataUnitGroup : SampleObjectLockOn
    {
        [Header("Unit Group Setup")]
        [SerializeField] public UnitType supportedUnits = UnitType.None;
        [SerializeField] private bool isTargetable = true;
        [SerializeField, HideInInspector] public float bakedMassValue = 1.0f;

        public bool IsTargetable => isTargetable;

        public void SetTargetable(bool targetable)
        {
            isTargetable = targetable;
            if (!isTargetable)
            {
                Highlight(false, false);
            }
        }

        public event Action<RuntimeDataUnitGroup, RuntimeDataUnit> OnUnitGroupChanged;

        public List<RuntimeDataUnit> UnitRuntimeDataList { get; private set; } = new();

        private void Awake()
        {
            if (UnitRuntimeDataList.Count == 0)
            {
                InitUnits();
            }
        }

        public void InitUnits()
        {
            UnitRuntimeDataList.Clear();

            // 플래그 체크 후 개별 인스턴스 생성
            if (HasUnit(UnitType.Mass))
            {
                CreateAndAddRuntimeUnit(UnitType.Mass, bakedMassValue);
            }
            if (HasUnit(UnitType.Volume))
            {
                CreateAndAddRuntimeUnit(UnitType.Volume, 1.0f);
            }
            if (HasUnit(UnitType.Vector))
            {
                CreateAndAddRuntimeUnit(UnitType.Vector, 1.0f);
            }
        }

        private void CreateAndAddRuntimeUnit(UnitType type, float value)
        {
            var runtimeData = new RuntimeDataUnit(type, value, null);
            runtimeData.OnUnitChanged += HandleUnitChanged;
            UnitRuntimeDataList.Add(runtimeData);
        }

        private void OnDestroy()
        {
            foreach (var data in UnitRuntimeDataList)
            {
                if (data != null) data.OnUnitChanged -= HandleUnitChanged;
            }
        }

        private void HandleUnitChanged(RuntimeDataUnit data)
        {
            OnUnitGroupChanged?.Invoke(this, data);
        }

        public bool HasUnit(UnitType targetType)
        {
            return (supportedUnits & targetType) == targetType && targetType != UnitType.None;
        }

        public RuntimeDataUnit GetMatchingUnitData(UnitType targetType)
        {
            if (!HasUnit(targetType)) return null;

            foreach (var unitData in UnitRuntimeDataList)
            {
                if ((unitData.CurrentUnit & targetType) == targetType)
                {
                    return unitData;
                }
            }
            return null;
        }

        public override void Highlight(bool enable, bool targetLock)
        {
            if (!isTargetable)
            {
                base.Highlight(false, false);
                return;
            }

            base.Highlight(enable, targetLock);
        }
    }
}
