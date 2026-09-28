using System;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace UnitSystem
{
    /// <summary>
    /// 단위 마법 퀵슬롯 UI View 컴포넌트입니다.
    /// RuntimeDataUnitQuickSlot의 변경 이벤트를 수신하여 슬롯 아이콘, 마나 코스트, 하이라이트를 수동 갱신합니다.
    /// </summary>
    public class UnitQuickSlotUIComponent : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private string iconElementName = "SelectedUnitIcon";
        [SerializeField] private string manaCostLabelName = "ManaCostText";
        [SerializeField] private string highlightOutlineName = "HighlightOutline";

        private VisualElement selectedUnitIconElement;
        private Label manaCostLabel;
        private VisualElement highlightOutlineElement;

        private IUnitCatalogService catalogService;
        private RuntimeDataUnitQuickSlot runtimeData;
        private PureDataUnit currentSelectedUnit;

        public PureDataUnit CurrentSelectedUnit => runtimeData != null && runtimeData.SelectedUnit != null ? runtimeData.SelectedUnit : currentSelectedUnit;
        public UnitType CurrentUnitType => CurrentSelectedUnit != null ? CurrentSelectedUnit.UnitType : UnitType.None;
        public IUnitCatalogService CatalogService => catalogService;
        public RuntimeDataUnitQuickSlot RuntimeData => runtimeData;

        public event Action<PureDataUnit> OnSelectedUnitChanged;

        [Inject]
        public void Construct(IUnitCatalogService catalogService, RuntimeDataUnitQuickSlot runtimeData = null)
        {
            UnbindEvents();
            this.catalogService = catalogService;
            this.runtimeData = runtimeData;
            BindEvents();

            if (CurrentSelectedUnit != null)
            {
                UpdateUI(CurrentSelectedUnit);
            }
        }

        private void OnEnable()
        {
            InitUIElements();
            BindEvents();

            if (CurrentSelectedUnit != null)
            {
                UpdateUI(CurrentSelectedUnit);
            }
        }

        private void Start()
        {
            InitUIElements();

            // 초기 기동 시 기본 선택 유닛 동기화
            if (CurrentSelectedUnit == null && catalogService != null)
            {
                var equippedUnits = catalogService.GetEquippedUnits();
                if (equippedUnits != null && equippedUnits.Count > 0)
                {
                    SelectSlot(equippedUnits[0]);
                }
            }
            else
            {
                UpdateUI(CurrentSelectedUnit);
            }
        }

        private void OnDisable()
        {
            UnbindEvents();
        }

        private void OnDestroy()
        {
            UnbindEvents();
        }

        private void InitUIElements()
        {
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }

            if (uiDocument != null && uiDocument.rootVisualElement != null)
            {
                var root = uiDocument.rootVisualElement;
                root.pickingMode = PickingMode.Ignore;

                selectedUnitIconElement = root.Q<VisualElement>(iconElementName);
                manaCostLabel = root.Q<Label>(manaCostLabelName);
                highlightOutlineElement = root.Q<VisualElement>(highlightOutlineName);
            }
        }

        private void BindEvents()
        {
            if (runtimeData != null)
            {
                runtimeData.OnSelectedUnitChanged -= HandleSelectedUnitChanged;
                runtimeData.OnSelectedUnitChanged += HandleSelectedUnitChanged;
            }

            if (catalogService != null)
            {
                catalogService.OnUnitEquipped -= HandleUnitEquipped;
                catalogService.OnUnitEquipped += HandleUnitEquipped;
                catalogService.OnUnitUnequipped -= HandleUnitUnequipped;
                catalogService.OnUnitUnequipped += HandleUnitUnequipped;
            }
        }

        private void UnbindEvents()
        {
            if (runtimeData != null)
            {
                runtimeData.OnSelectedUnitChanged -= HandleSelectedUnitChanged;
            }

            if (catalogService != null)
            {
                catalogService.OnUnitEquipped -= HandleUnitEquipped;
                catalogService.OnUnitUnequipped -= HandleUnitUnequipped;
            }
        }

        private void HandleSelectedUnitChanged(PureDataUnit unitData)
        {
            currentSelectedUnit = unitData;
            UpdateUI(unitData);
            OnSelectedUnitChanged?.Invoke(unitData);
        }

        private void HandleUnitEquipped(PureDataUnit equippedUnit)
        {
            // 아직 선택된 유닛이 없다면 새로 장착된 유닛 선택
            if (CurrentSelectedUnit == null && equippedUnit != null)
            {
                SelectSlot(equippedUnit);
            }
        }

        private void HandleUnitUnequipped(PureDataUnit unequippedUnit)
        {
            // 방금 해제된 유닛이 현재 선택 중이었다면 선택을 비운다(퀵슬롯 순환 대상에서 제외).
            if (unequippedUnit == null || CurrentSelectedUnit != unequippedUnit) return;

            var equippedUnits = catalogService != null ? catalogService.GetEquippedUnits() : null;
            if (equippedUnits != null && equippedUnits.Count > 0)
            {
                SelectSlot(equippedUnits[0]);
            }
            else
            {
                SelectSlot((PureDataUnit)null);
            }
        }

        public void SelectSlot(PureDataUnit unitData)
        {
            if (unitData == null)
            {
                if (runtimeData != null)
                {
                    runtimeData.ClearSelection();
                }
                else
                {
                    currentSelectedUnit = null;
                    UpdateUI(null);
                    OnSelectedUnitChanged?.Invoke(null);
                }
                return;
            }

            if (catalogService != null && !catalogService.IsEquipped(unitData))
            {
                Debug.LogWarning($"[UnitQuickSlotUIComponent] Unit {unitData.name} is not equipped!");
                return;
            }

            if (runtimeData != null)
            {
                var equippedUnits = catalogService != null ? catalogService.GetEquippedUnits() : null;
                int slotIndex = -1;
                if (equippedUnits != null)
                {
                    for (int i = 0; i < equippedUnits.Count; i++)
                    {
                        if (equippedUnits[i] == unitData)
                        {
                            slotIndex = i;
                            break;
                        }
                    }
                }
                runtimeData.SelectUnit(unitData, slotIndex);
            }
            else
            {
                currentSelectedUnit = unitData;
                UpdateUI(currentSelectedUnit);
                OnSelectedUnitChanged?.Invoke(currentSelectedUnit);
            }
        }

        public void SelectSlot(int slotIndex)
        {
            if (catalogService == null) return;

            var equippedUnits = catalogService.GetEquippedUnits();
            if (equippedUnits != null && slotIndex >= 0 && slotIndex < equippedUnits.Count)
            {
                SelectSlot(equippedUnits[slotIndex]);
            }
        }

        private void UpdateUI(PureDataUnit unitData)
        {
            if (selectedUnitIconElement != null)
            {
                if (unitData != null && unitData.Icon != null)
                {
                    selectedUnitIconElement.style.backgroundImage = new StyleBackground(unitData.Icon);
                }
                else
                {
                    selectedUnitIconElement.style.backgroundImage = StyleKeyword.Null;
                }
            }

            if (manaCostLabel != null)
            {
                manaCostLabel.text = unitData != null ? $"Cost: {unitData.BaseCost}" : "Cost: 0";
            }

            if (highlightOutlineElement != null)
            {
                highlightOutlineElement.style.display = unitData != null ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }
}
