using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace UnitSystem
{
    public class UnitUnlockPopupUIComponent : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private string popupPanelName = "PopupPanel";
        [SerializeField] private string popupTitleLabelName = "PopupTitleText";
        [SerializeField] private string popupIconElementName = "PopupIconImage";

        [SerializeField] private float autoCloseDuration = 2.0f;

        private VisualElement popupPanel;
        private Label popupTitleLabel;
        private VisualElement popupIconElement;

        private IUnitCatalogService catalogService;

        [Inject]
        public void Construct(IUnitCatalogService catalogService)
        {
            this.catalogService = catalogService;
            if (this.catalogService != null)
            {
                this.catalogService.OnUnitUnlocked += ShowUnlockPopup;
            }
        }

        private void OnEnable()
        {
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }

            if (uiDocument != null && uiDocument.rootVisualElement != null)
            {
                var root = uiDocument.rootVisualElement;
                popupPanel = root.Q<VisualElement>(popupPanelName);
                popupTitleLabel = root.Q<Label>(popupTitleLabelName);
                popupIconElement = root.Q<VisualElement>(popupIconElementName);

                if (popupPanel != null)
                {
                    popupPanel.UnregisterCallback<ClickEvent>(OnPopupClicked);
                    popupPanel.RegisterCallback<ClickEvent>(OnPopupClicked);
                }
            }
        }

        private void OnPopupClicked(ClickEvent evt)
        {
            ClosePopup();
        }

        private void OnDestroy()
        {
            if (catalogService != null)
            {
                catalogService.OnUnitUnlocked -= ShowUnlockPopup;
            }
        }

        public void ShowUnlockPopup(PureDataUnit unitData)
        {
            if (catalogService == null || unitData == null) return;

            if (popupTitleLabel != null) popupTitleLabel.text = $"Unit Unlocked: {unitData.UnitName}";
            if (popupIconElement != null && unitData.Icon != null) popupIconElement.style.backgroundImage = new StyleBackground(unitData.Icon);
            if (popupPanel != null) popupPanel.style.display = DisplayStyle.Flex;

            Debug.Log($"[UnitUnlockPopupUIComponent] Popup Displayed for {unitData.name}");

            // 자동 닫기 타이머 실행
            CancelInvoke(nameof(ClosePopup));
            Invoke(nameof(ClosePopup), autoCloseDuration);
        }

        public void ClosePopup()
        {
            CancelInvoke(nameof(ClosePopup));
            if (popupPanel != null) popupPanel.style.display = DisplayStyle.None;
        }
    }
}
