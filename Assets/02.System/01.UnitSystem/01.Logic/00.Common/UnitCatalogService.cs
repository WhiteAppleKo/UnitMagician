using System;
using System.Collections.Generic;
using CharacterSystem;
using UnityEngine;

namespace UnitSystem
{
    /// <summary>
    /// 마법 장착 목록 서비스(33번). 보유 목록은 캐릭터 인벤토리(<see cref="IInventoryService"/>)가 단일 원천이며,
    /// 장착 가능 여부의 "보유 중" 검사도 인벤토리에 묻는다. 장착 목록 순서는 인벤토리 마법 탭 순서를 따른다.
    /// 장착 목록 자체는 ③(장착 소유 이전) 전까지 여기에 남는다.
    /// </summary>
    public class UnitCatalogService : IUnitCatalogService
    {
        public event Action<PureDataUnit> OnUnitEquipped;
        public event Action<PureDataUnit> OnUnitUnequipped;

        private readonly HashSet<PureDataUnit> equippedUnits = new();
        private readonly IInventoryService inventoryService;
        private readonly IUnitEquipRequirementChecker requirementChecker;

        public UnitCatalogService(IInventoryService inventoryService, IUnitEquipRequirementChecker requirementChecker)
        {
            this.inventoryService = inventoryService;
            this.requirementChecker = requirementChecker;

            // 씬 시작 시 최소 하나는 바로 쓸 수 있어야 하므로 인벤토리 마법 탭의 첫 마법(시작 마법 첫 항목)을 자동 장착한다
            // (③ 전까지 카탈로그가 처리, 조건 검사는 스텁이라 항상 통과). 시작 마법은 Character 스코프 빌드 시
            // InventoryLogicSystem.Initialize가 이미 지급해 두었다(부모 스코프가 먼저 빌드됨).
            var magicItems = inventoryService.GetItems(ItemType.Magic);
            for (int i = 0; i < magicItems.Count; i++)
            {
                if (magicItems[i] is PureDataUnit initialUnit && initialUnit != null)
                {
                    equippedUnits.Add(initialUnit);
                    break;
                }
            }
        }

        public IReadOnlyList<PureDataUnit> GetEquippedUnits()
        {
            var list = new List<PureDataUnit>();
            var magicItems = inventoryService.GetItems(ItemType.Magic);
            for (int i = 0; i < magicItems.Count; i++)
            {
                if (magicItems[i] is PureDataUnit unitData && equippedUnits.Contains(unitData))
                {
                    list.Add(unitData);
                }
            }
            return list;
        }

        public bool IsEquipped(PureDataUnit unit)
        {
            if (unit == null) return false;
            return equippedUnits.Contains(unit);
        }

        public bool TryEquipUnit(PureDataUnit unit, out string failReason)
        {
            if (unit == null)
            {
                failReason = "Unit is null.";
                return false;
            }

            if (!inventoryService.IsPossessed(unit))
            {
                failReason = $"Unit {unit.name} is not possessed.";
                return false;
            }

            if (equippedUnits.Contains(unit))
            {
                failReason = string.Empty;
                return true;
            }

            if (requirementChecker != null && !requirementChecker.CanEquip(unit, out failReason))
            {
                return false;
            }

            failReason = string.Empty;

            if (equippedUnits.Add(unit))
            {
                Debug.Log($"[UnitCatalogService] Unit equipped: {unit.name}");
                OnUnitEquipped?.Invoke(unit);
            }

            return true;
        }

        public void UnequipUnit(PureDataUnit unit)
        {
            if (unit == null) return;

            if (equippedUnits.Remove(unit))
            {
                Debug.Log($"[UnitCatalogService] Unit unequipped: {unit.name}");
                OnUnitUnequipped?.Invoke(unit);
            }
        }
    }
}
