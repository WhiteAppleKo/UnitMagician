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
                var unlockedUnits = catalogService.GetUnlockedUnits();
                if (unlockedUnits != null && unlockedUnits.Count > 0)
                {
                    SelectSlot(unlockedUnits[0]);
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
                catalogService.OnUnitUnlocked -= HandleUnitUnlocked;
                catalogService.OnUnitUnlocked += HandleUnitUnlocked;
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
                catalogService.OnUnitUnlocked -= HandleUnitUnlocked;
            }
        }

        private void HandleSelectedUnitChanged(PureDataUnit unitData)
        {
            currentSelectedUnit = unitData;
            UpdateUI(unitData);
            OnSelectedUnitChanged?.Invoke(unitData);
        }

        private void HandleUnitUnlocked(PureDataUnit unlockedUnit)
        {
            // 아직 선택된 유닛이 없다면 해금된 첫 유닛 선택
            if (CurrentSelectedUnit == null && unlockedUnit != null)
            {
                SelectSlot(unlockedUnit);
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

            if (catalogService != null && !catalogService.IsUnlocked(unitData))
            {
                Debug.LogWarning($"[UnitQuickSlotUIComponent] Unit {unitData.name} is locked!");
                return;
            }

            if (runtimeData != null)
            {
                var unlockedUnits = catalogService != null ? catalogService.GetUnlockedUnits() : null;
                int slotIndex = -1;
                if (unlockedUnits != null)
                {
                    for (int i = 0; i < unlockedUnits.Count; i++)
                    {
                        if (unlockedUnits[i] == unitData)
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

            var unlockedUnits = catalogService.GetUnlockedUnits();
            if (unlockedUnits != null && slotIndex >= 0 && slotIndex < unlockedUnits.Count)
            {
                SelectSlot(unlockedUnits[slotIndex]);
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
