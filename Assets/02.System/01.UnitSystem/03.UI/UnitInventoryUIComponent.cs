using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using VContainer;
using Synty.AnimationBaseLocomotion.Samples.InputSystem;

namespace UnitSystem
{
    /// <summary>
    /// 소유 중인 유닛(마법) 인벤토리를 아이콘 그리드로 표시하고, 클릭으로 장착/해제를 토글하는 UI 컴포넌트입니다.
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
        private InputReader m_inputReader;
        private Common.InputSystem.IInputContextManager m_inputContextManager;

        private bool m_isOpen = false;

        // 인벤토리 메뉴가 씬에 둘 이상 존재해 동시에 InputReader.onInventoryToggled를 구독하는 경우에도,
        // Time.timeScale은 전역 자원이므로 인스턴스별 캐시가 아니라 정적 참조 카운트로 관리해
        // 마지막으로 닫힌 시점에만 원래 값으로 복원한다.
        // OptionUIComponent의 s_pauseRefCount/s_cachedTimeScale과는 별개의 정적 필드로 관리한다
        // (두 메뉴를 동시에 열 때의 Time.timeScale 상호작용은 이번 범위 밖의 알려진 한계).
        private static int s_pauseRefCount = 0;
        private static float s_cachedTimeScale = 1f;

        public bool IsOpen => m_isOpen;

        [Inject]
        public void Construct(
            IUnitCatalogService catalogService,
            IUnitEquipRequirementChecker requirementChecker = null,
            InputReader inputReader = null,
            Common.InputSystem.IInputContextManager contextManager = null,
            RuntimeDataMagicFavoriteSlots favoriteSlots = null)
        {
            UnbindEvents();

            this.catalogService = catalogService;
            this.requirementChecker = requirementChecker;
            m_inputReader = inputReader;
            m_inputContextManager = contextManager;
            this.favoriteSlots = favoriteSlots;

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

            inventoryContainer.Clear();
            foreach (var unitData in possessedUnits)
            {
                if (unitData == null) continue;

                inventoryContainer.Add(BuildUnitElement(unitData));
            }
        }

        private VisualElement BuildUnitElement(PureDataUnit unitData)
        {
            bool isEquipped = catalogService.IsEquipped(unitData);
            bool canEquip = isEquipped || requirementChecker == null || requirementChecker.CanEquip(unitData, out _);

            // 아이콘 + 라벨을 세로로 쌓기 위한 부모 컨테이너 (그리드 배치 단위는 이 컨테이너가 담당).
            var slotElement = new VisualElement();
            slotElement.style.width = 64;
            slotElement.style.marginRight = 8;
            slotElement.style.marginBottom = 8;
            slotElement.style.alignItems = Align.Center;

            var unitElement = new VisualElement();
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

            var nameLabel = new Label(unitData.UnitName);
            nameLabel.style.color = new StyleColor(Color.white);
            nameLabel.style.fontSize = 11;
            nameLabel.style.marginTop = 2;
            nameLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            nameLabel.pickingMode = PickingMode.Ignore;

            var statusLabel = new Label(statusText);
            statusLabel.style.color = new StyleColor(statusColor);
            statusLabel.style.fontSize = 10;
            statusLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            statusLabel.pickingMode = PickingMode.Ignore;

            slotElement.Add(unitElement);
            slotElement.Add(nameLabel);
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
            bool wasOpen = m_isOpen;
            m_isOpen = true;

            RefreshInventory();

            if (inventoryContainer != null)
            {
                inventoryContainer.style.display = DisplayStyle.Flex;
            }

            if (!wasOpen)
            {
                if (s_pauseRefCount == 0)
                {
                    s_cachedTimeScale = Time.timeScale;
                }
                s_pauseRefCount++;
            }

            Time.timeScale = 0f;

            var manager = m_inputContextManager;
            manager?.PushContext(manager.UIContext);
        }

        public void CloseInventory()
        {
            bool wasOpen = m_isOpen;
            m_isOpen = false;

            if (inventoryContainer != null)
            {
                inventoryContainer.style.display = DisplayStyle.None;
            }

            if (wasOpen)
            {
                s_pauseRefCount = Mathf.Max(0, s_pauseRefCount - 1);
            }

            if (s_pauseRefCount == 0)
            {
                Time.timeScale = s_cachedTimeScale;
            }

            var manager = m_inputContextManager;
            manager?.PopContext(manager.UIContext);
        }
    }
}
