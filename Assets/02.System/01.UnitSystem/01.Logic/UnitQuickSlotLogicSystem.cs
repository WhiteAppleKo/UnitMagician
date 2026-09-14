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

        private readonly Synty.AnimationBaseLocomotion.Samples.InputSystem.InputReader inputReader;
        private InputAction[] quickSlotActions;

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
            SetupInputActions();

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
            DisposeInputActions();

            if (catalogService != null)
            {
                catalogService.OnUnitUnlocked -= HandleUnitUnlocked;
            }

            if (inputReader != null)
            {
                inputReader.onMouseWheelScrolled -= HandleMouseWheelScrolled;
            }
        }

        private void SetupInputActions()
        {
            quickSlotActions = new InputAction[9];
            for (int i = 0; i < 9; i++)
            {
                int slotIndex = i;
                int digit = i + 1;
                var action = new InputAction($"QuickSlot_{digit}", InputActionType.Button);
                action.AddBinding($"<Keyboard>/{digit}");
                action.AddBinding($"<Keyboard>/numpad{digit}");
                action.performed += _ => OnQuickSlotKeyPressed(slotIndex);
                action.Enable();
                quickSlotActions[i] = action;
            }
        }

        private void DisposeInputActions()
        {
            if (quickSlotActions != null)
            {
                for (int i = 0; i < quickSlotActions.Length; i++)
                {
                    if (quickSlotActions[i] != null)
                    {
                        quickSlotActions[i].Disable();
                        quickSlotActions[i].Dispose();
                    }
                }
                quickSlotActions = null;
            }
        }

        private void OnQuickSlotKeyPressed(int slotIndex)
        {
            if (catalogService == null) return;
            var unlockedUnits = catalogService.GetUnlockedUnits();
            if (unlockedUnits == null || slotIndex >= unlockedUnits.Count) return;

            SelectSlot(slotIndex, unlockedUnits[slotIndex]);
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
            // 숫자키 슬롯 선택은 InputAction 이벤트 콜백으로 처리되므로 루프 폴링 제거됨
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
