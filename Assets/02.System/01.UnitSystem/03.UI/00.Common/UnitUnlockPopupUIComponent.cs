using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using CharacterSystem;

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

        // 33번: 습득 팝업은 카탈로그가 아니라 플레이어 인벤토리의 항목 추가 이벤트(마법 항목)를 구독한다.
        private RuntimeDataInventory inventory;

        [Inject]
        public void Construct(RuntimeDataInventory inventory)
        {
            if (this.inventory != null)
            {
                this.inventory.OnItemAdded -= HandleItemAdded;
            }

            this.inventory = inventory;
            if (this.inventory != null)
            {
                this.inventory.OnItemAdded += HandleItemAdded;
            }
        }

        private void OnEnable()
        {
            if (inventory != null)
            {
                inventory.OnItemAdded -= HandleItemAdded;
                inventory.OnItemAdded += HandleItemAdded;
            }

            InitUI();
        }

        private void Start()
        {
            InitUI();
        }

        private void InitUI()
        {
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }

            if (uiDocument != null && uiDocument.rootVisualElement != null)
            {
                var root = uiDocument.rootVisualElement;
                root.pickingMode = PickingMode.Ignore;

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

        private void OnDisable()
        {
            CancelInvoke(nameof(ClosePopup));
            if (inventory != null)
            {
                inventory.OnItemAdded -= HandleItemAdded;
            }
        }

        private void OnDestroy()
        {
            CancelInvoke(nameof(ClosePopup));
            if (inventory != null)
            {
                inventory.OnItemAdded -= HandleItemAdded;
            }
        }

        // 마법 항목(ItemType.Magic인 PureDataUnit)만 습득 팝업을 띄운다.
        private void HandleItemAdded(IInventoryItem item)
        {
            if (item == null || item.ItemType != ItemType.Magic) return;
            if (item is PureDataUnit unitData) ShowUnlockPopup(unitData);
        }

        public void ShowUnlockPopup(PureDataUnit unitData)
        {
            if (unitData == null) return;

            if (popupTitleLabel != null) popupTitleLabel.text = $"Unit Acquired: {unitData.UnitName}";
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
