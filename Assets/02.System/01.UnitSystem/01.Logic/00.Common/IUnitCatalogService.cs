using System;
using System.Collections.Generic;

namespace UnitSystem
{
    /// <summary>
    /// 마법 장착 목록 서비스. 33번: 보유(습득·보유 확인)는 캐릭터 인벤토리(<see cref="CharacterSystem.IInventoryService"/>)로 옮겼고,
    /// 이 서비스에는 장착 목록만 남는다(③에서 캐릭터 장착 슬롯으로 이전 예정).
    /// </summary>
    public interface IUnitCatalogService
    {
        event Action<PureDataUnit> OnUnitEquipped;
        event Action<PureDataUnit> OnUnitUnequipped;

        /// <summary>장착된 마법을 인벤토리 마법 탭 순서대로.</summary>
        IReadOnlyList<PureDataUnit> GetEquippedUnits();
        bool IsEquipped(PureDataUnit unit);

        bool TryEquipUnit(PureDataUnit unit, out string failReason);
        void UnequipUnit(PureDataUnit unit);
    }
}
