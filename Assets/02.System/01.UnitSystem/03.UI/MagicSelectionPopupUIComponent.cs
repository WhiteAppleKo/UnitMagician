using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using VContainer;

namespace UnitSystem
{
    /// <summary>
    /// MagicSelectionPopupLogicSystem의 이벤트를 구독해 UnitUI.uxml의 Q/W/E/F 선택지 팝업
    /// (MagicSelectionPopupContainer)을 표시/숨김하는 UI 컴포넌트입니다(12번 기획서).
    /// Tactical 입력 컨텍스트 + 팝업이 열려 있을 때만 Q/W/E/F 키를 폴링해 로직 시스템의 공개 메서드를
    /// 호출할 뿐, 스스로 판단 로직을 갖지 않습니다(DLV Rule 2 — Blind Logic/순수 View).
    /// </summary>
    public class MagicSelectionPopupUIComponent : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private string popupContainerName = "MagicSelectionPopupContainer";
        [SerializeField] private string fullListContainerName = "MagicFullListContainer";
        [SerializeField] private string fullListGridName = "MagicFullListGrid";

        private static readonly string[] FavoriteSlotElementNames = { "FavoriteSlotW", "FavoriteSlotE", "FavoriteSlotF" };
        private static readonly string[] FavoriteSlotIconNames = { "FavoriteSlotIconW", "FavoriteSlotIconE", "FavoriteSlotIconF" };
        private static readonly string[] FavoriteSlotLabelNames = { "FavoriteSlotLabelW", "FavoriteSlotLabelE", "FavoriteSlotLabelF" };

        private static readonly Color EmptySlotTint = new Color(0.25f, 0.25f, 0.25f, 0.6f);
        private static readonly Color FilledSlotTint = new Color(0.15f, 0.15f, 0.22f, 0.95f);
        private static readonly Color FullListEntryBorder = new Color(0.83f, 0.69f, 0.22f, 1f);

        private IMagicSelectionPopupLogic popupLogic;
        private RuntimeDataMagicFavoriteSlots favoriteSlots;
        private Common.InputSystem.IInputContextManager inputContextManager;

        private VisualElement popupContainer;
        private VisualElement fullListContainer;
        private VisualElement fullListGrid;
        private readonly VisualElement[] favoriteSlotElements = new VisualElement[RuntimeDataMagicFavoriteSlots.SlotCount];
        private readonly VisualElement[] favoriteSlotIcons = new VisualElement[RuntimeDataMagicFavoriteSlots.SlotCount];
        private readonly Label[] favoriteSlotLabels = new Label[RuntimeDataMagicFavoriteSlots.SlotCount];

        [Inject]
        public void Construct(
            IMagicSelectionPopupLogic popupLogic,
            RuntimeDataMagicFavoriteSlots favoriteSlots = null,
            Common.InputSystem.IInputContextManager inputContextManager = null)
        {
            UnbindEvents();

            this.popupLogic = popupLogic;
            this.favoriteSlots = favoriteSlots;
            this.inputContextManager = inputContextManager;

            BindEvents();
            RefreshFavoriteSlots();
            ApplyPopupVisibility();
            ApplyFullListVisibility();
        }

        private void OnEnable()
        {
            InitContainer();
            BindEvents();
            RefreshFavoriteSlots();
            ApplyPopupVisibility();
            ApplyFullListVisibility();
        }

        private void Start()
        {
            InitContainer();
            RefreshFavoriteSlots();
            ApplyPopupVisibility();
            ApplyFullListVisibility();
        }

        private void OnDisable()
        {
            UnbindEvents();
        }

        private void OnDestroy()
        {
            UnbindEvents();
        }

        private void InitContainer()
        {
            if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null || uiDocument.rootVisualElement == null) return;

            var root = uiDocument.rootVisualElement;
            root.pickingMode = PickingMode.Ignore;

            popupContainer = root.Q<VisualElement>(popupContainerName);
            fullListContainer = root.Q<VisualElement>(fullListContainerName);
            fullListGrid = root.Q<VisualElement>(fullListGridName);

            for (int i = 0; i < RuntimeDataMagicFavoriteSlots.SlotCount; i++)
            {
                favoriteSlotElements[i] = root.Q<VisualElement>(FavoriteSlotElementNames[i]);
                favoriteSlotIcons[i] = root.Q<VisualElement>(FavoriteSlotIconNames[i]);
                favoriteSlotLabels[i] = root.Q<Label>(FavoriteSlotLabelNames[i]);
            }
        }

        private void BindEvents()
        {
            if (popupLogic != null)
            {
                popupLogic.OnPopupOpened -= HandlePopupOpened;
                popupLogic.OnPopupOpened += HandlePopupOpened;
                popupLogic.OnPopupClosed -= HandlePopupClosed;
                popupLogic.OnPopupClosed += HandlePopupClosed;
                popupLogic.OnFullListToggled -= HandleFullListToggled;
                popupLogic.OnFullListToggled += HandleFullListToggled;
            }

            if (favoriteSlots != null)
            {
                favoriteSlots.OnFavoriteSlotChanged -= HandleFavoriteSlotChanged;
                favoriteSlots.OnFavoriteSlotChanged += HandleFavoriteSlotChanged;
            }
        }

        private void UnbindEvents()
        {
            if (popupLogic != null)
            {
                popupLogic.OnPopupOpened -= HandlePopupOpened;
                popupLogic.OnPopupClosed -= HandlePopupClosed;
                popupLogic.OnFullListToggled -= HandleFullListToggled;
            }

            if (favoriteSlots != null)
            {
                favoriteSlots.OnFavoriteSlotChanged -= HandleFavoriteSlotChanged;
            }
        }

        private void HandlePopupOpened()
        {
            RefreshFavoriteSlots();
            ApplyPopupVisibility();
        }

        private void HandlePopupClosed()
        {
            ApplyPopupVisibility();
            ApplyFullListVisibility();
        }

        private void HandleFullListToggled(bool isOpen)
        {
            ApplyFullListVisibility();
            if (isOpen)
            {
                RefreshFullList();
            }
        }

        private void HandleFavoriteSlotChanged(int index, PureDataUnit unit)
        {
            RefreshFavoriteSlots();
        }

        private void ApplyPopupVisibility()
        {
            if (popupContainer == null) return;

            bool open = popupLogic != null && popupLogic.IsPopupOpen;
            popupContainer.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void ApplyFullListVisibility()
        {
            if (fullListContainer == null) return;

            bool open = popupLogic != null && popupLogic.IsPopupOpen && popupLogic.IsFullListOpen;
            fullListContainer.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void RefreshFavoriteSlots()
        {
            if (favoriteSlots == null) return;

            for (int i = 0; i < RuntimeDataMagicFavoriteSlots.SlotCount; i++)
            {
                var unit = favoriteSlots.GetSlot(i);
                ApplyFavoriteSlotVisual(i, unit);
            }
        }

        private void ApplyFavoriteSlotVisual(int index, PureDataUnit unit)
        {
            var slotElement = favoriteSlotElements[index];
            var iconElement = favoriteSlotIcons[index];
            var labelElement = favoriteSlotLabels[index];

            if (slotElement != null)
            {
                slotElement.style.backgroundColor = new StyleColor(unit != null ? FilledSlotTint : EmptySlotTint);
                slotElement.style.opacity = unit != null ? 1f : 0.5f;
            }

            if (iconElement != null)
            {
                iconElement.style.backgroundImage = unit != null && unit.Icon != null
                    ? new StyleBackground(unit.Icon)
                    : new StyleBackground();
            }

            if (labelElement != null)
            {
                labelElement.text = unit != null ? unit.UnitName : "비어있음";
            }
        }

        private void RefreshFullList()
        {
            if (fullListGrid == null || popupLogic == null) return;

            fullListGrid.Clear();
            var equippedUnits = popupLogic.GetEquippedUnitsForFullList();
            if (equippedUnits == null) return;

            foreach (var unitData in equippedUnits)
            {
                if (unitData == null) continue;
                fullListGrid.Add(BuildFullListEntry(unitData));
            }
        }

        private VisualElement BuildFullListEntry(PureDataUnit unitData)
        {
            var slotElement = new VisualElement();
            slotElement.style.width = 64;
            slotElement.style.marginRight = 8;
            slotElement.style.marginBottom = 8;
            slotElement.style.alignItems = Align.Center;

            var iconElement = new VisualElement();
            iconElement.style.width = 48;
            iconElement.style.height = 48;
            iconElement.style.backgroundColor = new StyleColor(new Color(0.2f, 0.2f, 0.2f, 0.9f));
            iconElement.style.borderTopLeftRadius = 4;
            iconElement.style.borderTopRightRadius = 4;
            iconElement.style.borderBottomLeftRadius = 4;
            iconElement.style.borderBottomRightRadius = 4;
            iconElement.style.borderTopWidth = 1;
            iconElement.style.borderBottomWidth = 1;
            iconElement.style.borderLeftWidth = 1;
            iconElement.style.borderRightWidth = 1;
            iconElement.style.borderTopColor = FullListEntryBorder;
            iconElement.style.borderBottomColor = FullListEntryBorder;
            iconElement.style.borderLeftColor = FullListEntryBorder;
            iconElement.style.borderRightColor = FullListEntryBorder;

            if (unitData.Icon != null)
            {
                iconElement.style.backgroundImage = new StyleBackground(unitData.Icon);
            }

            PureDataUnit captureUnit = unitData;
            iconElement.RegisterCallback<ClickEvent>(_ => popupLogic?.ConfirmUnit(captureUnit));

            var nameLabel = new Label(unitData.UnitName);
            nameLabel.style.color = new StyleColor(Color.white);
            nameLabel.style.fontSize = 11;
            nameLabel.style.marginTop = 2;
            nameLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            nameLabel.pickingMode = PickingMode.Ignore;

            slotElement.Add(iconElement);
            slotElement.Add(nameLabel);
            return slotElement;
        }

        private void Update()
        {
            if (popupLogic == null || !popupLogic.IsPopupOpen) return;

            var currentContext = inputContextManager?.CurrentContext;
            bool isTactical = currentContext != null && currentContext.ContextType == Common.InputSystem.InputContextType.Tactical;
            if (!isTactical) return;

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.qKey.wasPressedThisFrame)
            {
                if (popupLogic.IsFullListOpen) popupLogic.CloseFullList();
                else popupLogic.OpenFullList();
            }

            if (keyboard.wKey.wasPressedThisFrame) popupLogic.ConfirmFavoriteSlot(0);
            if (keyboard.eKey.wasPressedThisFrame) popupLogic.ConfirmFavoriteSlot(1);
            if (keyboard.fKey.wasPressedThisFrame) popupLogic.ConfirmFavoriteSlot(2);
        }
    }
}
