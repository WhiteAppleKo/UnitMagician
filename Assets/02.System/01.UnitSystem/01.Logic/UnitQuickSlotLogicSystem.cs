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

        private readonly Synty.AnimationBaseLocomotion.Samples.InputSystem.InputReader inputReader;

        [Inject]
        public UnitQuickSlotLogicSystem(
            RuntimeDataUnitQuickSlot runtimeData,
            IUnitCatalogService catalogService,
            Synty.AnimationBaseLocomotion.Samples.InputSystem.InputReader inputReader = null)
        {
            this.runtimeData = runtimeData;
            this.catalogService = catalogService;
            this.inputReader = inputReader;
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

            if (inputReader != null)
            {
                inputReader.onMouseWheelScrolled += HandleMouseWheelScrolled;
            }
        }

        public void Dispose()
        {
            if (catalogService != null)
            {
                catalogService.OnUnitUnlocked -= HandleUnitUnlocked;
            }

            if (inputReader != null)
            {
                inputReader.onMouseWheelScrolled -= HandleMouseWheelScrolled;
            }
        }

        private void HandleMouseWheelScrolled(float scrollDelta)
        {
            // 휠 위로: 다음(1), 휠 아래로: 이전(-1)
            int direction = scrollDelta > 0f ? 1 : -1;
            CycleSlot(direction);
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
            if (catalogService == null) return;

            var unlockedUnits = catalogService.GetUnlockedUnits();
            if (unlockedUnits == null || unlockedUnits.Count == 0) return;

            // 숫자키 슬롯 선택
            if (keyboard != null)
            {
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

            // InputReader가 없는 경우의 마우스 휠 Fallback 처리
            if (inputReader == null && Mouse.current != null)
            {
                float scroll = Mouse.current.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    int direction = scroll > 0f ? 1 : -1;
                    CycleSlot(direction);
                }
            }
        }

        public void CycleSlot(int direction)
        {
            if (catalogService == null) return;

            var unlockedUnits = catalogService.GetUnlockedUnits();
            if (unlockedUnits == null || unlockedUnits.Count <= 1) return;

            int currentIndex = runtimeData.SelectedSlotIndex;
            if (currentIndex < 0) currentIndex = 0;

            int nextIndex = (currentIndex + direction) % unlockedUnits.Count;
            if (nextIndex < 0) nextIndex += unlockedUnits.Count;

            SelectSlot(nextIndex, unlockedUnits[nextIndex]);
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
