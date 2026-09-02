using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace UnitSystem
{
    public class UnitUnlockTestUIComponent : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;

        private IUnitCatalogService catalogService;
        private VisualElement buttonContainer;

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

            GenerateUnlockButtons();
        }

        private void OnEnable()
        {
            if (catalogService != null)
            {
                catalogService.OnUnitUnlocked -= HandleUnitUnlocked;
                catalogService.OnUnitUnlocked += HandleUnitUnlocked;
            }
            GenerateUnlockButtons();
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

        private void Start()
        {
            GenerateUnlockButtons();
        }

        private void GenerateUnlockButtons()
        {
            if (catalogService == null) return;

            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }

            if (uiDocument != null && uiDocument.rootVisualElement != null)
            {
                uiDocument.rootVisualElement.pickingMode = PickingMode.Ignore;
                buttonContainer = uiDocument.rootVisualElement.Q<VisualElement>("UnlockButtonContainer");
                
                if (buttonContainer != null)
                {
                    buttonContainer.Clear(); // 기존 버튼 초기화

                    var allUnits = catalogService.GetAllUnits();
                    foreach (var unitData in allUnits)
                    {
                        if (unitData == null || catalogService.IsUnlocked(unitData)) 
                            continue;

                        Button newBtn = new Button();
                        newBtn.name = $"Unlock_{unitData.name}_Btn";
                        newBtn.text = $"Unlock {unitData.UnitName}";
                        newBtn.style.height = 36;
                        newBtn.style.width = 160;
                        newBtn.style.marginBottom = 6;
                        
                        PureDataUnit captureUnit = unitData;
                        newBtn.clicked += () => Unlock(captureUnit);

                        buttonContainer.Add(newBtn);
                    }
                }
            }
            else
            {
                Debug.LogWarning("[UnitUnlockTestUIComponent] UIDocument or rootVisualElement is null.");
            }
        }

        private void HandleUnitUnlocked(PureDataUnit unlockedUnit)
        {
            // 해금된 단위의 버튼 찾아서 제거
            if (buttonContainer != null && unlockedUnit != null)
            {
                var targetBtn = buttonContainer.Q<Button>($"Unlock_{unlockedUnit.name}_Btn");
                if (targetBtn != null)
                {
                    buttonContainer.Remove(targetBtn);
                }
            }
        }

        private void Unlock(PureDataUnit unit)
        {
            Debug.Log($"[UnitUnlockTestUIComponent] Mouse Clicked Unlock Button: {unit.name}");

            if (catalogService != null)
            {
                catalogService.UnlockUnit(unit);
            }
            else
            {
                Debug.LogError("[UnitUnlockTestUIComponent] CatalogService is null. Check VContainer LifetimeScope injection!");
            }
        }
    }
}
