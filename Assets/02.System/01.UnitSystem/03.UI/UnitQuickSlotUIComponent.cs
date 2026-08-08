using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using VContainer;

namespace UnitSystem
{
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
        private PureDataUnit currentSelectedUnit;

        public PureDataUnit CurrentSelectedUnit => currentSelectedUnit;

        [Inject]
        public void Construct(IUnitCatalogService catalogService)
        {
            this.catalogService = catalogService;
        }

        private void OnEnable()
        {
            if (uiDocument != null && uiDocument.rootVisualElement != null)
            {
                var root = uiDocument.rootVisualElement;
                selectedUnitIconElement = root.Q<VisualElement>(iconElementName);
                manaCostLabel = root.Q<Label>(manaCostLabelName);
                highlightOutlineElement = root.Q<VisualElement>(highlightOutlineName);
            }
        }

        private void Update()
        {
            HandleInput();
        }

        private readonly Key[] quickSlotKeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9 };

        private void HandleInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || catalogService == null) return;

            var unlockedUnits = catalogService.GetUnlockedUnits();

            for (int i = 0; i < unlockedUnits.Count && i < quickSlotKeys.Length; i++)
            {
                if (keyboard[quickSlotKeys[i]].wasPressedThisFrame)
                {
                    SelectSlot(unlockedUnits[i]);
                    break;
                }
            }
        }

        public void SelectSlot(PureDataUnit unitData)
        {
            if (catalogService == null || unitData == null) return;
            if (!catalogService.IsUnlocked(unitData))
            {
                Debug.LogWarning($"[UnitQuickSlotUIComponent] Unit {unitData.name} is locked!");
                return;
            }

            currentSelectedUnit = unitData;
            
            if (currentSelectedUnit != null)
            {
                if (selectedUnitIconElement != null && currentSelectedUnit.Icon != null)
                {
                    selectedUnitIconElement.style.backgroundImage = new StyleBackground(currentSelectedUnit.Icon);
                }

                if (manaCostLabel != null)
                {
                    manaCostLabel.text = $"Cost: {currentSelectedUnit.BaseCost}";
                }

                if (highlightOutlineElement != null)
                {
                    highlightOutlineElement.style.display = DisplayStyle.Flex;
                }
            }
        }
    }
}
