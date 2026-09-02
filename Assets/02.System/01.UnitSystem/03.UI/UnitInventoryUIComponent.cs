using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace UnitSystem
{
    public class UnitInventoryUIComponent : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private string containerElementName = "InventoryContainer";

        private VisualElement inventoryContainer;
        private IUnitCatalogService catalogService;

        [Inject]
        public void Construct(IUnitCatalogService catalogService)
        {
            if (this.catalogService != null)
            {
                this.catalogService.OnUnitUnlocked -= HandleUnitUnlocked;
            }

            this.catalogService = catalogService;
            if (this.catalogService != null)
            {
                this.catalogService.OnUnitUnlocked += HandleUnitUnlocked;
            }

            RefreshInventory();
        }

        private void OnEnable()
        {
            if (catalogService != null)
            {
                catalogService.OnUnitUnlocked -= HandleUnitUnlocked;
                catalogService.OnUnitUnlocked += HandleUnitUnlocked;
            }

            InitContainer();
            RefreshInventory();
        }

        private void Start()
        {
            InitContainer();
            RefreshInventory();
        }

        private void InitContainer()
        {
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }

            if (uiDocument != null && uiDocument.rootVisualElement != null)
            {
                uiDocument.rootVisualElement.pickingMode = PickingMode.Ignore;
                inventoryContainer = uiDocument.rootVisualElement.Q<VisualElement>(containerElementName);
            }
        }

        private void OnDisable()
        {
            if (catalogService != null)
            {
                catalogService.OnUnitUnlocked -= HandleUnitUnlocked;
            }
        }

        private void OnDestroy()
        {
            if (catalogService != null)
            {
                catalogService.OnUnitUnlocked -= HandleUnitUnlocked;
            }
        }

        private void HandleUnitUnlocked(PureDataUnit unlockedUnit)
        {
            RefreshInventory();
        }

        public void RefreshInventory()
        {
            if (catalogService == null) return;

            var unlockedUnits = catalogService.GetUnlockedUnits();
            Debug.Log($"[UnitInventoryUIComponent] Inventory Refreshed. Unlocked Count: {unlockedUnits.Count}");

            if (inventoryContainer != null)
            {
                inventoryContainer.Clear();
                foreach (var unitData in unlockedUnits)
                {
                    var unitElement = new VisualElement();
                    unitElement.style.width = 48;
                    unitElement.style.height = 48;
                    unitElement.style.marginRight = 8;
                    unitElement.style.marginBottom = 8;
                    unitElement.style.backgroundColor = new StyleColor(new Color(0.2f, 0.2f, 0.2f, 0.8f));
                    unitElement.style.borderTopLeftRadius = 4;
                    unitElement.style.borderTopRightRadius = 4;
                    unitElement.style.borderBottomLeftRadius = 4;
                    unitElement.style.borderBottomRightRadius = 4;

                    if (unitData.Icon != null)
                    {
                        unitElement.style.backgroundImage = new StyleBackground(unitData.Icon);
                    }
                    inventoryContainer.Add(unitElement);
                }
            }
        }
    }
}
