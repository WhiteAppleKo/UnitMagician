using System;
using CharacterSystem;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using VContainer.Unity;

namespace UnitSystem
{
    /// <summary>
    /// 인벤토리 창의 마법 탭(33번 기획서 §3-2, UnitSystem UI). 초기화 시 <see cref="IInventoryTabRegistry"/>에
    /// <see cref="ItemType.Magic"/> 탭으로 자신을 등록하고, 해제 시 등록을 푼다.
    /// 내용은 기존 인벤토리 UI 컴포넌트(33번에서 삭제)의 목록·상세 패널·클릭 장착·우클릭 즐겨찾기를 그대로 옮긴 것이며,
    /// 목록은 플레이어 인벤토리의 마법 항목(<see cref="IInventoryService.GetItems"/>, PureDataUnit의 이름·아이콘·설명)으로 그린다.
    ///
    /// 보유 마법을 행 목록(아이콘 + 이름 + 설명 + 장착 상태)으로 표시하고, 아이콘 클릭으로 장착/해제를 토글한다.
    /// 행에 마우스를 올리면 우측 상세 패널에 이름/아이콘/전체 설명을 보여 준다(18번 기획서).
    /// 장착된 마법은 강조 테두리, 미장착이지만 장착 가능한 마법은 기본 테두리,
    /// 조건 미달로 장착 불가능한 마법(현재는 스텁 체커라 발생하지 않음)은 회색+비활성으로 표시한다.
    /// </summary>
    public class MagicInventoryTabView : IInventoryTabView, IInitializable, IDisposable
    {
        private const string MagicTabName = "마법";

        private static readonly Color EquippedBorderColor = new Color(1f, 0.85f, 0.2f, 1f);
        private static readonly Color DefaultBorderColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        private static readonly Color DisabledTint = new Color(0.35f, 0.35f, 0.35f, 0.8f);
        private static readonly Color EnabledTint = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        private static readonly Color FavoriteBadgeColor = new Color(1f, 0.85f, 0.2f, 1f);
        private static readonly string[] FavoriteSlotKeyLabels = { "W", "E", "F" };

        // 18번 기획서 §6 A안: 행 형태 목록 + 호버 시 우측 상세 패널(이름/아이콘/전체 설명).
        private static readonly Color DescriptionColor = new Color(0.78f, 0.78f, 0.78f, 1f);
        private const float DescriptionListFontSize = 11f;
        private const float DescriptionListMaxHeight = 28f; // 11pt(줄 높이 약 13.5) 기준 2줄, 패딩 0 — 초과분은 말줄임
        private const float DetailPanelWidth = 190f;

        private readonly IInventoryTabRegistry tabRegistry;
        private readonly IInventoryService inventoryService;
        private readonly RuntimeDataInventory inventory;
        private readonly IUnitCatalogService catalogService;
        private readonly IUnitEquipRequirementChecker requirementChecker;
        private readonly RuntimeDataMagicFavoriteSlots favoriteSlots;

        private VisualElement contentRoot;
        private VisualElement detailPanel;
        private PureDataUnit hoveredUnit;
        private bool isShown;

        public ItemType ItemType => ItemType.Magic;
        public string TabName => MagicTabName;

        [Inject]
        public MagicInventoryTabView(
            IInventoryTabRegistry tabRegistry,
            IInventoryService inventoryService,
            RuntimeDataInventory inventory,
            IUnitCatalogService catalogService,
            IUnitEquipRequirementChecker requirementChecker = null,
            RuntimeDataMagicFavoriteSlots favoriteSlots = null)
        {
            this.tabRegistry = tabRegistry;
            this.inventoryService = inventoryService;
            this.inventory = inventory;
            this.catalogService = catalogService;
            this.requirementChecker = requirementChecker;
            this.favoriteSlots = favoriteSlots;
        }

        public void Initialize()
        {
            inventory.OnItemAdded -= HandleInventoryChanged;
            inventory.OnItemAdded += HandleInventoryChanged;
            inventory.OnItemRemoved -= HandleInventoryChanged;
            inventory.OnItemRemoved += HandleInventoryChanged;

            catalogService.OnUnitEquipped -= HandleCatalogChanged;
            catalogService.OnUnitEquipped += HandleCatalogChanged;
            catalogService.OnUnitUnequipped -= HandleCatalogChanged;
            catalogService.OnUnitUnequipped += HandleCatalogChanged;

            if (favoriteSlots != null)
            {
                favoriteSlots.OnFavoriteSlotChanged -= HandleFavoriteSlotChanged;
                favoriteSlots.OnFavoriteSlotChanged += HandleFavoriteSlotChanged;
            }

            tabRegistry.RegisterTab(this);
        }

        public void Dispose()
        {
            inventory.OnItemAdded -= HandleInventoryChanged;
            inventory.OnItemRemoved -= HandleInventoryChanged;
            catalogService.OnUnitEquipped -= HandleCatalogChanged;
            catalogService.OnUnitUnequipped -= HandleCatalogChanged;
            if (favoriteSlots != null) favoriteSlots.OnFavoriteSlotChanged -= HandleFavoriteSlotChanged;

            // 레지스트리(Character UI 컴포넌트)가 먼저 파괴됐을 수 있다.
            if (tabRegistry is UnityEngine.Object registryObject && registryObject == null) return;
            tabRegistry.UnregisterTab(this);
        }

        public void Show(VisualElement contentRoot)
        {
            this.contentRoot = contentRoot;
            isShown = contentRoot != null;
            Refresh();
        }

        public void Hide()
        {
            isShown = false;
            hoveredUnit = null;
            detailPanel = null;
            contentRoot = null;
        }

        private void HandleInventoryChanged(IInventoryItem item)
        {
            if (item == null || item.ItemType != ItemType.Magic) return;
            Refresh();
        }

        private void HandleCatalogChanged(PureDataUnit unitData)
        {
            Refresh();
        }

        private void HandleFavoriteSlotChanged(int index, PureDataUnit unit)
        {
            Refresh();
        }

        /// <summary>마법 탭 내용을 다시 그린다(탭이 보이는 동안만).</summary>
        public void Refresh()
        {
            if (!isShown || contentRoot == null) return;

            var magicItems = inventoryService.GetItems(ItemType.Magic);
            Debug.Log($"[MagicInventoryTabView] Magic tab refreshed. Possessed Count: {magicItems.Count}");

            // 좌: 행 목록(스크롤), 우: 호버 상세 패널(2열 레이아웃).
            contentRoot.Clear();

            var layout = new VisualElement();
            layout.pickingMode = PickingMode.Ignore;
            layout.style.flexGrow = 1;
            layout.style.flexDirection = FlexDirection.Row;
            layout.style.flexWrap = Wrap.NoWrap;

            var listView = new ScrollView(ScrollViewMode.Vertical);
            listView.style.flexGrow = 1;
            listView.style.flexShrink = 1;
            listView.RegisterCallback<PointerLeaveEvent>(_ => SetHoveredUnit(null));

            bool hoveredStillPossessed = false;
            for (int i = 0; i < magicItems.Count; i++)
            {
                if (!(magicItems[i] is PureDataUnit unitData) || unitData == null) continue;

                if (unitData == hoveredUnit) hoveredStillPossessed = true;
                listView.Add(BuildUnitElement(unitData));
            }
            layout.Add(listView);

            detailPanel = BuildDetailPanel();
            layout.Add(detailPanel);
            contentRoot.Add(layout);

            // 클릭 등으로 목록이 다시 그려져도 포인터가 올라가 있던 마법의 상세는 유지한다.
            if (!hoveredStillPossessed) hoveredUnit = null;
            RefreshDetailPanel();
        }

        private VisualElement BuildDetailPanel()
        {
            var panel = new VisualElement();
            panel.name = "MagicDetailPanel";
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
            slotElement.name = $"MagicRow_{unitData.name}";
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
            unitElement.name = $"MagicIcon_{unitData.name}";
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
            // 우클릭 = 즐겨찾기 등록. 런타임 패널은 ContextClickEvent를 발행하지 않으므로(실제 입력 주입으로 확인, 33번 검증)
            // 포인터 다운의 버튼(1 = 오른쪽)으로 판별한다(12번 기획서 "PointerDownEvent+버튼 판별" 방식).
            unitElement.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 1) return;
                OnUnitContextClicked(captureUnit);
                evt.StopPropagation();
            });

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

            // 즐겨찾기(W/E/F) 등록 배지 — 장착 마법을 우클릭해 RuntimeDataMagicFavoriteSlots에
            // 등록되면 아이콘 모서리에 "[W]"/"[E]"/"[F]" 라벨로 표시한다(12번 기획서 §3 시청각 전달).
            int favoriteIndex = favoriteSlots != null ? favoriteSlots.FindSlotOf(unitData) : -1;
            if (favoriteIndex >= 0)
            {
                var favoriteBadge = new Label($"[{FavoriteSlotKeyLabels[favoriteIndex]}]");
                favoriteBadge.name = "MagicFavoriteBadge";
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
                Debug.LogWarning($"[MagicInventoryTabView] Cannot equip {unitData.name}: {failReason}");
            }
        }

        /// <summary>
        /// 장착된 마법 우클릭 시 즐겨찾기(W/E/F) 슬롯에 등록한다(12번 기획서 §3/§4).
        /// 이미 등록돼 있으면 다음 슬롯으로 순환하되, 다음 슬롯에 다른 마법이 있으면 지우지 않고
        /// 서로 자리를 맞바꾼다(스왑, 기획서 §6-1 — 다음 슬롯이 비어있었으면 원래 슬롯만 비워진다).
        /// 미등록 마법은 비어있는 슬롯이 있으면 그 중 빈 슬롯(W 우선)에 등록,
        /// 3개 모두 차있으면 W(0번)부터 덮어쓰는 것을 합리적 기본값으로 채택한다
        /// (기획서 §6 — 정확한 교체 정책은 범위 밖, 구현 시 기본값으로 정함).
        /// 미장착 마법의 우클릭은 무시한다.
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
    }
}
