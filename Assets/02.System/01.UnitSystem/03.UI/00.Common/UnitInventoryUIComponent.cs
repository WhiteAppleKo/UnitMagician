using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using VContainer;
using Synty.AnimationBaseLocomotion.Samples.InputSystem;

namespace UnitSystem
{
    /// <summary>
    /// 소유 중인 유닛(마법) 인벤토리를 행 목록(아이콘 + 이름 + 설명 + 장착 상태)으로 표시하고, 아이콘 클릭으로 장착/해제를 토글하는 UI 컴포넌트입니다.
    /// 행에 마우스를 올리면 우측 상세 패널에 이름/아이콘/전체 설명을 보여 줍니다(18번 기획서).
    /// 장착된 유닛은 강조 테두리, 미장착이지만 장착 가능한 유닛은 기본 테두리,
    /// 조건 미달로 장착 불가능한 유닛(현재는 스텁 체커라 발생하지 않음)은 회색+비활성으로 표시합니다.
    ///
    /// 다크소울3 레퍼런스에 맞춰 상시 표시 HUD가 아니라, 키 입력(기본 I)으로 열고 닫는 메뉴로 동작합니다.
    /// (Assets/02.System/04.OptionSystem/02.Visualizer/UI/OptionUIComponent.cs의 여닫는 메뉴 패턴을 그대로 따름)
    /// </summary>
    public class UnitInventoryUIComponent : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private string containerElementName = "InventoryContainer";

        private static readonly Color EquippedBorderColor = new Color(1f, 0.85f, 0.2f, 1f);
        private static readonly Color DefaultBorderColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        private static readonly Color DisabledTint = new Color(0.35f, 0.35f, 0.35f, 0.8f);
        private static readonly Color EnabledTint = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        private static readonly Color FavoriteBadgeColor = new Color(1f, 0.85f, 0.2f, 1f);
        private static readonly string[] FavoriteSlotKeyLabels = { "W", "E", "F" };

        private VisualElement inventoryContainer;
        private IUnitCatalogService catalogService;
        private IUnitEquipRequirementChecker requirementChecker;
        private RuntimeDataMagicFavoriteSlots favoriteSlots;
        // 이벤트(3단계 감속) 중 인벤토리 열기 차단용(선택적 주입 - 미주입이면 차단 없이 기존 동작).
        private CharacterSystem.RuntimeDataTimeSlow timeSlowData;
        private InputReader m_inputReader;
        private Common.InputSystem.IInputContextManager m_inputContextManager;

        // 18번 기획서 §6 A안: 행 형태 목록 + 호버 시 우측 상세 패널(이름/아이콘/전체 설명).
        private static readonly Color DescriptionColor = new Color(0.78f, 0.78f, 0.78f, 1f);
        private const float DescriptionListFontSize = 11f;
        private const float DescriptionListMaxHeight = 28f; // 11pt(줄 높이 약 13.5) 기준 2줄, 패딩 0 — 초과분은 말줄임
        private const float DetailPanelWidth = 190f;

        private VisualElement detailPanel;
        private PureDataUnit hoveredUnit;

        private bool m_isOpen = false;

        // 17번 기획서: Time.timeScale은 직접 쓰지 않고 일시정지 요청만 올리고 내린다(요청자 집합으로 멱등 관리).
        private CharacterSystem.ITimeScaleService timeScaleService;

        public bool IsOpen => m_isOpen;

        [Inject]
        public void Construct(
            IUnitCatalogService catalogService,
            IUnitEquipRequirementChecker requirementChecker = null,
            InputReader inputReader = null,
            Common.InputSystem.IInputContextManager contextManager = null,
            RuntimeDataMagicFavoriteSlots favoriteSlots = null,
            CharacterSystem.RuntimeDataTimeSlow timeSlowData = null,
            CharacterSystem.ITimeScaleService timeScaleService = null)
        {
            UnbindEvents();

            this.timeScaleService = timeScaleService;
            this.catalogService = catalogService;
            this.requirementChecker = requirementChecker;
            m_inputReader = inputReader;
            m_inputContextManager = contextManager;
            this.favoriteSlots = favoriteSlots;
            this.timeSlowData = timeSlowData;

            BindEvents();
            RefreshInventory();
        }

        private void OnEnable()
        {
            BindEvents();
            InitContainer();
            RefreshInventory();
        }

        private void Start()
        {
            InitContainer();
            RefreshInventory();
            CloseInventory();
        }

        private void Update()
        {
            // InputReader 미주입 시 폴백 처리
            if (m_inputReader == null && Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
            {
                ToggleInventory();
            }
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
            UnbindEvents();
        }

        private void OnDestroy()
        {
            UnbindEvents();
        }

        private void BindEvents()
        {
            if (catalogService != null)
            {
                catalogService.OnUnitAcquired -= HandleCatalogChanged;
                catalogService.OnUnitAcquired += HandleCatalogChanged;
                catalogService.OnUnitEquipped -= HandleCatalogChanged;
                catalogService.OnUnitEquipped += HandleCatalogChanged;
                catalogService.OnUnitUnequipped -= HandleCatalogChanged;
                catalogService.OnUnitUnequipped += HandleCatalogChanged;
            }

            if (m_inputReader != null)
            {
                m_inputReader.onInventoryToggled -= ToggleInventory;
                m_inputReader.onInventoryToggled += ToggleInventory;
            }

            if (favoriteSlots != null)
            {
                favoriteSlots.OnFavoriteSlotChanged -= HandleFavoriteSlotChanged;
                favoriteSlots.OnFavoriteSlotChanged += HandleFavoriteSlotChanged;
            }
        }

        private void UnbindEvents()
        {
            if (catalogService != null)
            {
                catalogService.OnUnitAcquired -= HandleCatalogChanged;
                catalogService.OnUnitEquipped -= HandleCatalogChanged;
                catalogService.OnUnitUnequipped -= HandleCatalogChanged;
            }

            if (m_inputReader != null)
            {
                m_inputReader.onInventoryToggled -= ToggleInventory;
            }

            if (favoriteSlots != null)
            {
                favoriteSlots.OnFavoriteSlotChanged -= HandleFavoriteSlotChanged;
            }
        }

        private void HandleFavoriteSlotChanged(int index, PureDataUnit unit)
        {
            RefreshInventory();
        }

        private void HandleCatalogChanged(PureDataUnit unitData)
        {
            RefreshInventory();
        }

        public void RefreshInventory()
        {
            if (catalogService == null) return;
            if (inventoryContainer == null) return;

            var possessedUnits = catalogService.GetPossessedUnits();
            Debug.Log($"[UnitInventoryUIComponent] Inventory Refreshed. Possessed Count: {possessedUnits.Count}");

            // 좌: 행 목록(스크롤), 우: 호버 상세 패널. 컨테이너는 UXML의 줄바꿈 그리드 대신 2열 레이아웃으로 쓴다.
            inventoryContainer.Clear();
            inventoryContainer.style.flexDirection = FlexDirection.Row;
            inventoryContainer.style.flexWrap = Wrap.NoWrap;

            var listView = new ScrollView(ScrollViewMode.Vertical);
            listView.style.flexGrow = 1;
            listView.style.flexShrink = 1;
            listView.RegisterCallback<PointerLeaveEvent>(_ => SetHoveredUnit(null));

            bool hoveredStillPossessed = false;
            foreach (var unitData in possessedUnits)
            {
                if (unitData == null) continue;

                if (unitData == hoveredUnit) hoveredStillPossessed = true;
                listView.Add(BuildUnitElement(unitData));
            }
            inventoryContainer.Add(listView);

            detailPanel = BuildDetailPanel();
            inventoryContainer.Add(detailPanel);

            // 클릭 등으로 목록이 다시 그려져도 포인터가 올라가 있던 마법의 상세는 유지한다.
            if (!hoveredStillPossessed) hoveredUnit = null;
            RefreshDetailPanel();
        }

        private VisualElement BuildDetailPanel()
        {
            var panel = new VisualElement();
            panel.pickingMode = PickingMode.Ignore;
            panel.style.width = DetailPanelWidth;
            panel.style.flexShrink = 0;
            panel.style.marginLeft = 12;
            panel.style.paddingTop = 10;
            panel.style.paddingBottom = 10;
            panel.style.paddingLeft = 10;
            panel.style.paddingRight = 10;
            panel.style.backgroundColor = new StyleColor(new Color(0.12f, 0.12f, 0.16f, 0.9f));
            panel.style.borderTopLeftRadius = 8;
            panel.style.borderTopRightRadius = 8;
            panel.style.borderBottomLeftRadius = 8;
            panel.style.borderBottomRightRadius = 8;
            panel.style.alignItems = Align.Center;
            panel.style.visibility = Visibility.Hidden;
            return panel;
        }

        private void SetHoveredUnit(PureDataUnit unitData)
        {
            hoveredUnit = unitData;
            RefreshDetailPanel();
        }

        /// <summary>호버 중인 마법의 이름 + 아이콘 + 전체 설명을 상세 패널에 그린다(설명이 비면 설명 요소를 만들지 않는다).</summary>
        private void RefreshDetailPanel()
        {
            if (detailPanel == null) return;

            detailPanel.Clear();
            if (hoveredUnit == null)
            {
                detailPanel.style.visibility = Visibility.Hidden;
                return;
            }

            var iconElement = new VisualElement();
            iconElement.pickingMode = PickingMode.Ignore;
            iconElement.style.width = 96;
            iconElement.style.height = 96;
            iconElement.style.marginBottom = 8;
            if (hoveredUnit.Icon != null)
            {
                iconElement.style.backgroundImage = new StyleBackground(hoveredUnit.Icon);
            }
            detailPanel.Add(iconElement);

            var nameLabel = new Label(hoveredUnit.UnitName);
            nameLabel.pickingMode = PickingMode.Ignore;
            nameLabel.style.color = new StyleColor(EquippedBorderColor);
            nameLabel.style.fontSize = 16;
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            nameLabel.style.whiteSpace = WhiteSpace.Normal;
            nameLabel.style.marginBottom = 6;
            detailPanel.Add(nameLabel);

            if (!string.IsNullOrEmpty(hoveredUnit.Description))
            {
                var descriptionLabel = new Label(hoveredUnit.Description);
                descriptionLabel.pickingMode = PickingMode.Ignore;
                descriptionLabel.style.color = new StyleColor(DescriptionColor);
                descriptionLabel.style.fontSize = 12;
                descriptionLabel.style.whiteSpace = WhiteSpace.Normal;
                descriptionLabel.style.unityTextAlign = TextAnchor.UpperLeft;
                detailPanel.Add(descriptionLabel);
            }

            detailPanel.style.visibility = Visibility.Visible;
        }

        private VisualElement BuildUnitElement(PureDataUnit unitData)
        {
            bool isEquipped = catalogService.IsEquipped(unitData);
            bool canEquip = isEquipped || requirementChecker == null || requirementChecker.CanEquip(unitData, out _);

            // 한 행 = 아이콘(클릭 대상) + 이름/설명 열 + 장착 상태. 행에 마우스를 올리면 우측 상세 패널을 갱신한다.
            var slotElement = new VisualElement();
            slotElement.style.flexDirection = FlexDirection.Row;
            slotElement.style.alignItems = Align.Center;
            slotElement.style.paddingTop = 6;
            slotElement.style.paddingBottom = 6;
            slotElement.style.paddingLeft = 8;
            slotElement.style.paddingRight = 8;
            slotElement.style.marginBottom = 4;
            slotElement.style.backgroundColor = new StyleColor(new Color(1f, 1f, 1f, 0.04f));
            slotElement.style.borderTopLeftRadius = 6;
            slotElement.style.borderTopRightRadius = 6;
            slotElement.style.borderBottomLeftRadius = 6;
            slotElement.style.borderBottomRightRadius = 6;
            PureDataUnit hoverUnit = unitData;
            slotElement.RegisterCallback<PointerEnterEvent>(_ => SetHoveredUnit(hoverUnit));

            var unitElement = new VisualElement();
            unitElement.style.flexShrink = 0;
            unitElement.style.width = 48;
            unitElement.style.height = 48;
            unitElement.style.backgroundColor = new StyleColor(canEquip ? EnabledTint : DisabledTint);
            unitElement.style.opacity = canEquip ? 1f : 0.5f;

            unitElement.style.borderTopLeftRadius = 4;
            unitElement.style.borderTopRightRadius = 4;
            unitElement.style.borderBottomLeftRadius = 4;
            unitElement.style.borderBottomRightRadius = 4;

            Color borderColor = isEquipped ? EquippedBorderColor : DefaultBorderColor;
            float borderWidth = isEquipped ? 3f : 1f;
            unitElement.style.borderTopWidth = borderWidth;
            unitElement.style.borderBottomWidth = borderWidth;
            unitElement.style.borderLeftWidth = borderWidth;
            unitElement.style.borderRightWidth = borderWidth;
            unitElement.style.borderTopColor = borderColor;
            unitElement.style.borderBottomColor = borderColor;
            unitElement.style.borderLeftColor = borderColor;
            unitElement.style.borderRightColor = borderColor;

            if (unitData.Icon != null)
            {
                unitElement.style.backgroundImage = new StyleBackground(unitData.Icon);
            }

            PureDataUnit captureUnit = unitData;
            unitElement.RegisterCallback<ClickEvent>(_ => OnUnitClicked(captureUnit));
            unitElement.RegisterCallback<ContextClickEvent>(_ => OnUnitContextClicked(captureUnit));

            string statusText = isEquipped ? "장착됨" : (canEquip ? "미장착" : "장착 불가");
            Color statusColor = isEquipped ? EquippedBorderColor : (canEquip ? DefaultBorderColor : DisabledTint);

            var textColumn = new VisualElement();
            textColumn.pickingMode = PickingMode.Ignore;
            textColumn.style.flexGrow = 1;
            textColumn.style.flexShrink = 1;
            textColumn.style.marginLeft = 10;
            textColumn.style.marginRight = 8;

            var nameLabel = new Label(unitData.UnitName);
            nameLabel.style.color = new StyleColor(Color.white);
            nameLabel.style.fontSize = 13;
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            nameLabel.pickingMode = PickingMode.Ignore;
            textColumn.Add(nameLabel);

            // 설명이 비어 있으면 설명 요소를 만들지 않는다(기존과 같은 모습).
            if (!string.IsNullOrEmpty(unitData.Description))
            {
                var descriptionLabel = new Label(unitData.Description);
                descriptionLabel.pickingMode = PickingMode.Ignore;
                descriptionLabel.style.color = new StyleColor(DescriptionColor);
                descriptionLabel.style.fontSize = DescriptionListFontSize;
                descriptionLabel.style.unityTextAlign = TextAnchor.UpperLeft;
                descriptionLabel.style.whiteSpace = WhiteSpace.Normal;
                descriptionLabel.style.overflow = Overflow.Hidden;
                descriptionLabel.style.textOverflow = TextOverflow.Ellipsis;
                descriptionLabel.style.maxHeight = DescriptionListMaxHeight; // 최대 2줄
                descriptionLabel.style.marginTop = 2;
                descriptionLabel.style.paddingTop = 0;
                descriptionLabel.style.paddingBottom = 0;
                textColumn.Add(descriptionLabel);
            }

            var statusLabel = new Label(statusText);
            statusLabel.style.color = new StyleColor(statusColor);
            statusLabel.style.fontSize = 10;
            statusLabel.style.flexShrink = 0;
            statusLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            statusLabel.pickingMode = PickingMode.Ignore;

            slotElement.Add(unitElement);
            slotElement.Add(textColumn);
            slotElement.Add(statusLabel);

            // 즐겨찾기(W/E/F) 등록 배지 — 인벤토리에서 장착 유닛을 우클릭해 RuntimeDataMagicFavoriteSlots에
            // 등록되면 아이콘 모서리에 "[W]"/"[E]"/"[F]" 라벨로 표시한다(12번 기획서 §3 시청각 전달).
            int favoriteIndex = favoriteSlots != null ? favoriteSlots.FindSlotOf(unitData) : -1;
            if (favoriteIndex >= 0)
            {
                var favoriteBadge = new Label($"[{FavoriteSlotKeyLabels[favoriteIndex]}]");
                favoriteBadge.pickingMode = PickingMode.Ignore;
                favoriteBadge.style.position = Position.Absolute;
                favoriteBadge.style.top = -6;
                favoriteBadge.style.right = -4;
                favoriteBadge.style.color = new StyleColor(Color.black);
                favoriteBadge.style.backgroundColor = new StyleColor(FavoriteBadgeColor);
                favoriteBadge.style.fontSize = 9;
                favoriteBadge.style.unityFontStyleAndWeight = FontStyle.Bold;
                favoriteBadge.style.paddingLeft = 3;
                favoriteBadge.style.paddingRight = 3;
                favoriteBadge.style.borderTopLeftRadius = 3;
                favoriteBadge.style.borderTopRightRadius = 3;
                favoriteBadge.style.borderBottomLeftRadius = 3;
                favoriteBadge.style.borderBottomRightRadius = 3;
                unitElement.Add(favoriteBadge);
            }

            return slotElement;
        }

        private void OnUnitClicked(PureDataUnit unitData)
        {
            if (catalogService == null || unitData == null) return;

            if (catalogService.IsEquipped(unitData))
            {
                catalogService.UnequipUnit(unitData);
                return;
            }

            if (!catalogService.TryEquipUnit(unitData, out var failReason))
            {
                Debug.LogWarning($"[UnitInventoryUIComponent] Cannot equip {unitData.name}: {failReason}");
            }
        }

        /// <summary>
        /// 장착된 유닛 우클릭 시 즐겨찾기(W/E/F) 슬롯에 등록한다(12번 기획서 §3/§4).
        /// 이미 등록돼 있으면 다음 슬롯으로 순환하되, 다음 슬롯에 다른 유닛이 있으면 지우지 않고
        /// 서로 자리를 맞바꾼다(스왑, 기획서 §6-1 — 다음 슬롯이 비어있었으면 원래 슬롯만 비워진다).
        /// 미등록 유닛은 비어있는 슬롯이 있으면 그 중 빈 슬롯(W 우선)에 등록,
        /// 3개 모두 차있으면 W(0번)부터 덮어쓰는 것을 합리적 기본값으로 채택한다
        /// (기획서 §6 — 정확한 교체 정책은 범위 밖, 구현 시 기본값으로 정함).
        /// 미장착 유닛의 우클릭은 무시한다.
        /// </summary>
        private void OnUnitContextClicked(PureDataUnit unitData)
        {
            if (catalogService == null || favoriteSlots == null || unitData == null) return;
            if (!catalogService.IsEquipped(unitData)) return;

            int existingIndex = favoriteSlots.FindSlotOf(unitData);
            if (existingIndex >= 0)
            {
                int nextIndex = (existingIndex + 1) % RuntimeDataMagicFavoriteSlots.SlotCount;
                PureDataUnit displaced = favoriteSlots.GetSlot(nextIndex);
                favoriteSlots.AssignSlot(nextIndex, unitData);
                favoriteSlots.AssignSlot(existingIndex, displaced);
                return;
            }

            for (int i = 0; i < RuntimeDataMagicFavoriteSlots.SlotCount; i++)
            {
                if (favoriteSlots.GetSlot(i) == null)
                {
                    favoriteSlots.AssignSlot(i, unitData);
                    return;
                }
            }

            favoriteSlots.AssignSlot(0, unitData);
        }

        public void ToggleInventory()
        {
            if (m_isOpen)
            {
                CloseInventory();
            }
            else
            {
                OpenInventory();
            }
        }

        public void OpenInventory()
        {
            // 13번 기획서 §3 "이벤트 중 차단": 이벤트(3단계 감속) 진행 중에는 인벤토리를 열지 않는다.
            // (이미 열려 있는 경우의 닫기는 CloseInventory가 그대로 허용한다.)
            if (timeSlowData != null && timeSlowData.IsEventTriggered) return;

            m_isOpen = true;

            RefreshInventory();

            if (inventoryContainer != null)
            {
                inventoryContainer.style.display = DisplayStyle.Flex;
            }

            timeScaleService?.SetPaused(this, true);

            var manager = m_inputContextManager;
            manager?.PushContext(manager.UIContext);
        }

        public void CloseInventory()
        {
            m_isOpen = false;
            SetHoveredUnit(null);

            if (inventoryContainer != null)
            {
                inventoryContainer.style.display = DisplayStyle.None;
            }

            timeScaleService?.SetPaused(this, false);

            var manager = m_inputContextManager;
            manager?.PopContext(manager.UIContext);
        }
    }
}
