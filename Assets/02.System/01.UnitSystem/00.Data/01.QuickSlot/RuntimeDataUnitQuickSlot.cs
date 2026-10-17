using System;
using UnityEngine;

namespace UnitSystem
{
    /// <summary>
    /// 현재 선택된 단위 마법 슬롯 및 유닛 데이터를 관리하는 RuntimeData 모델입니다.
    /// </summary>
    public class RuntimeDataUnitQuickSlot
    {
        public PureDataUnit SelectedUnit { get; private set; }
        public int SelectedSlotIndex { get; private set; } = -1;

        public UnitType SelectedUnitType => SelectedUnit != null ? SelectedUnit.UnitType : UnitType.None;

        public event Action<PureDataUnit> OnSelectedUnitChanged;
        public event Action<int> OnSelectedSlotIndexChanged;

        public void SelectUnit(PureDataUnit unit, int slotIndex = -1)
        {
            if (SelectedUnit == unit && SelectedSlotIndex == slotIndex) return;

            SelectedUnit = unit;
            SelectedSlotIndex = slotIndex;

            Debug.Log($"<color=cyan>[RuntimeDataUnitQuickSlot] Selected Unit Changed:</color> {(unit != null ? unit.UnitName : "None")} (Slot: {slotIndex})");

            OnSelectedUnitChanged?.Invoke(SelectedUnit);
            OnSelectedSlotIndexChanged?.Invoke(SelectedSlotIndex);
        }

        public void ClearSelection()
        {
            if (SelectedUnit == null && SelectedSlotIndex == -1) return;

            SelectedUnit = null;
            SelectedSlotIndex = -1;

            Debug.Log("<color=cyan>[RuntimeDataUnitQuickSlot] Selection Cleared.</color>");

            OnSelectedUnitChanged?.Invoke(null);
            OnSelectedSlotIndexChanged?.Invoke(-1);
        }
    }
}
