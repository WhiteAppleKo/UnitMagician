using System;
using System.Collections.Generic;
using UnityEngine;
using Synty.AnimationBaseLocomotion.Samples;

namespace UnitSystem
{
    /// <summary>
    /// 오브젝트에 부착되는 단위 데이터 그룹 및 락온 타겟 컴포넌트입니다.
    /// Mass, Volume, Vector 등 다중 단위 런타임 데이터를 관리하고 상태 변경 이벤트를 발행합니다.
    /// </summary>
    public class RuntimeDataUnitGroup : SampleObjectLockOn
    {
        [Header("Unit Group Setup")]
        [SerializeField] public UnitType supportedUnits = UnitType.None;
        [SerializeField] private bool isTargetable = true;
        [SerializeField, HideInInspector] public float bakedMassValue = 1.0f;

        public UnitType SupportedUnits => supportedUnits;
        public bool IsTargetable => isTargetable;
        public float BakedMassValue
        {
            get => bakedMassValue;
            set => bakedMassValue = value;
        }

        public event Action<RuntimeDataUnitGroup, RuntimeDataUnit> OnUnitGroupChanged;
        public event Action<bool> OnTargetableChanged;

        public List<RuntimeDataUnit> UnitRuntimeDataList { get; private set; } = new();
        public IReadOnlyList<RuntimeDataUnit> Units => UnitRuntimeDataList;

        private void Awake()
        {
            if (UnitRuntimeDataList.Count == 0)
            {
                InitUnits();
            }
        }

        public void InitUnits()
        {
            UnbindUnitEvents();
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
            UnbindUnitEvents();
        }

        private void UnbindUnitEvents()
        {
            if (UnitRuntimeDataList == null) return;

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
                if (unitData != null && (unitData.CurrentUnit & targetType) == targetType)
                {
                    return unitData;
                }
            }
            return null;
        }

        public void SetTargetable(bool targetable)
        {
            if (isTargetable == targetable) return;

            isTargetable = targetable;
            if (!isTargetable)
            {
                Highlight(false, false);
            }

            OnTargetableChanged?.Invoke(isTargetable);
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
