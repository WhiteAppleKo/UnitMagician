using System;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

namespace UnitSystem
{
    /// <summary>
    /// 키보드 숫자키 입력 및 슬롯 선택 로직을 처리하여 RuntimeDataUnitQuickSlot을 갱신하는 Logic System입니다.
    /// </summary>
    public class UnitQuickSlotLogicSystem : ITickable, IInitializable, IDisposable
    {
        private readonly RuntimeDataUnitQuickSlot runtimeData;
        private readonly IUnitCatalogService catalogService;

        private readonly Key[] quickSlotKeys =
        {
            Key.Digit1, Key.Digit2, Key.Digit3,
            Key.Digit4, Key.Digit5, Key.Digit6,
            Key.Digit7, Key.Digit8, Key.Digit9
        };

        private readonly Key[] quickSlotNumpadKeys =
        {
            Key.Numpad1, Key.Numpad2, Key.Numpad3,
            Key.Numpad4, Key.Numpad5, Key.Numpad6,
            Key.Numpad7, Key.Numpad8, Key.Numpad9
        };

        [Inject]
        public UnitQuickSlotLogicSystem(
            RuntimeDataUnitQuickSlot runtimeData,
            IUnitCatalogService catalogService)
        {
            this.runtimeData = runtimeData;
            this.catalogService = catalogService;
        }

        public void Initialize()
        {
            if (catalogService != null)
            {
                catalogService.OnUnitUnlocked -= HandleUnitUnlocked;
                catalogService.OnUnitUnlocked += HandleUnitUnlocked;

                // 초기 해금 유닛 중 첫 번째 유닛 기본 선택
                if (runtimeData.SelectedUnit == null)
                {
                    var unlockedUnits = catalogService.GetUnlockedUnits();
                    if (unlockedUnits != null && unlockedUnits.Count > 0)
                    {
                        SelectSlot(0, unlockedUnits[0]);
                    }
                }
            }
        }

        public void Dispose()
        {
            if (catalogService != null)
            {
                catalogService.OnUnitUnlocked -= HandleUnitUnlocked;
            }
        }

        private void HandleUnitUnlocked(PureDataUnit unlockedUnit)
        {
            if (unlockedUnit == null) return;

            // 현재 선택된 유닛이 없으면 새로 해금된 유닛을 기본 선택
            if (runtimeData.SelectedUnit == null)
            {
                SelectUnit(unlockedUnit);
            }
        }

        public void Tick()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || catalogService == null) return;

            var unlockedUnits = catalogService.GetUnlockedUnits();
            if (unlockedUnits == null || unlockedUnits.Count == 0) return;

            for (int i = 0; i < unlockedUnits.Count && i < quickSlotKeys.Length; i++)
            {
                bool isDigitPressed = keyboard[quickSlotKeys[i]].wasPressedThisFrame;
                bool isNumpadPressed = i < quickSlotNumpadKeys.Length && keyboard[quickSlotNumpadKeys[i]].wasPressedThisFrame;

                if (isDigitPressed || isNumpadPressed)
                {
                    SelectSlot(i, unlockedUnits[i]);
                    break;
                }
            }
        }

        public bool SelectSlot(int slotIndex, PureDataUnit unitData)
        {
            if (catalogService == null || unitData == null) return false;

            if (!catalogService.IsUnlocked(unitData))
            {
                Debug.LogWarning($"[UnitQuickSlotLogicSystem] Unit {unitData.name} is locked!");
                return false;
            }

            runtimeData.SelectUnit(unitData, slotIndex);
            return true;
        }

        public bool SelectSlot(int slotIndex)
        {
            if (catalogService == null) return false;

            var unlockedUnits = catalogService.GetUnlockedUnits();
            if (slotIndex >= 0 && slotIndex < unlockedUnits.Count)
            {
                return SelectSlot(slotIndex, unlockedUnits[slotIndex]);
            }

            return false;
        }

        public bool SelectUnit(PureDataUnit unitData)
        {
            if (catalogService == null || unitData == null) return false;

            if (!catalogService.IsUnlocked(unitData))
            {
                Debug.LogWarning($"[UnitQuickSlotLogicSystem] Unit {unitData.name} is locked!");
                return false;
            }

            var unlockedUnits = catalogService.GetUnlockedUnits();
            int index = -1;
            if (unlockedUnits != null)
            {
                for (int i = 0; i < unlockedUnits.Count; i++)
                {
                    if (unlockedUnits[i] == unitData)
                    {
                        index = i;
                        break;
                    }
                }
            }

            runtimeData.SelectUnit(unitData, index);
            return true;
        }
    }
}
