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
        private readonly Common.InputSystem.IInputContextManager contextManager;
        private InputAction[] quickSlotActions;

        [Inject]
        public UnitQuickSlotLogicSystem(
            RuntimeDataUnitQuickSlot runtimeData,
            IUnitCatalogService catalogService,
            Synty.AnimationBaseLocomotion.Samples.InputSystem.InputReader inputReader = null,
            Common.InputSystem.IInputContextManager contextManager = null)
        {
            this.runtimeData = runtimeData;
            this.catalogService = catalogService;
            this.inputReader = inputReader;
            this.contextManager = contextManager;
        }

        public void Initialize()
        {
            SetupInputActions();

            if (catalogService != null)
            {
                catalogService.OnUnitEquipped -= HandleUnitEquipped;
                catalogService.OnUnitEquipped += HandleUnitEquipped;
                catalogService.OnUnitUnequipped -= HandleUnitUnequipped;
                catalogService.OnUnitUnequipped += HandleUnitUnequipped;

                // 초기 장착 유닛 중 첫 번째 유닛 기본 선택
                if (runtimeData.SelectedUnit == null)
                {
                    var equippedUnits = catalogService.GetEquippedUnits();
                    if (equippedUnits != null && equippedUnits.Count > 0)
                    {
                        SelectSlot(0, equippedUnits[0]);
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
                catalogService.OnUnitEquipped -= HandleUnitEquipped;
                catalogService.OnUnitUnequipped -= HandleUnitUnequipped;
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
            var equippedUnits = catalogService.GetEquippedUnits();
            if (equippedUnits == null || slotIndex >= equippedUnits.Count) return;

            SelectSlot(slotIndex, equippedUnits[slotIndex]);
        }

        private void HandleMouseWheelScrolled(float scrollDelta)
        {
            // 전술(시간 정지) 컨텍스트에서만 휠로 퀵슬롯 순환 (그 외에는 카메라 줌이 휠을 사용)
            var currentContext = contextManager?.CurrentContext;
            if (currentContext != null && currentContext.ContextType != Common.InputSystem.InputContextType.Tactical)
            {
                return;
            }

            // 휠 위로: 다음(1), 휠 아래로: 이전(-1)
            int direction = scrollDelta > 0f ? 1 : -1;
            CycleSlot(direction);
        }

        private void HandleUnitEquipped(PureDataUnit equippedUnit)
        {
            if (equippedUnit == null) return;

            // 현재 선택된 유닛이 없으면 새로 장착된 유닛을 기본 선택
            if (runtimeData.SelectedUnit == null)
            {
                SelectUnit(equippedUnit);
            }
        }

        private void HandleUnitUnequipped(PureDataUnit unequippedUnit)
        {
            if (unequippedUnit == null) return;

            // 방금 해제된 유닛이 현재 선택 중이었다면, 다른 장착 유닛으로 전환하거나 선택을 비운다.
            if (runtimeData.SelectedUnit != unequippedUnit) return;

            var equippedUnits = catalogService?.GetEquippedUnits();
            if (equippedUnits != null && equippedUnits.Count > 0)
            {
                SelectSlot(0, equippedUnits[0]);
            }
            else
            {
                runtimeData.ClearSelection();
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

            var equippedUnits = catalogService.GetEquippedUnits();
            if (equippedUnits == null || equippedUnits.Count <= 1) return;

            int currentIndex = runtimeData.SelectedSlotIndex;
            if (currentIndex < 0) currentIndex = 0;

            int nextIndex = (currentIndex + direction) % equippedUnits.Count;
            if (nextIndex < 0) nextIndex += equippedUnits.Count;

            SelectSlot(nextIndex, equippedUnits[nextIndex]);
        }

        public bool SelectSlot(int slotIndex, PureDataUnit unitData)
        {
            if (catalogService == null || unitData == null) return false;

            if (!catalogService.IsEquipped(unitData))
            {
                Debug.LogWarning($"[UnitQuickSlotLogicSystem] Unit {unitData.name} is not equipped!");
                return false;
            }

            runtimeData.SelectUnit(unitData, slotIndex);
            return true;
        }

        public bool SelectSlot(int slotIndex)
        {
            if (catalogService == null) return false;

            var equippedUnits = catalogService.GetEquippedUnits();
            if (slotIndex >= 0 && slotIndex < equippedUnits.Count)
            {
                return SelectSlot(slotIndex, equippedUnits[slotIndex]);
            }

            return false;
        }

        public bool SelectUnit(PureDataUnit unitData)
        {
            if (catalogService == null || unitData == null) return false;

            if (!catalogService.IsEquipped(unitData))
            {
                Debug.LogWarning($"[UnitQuickSlotLogicSystem] Unit {unitData.name} is not equipped!");
                return false;
            }

            var equippedUnits = catalogService.GetEquippedUnits();
            int index = -1;
            if (equippedUnits != null)
            {
                for (int i = 0; i < equippedUnits.Count; i++)
                {
                    if (equippedUnits[i] == unitData)
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
