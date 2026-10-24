using System;

namespace UnitSystem
{
    /// <summary>
    /// 인벤토리에서 미리 등록해 둔 W/E/F 즐겨찾기 유닛 슬롯 3개를 관리하는 RuntimeData입니다.
    /// 12번 기획서(시간정지 마법 선택 UI)의 즉시 확정(W/E/F)용 데이터로 사용되며,
    /// 순수 C# 클래스로 MonoBehaviour/Transform을 직접 참조하지 않습니다.
    /// 상태 변경은 전용 메서드(AssignSlot/ClearSlot)로만 수행하고, 변경 시 OnFavoriteSlotChanged만 발행합니다
    /// (어떤 슬롯에 무엇을 넣을지에 대한 판단은 호출부인 MagicInventoryTabView(인벤토리 마법 탭)의 우클릭 핸들러가 담당).
    /// </summary>
    public class RuntimeDataMagicFavoriteSlots
    {
        public const int SlotCount = 3;

        private readonly PureDataUnit[] slots = new PureDataUnit[SlotCount];

        public event Action<int, PureDataUnit> OnFavoriteSlotChanged;

        public PureDataUnit GetSlot(int index)
        {
            if (index < 0 || index >= SlotCount) return null;
            return slots[index];
        }

        public void AssignSlot(int index, PureDataUnit unit)
        {
            if (index < 0 || index >= SlotCount) return;
            if (slots[index] == unit) return;

            slots[index] = unit;
            OnFavoriteSlotChanged?.Invoke(index, unit);
        }

        public void ClearSlot(int index)
        {
            AssignSlot(index, null);
        }

        /// <summary>
        /// 주어진 유닛이 현재 등록돼 있는 슬롯 인덱스(W=0/E=1/F=2)를 반환합니다. 없으면 -1.
        /// </summary>
        public int FindSlotOf(PureDataUnit unit)
        {
            if (unit == null) return -1;

            for (int i = 0; i < SlotCount; i++)
            {
                if (slots[i] == unit) return i;
            }

            return -1;
        }
    }
}
